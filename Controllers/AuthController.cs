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
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Username.ToLower());

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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
            if (request == null)
                return BadRequest(new { message = "Request body is empty" });

            try
            {
                // validate
                if (string.IsNullOrWhiteSpace(request.OrganizationName))
                    return BadRequest(new { message = "Organization name required" });

                if (string.IsNullOrWhiteSpace(request.AdminEmail))
                    return BadRequest(new { message = "Admin email required" });

                if (string.IsNullOrWhiteSpace(request.AdminPassword))
                    return BadRequest(new { message = "Admin password required" });

                if (await _context.Organizations.AnyAsync(o => o.Name == request.OrganizationName))
                    return BadRequest(new { message = "Organization already exists" });

                if (!await _userService.IsEmailUniqueAsync(request.AdminEmail))
                    return BadRequest(new { message = "Email already exists" });

                if (!_userService.IsPasswordStrong(request.AdminPassword))
                    return BadRequest(new { message = "Weak password" });

                // create org
                var org = await _orgService.CreateAsync(new CreateOrganizationDto
                {
                    Name = request.OrganizationName.Trim()
                });

                if (org == null)
                    return StatusCode(500, new { message = "Organization creation failed" });

                // create admin
                var user = await _userService.CreateAsync(new CreateUserDto
                {
                    Username = request.AdminUsername?.Trim(),
                    Email = request.AdminEmail.Trim().ToLower(),
                    Password = request.AdminPassword,
                    RoleName = "OrgAdmin",
                    OrganizationId = org.Id
                });

                if (user == null)
                    return StatusCode(500, new { message = "Admin user creation failed" });

                return Ok(new { message = "Organization created successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    message = "Database error",
                    detail = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Server error",
                    detail = ex.Message
                });
            }
        }
    }
}