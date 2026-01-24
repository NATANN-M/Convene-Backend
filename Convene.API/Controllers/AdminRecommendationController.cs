using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Recommendation;
using Convene.Application.Interfaces;
using System.Threading.Tasks;

namespace Convene.API.Controllers.Recommendation
{
    [ApiController]
    [Route("api/admin/recommend")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminRecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public AdminRecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("metrics")]
        public async Task<ActionResult<AdminMetricsDto>> GetMetrics()
        {
            var metrics = await _recommendationService.GetMetricsAsync();
            return Ok(metrics);

        }

        [HttpPost("retrain")]
        public async Task<IActionResult> RetrainAll()
        {
            await _recommendationService.RetrainGlobalModelAsync();
            return Ok(new { message = "Global model retrained successfully." });
        }

        [HttpPost("retrain/{userId}")]
        public async Task<IActionResult> RetrainUser(Guid userId)
        {
            await _recommendationService.RetrainForUserAsync(userId);
            return Ok(new { message = $"Model retrained for user {userId} successfully." });
        }
    }
}
