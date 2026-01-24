using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.AdminPlatformSetting;
using Convene.Application.DTOs.Credits;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminPlatformController : ControllerBase
    {
        private readonly ICreditService _creditService;
        private readonly IBoostService _boostService;
        private readonly IPlatformSettingsService _settingsService;

        public AdminPlatformController(ICreditService creditService, IBoostService boostService, IPlatformSettingsService settingsService)
        {
            _creditService = creditService;
            _boostService = boostService;
            _settingsService = settingsService;
        }

        [HttpGet("Get-Platform-Revenue")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            var summary = await _creditService.GetPlatformRevenueSummaryAsync();
            return Ok(summary);
        }

        
        [HttpGet("BoostAnalytics")]
        [ProducesResponseType(typeof(IEnumerable<BoostAnalyticsDto>), 200)]
        public async Task<IActionResult> GetBoostAnalytics()
        {
            var analytics = await _boostService.GetBoostAnalyticsAsync();
            if (analytics == null)
            {

                return NotFound(new { Message = "No Data Found" });
            }
            return Ok(analytics);
        }

        [HttpGet("get-credit-transactions")]
        public async Task<IActionResult> GetCreditTransactions(
     [FromQuery] CreditTransactionFilterDto filter,
     [FromQuery] bool export = false)
        {
            var data = await _creditService.GetCreditTransactionsAsync(filter);

            if (!export)
                return Ok(data);

            // Export
            var csv = ExportToCsv(data);



            return File(csv, "text/csv", "credit-transactions.csv");
        }


        [HttpGet("Get-Platform-Settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return Ok(settings);
        }

        
        [HttpPut("Update-Platform-Settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdatePlatformSettingsDto dto)
        {
            await _settingsService.UpdateSettingsAsync(dto);
            return Ok();
        }

        [HttpPost("Add-Credit-to-Organizer")]

        public async Task <IActionResult> AddCreditToOrganizer([FromQuery] AdminAddCreditDto request)
        {

            await _creditService.AdminAddCreditsAsync(request);

            return Ok();


        }



        private byte[] ExportToCsv(List<CreditTransactionViewDto> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Type,Credits,Amount,Status,Organizer,CreatedAt");

            foreach (var d in data)
            {
                sb.AppendLine(
                    $"{d.Type},{d.CreditsChanged},{d.TotalAmount},{d.Status}," +
                    $"{d.OrganizerName},{d.CreatedAt:yyyy-MM-dd}"
                );
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

    }
}
