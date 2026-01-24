using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Npgsql.BackendMessages;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Application.Settings;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = null)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder ?? "default/images"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            return uploadResult?.SecureUrl?.ToString();
        }

        public async Task<string> UploadVideoAsync(IFormFile file, string folder = null)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder ?? "default/videos"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            return uploadResult?.SecureUrl?.ToString();
        }

        public async Task<List<string>> UploadManyAsync(List<IFormFile> files, string folder = null)
        {
            var urls = new List<string>();

            foreach (var file in files)
            {
                string url;

                if (file.ContentType.StartsWith("video"))
                    url = await UploadVideoAsync(file, folder);
                else
                    url = await UploadImageAsync(file, folder);

                if (url != null)
                    urls.Add(url);
            }

            return urls;
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return false;

            // Extract public ID from URL
            var uri = new Uri(fileUrl);
            var parts = uri.AbsolutePath.Split('/');
            var fileName = parts[^1]; // last segment
            var publicId = Path.Combine(parts[^2], Path.GetFileNameWithoutExtension(fileName)); // folder/file

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image // or Video? You can check extension
            };

            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok" || result.Result == "not_found";
        }


        public async Task TestConnectionAsync()
        {
            await Task.CompletedTask; // simple, no upload
        }

    }
}
