using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllByOrgAsync(Guid? orgId);
        Task<UserDto?> CreateAsync(CreateUserDto dto);
        Task<bool> IsEmailUniqueAsync(string email);
        Task<bool> IsUsernameUniqueAsync(string username, Guid? organizationId = null);
        Task<(bool success, string message)> DeleteAsync(Guid userId);
        Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto);
        bool IsPasswordStrong(string password);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        // ---------------- GET USERS ----------------
        public async Task<List<UserDto>> GetAllByOrgAsync(Guid? orgId)
        {
            var query = _db.Users.Include(u => u.Role).AsQueryable();

            if (orgId.HasValue)
                query = query.Where(u => u.OrganizationId == orgId.Value);

            return await query.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role != null ? u.Role.Name : "",
                OrganizationId = u.OrganizationId
            }).ToListAsync();
        }

        // ---------------- EMAIL CHECK ----------------
        public async Task<bool> IsEmailUniqueAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var normalized = email.Trim().ToLower();

            return !await _db.Users.AnyAsync(u =>
                u.Email.ToLower() == normalized);
        }

        // ---------------- USERNAME CHECK (NOT STRICT) ----------------
        public async Task<bool> IsUsernameUniqueAsync(string username, Guid? organizationId = null)
        {
            // username is NOT enforced strictly in your system
            return await Task.FromResult(true);
        }

        // ---------------- PASSWORD STRENGTH ----------------
        public bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        // ---------------- CREATE USER ----------------
        public async Task<UserDto?> CreateAsync(CreateUserDto dto)
        {
            if (dto == null)
                return null;

            var email = dto.Email?.Trim().ToLower();
            var username = dto.Username?.Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return null;

            if (!await IsEmailUniqueAsync(email))
                return null;

            if (!IsPasswordStrong(dto.Password))
                return null;

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null)
                return null;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = role.Id,
                OrganizationId = dto.OrganizationId
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = role.Name,
                OrganizationId = user.OrganizationId
            };
        }

        // ---------------- UPDATE USER ----------------
        public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return null;

            var newEmail = dto.Email?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(newEmail))
                return null;

            if (user.Email.ToLower() != newEmail)
            {
                if (await _db.Users.AnyAsync(u => u.Email.ToLower() == newEmail))
                    return null;
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == dto.RoleName);
            if (role == null)
                return null;

            user.Username = dto.Username?.Trim() ?? user.Username;
            user.Email = newEmail;
            user.RoleId = role.Id;

            await _db.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = role.Name,
                OrganizationId = user.OrganizationId
            };
        }

        // ---------------- DELETE USER ----------------
        public async Task<(bool success, string message)> DeleteAsync(Guid userId)
        {
            try
            {
                var user = await _db.Users.FindAsync(userId);

                if (user == null)
                    return (false, "User not found");

                _db.Users.Remove(user);

                var saved = await _db.SaveChangesAsync() > 0;

                return (saved, saved ? "Deleted successfully" : "No changes made");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
