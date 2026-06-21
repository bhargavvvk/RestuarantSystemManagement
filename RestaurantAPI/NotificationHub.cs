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
        }

        if (role == "KitchenStaff")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
        }

        await base.OnConnectedAsync();
    }
    public async Task JoinSessionGroup(int sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId,$"session-{sessionId}");
    }
}
