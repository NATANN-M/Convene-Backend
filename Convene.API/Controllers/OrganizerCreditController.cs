using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Credits;
using Convene.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/organizer-credit")]
    [Authorize(Roles = "Organizer,SuperAdmin")]
    public class OrganizerCreditController : ControllerBase
    {
        private readonly ICreditService _creditService;
      
        public OrganizerCreditController(ICreditService creditService)
        {
            _creditService = creditService;
           
        }

      
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var organizerProfileIdClaim = User.FindFirstValue (ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(organizerProfileIdClaim))
            {
                return Unauthorized(new { Message = "Unauthorized Please Login And Try Again" });

            }

            var organizerProfileId = Guid.Parse(organizerProfileIdClaim);

            var balance = await _creditService.GetBalanceAsync(organizerProfileId);
            return Ok(new { balance });
        }

       
       

        //show info about prices
        [HttpGet("purchase-info")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> GetCreditPurchaseInfo()
        {
            var info = await _creditService.GetPurchaseInfoAsync();
            return Ok(info);
        }


        [HttpPost("buy")]

        public async Task<IActionResult> BuyCredits([FromQuery] int credits)
        {
            var organizerProfileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(organizerProfileIdClaim))
            {
                return Unauthorized(new { Message = "Unauthorized Please Login And Try Again" });

            }

            var organizerProfileId = Guid.Parse(organizerProfileIdClaim);


            var tx = await _creditService.CreatePendingTransactionAsync(organizerProfileId, credits);

            var response = new CreditPurchaseResponseDto
            {

                CreditTransactionId = tx.Id,
                TotalAmount=tx.TotalAmount ?? 0,
                CreditsPurchased = tx.CreditsChanged,

                Descritption=tx.Description ?? "NO Description",
                Status = tx.Status.ToString()

            };

            return Ok(response);
            
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var organizerProfileIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(organizerProfileIdClaim))
            {
                return Unauthorized(new { Message = "Unauthorized Please Login And Try Again" });

            }

            var organizerProfileId = Guid.Parse(organizerProfileIdClaim);

            var transactions = await _creditService.GetTransactionHistoryAsync(organizerProfileId);
            return Ok(transactions);
        }
    }
}
