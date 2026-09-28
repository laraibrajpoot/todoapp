namespace todolist.Models
{
    public class UserTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed
        public bool IsPersonal { get; set; }
    }

    public class UserNotification
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class UserProfileModel
    {
        public string FullName { get; set; } = "John Doe";
        public string Email { get; set; } = "john.doe@company.com";
        public string Department { get; set; } = "Software Engineering";
    }
}