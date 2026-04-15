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

            // Ensure DB exists + migrations applied safely
            await context.Database.MigrateAsync();

            // =========================
            // ROLES SEED (SAFE VERSION)
            // =========================
            if (!await context.Roles.AnyAsync())
            {
                var roleNames = new List<string>
                {
                    "SuperAdmin",
                    "OrgAdmin",
                    "Project Manager",
                    "HR Manager",
                    "Tech Lead",
                    "Product Owner",
                    "Resource Manager",
                    "Sr. Developer",
                    "Jr. Developer",
                    "Full-Stack Developer",
                    "QA Tester",
                    "UI/UX Designer",
                    "Intern"
                };

                var roles = roleNames.Select(name => new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name
                });

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

                if (superAdminRole != null)
                {
                    var superAdmin = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = "superuser",
                        Email = "superuser@pms.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("System$123"),
                        RoleId = superAdminRole.Id
                    };

                    await context.Users.AddAsync(superAdmin);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}