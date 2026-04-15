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

            // Apply migrations implicitly if any exist
            await context.Database.MigrateAsync();

            if (!await context.Roles.AnyAsync(r => r.Name == "Resource Manager")) // Use the new role as the check to trigger refresh
            {
                var roleNames = new List<string> {
                    "SuperAdmin", "OrgAdmin",
                    "Project Manager", "HR Manager", "Tech Lead", "Product Owner", "Resource Manager",
                    "Sr. Developer", "Jr. Developer", "Full-Stack Developer", "QA Tester", "UI/UX Designer", "Intern"
                };

                // Remove existing generic roles to prevent corruption
                var existingRoles = await context.Roles.ToListAsync();
                if(existingRoles.Any()) {
                    context.Roles.RemoveRange(existingRoles);
                    context.Users.RemoveRange(await context.Users.ToListAsync()); // If roles drop, users must drop (FK constraint) for a clean migration in dev mode
                    await context.SaveChangesAsync();
                }

                var roles = roleNames.Select(name => new Role { Id = Guid.NewGuid(), Name = name }).ToList();
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            if (!await context.Users.AnyAsync(u => u.Email == "superuser@pms.com"))
            {
                var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
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
