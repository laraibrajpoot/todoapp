public class TaskDeletionRequestModel
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty; // Added property to fix CS1061 / CS0117
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.Now;
}