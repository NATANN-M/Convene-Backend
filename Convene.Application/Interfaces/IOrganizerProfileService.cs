using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Convene.Application.DTOs.OrganizerProfile;

namespace Convene.Application.Interfaces
{
    public interface IOrganizerProfileService
    {
        Task<OrganizerProfileDto> GetProfileAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateOrganizerProfileDto dto);
        Task<string> UpdateProfileImageAsync(Guid userId, IFormFile file);
    }

}
