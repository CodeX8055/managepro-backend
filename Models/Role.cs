namespace backend.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // SuperAdmin, OrgAdmin, Manager, Employee
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
