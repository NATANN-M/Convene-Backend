//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Http;
//using Convene.Application.DTOs.Image_Generation;
//using Convene.Application.Interfaces;
//using Microsoft.AspNetCore.Http.HttpResults;

//namespace Convene.API.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class CoverImageController : ControllerBase
//    {
//        private readonly ICoverGenerationService _coverService;
//        private readonly ILogger<CoverImageController> _logger;

//        public CoverImageController(
//            ICoverGenerationService coverService,
//            ILogger<CoverImageController> logger)
//        {
//            _coverService = coverService;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Generate event cover image
//        /// </summary>
//        /// <param name="request">Event details and optional image</param>
//        /// <returns>Generated cover image</returns>
//        [HttpPost("generate")]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        [ProducesResponseType(StatusCodes.Status400BadRequest)]
//        public async Task<IActionResult> GenerateCover([FromForm] EventCoverDto request)
//        {
//            try
//            {
//                _logger.LogInformation("Generating cover for event: {EventName}", request.EventName);

//                var result = await _coverService.GenerateCoverAsync(request);

//                if (!result.Success || result.CoverImage == null)
//                {
//                    return BadRequest(new { error = "Failed to generate cover image" });
//                }

//                // Return as image file
//                return File(result.CoverImage, $"image/{result.ImageFormat?.ToLower() ?? "webp"}");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating cover image");
//                return StatusCode(500, new { error = "Internal server error" });
//            }
//        }

//        /// <summary>
//        /// Generate cover and return as JSON with base64
//        /// </summary>
//        [HttpPost("generate/json")]
//        [ProducesResponseType(StatusCodes.Status200OK)]
//        public async Task<ActionResult<CoverImageResponse>> GenerateCoverJson([FromForm] EventCoverDto request)
//        {
//            try
//            {
//                var result = await _coverService.GenerateCoverAsync(request);

//                if (result.Success && result.CoverImage != null)
//                {
//                    // Convert to base64 for JSON response
//                    result.CoverImage = null; // Clear binary data if you want base64 instead
//                    // Or convert: result.ImageBase64 = Convert.ToBase64String(result.CoverImage);
//                }

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating cover");
//                return StatusCode(500);
//            }
//        }

//        /// <summary>
//        /// Quick test endpoint
//        /// </summary>
//        [HttpGet("test")]
//        public async Task<IActionResult> Test()
//        {
//            try
//            {
//                var testRequest = new EventCoverDto
//                {
//                    EventName = "Test Event Conference",
//                    EventDate = DateTime.Now.AddDays(30),
//                    Venue = "Test Convention Center",
//                    Description = "Annual technology conference with keynote speakers",
//                    Styles = new List<string> { "Modern", "Tech" },
//                    PrimaryColor = "#3B82F6"
//                };

//                var result = await _coverService.GenerateCoverAsync(testRequest);

//                if (result.Success && result.CoverImage != null)
//                {
//                    return File(result.CoverImage, $"image/{result.ImageFormat?.ToLower() ?? "webp"}");
//                }

//                return BadRequest("Test generation failed");
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Test failed: {ex.Message}");
//            }
//        }
//    }
//}
