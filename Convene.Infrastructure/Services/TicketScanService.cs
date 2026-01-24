using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.GatePerson;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Scanner;

using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Common;
using Convene.Infrastructure.Persistence;

public class TicketScanService : ITicketScanService
{
    private readonly ConveneDbContext _context;


    public TicketScanService(ConveneDbContext context)
    {
        _context = context;

    }

    public async Task<ScanTicketResponseDto> ScanAsync(ScanTicketRequestDto request, Guid gatePersonId)
    {
        // 1. Get ticket
        var ticket = await _context.Tickets
            .Include(t => t.TicketType)
            .Include(t => t.Event)
            .Include(t => t.Booking)
                .ThenInclude(b => b.User)
            .FirstOrDefaultAsync(t => t.QrCode == request.QrCode);

        if (ticket == null)
        {
            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "Invalid QR Code"
            };
        }


        var scanner = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == gatePersonId);

        if (scanner == null)
        {
            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "Unauthorized scanner"
            };
        }


        var gatePerson = await _context.GatePersons
            .FirstOrDefaultAsync(g => g.UserId == gatePersonId && g.IsActive);

        if (gatePerson == null)
        {
            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "Gate person not active"
            };
        }


        List<Guid> assignedEventIds = new();

        if (!string.IsNullOrEmpty(gatePerson.AssignmentsJson))
        {
            try
            {
                var assignments = JsonSerializer.Deserialize<List<GatePersonAssignmentDto>>(
                    gatePerson.AssignmentsJson);

                if (assignments != null)
                {
                    assignedEventIds = assignments
                        .Select(a => a.EventId)
                        .ToList();
                }
            }
            catch
            {
                assignedEventIds = new List<Guid>();
            }
        }

        var scanLog = new TicketScanLog
        {
            ScannerUserId = gatePersonId,
            ScannerName = scanner.FullName,
            ScannerEmail = scanner.Email,
            DeviceId = request.DeviceId,
            Location = request.Location,
            TicketId = ticket.Id,
            TicketTypeName = ticket.TicketType.Name,
            TicketHolderName = ticket.Booking.User.FullName,
            TicketHolderEmail = ticket.Booking.User.Email,
            EventId = ticket.EventId,
            EventName = ticket.Event.Title,
            EventStart = ticket.Event.StartDate,
            EventEnd = ticket.Event.EndDate
        };


        if (assignedEventIds.Any() && !assignedEventIds.Contains(ticket.EventId))
        {
            scanLog.IsValid = false;
            scanLog.Reason = "Gate person not assigned to this event.";

            _context.TicketScanLogs.Add(scanLog);
            await _context.SaveChangesAsync();

            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "You are not assigned to scan this event."
            };
        }


        if (ticket.Status == TicketStatus.CheckedIn)
        {
            scanLog.IsValid = false;
            scanLog.Reason = "Ticket has already been used.";

            _context.TicketScanLogs.Add(scanLog);
            await _context.SaveChangesAsync();

            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "Ticket already used.",
                EventName = ticket.Event.Title,
                EventStart = ticket.Event.StartDate,
                EventEnd = ticket.Event.EndDate,
                TicketHolder = ticket.Booking.User.FullName,
                TicketType = ticket.TicketType.Name
            };
        }

        if (DateTime.UtcNow.Date < ticket.Event.StartDate.Date)
        {
            scanLog.IsValid = false;
            scanLog.Reason = "Event has not started yet.";

            _context.TicketScanLogs.Add(scanLog);
            await _context.SaveChangesAsync();

            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "Event has not started yet."
            };
        }

        if (DateTime.UtcNow > ticket.Event.EndDate)
        {
            scanLog.IsValid = false;
            scanLog.Reason = "Event already ended.";

            _context.TicketScanLogs.Add(scanLog);
            await _context.SaveChangesAsync();

            return new ScanTicketResponseDto
            {
                IsValid = false,
                Message = "Event already ended."
            };
        }


        ticket.Status = TicketStatus.CheckedIn;
        scanLog.IsValid = true;

        _context.TicketScanLogs.Add(scanLog);
        await _context.SaveChangesAsync();

        return new ScanTicketResponseDto
        {
            IsValid = true,
            Message = "Ticket valid. Welcome!",
            EventName = ticket.Event.Title,
            EventStart = ticket.Event.StartDate,
            EventEnd = ticket.Event.EndDate,
            TicketHolder = ticket.Booking.User.FullName,
            TicketType = ticket.TicketType.Name
        };
    }


    public async Task<List<TicketScanLog>> GetScanLogByuserID(Guid gatePersonId)
    {
        var gatePersonExist = await _context.GatePersons.AnyAsync(gp => gp.UserId == gatePersonId);
        if (!gatePersonExist)
        {
            throw new ArgumentException("Gate Person not Found");
        }

        return await _context.TicketScanLogs
            .Where(tl => tl.ScannerUserId == gatePersonId)
            .OrderByDescending(tl => tl.ScannedAt)
            .ToListAsync();
    }




    public async Task<PaginatedResult<TicketScanLogResponseDto>> GetEventScanLogsAsync(
     Guid organizerUserId,
     Guid eventId,
     PagedAndSortedRequest request,
     DateTime? from = null,
     DateTime? to = null)
    {
        // Validate that the organizer owns the event
        var ownsEvent = await _context.Events
            .AnyAsync(e => e.Id == eventId && e.OrganizerId == organizerUserId);

        if (!ownsEvent)
            throw new Exception("You do not have permission to view logs for this event.");

        var query = _context.TicketScanLogs
            .Where(l => l.EventId == eventId);

        if (from.HasValue)
            query = query.Where(l => l.ScannedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.ScannedAt <= to.Value);

        // Apply sorting by ScannedAt by default if no sort specified
        if (string.IsNullOrEmpty(request.SortBy))
        {
            request.SortBy = "ScannedAt";
            request.SortDirection = "desc";
        }

        var projectedQuery = query.Select(l => new TicketScanLogResponseDto
        {
            Id = l.Id,
            ScannedAt = l.ScannedAt,
            ScannerUserId = l.ScannerUserId,
            ScannerName = l.ScannerName,
            ScannerEmail = l.ScannerEmail,
            TicketTypeName = l.TicketTypeName,
            TicketHolderName = l.TicketHolderName,
            TicketHolderEmail = l.TicketHolderEmail,
            EventId = l.EventId,
            EventName = l.EventName,
            EventStart = l.EventStart,
            EventEnd = l.EventEnd,
            Location = l.Location,
            DeviceId = l.DeviceId,
            IsValid = l.IsValid,
            Reason = l.Reason
        });

        return await projectedQuery.ApplyPaginationAndSortingAsync(request);
    }


    public async Task<ScanSummaryResponseDto> GetScanSummaryAsync(Guid organizerUserId, Guid eventId)
    {
        // validate event ownership
        var ownsEvent = await _context.Events
            .AnyAsync(e => e.Id == eventId && e.OrganizerId == organizerUserId);

        if (!ownsEvent)
            throw new Exception("You do not have permission to view this event summary.");

        var logs = _context.TicketScanLogs.Where(l => l.EventId == eventId);

        var summary = new ScanSummaryResponseDto
        {
            TotalScans = await logs.CountAsync(),
            ValidScans = await logs.CountAsync(l => l.IsValid),
            InvalidScans = await logs.CountAsync(l => !l.IsValid),
            UniqueTicketHolders = await logs
                .Select(l => l.TicketHolderEmail)
                .Distinct()
                .CountAsync(),
            FirstScan = await logs.OrderBy(l => l.ScannedAt).Select(l => l.ScannedAt).FirstOrDefaultAsync(),
            LastScan = await logs.OrderByDescending(l => l.ScannedAt).Select(l => l.ScannedAt).FirstOrDefaultAsync(),
            GatePersons = await logs
                .GroupBy(l => new { l.ScannerUserId, l.ScannerName })
                .Select(g => new GatePersonSummaryDto
                {
                    GatePersonId = g.Key.ScannerUserId ?? Guid.Empty,
                    GatePersonName = g.Key.ScannerName,
                    TotalScans = g.Count(),
                    ValidScans = g.Count(x => x.IsValid),
                    InvalidScans = g.Count(x => !x.IsValid)
                }).ToListAsync()
        };

        return summary;
    }

    public async Task<List<GatePersonScanHistoryDto>> GetGatePersonRecentScansAsync(Guid gatePersonId)
    {
        return await _context.TicketScanLogs
            .Where(l => l.ScannerUserId == gatePersonId)
            .OrderByDescending(l => l.ScannedAt)
            .Take(20)  // last 20 scans
            .Select(l => new GatePersonScanHistoryDto
            {
                Id = l.Id,
                ScannedAt = l.ScannedAt,
                TicketHolderName = l.TicketHolderName,
                TicketHolderEmail = l.TicketHolderEmail,
                TicketTypeName = l.TicketTypeName,
                IsValid = l.IsValid,
                Reason = l.Reason,
                EventName = l.EventName
            })
            .ToListAsync();
    }


}



