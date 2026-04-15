using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin")]
    public class OrganizationController : ControllerBase
    {
        private readonly IOrganizationService _service;

        public OrganizationController(IOrganizationService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }
    }
}
