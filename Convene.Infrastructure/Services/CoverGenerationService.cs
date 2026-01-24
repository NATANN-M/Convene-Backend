using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Convene.Application.DTOs.Image_Generation;
using Convene.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Convene.Infrastructure.Services
{
    public class CoverGenerationService : ICoverGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CoverGenerationService> _logger;
        private readonly IConfiguration _configuration;

        public CoverGenerationService(
            HttpClient httpClient,
            ILogger<CoverGenerationService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<CoverImageResponse> GenerateCoverAsync(EventCoverDto request)
        {
            try
            {
                // Get configuration from appsettings
                var baseUrl = _configuration["FalAI:BaseUrl"] ?? "https://queue.fal.run";
                var defaultModel = _configuration["FalAI:DefaultModel"] ?? "fal-ai/flux";

                // 1. Prepare prompt from event data
                var prompt = BuildEventPrompt(request);

                byte[] imageBytes;

                // 2. Determine generation type
                if (request.ExistingImage != null)
                {
                    imageBytes = await GenerateImageToImageAsync(prompt, request.ExistingImage, defaultModel);
                }
                else
                {
                    imageBytes = await GenerateTextToImageAsync(prompt, defaultModel);
                }

                return new CoverImageResponse
                {
                    Success = true,
                    CoverImage = imageBytes,
                    ImageFormat = "webp"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cover generation failed");
                return new CoverImageResponse
                {
                    Success = false,
                    CoverImage = null
                };
            }
        }

        private string BuildEventPrompt(EventCoverDto request)
        {
            var prompt = $"Professional event cover design for '{request.EventName}'. ";

            // Date formatting
            prompt += $"Event Date: {request.EventDate:MMMM dd, yyyy}. ";

            // Venue
            prompt += $"Location: {request.Venue}. ";

            // Optional description
            if (!string.IsNullOrEmpty(request.Description))
                prompt += $"Description: {request.Description}. ";

            // Style preferences
            if (request.Styles != null && request.Styles.Any())
                prompt += $"Design Style: {string.Join(", ", request.Styles)}. ";

            // Color preferences
            if (!string.IsNullOrEmpty(request.PrimaryColor))
                prompt += $"Primary Color: {request.PrimaryColor}. ";

            if (!string.IsNullOrEmpty(request.ColorTheme))
                prompt += $"Color Theme: {request.ColorTheme}. ";

            if (!string.IsNullOrEmpty(request.Brightness))
                prompt += $"Brightness Level: {request.Brightness}. ";

            // Final touch for better AI understanding
            prompt += "High quality website banner, professional graphic design, clean layout, ";
            prompt += "perfect for social media and website header, 1200x630 aspect ratio.";

            return prompt;
        }

        private async Task<byte[]> GenerateTextToImageAsync(string prompt, string modelName)
        {
            var payload = new
            {
                model_name = modelName,
                prompt = prompt,
                image_size = new
                {
                    width = 1200,
                    height = 630
                },
                num_inference_steps = 25,
                guidance_scale = 7.5,
                num_images = 1,
                enable_safety_checker = true,
                output_format = "webp"
            };

            var response = await _httpClient.PostAsJsonAsync("", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<FalResponse>();
            return await DownloadImageAsync(result.images[0].url);
        }

        private async Task<byte[]> GenerateImageToImageAsync(
            string prompt,
            IFormFile imageFile,
            string modelName)
        {
            // Convert uploaded image to base64
            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            var base64Image = Convert.ToBase64String(ms.ToArray());

            // Get file extension for correct MIME type
            var extension = Path.GetExtension(imageFile.FileName).ToLower();
            var mimeType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            var payload = new
            {
                model_name = modelName,
                image_url = $"data:{mimeType};base64,{base64Image}",
                prompt = prompt,
                strength = 0.7,
                num_inference_steps = 25,
                guidance_scale = 7.0,
                num_images = 1,
                output_format = "webp"
            };

            var response = await _httpClient.PostAsJsonAsync("", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<FalResponse>();
            return await DownloadImageAsync(result.images[0].url);
        }

        private async Task<byte[]> DownloadImageAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        private class FalResponse
        {
            public List<FalImage> images { get; set; }
        }

        private class FalImage
        {
            public string url { get; set; }
        }
    }
}
