
namespace Convene.Application.DTOs.Requests
{
    public class UpdateUserStatusRequest
    {
        public Guid UserId { get; set; }
        public bool IsActive { get; set; } // true = Active, false = Inactive
    }
}
