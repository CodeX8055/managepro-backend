using System;
using System.Linq;
using System.Threading.Tasks;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
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

        // ---------------- LOGIN ----------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request is empty" });

            var email = request.Username?.Trim().ToLower();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Email/password required" });

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            if (string.IsNullOrEmpty(user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isValid)
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

        // ---------------- REGISTER ORGANIZATION ----------------
        [HttpPost("register-organization")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterOrganization([FromBody] RegisterOrganizationRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request is null" });

            var orgName = request.OrganizationName?.Trim();

            if (string.IsNullOrEmpty(orgName) ||
                string.IsNullOrEmpty(request.AdminEmail) ||
                string.IsNullOrEmpty(request.AdminUsername) ||
                string.IsNullOrEmpty(request.AdminPassword))
            {
                return BadRequest(new { message = "All fields required" });
            }

            var orgExists = await _context.Organizations
                .AnyAsync(o => o.Name.ToLower() == orgName.ToLower());

            if (orgExists)
                return BadRequest(new { message = "Organization already exists" });

            if (!await _userService.IsEmailUniqueAsync(request.AdminEmail))
                return BadRequest(new { message = "Email already exists" });

            if (!_userService.IsPasswordStrong(request.AdminPassword))
                return BadRequest(new { message = "Weak password" });

            var org = await _orgService.CreateAsync(new CreateOrganizationDto
            {
                Name = orgName
            });

            if (org == null)
                return StatusCode(500, new { message = "Organization creation failed" });

            var user = await _userService.CreateAsync(new CreateUserDto
            {
                Username = request.AdminUsername.Trim(),
                Email = request.AdminEmail.Trim().ToLower(),
                Password = request.AdminPassword,
                RoleName = "OrgAdmin",
                OrganizationId = org.Id
            });

            if (user == null)
                return StatusCode(500, new { message = "Admin user creation failed" });

            return Ok(new { message = "Organization created successfully" });
        }
    }
}
