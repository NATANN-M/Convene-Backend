using Microsoft.Extensions.Configuration;
using Convene.Application.DTOs;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class TelegramService : ITelegramService
{
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly HttpClient _httpClient;
    private readonly ILogger <TelegramService> _logger;

    public TelegramService(IConfiguration config,ILogger<TelegramService> logger)
    {
        _botToken = config["Telegram:BotToken"];
        _chatId = config["Telegram:ChatId"];
        _httpClient = new HttpClient();
        _logger = logger;
    }

    public async Task SendEventToChannelAsync(EventTelegramDto dto)
    {
        if (dto == null) return;

        // Caption text
        string caption =
            $"?? *{dto.Title}*\n\n" +
            $"{dto.Description}\n\n" +
            $"?? Location: {dto.Location}\n" +
            $"?? Venue: {dto.Venue}\n" +
            $"?? {dto.StartDate:MMM dd, yyyy HH:mm} - {dto.EndDate:MMM dd, yyyy HH:mm}\n" +
            $"?? Tickets from: {dto.LowestTicketPrice} ETB\n" +
            $"?? Category: {dto.Category}";

        // Create a single media group
        var allMedia = new List<object>();

        // Track if we've added any media yet
        bool hasMedia = false;

        // Add images to media group (images work fine with URLs)
        if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
        {
            for (int i = 0; i < dto.ImageUrls.Count; i++)
            {
                if (!hasMedia) // This is the first media item (gets the caption)
                {
                    var mediaItem = new
                    {
                        type = "photo",
                        media = dto.ImageUrls[i],
                        caption = caption,
                        parse_mode = "Markdown"
                    };
                    allMedia.Add(mediaItem);
                    hasMedia = true;
                }
                else
                {
                    var mediaItem = new
                    {
                        type = "photo",
                        media = dto.ImageUrls[i]
                    };
                    allMedia.Add(mediaItem);
                }

                if (allMedia.Count >= 10) break;
            }
        }

        // For videos in media groups, we need to upload them first
        List<string> videoFileIds = new List<string>();
        if (dto.VideoUrls != null && dto.VideoUrls.Count > 0)
        {
            foreach (var videoUrl in dto.VideoUrls)
            {
                if (allMedia.Count >= 10) break;

                try
                {
                    // Upload video to Telegram and get file_id
                    var fileId = await UploadVideoAndGetFileId(videoUrl);
                    if (!string.IsNullOrEmpty(fileId))
                    {
                        videoFileIds.Add(fileId);
                    }
                    else
                    {
                        // If upload fails, skip this video
                        continue;
                    }
                }
                catch
                {
                    // If video upload fails, skip this video
                    continue;
                }
            }
        }

        // Add uploaded videos to media group using their file_id
        foreach (var fileId in videoFileIds)
        {
            if (allMedia.Count >= 10) break;

            if (!hasMedia) // This is the first media item (gets the caption)
            {
                var videoItem = new
                {
                    type = "video",
                    media = fileId, // Use file_id instead of URL
                    caption = caption,
                    parse_mode = "Markdown"
                };
                allMedia.Add(videoItem);
                hasMedia = true;
            }
            else
            {
                var videoItem = new
                {
                    type = "video",
                    media = fileId // Use file_id instead of URL
                };
                allMedia.Add(videoItem);
            }
        }

        // Send everything as ONE media group
        if (allMedia.Count > 0)
        {
            var payload = new
            {
                chat_id = _chatId,
                media = allMedia
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var url = $"https://api.telegram.org/bot{_botToken}/sendMediaGroup";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var respContent = await response.Content.ReadAsStringAsync();

              
                // (images in gallery, videos separately)
                await SendFallback(dto, caption);
                return;

                
                 throw new Exception($"Telegram gallery post failed: {respContent}");
            }
        }
        else
        {
            // No media ? send text only
            await SendTextMessage(caption);
        }
    }

    private async Task<string> UploadVideoAndGetFileId(string videoUrl)
    {
        try
        {
            // Download the video
            var videoBytes = await _httpClient.GetByteArrayAsync(videoUrl);

            // Upload to Telegram
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_chatId), "chat_id");
            form.Add(new ByteArrayContent(videoBytes), "video", "video.mp4");

            var url = $"https://api.telegram.org/bot{_botToken}/sendVideo";
            var response = await _httpClient.PostAsync(url, form);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse the response to get file_id
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("video", out var video) &&
                    video.TryGetProperty("file_id", out var fileId))
                {
                    return fileId.GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task SendFallback(EventTelegramDto dto, string caption)
    {
        // Fallback: Send images as gallery, videos separately (your original approach)
        if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
        {
            var mediaList = new List<object>();

            for (int i = 0; i < dto.ImageUrls.Count; i++)
            {
                if (i == 0)
                {
                    var mediaItem = new
                    {
                        type = "photo",
                        media = dto.ImageUrls[i],
                        caption = caption,
                        parse_mode = "Markdown"
                    };
                    mediaList.Add(mediaItem);
                }
                else
                {
                    var mediaItem = new
                    {
                        type = "photo",
                        media = dto.ImageUrls[i]
                    };
                    mediaList.Add(mediaItem);
                }
            }

            var payload = new
            {
                chat_id = _chatId,
                media = mediaList
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var url = $"https://api.telegram.org/bot{_botToken}/sendMediaGroup";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var respContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Telegram images gallery failed: {respContent}");
            }
        }
        else
        {
            await SendTextMessage(caption);
        }

        // Send videos separately
        if (dto.VideoUrls != null && dto.VideoUrls.Count > 0)
        {
            foreach (var videoUrl in dto.VideoUrls)
            {
                var payload = new Dictionary<string, string>
                {
                    ["chat_id"] = _chatId,
                    ["video"] = videoUrl
                };

                var content = new FormUrlEncodedContent(payload);
                var url = $"https://api.telegram.org/bot{_botToken}/sendVideo";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Telegram video post failed: {respContent}");
                }
            }
        }
    }

    private async Task SendTextMessage(string text)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = _chatId,
            ["text"] = text,
            ["parse_mode"] = "Markdown"
        });

        var response = await _httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            var respContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Telegram text post failed: {respContent}");
        }
    }
}
