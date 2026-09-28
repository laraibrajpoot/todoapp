using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace todolist.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime Deadline { get; set; } = DateTime.Now;

        public string Department { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public string Priority { get; set; } = "Medium"; // Added Priority property with default value

        public string Type { get; set; } = "Personal";

        // Foreign key to tie task to a specific user
        public int UserId { get; set; }

        [NotMapped]
        public string? AssignedTo { get; set; }
    }
}