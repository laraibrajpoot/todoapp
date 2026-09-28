using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using todolist.Data;
using todolist.Hubs;
using todolist.Models;

namespace todolist.Services
{
    public class TaskService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly NotificationService _notificationService;

        public TaskService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IHubContext<NotificationHub> hubContext,
            NotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            // Persist database notification for the target user (Title, Message)
            if (task.UserId > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    task.UserId,
                    "New Task Assigned",
                    $"You have been assigned a new task: '{task.Title}'"
                );
            }

            // Dispatch realtime notification event
            await _hubContext.Clients.All.SendAsync("ReceiveRequest", task.Title, task.Description);

            return task;
        }

        public async Task AddTaskAsync(TaskItem task, int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            task.UserId = userId;
            context.Tasks.Add(task);
            await context.SaveChangesAsync();

            // Persist database notification for the target user (Title, Message)
            if (userId > 0)
            {
                await _notificationService.CreateNotificationAsync(
                    userId,
                    "New Task Assigned",
                    $"You have been assigned a new task: '{task.Title}'"
                );
            }

            // Dispatch realtime notification event
            await _hubContext.Clients.All.SendAsync("ReceiveRequest", task.Title, task.Description);
        }

        public async Task<List<TaskItem>> GetTasksForUserAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasks
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task UpdateTaskAsync(TaskItem task)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Tasks.Update(task);
            await context.SaveChangesAsync();

            // Dispatch realtime update event
            await _hubContext.Clients.All.SendAsync("TaskUpdated", task.Id, task.Status);
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasks.AsNoTracking().ToListAsync();
        }

        public async Task<int> GetActiveTasksCountAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tasks.CountAsync(t => t.Status != "Completed");
        }

        public async Task DeleteTaskAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var task = await context.Tasks.FindAsync(id);
            if (task != null)
            {
                context.Tasks.Remove(task);
                await context.SaveChangesAsync();

                // Dispatch realtime deletion event
                await _hubContext.Clients.All.SendAsync("TaskDeleted", id);
            }
        }
    }
}