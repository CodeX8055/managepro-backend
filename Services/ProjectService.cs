using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public interface IProjectService { 
        Task<List<ProjectDto>> GetAllByOrgAsync(Guid orgId, Guid userId, string role); 
        Task<ProjectDto?> GetByIdAsync(Guid id);
        Task<ProjectDto?> CreateAsync(CreateProjectDto dto, Guid userId); 
        Task<bool> DeleteAsync(Guid id);
    }

    public class ProjectService : IProjectService {
        private readonly AppDbContext _db;
        public ProjectService(AppDbContext db) => _db = db;

        public async Task<ProjectDto?> GetByIdAsync(Guid id) {
            var p = await _db.Projects.FindAsync(id);
            if (p == null) return null;
            return new ProjectDto { Id=p.Id, Name=p.Name, Description=p.Description, OrganizationId=p.OrganizationId, CreatedAt=p.CreatedAt };
        }

        public async Task<List<ProjectDto>> GetAllByOrgAsync(Guid orgId, Guid userId, string role) {
            var query = _db.Projects.Where(p => p.OrganizationId == orgId);

            // Role-based restrictions have been removed per user architecture update.
            // Anyone who has access to view projects will see all of them in the organization.

            return await query
                .Select(p => new ProjectDto{ Id=p.Id, Name=p.Name, Description=p.Description, OrganizationId=p.OrganizationId, CreatedAt=p.CreatedAt })
                .ToListAsync();
        }

        public async Task<ProjectDto?> CreateAsync(CreateProjectDto dto, Guid userId) {
            var proj = new Project { Id=Guid.NewGuid(), Name=dto.Name, Description=dto.Description, OrganizationId=dto.OrganizationId!.Value };
            _db.Projects.Add(proj);
            _db.UserProjects.Add(new UserProject { UserId = userId, ProjectId = proj.Id });
            await _db.SaveChangesAsync();
            return new ProjectDto { Id=proj.Id, Name=proj.Name, Description=proj.Description, OrganizationId=proj.OrganizationId, CreatedAt=proj.CreatedAt };
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var proj = await _db.Projects.FindAsync(id);
            if (proj == null) return false;

            // Optional: EF Core might cascade this automatically, but doing it explicitly guarantees related entities drop efficiently without FK errors
            var tasks = _db.Tasks.Where(t => t.ProjectId == id);
            _db.Tasks.RemoveRange(tasks);

            var userProjects = _db.UserProjects.Where(up => up.ProjectId == id);
            _db.UserProjects.RemoveRange(userProjects);

            _db.Projects.Remove(proj);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
