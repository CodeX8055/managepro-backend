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

        public UserController(IUserService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orgIdClaim = User.Claims.FirstOrDefault(c => c.Type == "OrganizationId" || c.Type.EndsWith("/OrganizationId"))?.Value;
            Guid? orgId = string.IsNullOrEmpty(orgIdClaim) ? null : Guid.Parse(orgIdClaim);
            
            // SuperAdmin has null OrgId and sees all or based on query param ideally; here we keep simple.
            // OrgAdmin sees users in their org.
            return Ok(await _service.GetAllByOrgAsync(orgId));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,OrgAdmin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var orgIdClaim = User.Claims.FirstOrDefault(c => c.Type == "OrganizationId" || c.Type.EndsWith("/OrganizationId"))?.Value;

            if (userRole == "OrgAdmin")
            {
                // OrgAdmin can create any management or employee IT roles
                if (!RoleConfig.IsManager(dto.RoleName) && !RoleConfig.IsEmployee(dto.RoleName))
                    return BadRequest("Organization Administrators can only create verified IT personnel roles.");

                if (string.IsNullOrEmpty(orgIdClaim)) return Forbid();
                dto.OrganizationId = Guid.Parse(orgIdClaim); 
            }
            else if (userRole == "SuperAdmin")
            {
                // SuperAdmin can create OrgAdmin, but usually they shouldn't create Managers/Employees directly without Org context
                // But for now, we allow them to create anything.
            }
            else
            {
                return Forbid();
            }

            var result = await _service.CreateAsync(dto);
            if (result == null)
            {
                // Double check exactly what failed to provide a master-level specific error
                if (!await _service.IsEmailUniqueAsync(dto.Email))
                    return BadRequest(new { message = $"Email '{dto.Email}' is already in use." });
                if (!_service.IsPasswordStrong(dto.Password))
                    return BadRequest(new { message = "Password does not meet strength requirements." });

                return BadRequest(new { message = "User creation failed. Please check your inputs." });
            }
            return Ok(result);
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            return Ok(new { isUnique = await _service.IsEmailUniqueAsync(email) });
        }

        [HttpGet("check-username")]
        public async Task<IActionResult> CheckUsername([FromQuery] string username)
        {
            // Usernames are no longer unique in this system
            return Ok(new { isUnique = true });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,OrgAdmin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Security: Prevent OrgAdmins from demoting themselves to employee roles
            if (userRole == "OrgAdmin" && currentUserId != null && Guid.Parse(currentUserId) == id)
            {
                if (RoleConfig.IsEmployee(dto.RoleName))
                {
                    return BadRequest(new { message = "SYSTEM SAFETY LOCK: As the Primary Organization Administrator, you cannot demote your own account to an employee role. This is to ensure you do not lose management access to your organization." });
                }
            }

            var result = await _service.UpdateAsync(id, dto);
            if (result == null)
            {
                return BadRequest(new { message = "Update failed. The Email or Username might already be taken by another user." });
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin,OrgAdmin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // PRO-TRAY: Professional Safety Checks
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != null && Guid.Parse(currentUserId) == id)
            {
                return BadRequest(new { message = "SYSTEM SAFETY LOCK: You cannot remove your own active account while logged in. Please have another administrator perform this action if necessary." });
            }

            // Master-level Safety Check: Do not allow deletion of OrgAdmins (unless by SuperAdmin, but here we keep it strict)
            var allUsers = await _service.GetAllByOrgAsync(null); 
            var userToDelete = allUsers.FirstOrDefault(u => u.Id == id);
            
            if (userToDelete != null && userToDelete.Role == "OrgAdmin")
            {
                return BadRequest(new { message = "CRITICAL SAFETY ALERT: Primary administrative accounts (OrgAdmin) are protected and cannot be removed via this interface." });
            }

            var (success, message) = await _service.DeleteAsync(id);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
    }
}
