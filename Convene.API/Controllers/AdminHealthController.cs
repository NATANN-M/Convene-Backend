// WebAPI/Controllers/AdminHealthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Infrastructure.Services;
using System.Threading.Tasks;

namespace Convene.WebAPI.Controllers
{
    [ApiController]
    [Route("api/admin/health")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminHealthController : ControllerBase
    {
        private readonly HealthService _healthService;

        public AdminHealthController(HealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckNow()
        {
            var health = await _healthService.CheckHealthAsync(storeSnapshot: true);
            return Ok(health);
        }
    }
}
