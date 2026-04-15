namespace backend.Models
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime? Deadline { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public string? ProjectName { get; set; }
    }

    public class CreateTaskDto
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "To Do";
        public string Priority { get; set; } = "Medium";
        public DateTime? Deadline { get; set; }
        public Guid? AssignedToUserId { get; set; }
    }
}
