using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _service;
        public ProjectController(IProjectService service) => _service = service;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var project = await _service.GetByIdAsync(id);
            if (project == null) return NotFound();
            return Ok(project);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(orgIdClaim) || string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(role)) 
                return Forbid();
            
            return Ok(await _service.GetAllByOrgAsync(Guid.Parse(orgIdClaim), Guid.Parse(userIdStr), role));
        }

        [HttpPost]
        [Authorize(Roles = RoleConfig.AllManagersAndAdmins)]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(orgIdClaim) || string.IsNullOrEmpty(userIdStr)) return Forbid();
            
            dto.OrganizationId = Guid.Parse(orgIdClaim);
            var result = await _service.CreateAsync(dto, Guid.Parse(userIdStr));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConfig.AllManagersAndAdmins)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound(new { message = "Project not found or could not be deleted." });
            return Ok(new { message = "Project deleted successfully." });
        }
    }
}
