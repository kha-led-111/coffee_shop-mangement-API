using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CoffeeShopAPI.Hubs;

[Authorize]
public class OrderHub : Hub
{
    /// <summary>
    /// Clients join a group per order to receive targeted status updates.
    /// Flutter calls: HubConnection.invoke("JoinOrderGroup", orderId)
    /// </summary>
    public async Task JoinOrderGroup(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    public async Task LeaveOrderGroup(int orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    /// <summary>
    /// Staff join the kitchen group to receive all new orders.
    /// Flutter kitchen display calls: HubConnection.invoke("JoinKitchen")
    /// </summary>
    public async Task JoinKitchen()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
