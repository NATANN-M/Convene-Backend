namespace Convene.Application.DTOs.OrganizerAnalytics
{
    public class OrganizerBookedUserDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public decimal TotalSpent { get; set; }

        public List<UserTicketDto> Tickets { get; set; } = new List<UserTicketDto>();
    }

    public class UserTicketDto
    {
        public Guid TicketId { get; set; }
        public string TicketTypeName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string AppliedPricingRuleName { get; set; }
        public DateTime PurchaseDate { get; set; }
    }

}
