using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Convene.Application.DTOs.AttendeeProfile;

namespace Convene.Application.Interfaces
{
    public interface IAttendeeProfileService
    {
        Task<AttendeeProfileDto> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateAttendeeProfileDto dto);
        Task<string> UpdateProfileImageAsync(Guid userId, IFormFile file);
    }

}
