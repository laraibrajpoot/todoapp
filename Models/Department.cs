namespace todolist.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}