using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.DTOs.Image_Generation
{
    public class CoverImageResponse
    {
        public bool Success { get; set; }
        public byte[]? CoverImage { get; set; }
        public string? ImageFormat { get; set; }
    }
}
