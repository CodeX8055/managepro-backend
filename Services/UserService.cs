using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace backend.Services
{
    public interface IUserService { 
        Task<List<UserDto>> GetAllByOrgAsync(Guid? orgId); 
        Task<UserDto?> CreateAsync(CreateUserDto dto); 
        Task<bool> IsEmailUniqueAsync(string email);
        Task<bool> IsUsernameUniqueAsync(string username, Guid? organizationId = null);
        Task<(bool success, string message)> DeleteAsync(Guid userId);
        Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto);
        bool IsPasswordStrong(string password);
    }

    public class UserService : IUserService {
        private readonly AppDbContext _db;
        public UserService(AppDbContext db) => _db = db;

        public async Task<List<UserDto>> GetAllByOrgAsync(Guid? orgId) {
            var query = _db.Users.Include(u => u.Role).AsQueryable();
            if (orgId.HasValue) query = query.Where(u => u.OrganizationId == orgId.Value);
            return await query.Select(u => new UserDto{ Id=u.Id, Username=u.Username, Email=u.Email, Role=u.Role!.Name, OrganizationId=u.OrganizationId }).ToListAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email) {
            return !await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> IsUsernameUniqueAsync(string username, Guid? organizationId = null) {
            // PRO UPDATE: Usernames are now treated as display names and do not need to be unique.
            // Only Email is used as the unique identity key.
            return await Task.FromResult(true);
        }

        public bool IsPasswordStrong(string password) {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        public async Task<UserDto?> CreateAsync(CreateUserDto dto) {
            // Service level validation as a fallback
            if (!await IsEmailUniqueAsync(dto.Email)) return null;
            if (!await IsUsernameUniqueAsync(dto.Username, dto.OrganizationId)) return null;
            if (!IsPasswordStrong(dto.Password)) return null;

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null) return null;

            var user = new User { 
                Id=Guid.NewGuid(), Username=dto.Username, Email=dto.Email, 
                PasswordHash=BCrypt.Net.BCrypt.HashPassword(dto.Password), 
                RoleId=role.Id, OrganizationId=dto.OrganizationId 
            };
            _db.Users.Add(user); await _db.SaveChangesAsync();
            return new UserDto { Id=user.Id, Username=user.Username, Email=user.Email, Role=role.Name, OrganizationId=user.OrganizationId };
        }

        public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto) {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            // Check uniqueness for email if it is changing
            if (user.Email.ToLower() != dto.Email.ToLower()) {
                if (await _db.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower())) return null;
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null) return null;

            user.Username = dto.Username;
            user.Email = dto.Email;
            user.RoleId = role.Id;

            await _db.SaveChangesAsync();
            return new UserDto { Id=user.Id, Username=user.Username, Email=user.Email, Role=role.Name, OrganizationId=user.OrganizationId };
        }

        public async Task<(bool success, string message)> DeleteAsync(Guid userId) {
            try 
            {
                var user = await _db.Users.FindAsync(userId);
                if (user == null) return (false, "User not found.");

                _db.Users.Remove(user);
                var result = await _db.SaveChangesAsync() > 0;
                return (result, result ? "Success" : "No changes made to database.");
            }
            catch (Exception ex)
            {
                return (false, "Master-level Diagnostic: " + ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
            }
        }
    }
}
