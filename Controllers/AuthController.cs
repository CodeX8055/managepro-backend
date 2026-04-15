using System;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

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

        public AuthController(
            AppDbContext context,
            IJwtService jwtService,
            IUserService userService,
            IOrganizationService orgService)
        {
            _context = context;
            _jwtService = jwtService;
            _userService = userService;
            _orgService = orgService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is empty" });

            var user = await _context.Users
                .Include(u => u.Role)
                var email = request.Username?.Trim().ToLower();

var user = await _context.Users
    .Include(u => u.Role)
    .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            if (string.IsNullOrEmpty(user.PasswordHash) ||
    !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
{
    return Unauthorized(new { message = "Invalid credentials" });
}
                return Unauthorized(new { message = "Invalid credentials" });

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
        [AllowAnonymous]
        public async Task<IActionResult> RegisterOrganization([FromBody] RegisterOrganizationRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request is null");

                if (string.IsNullOrWhiteSpace(request.OrganizationName.Trim()) ||
                    string.IsNullOrWhiteSpace(request.AdminEmail) ||
                    string.IsNullOrWhiteSpace(request.AdminUsername) ||
                    string.IsNullOrWhiteSpace(request.AdminPassword))
                {
                    return BadRequest("All fields are required");
                }

                var orgExists = await _context.Organizations
                    .AnyAsync(o => o.Name == request.OrganizationName.Trim());

                if (orgExists)
                    return BadRequest("Organization already exists");

                if (!await _userService.IsEmailUniqueAsync(request.AdminEmail))
                    return BadRequest("Email already exists");

                if (!_userService.IsPasswordStrong(request.AdminPassword))
                    return BadRequest(new { message = "Weak password" });

                var org = await _orgService.CreateAsync(new CreateOrganizationDto
                {
                    Name = request.OrganizationName.Trim().Trim()
                });

                if (org == null)
                    return StatusCode(500, "Organization creation failed");

                var user = await _userService.CreateAsync(new CreateUserDto
                {
                    Username = request.AdminUsername,
                    Email = request.AdminEmail,
                    Password = request.AdminPassword,
                    RoleName = "OrgAdmin",
                    OrganizationId = org.Id
                });

                if (user == null)
                    return StatusCode(500, "Admin user creation failed");

                return Ok(new { message = "Organization created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
    }
}