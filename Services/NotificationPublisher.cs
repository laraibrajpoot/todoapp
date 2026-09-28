using System;
using System.Threading.Tasks;
using todolist.Models;

namespace todolist.Services
{
    // Simple in-memory publisher for server-side notification delivery to Blazor components
    public class NotificationPublisher
    {
        public event Func<Notification, Task>? NotificationReceived;

        public async Task PublishAsync(Notification notification)
        {
            var handler = NotificationReceived;
            if (handler != null)
            {
                await handler.Invoke(notification);
            }
        }
    }
}
