using Microsoft.AspNetCore.SignalR;

namespace todolist.Hubs
{
    public class NotificationHub : Hub
    {
        // Call this to notify a specific user by ID/Connection
        public async Task SendNotificationToUser(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        // Call this to broadcast task requests to all connected admins
        public async Task BroadcastRequest(string title, string details)
        {
            await Clients.All.SendAsync("ReceiveRequest", title, details);
        }
    }
}