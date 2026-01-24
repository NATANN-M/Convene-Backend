using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Payment;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("init")]
        public async Task<IActionResult> InitPayment([FromBody] InitializePaymentRequest request)
        {
            var result = await _paymentService.InitializePaymentAsync(request);
            return Ok(result);
        }



        // Webhook endpoint for Chapa callback
        [HttpPost("callback")]
        public async Task<IActionResult> PaymentCallback([FromBody] ChapaCallbackDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TxRef))
                return BadRequest(new { error = "Transaction reference missing." });

            try
            {
                bool verified = await _paymentService.VerifyPaymentAsync(dto.TxRef);
                return Ok(new { Verified = verified });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }

        }
       


        [HttpPost("credit/initialize")]
        [Authorize(Roles = "Organizer,SuperAdmin")]
        public async Task<IActionResult> InitializeCreditPurchase([FromQuery] Guid creditTransactionId)
        {
            var result = await _paymentService.InitializeCreditPurchaseAsync(creditTransactionId);
            return Ok(result);
        }

        [HttpGet("credit/callback")]
        public async Task<IActionResult> CreditPaymentCallback([FromQuery] string tx_ref)
        {
            var success = await _paymentService.ProcessCreditCallbackAsync(tx_ref);

            if (!success)
                return BadRequest("Payment verification failed");

            return Ok("Credit purchase completed successfully!");
        }



        [HttpGet("UserPaymnts")]
        public async Task<IActionResult> GetUsersPayment()
        {
            var result = await _paymentService.GetUsersPaymants();
            if (result == null)
            {

                return NotFound(new { message = "No payments found." });
            }

            return Ok(result);


        }
    }

}
