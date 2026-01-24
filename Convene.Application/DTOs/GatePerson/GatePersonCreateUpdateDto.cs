

namespace Convene.Application.DTOs.GatePerson    
{
    public class GatePersonCreateUpdateDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        // Password is required for create, optional for update
        public string? Password { get; set; }

      //  public string? ProfileImageUrl { get; set; }

        // List of assignments (will be serialized to JSON in service)
        public List<GatePersonAssignmentDto> Assignments { get; set; } = new List<GatePersonAssignmentDto>();
    }

}
