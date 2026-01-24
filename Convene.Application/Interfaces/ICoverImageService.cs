using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.Image_Generation;

namespace Convene.Application.Interfaces
{
    public interface ICoverGenerationService
    {
        Task<CoverImageResponse> GenerateCoverAsync(EventCoverDto request);
    }
}
