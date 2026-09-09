using Microsoft.AspNetCore.SignalR;

namespace prjGoHike.Hubs
{
    public class CommentHub : Hub
    {
        public async Task JoinArticleGroup(int articleId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"Article_{articleId}"
            );
        }

        public async Task LeaveArticleGroup(int articleId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"Article_{articleId}"
            );
        }
    }
}
