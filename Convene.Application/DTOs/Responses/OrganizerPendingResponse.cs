namespace Convene.Application.DTOs.Responses
{
    public class OrganizerPendingResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string BusinessName { get; set; } = null!;
        public string BusinessEmail { get; set; } = null!;
        public string KYCIdDocument { get; set; } = null!;
    }
}
