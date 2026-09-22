using Microsoft.AspNetCore.SignalR;

namespace prjGoHike.Hubs
{
    public class NotificationHub : Hub
    {
        // 加入目前使用者自己的通知群組
        public async Task JoinUserGroup(long userId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"User_{userId}"
            );
        }

        // 離開目前使用者自己的通知群組
        public async Task LeaveUserGroup(long userId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"User_{userId}"
            );
        }
    }
}
