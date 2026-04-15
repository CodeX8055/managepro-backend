using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public interface IDashboardService { Task<DashboardDto> GetStatsAsync(Guid? orgId, Guid userId, string role); }

    public class DashboardService : IDashboardService {
        private readonly AppDbContext _db;
        public DashboardService(AppDbContext db) => _db = db;

        public async Task<DashboardDto> GetStatsAsync(Guid? orgId, Guid userId, string role) {
            var projectsQuery = _db.Projects.AsQueryable();
            var tasksQuery = _db.Tasks.AsQueryable();

            if (orgId.HasValue) {
                projectsQuery = projectsQuery.Where(p => p.OrganizationId == orgId.Value);
                tasksQuery = tasksQuery.Where(t => t.Project!.OrganizationId == orgId.Value);
            }

            var projectsCount = await projectsQuery.CountAsync();
            var tasksCount = await tasksQuery.CountAsync();
            var completedCount = await tasksQuery.CountAsync(t => t.Status == "Done");
            var pendingCount = tasksCount - completedCount;

            return new DashboardDto { TotalProjects = projectsCount, TotalTasks = tasksCount, CompletedTasks = completedCount, PendingTasks = pendingCount };
        }
    }
}
