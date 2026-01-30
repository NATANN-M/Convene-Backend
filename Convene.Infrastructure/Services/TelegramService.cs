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
    private readonly ILogger<TelegramService> _logger;
    private readonly string _frontendUrl;

    public TelegramService(IConfiguration config, ILogger<TelegramService> logger)
    {
        _botToken = config["Telegram:BotToken"];
        _chatId = config["Telegram:ChatId"];
        _frontendUrl = config["FrontendUrl"];
        _httpClient = new HttpClient();
        _logger = logger;
    }

    public async Task SendEventToChannelAsync(EventTelegramDto dto)
    {
        if (dto == null) return;

        // Build Google Maps URL from lat|lng
        var mapUrl = BuildGoogleMapsUrl(dto.Location);

        var eventUrl = $"{_frontendUrl}/";

        // Caption text
        string caption =
      $"🎉 *{dto.Title}*\n\n" +
      $"📝 *About the Event*\n" +
      $"{dto.Description}\n\n" +
      $"📍 *Location*\n" +
      $"[View on Google Maps]({mapUrl})\n" +
      $"🏛 Venue: {dto.Venue}\n\n" +
      $"🗓 *Date & Time*\n" +
      $"{dto.StartDate:MMM dd, yyyy HH:mm} → {dto.EndDate:MMM dd, yyyy HH:mm}\n\n" +
      $"🎟 *Tickets*\n" +
      $"From *{dto.LowestTicketPrice} ETB*\n\n" +
      $"🏷 *Category*\n" +
      $"{dto.Category}\n\n";

        // Create a single media group
        var allMedia = new List<object>();
        bool hasMedia = false;

        // Add images
        if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
        {
            for (int i = 0; i < dto.ImageUrls.Count; i++)
            {
                if (!hasMedia)
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

        // Upload videos and get file IDs
        List<string> videoFileIds = new List<string>();
        if (dto.VideoUrls != null && dto.VideoUrls.Count > 0)
        {
            foreach (var videoUrl in dto.VideoUrls)
            {
                if (allMedia.Count >= 10) break;

                try
                {
                    var fileId = await UploadVideoAndGetFileId(videoUrl);
                    if (!string.IsNullOrEmpty(fileId))
                        videoFileIds.Add(fileId);
                }
                catch
                {
                    continue;
                }
            }
        }

        // Add uploaded videos to media group
        foreach (var fileId in videoFileIds)
        {
            if (allMedia.Count >= 10) break;

            if (!hasMedia)
            {
                var videoItem = new
                {
                    type = "video",
                    media = fileId,
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
                    media = fileId
                };
                allMedia.Add(videoItem);
            }
        }

        // Send media group
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
                await SendFallback(dto, caption);
                return;
            }

            await SendInlineButtonAsync(); // ✅ Send button after media
        }
        else
        {
            await SendTextMessage(caption);
            await SendInlineButtonAsync(); // ✅ Send button after text
        }
    }

    private static string BuildGoogleMapsUrl(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "https://maps.google.com";

        var normalized = location.Replace("|", ",");
        return $"https://www.google.com/maps?q={normalized}";
    }

    private async Task<string> UploadVideoAndGetFileId(string videoUrl)
    {
        try
        {
            var videoBytes = await _httpClient.GetByteArrayAsync(videoUrl);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(_chatId), "chat_id");
            form.Add(new ByteArrayContent(videoBytes), "video", "video.mp4");

            var url = $"https://api.telegram.org/bot{_botToken}/sendVideo";
            var response = await _httpClient.PostAsync(url, form);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
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
        if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
        {
            var mediaList = new List<object>();

            for (int i = 0; i < dto.ImageUrls.Count; i++)
            {
                if (i == 0)
                {
                    mediaList.Add(new
                    {
                        type = "photo",
                        media = dto.ImageUrls[i],
                        caption = caption,
                        parse_mode = "Markdown"
                    });
                }
                else
                {
                    mediaList.Add(new
                    {
                        type = "photo",
                        media = dto.ImageUrls[i]
                    });
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

    private async Task SendInlineButtonAsync()
    {
        if (string.IsNullOrWhiteSpace(_frontendUrl))
            return;

        var payload = new
        {
            chat_id = _chatId,
            text = "👇 Open in browser",
            reply_markup = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new
                        {
                            text = "🌐 Open Website",
                            url = _frontendUrl
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

        await _httpClient.PostAsync(url, content);
    }
}
