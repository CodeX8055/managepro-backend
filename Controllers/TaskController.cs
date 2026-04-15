using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Globally authorized for all logged in users, we'll let service filter
    public class TaskController : ControllerBase
    {
        private readonly ITaskService _service;
        public TaskController(ITaskService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(role)) return Forbid();

            Guid? orgId = string.IsNullOrEmpty(orgIdClaim) ? null : Guid.Parse(orgIdClaim);
            return Ok(await _service.GetAllAsync(orgId, Guid.Parse(userIdStr), role));
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(Guid projectId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(role)) return Forbid();

            return Ok(await _service.GetByProjectAsync(projectId, Guid.Parse(userIdStr), role));
        }

        [HttpPost]
        [Authorize(Roles = RoleConfig.AllManagersAndAdmins)]
        public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = RoleConfig.AllManagersAndAdmins)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateTaskDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = RoleConfig.AllManagersAndAdmins)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return Ok(new { message = "Task deleted successfully." });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = RoleConfig.Everybody)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusDto dto)
        {
            var success = await _service.UpdateStatusAsync(id, dto.Status);
            if (!success) return NotFound();
            return NoContent();
        }
    }

    public class UpdateTaskStatusDto {
        public string Status { get; set; } = string.Empty;
    }
}
