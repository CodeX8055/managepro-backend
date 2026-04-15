namespace backend.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}
