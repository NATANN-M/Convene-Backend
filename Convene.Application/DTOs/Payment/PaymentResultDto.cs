using Convene.Domain.Enums;

public class PaymentResultDto
{
    public Guid PaymentId { get; set; }
    public string CheckoutUrl { get; set; } = null!;
    public string PaymentReference { get; set; } = null!;
    public PaymentStatus Status { get; set; }
}
