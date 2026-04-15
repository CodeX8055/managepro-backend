namespace backend.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties for Multi-tenancy and RBAC
        public Guid? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public Guid RoleId { get; set; }
        public Role? Role { get; set; }
        
        // Navigation property for many-to-many
        public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    }
}
