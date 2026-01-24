using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    using Microsoft.AspNetCore.Http;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    namespace Convene.Application.Interfaces
    {
        public interface ICloudinaryService
        {
            Task<string> UploadImageAsync(IFormFile file, string folder = null);
            Task<string> UploadVideoAsync(IFormFile file, string folder = null);
            Task<List<string>> UploadManyAsync(List<IFormFile> files, string folder = null);
            Task<bool> DeleteFileAsync(string fileUrl);
            Task TestConnectionAsync();

        }
    }

}
