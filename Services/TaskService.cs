using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public interface ITaskService { 
        Task<List<TaskDto>> GetAllAsync(Guid? orgId, Guid userId, string role);
        Task<List<TaskDto>> GetByProjectAsync(Guid projectId, Guid userId, string role); 
        Task<TaskDto?> CreateAsync(CreateTaskDto dto); 
        Task<TaskDto?> UpdateAsync(Guid taskId, CreateTaskDto dto); 
        Task<bool> UpdateStatusAsync(Guid taskId, string status);
        Task<bool> DeleteAsync(Guid taskId);
    }

    public class TaskService : ITaskService {
        private readonly AppDbContext _db;
        public TaskService(AppDbContext db) => _db = db;

        public async Task<List<TaskDto>> GetAllAsync(Guid? orgId, Guid userId, string role) {
            var query = _db.Tasks.Include(t => t.AssignedToUser).Include(t => t.Project).AsQueryable();

            if (orgId.HasValue) {
                query = query.Where(t => t.Project!.OrganizationId == orgId.Value);
            }

            return await query
                .Select(t => new TaskDto{ 
                    Id=t.Id, ProjectId=t.ProjectId, Title=t.Title, Description=t.Description, 
                    Status=t.Status, Priority=t.Priority, Deadline=t.Deadline, 
                    AssignedToUserId=t.AssignedToUserId, AssignedToUserName=t.AssignedToUser!=null?t.AssignedToUser.Username:null,
                    ProjectName=t.Project != null ? t.Project.Name : null
                }).ToListAsync();
        }

        public async Task<List<TaskDto>> GetByProjectAsync(Guid projectId, Guid userId, string role) {
            var query = _db.Tasks.Include(t => t.AssignedToUser).Where(t => t.ProjectId == projectId);

            // Roles no longer restrict viewing tasks inside a project.
            // If they can see the project/tasks section, they see all tasks.

            return await query
                .Select(t => new TaskDto{ 
                    Id=t.Id, ProjectId=t.ProjectId, Title=t.Title, Description=t.Description, 
                    Status=t.Status, Priority=t.Priority, Deadline=t.Deadline, 
                    AssignedToUserId=t.AssignedToUserId, AssignedToUserName=t.AssignedToUser!=null?t.AssignedToUser.Username:null 
                }).ToListAsync();
        }

        public async Task<TaskDto?> CreateAsync(CreateTaskDto dto) {
            DateTime? utcDeadline = dto.Deadline.HasValue 
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc) 
                : null;

            var task = new TaskItem { 
                Id=Guid.NewGuid(), ProjectId=dto.ProjectId, Title=dto.Title, 
                Description=dto.Description, Status=dto.Status, Priority=dto.Priority, 
                Deadline=utcDeadline, AssignedToUserId=dto.AssignedToUserId 
            };
            _db.Tasks.Add(task); await _db.SaveChangesAsync();
            return new TaskDto { Id=task.Id, ProjectId=task.ProjectId, Title=task.Title, Status=task.Status, Priority=task.Priority, Deadline=task.Deadline, AssignedToUserId=task.AssignedToUserId };
        }

        public async Task<TaskDto?> UpdateAsync(Guid taskId, CreateTaskDto dto) {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return null;

            DateTime? utcDeadline = dto.Deadline.HasValue 
                ? DateTime.SpecifyKind(dto.Deadline.Value, DateTimeKind.Utc) 
                : null;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.Deadline = utcDeadline;
            task.AssignedToUserId = dto.AssignedToUserId;

            await _db.SaveChangesAsync();
            return new TaskDto { 
                Id=task.Id, ProjectId=task.ProjectId, Title=task.Title, 
                Status=task.Status, Priority=task.Priority, Deadline=task.Deadline, 
                AssignedToUserId=task.AssignedToUserId 
            };
        }

        public async Task<bool> DeleteAsync(Guid taskId) {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return false;
            _db.Tasks.Remove(task);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStatusAsync(Guid taskId, string status) {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return false;
            task.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
