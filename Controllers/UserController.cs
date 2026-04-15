using System;
using System.Linq;
using System.Threading.Tasks;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;

        public UserController(IUserService service)
        {
            _service = service;
        }

        // ================= GET ALL USERS =================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orgIdClaim = User.Claims
                .FirstOrDefault(c =>
                    c.Type == "OrganizationId" ||
                    c.Type.EndsWith("/OrganizationId"))
                ?.Value;

            Guid? orgId = Guid.TryParse(orgIdClaim, out var parsed) ? parsed : null;

            var users = await _service.GetAllByOrgAsync(orgId);
            return Ok(users);
        }

        // ================= CREATE USER =================
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,OrgAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid request" });

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var orgIdClaim = User.Claims
                .FirstOrDefault(c =>
                    c.Type == "OrganizationId" ||
                    c.Type.EndsWith("/OrganizationId"))
                ?.Value;

            if (userRole == "OrgAdmin")
            {
                if (!RoleConfig.IsManager(dto.RoleName) &&
                    !RoleConfig.IsEmployee(dto.RoleName))
                {
                    return BadRequest(new
                    {
                        message = "OrgAdmin can only create manager or employee roles."
                    });
                }

                if (!Guid.TryParse(orgIdClaim, out var orgId))
                    return Forbid();

                dto.OrganizationId = orgId;
            }

            var result = await _service.CreateAsync(dto);

            if (result == null)
            {
                if (!await _service.IsEmailUniqueAsync(dto.Email))
                    return BadRequest(new { message = "Email already exists" });

                if (!_service.IsPasswordStrong(dto.Password))
                    return BadRequest(new { message = "Weak password" });

                return BadRequest(new { message = "User creation failed" });
            }

            return Ok(result);
        }

        // ================= CHECK EMAIL =================
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email required" });

            return Ok(new
            {
                isUnique = await _service.IsEmailUniqueAsync(email)
            });
        }

        // ================= CHECK USERNAME =================
        [HttpGet("check-username")]
        public IActionResult CheckUsername([FromQuery] string username)
        {
            return Ok(new { isUnique = true });
        }

        // ================= UPDATE USER =================
        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,OrgAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userRole == "OrgAdmin" &&
                Guid.TryParse(currentUserId, out var currentId) &&
                currentId == id)
            {
                if (RoleConfig.IsEmployee(dto.RoleName))
                {
                    return BadRequest(new
                    {
                        message = "You cannot demote your own admin account."
                    });
                }
            }

            var result = await _service.UpdateAsync(id, dto);

            if (result == null)
            {
                return BadRequest(new
                {
                    message = "Update failed (email or username may already exist)"
                });
            }

            return Ok(result);
        }

        // ================= DELETE USER =================
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,OrgAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(currentUserId, out var currentId) && currentId == id)
            {
                return BadRequest(new
                {
                    message = "You cannot delete your own account"
                });
            }

            var allUsers = await _service.GetAllByOrgAsync(null);
            var userToDelete = allUsers.FirstOrDefault(u => u.Id == id);

            if (userToDelete != null && userToDelete.Role == "OrgAdmin")
            {
                return BadRequest(new
                {
                    message = "OrgAdmin accounts are protected"
                });
            }

            var (success, message) = await _service.DeleteAsync(id);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
    }
}