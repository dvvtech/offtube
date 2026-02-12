
namespace Offtube.Api.Hub
{
    public class DownloadHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public async Task JoinDownloadGroup(string downloadId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, downloadId);
        }
    }
}
