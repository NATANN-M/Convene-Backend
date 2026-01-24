using Convene.Application.DTOs.Payment;
using Convene.Domain.Entities;
using System.Threading.Tasks;

public interface IPaymentService
{
    Task<PaymentResultDto> InitializePaymentAsync(InitializePaymentRequest request);
    Task<bool> VerifyPaymentAsync(string transactionReference);

    Task<List<UserPaymentsDto>> GetUsersPaymants();

    //for credit payment
   Task<PaymentResultDto> InitializeCreditPurchaseAsync(Guid CreditTransactionId);

    //call back for creadit payment
    Task<bool> ProcessCreditCallbackAsync(string txRef);

    Task<List<Payment>>GetPendingBookingPaymentsAsync();

    Task<List<CreditTransaction>> GetPendingCreditTransactionsAsync();

}
