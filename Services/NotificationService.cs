using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using todolist.Data;
using todolist.Hubs;
using todolist.Models;

namespace todolist.Services
{
    public class NotificationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly NotificationPublisher _publisher;

        public NotificationService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger,
            NotificationPublisher publisher)
        {
            _contextFactory = contextFactory;
            _hubContext = hubContext;
            _logger = logger;
            _publisher = publisher;
        }

        #region Task Deletion Request Methods

        public async Task CreateDeletionRequestAsync(TaskDeletionRequestModel request)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.TaskDeletionRequests.Add(request);
            await context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveRequest", "New Deletion Request", $"User {request.UserId} requested deletion for Task #{request.TaskId}");
        }

        public async Task<List<TaskDeletionRequestModel>> GetPendingDeletionRequestsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.TaskDeletionRequests.AsNoTracking().ToListAsync();
        }

        public async Task ResolveDeletionRequestAsync(int requestId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var request = await context.TaskDeletionRequests.FindAsync(requestId);
            if (request != null)
            {
                context.TaskDeletionRequests.Remove(request);
                await context.SaveChangesAsync();
            }
        }

        public async Task ProcessDeletionRequestAsync(int requestId, bool isApproved, string? adminReason = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var request = await context.TaskDeletionRequests.FindAsync(requestId);
            if (request == null) return;

            var task = await context.Tasks.FindAsync(request.TaskId);
            string taskTitle = task?.Title ?? $"ID: {request.TaskId}";

            if (isApproved && task != null)
            {
                context.Tasks.Remove(task);
            }

            string status = isApproved ? "Approved" : "Denied";
            string title = $"Deletion Request {status}";
            string message = isApproved
                ? $"Your request to delete task '{taskTitle}' has been approved."
                : $"Your request to delete task '{taskTitle}' has been denied.";

            if (!string.IsNullOrWhiteSpace(adminReason))
            {
                message += $" Reason: {adminReason}";
            }

            var notification = new Notification
            {
                UserId = request.UserId,
                Title = title,
                Message = message,
                Url = "/user/tasks",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            context.Notifications.Add(notification);
            context.TaskDeletionRequests.Remove(request);

            await context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", request.UserId, title, message);
                try
                {
                    await _publisher.PublishAsync(notification);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "PublishAsync failed for notification {Id}", notification.Id);
                }
            }
            catch
            {
                // Swallow hub failures; persisted in DB
            }
        }

        #endregion

        #region User Notification Methods

        public async Task CreateNotificationAsync(int userId, string title, string message, string? url = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Url = url,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", userId, title, message);
            }
            catch
            {
                // ignore hub errors
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Marks a single notification as read using its primary key NotificationId.
        /// </summary>
        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var notification = await context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Marks all notifications for a specific user as read using their UserId.
        /// </summary>
        public async Task MarkAllAsReadForUserAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var unreadNotifications = await context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await context.SaveChangesAsync();
        }

        #endregion
    }
}