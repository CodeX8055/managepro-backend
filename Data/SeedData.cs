using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // =========================
            // DB MIGRATION
            // =========================
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] Migration failed: {ex.Message}");
                throw; // important: do NOT silently continue in production
            }

            // =========================
            // ROLES SEED
            // =========================
            var existingRoles = await context.Roles.AnyAsync();

            if (!existingRoles)
            {
                var roleNames = new[]
                {
                    "SuperAdmin","OrgAdmin","Project Manager","HR Manager",
                    "Tech Lead","Product Owner","Resource Manager",
                    "Sr. Developer","Jr. Developer","Full-Stack Developer",
                    "QA Tester","UI/UX Designer","Intern"
                };

                var roles = roleNames.Select(name => new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name
                }).ToList();

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // =========================
            // SUPER ADMIN SEED
            // =========================
            var superAdminExists = await context.Users
                .AnyAsync(u => u.Email == "superuser@pms.com");

            if (!superAdminExists)
            {
                var superAdminRole = await context.Roles
                    .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

                if (superAdminRole == null)
                {
                    Console.WriteLine("[SeedData] SuperAdmin role not found");
                    return;
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "superuser",
                    Email = "superuser@pms.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("System$123"),
                    RoleId = superAdminRole.Id
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }
        }
    }
}