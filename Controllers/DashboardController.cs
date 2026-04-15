using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;
        public DashboardController(IDashboardService service) => _service = service;

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(role)) return Forbid();

            Guid? orgId = string.IsNullOrEmpty(orgIdClaim) ? null : Guid.Parse(orgIdClaim);
            
            var stats = await _service.GetStatsAsync(orgId, Guid.Parse(userIdStr), role);
            return Ok(stats);
        }
    }
}
