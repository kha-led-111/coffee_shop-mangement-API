using CoffeeShopAPI.Data;
using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Hubs;
using CoffeeShopAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShopAPI.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext            _db;
    private readonly IHubContext<OrderHub>   _hub;

    public OrderService(AppDbContext db, IHubContext<OrderHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(int page, int pageSize, OrderStatus? status)
    {
        var query = _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Table)
            .Include(o => o.Cashier)
            .AsQueryable();

        if (status.HasValue) query = query.Where(o => o.Status == status);

        var total  = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items      = orders.Select(MapOrder).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Table)
            .Include(o => o.Cashier)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : MapOrder(order);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, int cashierId)
    {
        // Validate all menu items exist and are available
        var menuItemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var menuItems   = await _db.MenuItems
            .Where(m => menuItemIds.Contains(m.Id) && m.IsAvailable)
            .ToListAsync();

        if (menuItems.Count != menuItemIds.Count)
            throw new InvalidOperationException("One or more menu items are unavailable.");

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            Type        = request.Type,
            TableId     = request.TableId,
            Notes       = request.Notes,
            CashierId   = cashierId,
            Status      = OrderStatus.Pending
        };

        foreach (var itemReq in request.Items)
        {
            var menuItem = menuItems.First(m => m.Id == itemReq.MenuItemId);
            var subTotal = menuItem.Price * itemReq.Quantity;

            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity   = itemReq.Quantity,
                UnitPrice  = menuItem.Price,
                SubTotal   = subTotal,
                Notes      = itemReq.Notes
            });

            order.TotalAmount += subTotal;
        }

        _db.Orders.Add(order);

        // Mark table as occupied if dine-in
        if (request.TableId.HasValue && request.Type == OrderType.DineIn)
        {
            var table = await _db.Tables.FindAsync(request.TableId.Value);
            if (table != null) table.Status = TableStatus.Occupied;
        }

        await _db.SaveChangesAsync();

        // Reload full order for response
        var created = await GetOrderByIdAsync(order.Id);

        // Notify kitchen via SignalR
        await _hub.Clients.Group("kitchen")
            .SendAsync("NewOrder", created);

        return created!;
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.MenuItem)
            .Include(o => o.Table)
            .Include(o => o.Cashier)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return null;

        order.Status    = status;
        order.UpdatedAt = DateTime.UtcNow;

        // Free table when order is delivered or cancelled
        if ((status == OrderStatus.Delivered || status == OrderStatus.Cancelled)
            && order.TableId.HasValue)
        {
            var table = await _db.Tables.FindAsync(order.TableId.Value);
            if (table != null) table.Status = TableStatus.Available;
        }

        await _db.SaveChangesAsync();

        var dto    = MapOrder(order);
        var update = new OrderStatusUpdate
        {
            OrderId     = order.Id,
            OrderNumber = order.OrderNumber,
            Status      = order.Status,
            StatusLabel = order.Status.ToString(),
            UpdatedAt   = order.UpdatedAt!.Value
        };

        // Notify the specific order group and kitchen
        await _hub.Clients.Group($"order-{id}").SendAsync("OrderStatusChanged", update);
        await _hub.Clients.Group("kitchen").SendAsync("OrderStatusChanged", update);

        return dto;
    }

    public async Task<bool> CancelOrderAsync(int id)
    {
        var result = await UpdateOrderStatusAsync(id, OrderStatus.Cancelled);
        return result != null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _db.Orders
            .CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date) + 1;
        return $"ORD-{today}-{count:D3}";
    }

    private static OrderDto MapOrder(Order o) => new()
    {
        Id          = o.Id,
        OrderNumber = o.OrderNumber,
        Status      = o.Status,
        StatusLabel = o.Status.ToString(),
        Type        = o.Type,
        TotalAmount = o.TotalAmount,
        Notes       = o.Notes,
        CreatedAt   = o.CreatedAt,
        TableId     = o.TableId,
        TableNumber = o.Table?.TableNumber,
        CashierName = o.Cashier?.Name ?? string.Empty,
        Items       = o.Items.Select(i => new OrderItemDto
        {
            Id           = i.Id,
            MenuItemId   = i.MenuItemId,
            MenuItemName = i.MenuItem?.Name ?? string.Empty,
            ImageUrl     = i.MenuItem?.ImageUrl,
            Quantity     = i.Quantity,
            UnitPrice    = i.UnitPrice,
            SubTotal     = i.SubTotal,
            Notes        = i.Notes
        }).ToList()
    };
}
