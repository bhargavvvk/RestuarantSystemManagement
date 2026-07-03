using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RestaurantAPI;
[Authorize]
public class NotificationHub:Hub
{
    public override async Task OnConnectedAsync()
    {
        var role =Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Customer")
        {
            var sessionId =Context.User?.FindFirst("SessionId")?.Value;
            if (!string.IsNullOrEmpty(sessionId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId,$"session-{sessionId}");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, "customers");
        }

        if (role == "KitchenStaff")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
        }

        if (role == "Waiter")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "waiters");
        }

        if (role == "Admin")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        await base.OnConnectedAsync();
    }
    public async Task JoinSessionGroup(int sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId,$"session-{sessionId}");
    }
}
