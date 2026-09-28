using System;

namespace todolist.Models
{
    public class TaskModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsAssigned { get; set; }
    }
}
