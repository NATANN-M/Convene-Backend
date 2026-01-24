using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.EventFeedback;
using Convene.Application.DTOs.Feedback;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class EventFeedbackService : IEventFeedbackService
    {
        private readonly ConveneDbContext _context;
        private readonly INotificationService _notificationService;

        public EventFeedbackService(
            ConveneDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // =========================================================
        // PRIVATE: update organizer rating cache (CORE OPTIMIZATION)
        // =========================================================
        private async Task UpdateOrganizerRatingCacheAsync(Guid organizerId)
        {
            var stats = await _context.EventFeedbacks
                .Where(f =>
                    _context.Events
                        .Where(e => e.OrganizerId == organizerId)
                        .Select(e => e.Id)
                        .Contains(f.EventId))
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Average = Math.Round(g.Average(x => x.Rating), 1)
                })
                .FirstOrDefaultAsync();

            var profile = await _context.OrganizerProfiles
                .FirstOrDefaultAsync(o => o.UserId == organizerId);

            if (profile == null) return;

            profile.TotalRatings = stats?.Total ?? 0;
            profile.AverageRating = stats?.Average ?? 0;

            await _context.SaveChangesAsync();
        }

        // =========================================================
        // Eligible events for feedback
        // =========================================================
        public async Task<List<EligibleEventForFeedbackDto>> GetEligibleEventsForFeedbackAsync(Guid userId)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(b =>
                    b.UserId == userId &&
                    b.Status == BookingStatus.Confirmed &&
                    b.Event.EndDate < DateTime.UtcNow &&
                    !_context.EventFeedbacks.Any(f => f.EventId == b.EventId && f.UserId == userId))
                .Select(b => new EligibleEventForFeedbackDto
                {
                    EventId = b.Event.Id,
                    Title = b.Event.Title,
                    EndDate = b.Event.EndDate,
                    CoverImageUrl = b.Event.CoverImageUrl,
                    OrganizerName = _context.Users
                        .Where(u => u.Id == b.Event.OrganizerId)
                        .Select(u => u.FullName)
                        .FirstOrDefault() ?? "Organizer"
                })
                .Distinct()
                .ToListAsync();
        }

        // =========================================================
        // Submit feedback
        // =========================================================
        public async Task<FeedbackViewDto> SubmitFeedbackAsync(Guid eventId, CreateFeedbackDto dto, Guid userId)
        {
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (ev == null) throw new KeyNotFoundException("Event not found.");
            if (ev.EndDate >= DateTime.UtcNow) throw new InvalidOperationException("Event has not ended.");

            var hasBooking = await _context.Bookings.AnyAsync(b =>
                b.EventId == eventId &&
                b.UserId == userId &&
                b.Status == BookingStatus.Confirmed);

            if (!hasBooking)
                throw new InvalidOperationException("You can only give feedback for events you attended.");

            var exists = await _context.EventFeedbacks.AnyAsync(f =>
                f.EventId == eventId && f.UserId == userId);

            if (exists)
                throw new InvalidOperationException("You already submitted feedback for this event.");

            var feedback = new EventFeedback
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EventFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // ?? update cached organizer rating
            await UpdateOrganizerRatingCacheAsync(ev.OrganizerId);

            // Notify user
            await _notificationService.SendNotificationAsync(
                userId,
                "Feedback received",
                $"Thanks — your feedback for '{ev.Title}' has been saved.",
                NotificationType.General);

            // Notify organizer (best effort)
            await SafeNotifyAsync(
                ev.OrganizerId,
                "New feedback",
                $"Your event '{ev.Title}' received new feedback.",
                ev.Id.ToString());

            var user = await _context.Users.FindAsync(userId);

            return new FeedbackViewDto
            {
                FeedbackId = feedback.Id,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                UserName = user?.FullName ?? "Anonymous",
                CreatedAt = feedback.CreatedAt
            };
        }

        // =========================================================
        // Update feedback
        // =========================================================
        public async Task<FeedbackViewDto> UpdateFeedbackAsync(Guid feedbackId, UpdateFeedbackDto dto, Guid userId)
        {
            var feedback = await _context.EventFeedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId && f.UserId == userId);

            if (feedback == null)
                throw new KeyNotFoundException("Feedback not found or access denied.");

            feedback.Rating = dto.Rating;
            feedback.Comment = dto.Comment;
            feedback.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var organizerId = await _context.Events
                .Where(e => e.Id == feedback.EventId)
                .Select(e => e.OrganizerId)
                .FirstAsync();

            await UpdateOrganizerRatingCacheAsync(organizerId);

            var user = await _context.Users.FindAsync(userId);

            return new FeedbackViewDto
            {
                FeedbackId = feedback.Id,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                UserName = user?.FullName ?? "Anonymous",
                CreatedAt = feedback.CreatedAt
            };
        }

        // =========================================================
        // Delete feedback
        // =========================================================
        public async Task<bool> DeleteFeedbackAsync(Guid feedbackId, Guid userId)
        {
            var feedback = await _context.EventFeedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId && f.UserId == userId);

            if (feedback == null) return false;

            var organizerId = await _context.Events
                .Where(e => e.Id == feedback.EventId)
                .Select(e => e.OrganizerId)
                .FirstAsync();

            _context.EventFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            await UpdateOrganizerRatingCacheAsync(organizerId);

            return true;
        }

        // =========================================================
        // User feedback history (optimized)
        // =========================================================
        public async Task<List<FeedbackViewDto>> GetUserFeedbackHistoryAsync(Guid userId)
        {
            return await _context.EventFeedbacks
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackViewDto
                {
                    FeedbackId = f.Id,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    UserName = f.User.FullName,
                    UserAvatarUrl = f.User.ProfileImageUrl,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        // =========================================================
        // Public: feedbacks for event
        // =========================================================
        public async Task<List<FeedbackViewDto>> GetFeedbacksForEventAsync(Guid eventId)
        {
            return await _context.EventFeedbacks
                .AsNoTracking()
                .Where(f => f.EventId == eventId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackViewDto
                {
                    FeedbackId = f.Id,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    UserName = f.User.FullName,
                    UserAvatarUrl=f.User.ProfileImageUrl,
                    EventTitle = f.Event.Title,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        // =========================================================
        // Public: event feedback summary
        // =========================================================
        public async Task<EventFeedbackSummaryDto> GetEventFeedbackSummaryAsync(Guid eventId)
        {
            var stats = await _context.EventFeedbacks
                .Where(f => f.EventId == eventId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Average = g.Average(x => x.Rating)
                })
                .FirstOrDefaultAsync();

            var feedbacks = await GetFeedbacksForEventAsync(eventId);

            return new EventFeedbackSummaryDto
            {
                EventId = eventId,
                TotalFeedbacks = stats?.Total ?? 0,
                AverageRating = stats?.Average ?? 0,
                Feedbacks = feedbacks
            };
        }

        // =========================================================
        // User feedback for event
        // =========================================================
        public async Task<UserFeedbackForEventDto?> GetUserFeedbackForEventAsync(Guid userId, Guid eventId)
        {
            var ev = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null) return null;

            var feedback = await _context.EventFeedbacks
                .AsNoTracking()
                .Where(f => f.EventId == eventId && f.UserId == userId)
                .Select(f => new FeedbackViewDto
                {
                    FeedbackId = f.Id,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    UserName = f.User.FullName,
                    CreatedAt = f.CreatedAt
                })
                .FirstOrDefaultAsync();

            return new UserFeedbackForEventDto
            {
                EventId = ev.Id,
                EventTitle = ev.Title,
                EventEndDate = ev.EndDate,
                Feedback = feedback
            };
        }

        // for organizer all events he organized feedbacks
        public async Task<List<FeedbackViewDto>> GetFeedbacksForOrganizerAsync(Guid organizerId)
        {
            // Always update organizer rating cache before fetching feedbacks
            await UpdateOrganizerRatingCacheAsync(organizerId);

            // Fetch feedbacks for all events of the organizer
            var feedbacks = await _context.EventFeedbacks
                .AsNoTracking()
                .Where(f => _context.Events
                    .Where(e => e.OrganizerId == organizerId)
                    .Select(e => e.Id)
                    .Contains(f.EventId))
                .Include(f => f.User)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackViewDto
                {
                    FeedbackId = f.Id,
                    Rating = f.Rating,
                    Comment = f.Comment,
                    UserName = f.User.FullName,
                    UserAvatarUrl=f.User.ProfileImageUrl,
                    EventTitle = f.Event.Title,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            return feedbacks;
        }





        // =========================================================
        // Safe notification helper
        // =========================================================
        private async Task SafeNotifyAsync(Guid userId, string title, string message, string? referenceKey = null)
        {
            try
            {
                await _notificationService.SendNotificationAsync(
                    userId, title, message, NotificationType.General, referenceKey);
            }
            catch
            {
                // intentionally ignored
            }
        }
    }
}
