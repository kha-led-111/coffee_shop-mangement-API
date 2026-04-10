using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Models;
using CoffeeShopAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoffeeShopAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    /// <summary>GET /api/orders?page=1&pageSize=20&status=Pending</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetOrders(
        [FromQuery] int          page     = 1,
        [FromQuery] int          pageSize = 20,
        [FromQuery] OrderStatus? status   = null)
    {
        var result = await _orderService.GetOrdersAsync(page, pageSize, status);
        return Ok(ApiResponse<PagedResult<OrderDto>>.Ok(result));
    }

    /// <summary>GET /api/orders/{id}</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(int id)
    {
        var result = await _orderService.GetOrderByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<OrderDto>.Fail("Order not found."));
        return Ok(ApiResponse<OrderDto>.Ok(result));
    }

    /// <summary>POST /api/orders — Create a new order</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var cashierId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result    = await _orderService.CreateOrderAsync(request, cashierId);

        return CreatedAtAction(nameof(GetOrder), new { id = result.Id },
            ApiResponse<OrderDto>.Ok(result, "Order created successfully."));
    }

    /// <summary>PATCH /api/orders/{id}/status — Update order status</summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        if (result == null) return NotFound(ApiResponse<OrderDto>.Fail("Order not found."));
        return Ok(ApiResponse<OrderDto>.Ok(result, $"Order status updated to {request.Status}."));
    }

    /// <summary>DELETE /api/orders/{id} — Cancel an order</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> CancelOrder(int id)
    {
        var result = await _orderService.CancelOrderAsync(id);
        if (!result) return NotFound(ApiResponse<object>.Fail("Order not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Order cancelled."));
    }
}
