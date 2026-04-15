namespace backend.Models
{
    public class OrganizationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateOrganizationDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
