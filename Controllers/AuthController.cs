using System;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IUserService _userService;
        private readonly IOrganizationService _orgService;

        public AuthController(AppDbContext context, IJwtService jwtService, IUserService userService, IOrganizationService orgService)
        {
            _context = context;
            _jwtService = jwtService;
            _userService = userService;
            _orgService = orgService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // PRO UPGRADE: In a professional multi-tenant system, Email is the global unique identifier.
            // This allows different organizations to safely use the same usernames (like 'admin').
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Username.ToLower());
                
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid credentials. Please ensure you are using your registered Email address to login." });
            }

            var token = _jwtService.GenerateToken(user, user.Role?.Name ?? "");

            return Ok(new AuthResponse 
            { 
                Token = token, 
                Username = user.Username, 
                Role = user.Role?.Name ?? "", 
                OrganizationId = user.OrganizationId 
            });
        }

        [HttpPost("register-organization")]
        public async Task<IActionResult> RegisterOrganization([FromBody] RegisterOrganizationRequest request)
        {
            try 
            {
                // 1. Validation
                if (string.IsNullOrWhiteSpace(request.OrganizationName))
                    return BadRequest(new { message = "Organization name is required." });

                if (await _context.Organizations.AnyAsync(o => o.Name == request.OrganizationName))
                    return BadRequest(new { message = "Organization name already exists." });

                if (!await _userService.IsEmailUniqueAsync(request.AdminEmail))
                    return BadRequest(new { message = "Email already in use." });
                
                // Note: Username uniqueness is now handled per-organization.
                // Since this is a new organization, any username is acceptable.

                if (!_userService.IsPasswordStrong(request.AdminPassword))
                    return BadRequest(new { message = "Password does not meet strength requirements (Min 8 chars, Mixed Case, Number, Special Char)." });

                // 2. Create Organization
                var org = await _orgService.CreateAsync(new CreateOrganizationDto { Name = request.OrganizationName });
                if (org == null) return BadRequest(new { message = "Failed to create organization. Internal database error." });

                // 3. Create Admin User
                var userDto = await _userService.CreateAsync(new CreateUserDto
                {
                    Username = request.AdminUsername,
                    Email = request.AdminEmail,
                    Password = request.AdminPassword,
                    RoleName = "OrgAdmin",
                    OrganizationId = org.Id
                });

                if (userDto == null) return BadRequest(new { message = "Failed to create administrator account. Please check your inputs." });

                return Ok(new { message = "Organization registered successfully! You can now login." });
            }
            catch (Exception ex)
            {
                // Master-level diagnostic report
                return StatusCode(500, new { message = "CRITICAL ERROR: " + ex.Message, detail = ex.InnerException?.Message });
            }
        }
    }
}
