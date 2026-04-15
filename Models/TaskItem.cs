namespace backend.Models
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "To Do"; // ToDo, InProgress, Done
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Project? Project { get; set; }
        
        public Guid? AssignedToUserId { get; set; }
        public User? AssignedToUser { get; set; }
    }
}
