using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Recommendation;
using Convene.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Convene.Application.DTOs.EventBrowsing;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Convene.API.Controllers.Recommendation
{
    [ApiController]
    [Route("api/recommend")]
    [Authorize]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("getRecommendationForUser")]
        public async Task<ActionResult<List<EventSummaryDto>>> GetUserRecommendations()
        {
            var userClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userClaim == null)
            {
                return Unauthorized(new { message = "Unautherized Please Login And Try Again." });
            }

            var userId=Guid.Parse(userClaim);

            var recommendations = await _recommendationService.GetRecommendationsForUserAsync(userId);
            return Ok(recommendations);
        }

    }
}
