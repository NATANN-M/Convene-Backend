using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;

using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class EventService : IEventService
    {
        private readonly ConveneDbContext _context;
        private readonly IPricingService _pricingService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICreditService _creditService;


        public EventService(ConveneDbContext context, 
            IPricingService pricingService ,
            ICloudinaryService cloudinaryService,
            ICreditService creditService
            )
        {
            _context = context;
            _pricingService = pricingService;
            _cloudinaryService = cloudinaryService;
            _creditService = creditService;
        }

        public async Task<EventResponseDto> CreateEventAsync(EventCreateDto dto, Guid organizerId)
        {
            var categoryExists = await _context.EventCategories.AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("select valid category or select category and try again");

            if(dto.TicketSalesStart != null && dto.TicketSalesEnd != null &&
                dto.TicketSalesStart >= dto.TicketSalesEnd)
            {
                throw new Exception("Ticket sales start date must be before end date.");
            }

            if(dto.StartDate >= dto.EndDate)
            {
                throw new Exception("Event start date must be before end date.");
            }
            if (dto.TicketSalesEnd != null)
            {
                if (dto.TicketSalesEnd > dto.StartDate)
                {
                    throw new Exception("Ticket sales end date must be before event start date.");
                }
            }

            var ev = new Event
            {
                Id = Guid.NewGuid(),
                OrganizerId = organizerId,
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                Venue = dto.Venue,
                Location = dto.Location,
                TicketSalesStart=(DateTime?)dto.TicketSalesStart ?? DateTime.UtcNow,
                TicketSalesEnd = dto.TicketSalesEnd ?? dto.StartDate,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalCapacity = dto.TotalCapacity,
                Status = EventStatus.Draft
            };

            // Cloudinary folder for this event
            string eventFolder = $"events/{ev.Id}";

            // Build EventMediaDto
            var media = new EventMediaDto();

            // Keep track of uploaded files to delete if anything fails
            var uploadedFiles = new List<string>();

            try
            {
                // 1 Upload Cover Image
                if (dto.CoverImage != null)
                {
                    media.CoverImage = await _cloudinaryService.UploadImageAsync(dto.CoverImage, $"{eventFolder}/cover");
                    if (media.CoverImage == null)
                        throw new Exception("Cover image upload failed.");
                    uploadedFiles.Add(media.CoverImage);
                }

                //  Upload Additional Images
                media.AdditionalImages = new List<string>();
                if (dto.AdditionalImages?.Any() == true)
                {
                    foreach (var file in dto.AdditionalImages)
                    {
                        var imgUrl = await _cloudinaryService.UploadImageAsync(file, $"{eventFolder}/images");
                        if (imgUrl == null)
                            throw new Exception("Additional image upload failed.");
                        media.AdditionalImages.Add(imgUrl);
                        uploadedFiles.Add(imgUrl);
                    }
                }

                //  Upload Videos
                media.Videos = new List<string>();
                if (dto.Videos?.Any() == true)
                {
                    foreach (var file in dto.Videos)
                    {
                        var videoUrl = await _cloudinaryService.UploadVideoAsync(file, $"{eventFolder}/videos");
                        if (videoUrl == null)
                            throw new Exception("Video upload failed.");
                        media.Videos.Add(videoUrl);
                        uploadedFiles.Add(videoUrl);
                    }
                }

                //  Save media JSON
                ev.CoverImageUrl = System.Text.Json.JsonSerializer.Serialize(media);

                //  Add event to DB
                await _context.Events.AddAsync(ev);
                await _context.SaveChangesAsync();

                return await GetEventByIdAsync(ev.Id);
            }
            catch (Exception ex)
            {
                //  Something failed ? delete all uploaded files
                foreach (var fileUrl in uploadedFiles)
                {
                    await _cloudinaryService.DeleteFileAsync(fileUrl);
                }

                throw new Exception("Event creation failed: " + ex.Message);
            }
        }




        public async Task<EventResponseDto> UpdateEventAsync(EventUpdateDto dto ,Guid eventid)
        {
            var categoryExists = await _context.EventCategories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                throw new Exception("select valid category or select category and try again");

            var ev = await _context.Events
                .Include(e => e.TicketTypes)
                .ThenInclude(t => t.PricingRules)
                .FirstOrDefaultAsync(e => e.Id == eventid);

            if (ev == null)
                throw new Exception("Event not found");

            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.CategoryId = dto.CategoryId;
            ev.Venue = dto.Venue;
            ev.Location = dto.Location;
            ev.TicketSalesStart = dto.TicketSalesStart ?? DateTime.Now;
            ev.TicketSalesEnd = dto.TicketSalesEnd ?? dto.StartDate;
            ev.StartDate = dto.StartDate;
            ev.EndDate = dto.EndDate;
            ev.TotalCapacity = dto.TotalCapacity;

            // ------------------------------
            // Handle media files with Cloudinary
            // ------------------------------

            // Deserialize existing media JSON
            EventMediaDto media;
            if (!string.IsNullOrEmpty(ev.CoverImageUrl))
            {
                media = System.Text.Json.JsonSerializer.Deserialize<EventMediaDto>(ev.CoverImageUrl)
                        ?? new EventMediaDto();
            }
            else
            {
                media = new EventMediaDto();
            }

            // Keep track of newly uploaded files for cleanup if needed
            var newlyUploadedFiles = new List<string>();

            // Cloudinary folder for this event
            string eventFolder = $"events/{ev.Id}";

            try
            {
                // 1?? Update Cover Image if provided
                if (dto.CoverImage != null)
                {
                    // Delete old cover image
                    if (!string.IsNullOrEmpty(media.CoverImage))
                        await _cloudinaryService.DeleteFileAsync(media.CoverImage);

                    media.CoverImage = await _cloudinaryService.UploadImageAsync(dto.CoverImage, $"{eventFolder}/cover");
                    if (media.CoverImage == null)
                        throw new Exception("Cover image upload failed.");
                    newlyUploadedFiles.Add(media.CoverImage);
                }

                // 2?? Update Additional Images if provided
                if (dto.AdditionalImages?.Any() == true)
                {
                    media.AdditionalImages ??= new List<string>();
                    foreach (var file in dto.AdditionalImages)
                    {
                        var imgUrl = await _cloudinaryService.UploadImageAsync(file, $"{eventFolder}/images");
                        if (imgUrl == null)
                            throw new Exception("Additional image upload failed.");
                        media.AdditionalImages.Add(imgUrl);
                        newlyUploadedFiles.Add(imgUrl);
                    }
                }

                // 3?? Update Videos if provided
                if (dto.Videos?.Any() == true)
                {
                    media.Videos ??= new List<string>();
                    foreach (var file in dto.Videos)
                    {
                        var videoUrl = await _cloudinaryService.UploadVideoAsync(file, $"{eventFolder}/videos");
                        if (videoUrl == null)
                            throw new Exception("Video upload failed.");
                        media.Videos.Add(videoUrl);
                        newlyUploadedFiles.Add(videoUrl);
                    }
                }

                // Serialize media back to CoverImageUrl
                ev.CoverImageUrl = System.Text.Json.JsonSerializer.Serialize(media);

                await _context.SaveChangesAsync();
                return await GetEventByIdAsync(ev.Id);
            }
            catch (Exception ex)
            {
                // If something failed ? delete all newly uploaded files
                foreach (var fileUrl in newlyUploadedFiles)
                {
                    await _cloudinaryService.DeleteFileAsync(fileUrl);
                }

                throw new Exception("Event update failed: " + ex.Message);
            }
        }

        public async Task<bool> PublishEventAsync(Guid eventId)
        {
            var ev = await _context.Events
                .Include(e => e.TicketTypes)
                    .ThenInclude(t => t.PricingRules)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                throw new Exception("Event not found");

            // Must have at least one active ticket type
            var activeTicketTypes = ev.TicketTypes.Where(t => t.IsActive).ToList();
            if (!activeTicketTypes.Any())
                throw new Exception("Cannot publish event: At least one active ticket type is required.");

        
            var settings = await _creditService.GetPlatformSettingsAsync();
            var publishCost = settings.EventPublishCost;

            var balance = await _creditService.GetBalanceAsync(ev.OrganizerId);

            if (balance < publishCost)
                throw new Exception($"Not enough credits. Required: {publishCost}, Available: {balance}");

        
            await _creditService.DeductCreditsAsync(
                ev.OrganizerId,
                publishCost,
                type: "PublishEvent",
                description: $"Published event '{ev.Title}'"
            );

           
            ev.Status = EventStatus.Published;
           

            await _context.SaveChangesAsync();
            return true;
        }




        public async Task<EventResponseDto> GetEventByIdAsync(Guid eventId)
        {
            var ev = await _context.Events
                .Include(e => e.TicketTypes)
                .ThenInclude(t => t.PricingRules)
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                throw new Exception("Event not found");

            return await MapEventToDtoAsync(ev);
        }

        public async Task<List<EventResponseDto>> GetOrganizerEventsAsync(Guid organizerId)
        {
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerId)
                .Include(e => e.TicketTypes)
                .ThenInclude(t => t.PricingRules)
                .Include(e => e.Category)
                .ToListAsync();

            var result = new List<EventResponseDto>();
            foreach (var ev in events)
                result.Add(await MapEventToDtoAsync(ev));

            return result;
        }

        public List<TicketTypeResponseDto> GetDefaultTicketTypes() => new()
        {
            new TicketTypeResponseDto {Id=Guid.NewGuid(), Name = "Regular", Description = "Standard ticket", BasePrice = 0 },
            new TicketTypeResponseDto {Id=Guid.NewGuid(), Name = "VIP", Description = "Premium access", BasePrice = 0 },
            new TicketTypeResponseDto {Id = Guid.NewGuid(),  Name = "VVIP", Description = "Exclusive access", BasePrice = 0 }
        };

        public List<PricingRuleResponseDto> GetDefaultPricingRules() => new()
        {
            new PricingRuleResponseDto {Id=Guid.NewGuid(), RuleType = PricingRuleType.EarlyBird, Description = "Discount for early buyers (20% off before event start)", DiscountPercent = 20 },
            new PricingRuleResponseDto {Id = Guid.NewGuid(),  RuleType = PricingRuleType.LastMinute, Description = "Discount for last-minute buyers (15% off in final 2 days)", LastNDaysBeforeEvent = 2, DiscountPercent = 15 },
            new PricingRuleResponseDto {Id=Guid.NewGuid(), RuleType = PricingRuleType.DemandBased, Description = "Increase price after 80% sold (+10%)", ThresholdPercentage = 80, PriceIncreasePercent = 10 }
        };

        #region Mappers 
        private async Task<EventResponseDto> MapEventToDtoAsync(Event ev)
        {
            var ticketDtos = new List<TicketTypeResponseDto>();
            foreach (var t in ev.TicketTypes)
                ticketDtos.Add(await MapTicketTypeToDtoAsync(t));

            // Deserialize JSON to Media
            EventMediaDto? media = null;
            if (!string.IsNullOrEmpty(ev.CoverImageUrl))
            {
                try
                {
                    media = System.Text.Json.JsonSerializer.Deserialize<EventMediaDto>(ev.CoverImageUrl);
                }
                catch
                {
                    media = null;
                }
            }

            return new EventResponseDto
            {
                EventId = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                CategoryName = ev.Category?.Name ?? "",
                Venue = ev.Venue,
                Location = ev.Location,
                TicketSalesStart = ev.TicketSalesStart,
                TicketSalesEnd = ev.TicketSalesEnd,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                TotalCapacity = ev.TotalCapacity,
                Status = ev.Status.ToString(),
                Media = media,  // only Media now
                TicketTypes = ticketDtos
            };
        }

        private async Task<TicketTypeResponseDto> MapTicketTypeToDtoAsync(TicketType t)
        {
            decimal currentPrice = await _pricingService.GetCurrentPriceAsync(t.Id);
            return new TicketTypeResponseDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                BasePrice = t.BasePrice,
                Quantity = t.Quantity,
                Sold = t.Sold,
                CurrentPrice = currentPrice,
                IsActive=t.IsActive,
                PricingRules = t.PricingRules.Select(r => new PricingRuleResponseDto
                {
                    Id = r.Id,
                    RuleType = r.RuleType,
                    Description = r.Description,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    DiscountPercent = r.DiscountPercent,
                    LastNDaysBeforeEvent = r.LastNDaysBeforeEvent,
                    ThresholdPercentage = r.ThresholdPercentage,
                    PriceIncreasePercent = r.PriceIncreasePercent,
                    IsActive = r.IsActive
                }).ToList()
            };
        }



        public async Task<EventTelegramDto?> CompileEventTelegramDataAsync(Guid eventId)
        {
            var evt = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null) return null;

            // Deserialize media JSON
            EventMediaDto? media = null;
            if (!string.IsNullOrWhiteSpace(evt.CoverImageUrl))
            {
                try
                {
                    media = JsonSerializer.Deserialize<EventMediaDto>(evt.CoverImageUrl);
                }
                catch
                {
                    media = new EventMediaDto(); // fallback
                }
            }

            var imageUrls = new List<string>();
            var videoUrls = new List<string>();

            // Add Cover Image
            if (!string.IsNullOrWhiteSpace(media?.CoverImage))
                imageUrls.Add(media.CoverImage);

            // Add Additional Images
            if (media?.AdditionalImages != null)
            {
                foreach (var img in media.AdditionalImages)
                {
                    if (!string.IsNullOrWhiteSpace(img))
                        imageUrls.Add(img);
                }
            }

            // Add Videos
            if (media?.Videos != null)
            {
                foreach (var vid in media.Videos)
                {
                    if (!string.IsNullOrWhiteSpace(vid))
                        videoUrls.Add(vid);
                }
            }

            var dto = new EventTelegramDto
            {
                Title = evt.Title,
                Description = evt.Description,
                Category = evt.Category?.Name ?? "Unknown",
                Venue = evt.Venue,
                Location = evt.Location,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                LowestTicketPrice = evt.TicketTypes
                    .Where(x => x.BasePrice > 0)
                    .OrderBy(x => x.BasePrice)
                    .FirstOrDefault()?.BasePrice ?? 0,
                ImageUrls = imageUrls,
                VideoUrls = videoUrls
            };

            return dto;
        }




        #endregion
    }
}
