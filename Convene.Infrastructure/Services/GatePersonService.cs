using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.GatePerson;
using Convene.Domain.Enums;
using System.Text.Json;

public class GatePersonService : IGatePersonService
{
    private readonly ConveneDbContext _context;

    public GatePersonService(ConveneDbContext context)
    {
        _context = context;
    }


    public async Task<List<AssignmentEventDto>> GetEventsForAssignmentAsync(Guid organizerId)
    {
        return await _context.Events
            .Where(e => e.OrganizerId == organizerId && e.Status == EventStatus.Published)
            .Select(e => new AssignmentEventDto
            {
                EventId = e.Id,
                EventName = e.Title
            })
            .ToListAsync();
    }



    public async Task<GatePersonViewDto> CreateGatePersonAsync(Guid organizerId, GatePersonCreateUpdateDto dto)
    {
        // Check if email already exists
        var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
            throw new InvalidOperationException("Email is already in use by another user.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.GatePerson,
            Status = UserStatus.Active,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var gatePerson = new GatePerson
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedByOrganizerId = organizerId,
            AssignmentsJson = JsonSerializer.Serialize(dto.Assignments),
            IsActive = true
        };

        _context.GatePersons.Add(gatePerson);
        await _context.SaveChangesAsync();

        return new GatePersonViewDto
        {
            UserId = gatePerson.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Assignments = dto.Assignments,
            IsActive = gatePerson.IsActive,
        };
    }


    public async Task<GatePersonViewDto> UpdateGatePersonAsync(Guid id, GatePersonCreateUpdateDto dto)
    {
        var gatePerson = await _context.GatePersons
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.UserId == id);

        if (gatePerson == null)
            throw new KeyNotFoundException("Gate person not found.");

       
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email && u.Id != gatePerson.UserId);

        if (emailExists)
            throw new InvalidOperationException("Email is already in use by another user.");

        gatePerson.User.FullName = dto.FullName;
        gatePerson.User.Email = dto.Email;
        gatePerson.User.PhoneNumber = dto.PhoneNumber;

        // Update password if provided
        if (!string.IsNullOrEmpty(dto.Password))
        {
            gatePerson.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        gatePerson.AssignmentsJson = JsonSerializer.Serialize(dto.Assignments);

        await _context.SaveChangesAsync();

        return new GatePersonViewDto
        {
            UserId = gatePerson.Id,
            FullName = gatePerson.User?.FullName ?? "",
            Email = gatePerson.User?.Email ?? "",
            PhoneNumber = gatePerson.User?.PhoneNumber ?? "",
            Assignments = dto.Assignments,
            IsActive = gatePerson.IsActive
        };
    }


    public async Task<bool> DeleteGatePersonAsync(Guid id, bool hardDelete = false)
    {
        var gatePerson = await _context.GatePersons
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gatePerson == null)
            return false;

        if (hardDelete)
        {
            // Delete GatePerson
            _context.GatePersons.Remove(gatePerson);

            // Also delete the associated User account
            if (gatePerson.User != null)
            {
                _context.Users.Remove(gatePerson.User);
            }

            await _context.SaveChangesAsync();
        }
        else
        {
            // Soft delete: Deactivate GatePerson
            gatePerson.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<List<GatePersonViewDto>> GetAllGatePersonsAsync(Guid organizerId)
    {
        var list = await _context.GatePersons
            .Where(g => g.CreatedByOrganizerId == organizerId)
            .Include(g => g.User) // Include User for common fields
            .ToListAsync();

        return list.Select(g =>
        {
            // Deserialize AssignmentsJson to list
            List<GatePersonAssignmentDto> assignments = new();

            if (!string.IsNullOrEmpty(g.AssignmentsJson))
            {
                try
                {
                    assignments = JsonSerializer.Deserialize<List<GatePersonAssignmentDto>>(g.AssignmentsJson)
                        ?? new List<GatePersonAssignmentDto>();
                }
                catch
                {
                    // If JSON is invalid, return empty list
                    assignments = new List<GatePersonAssignmentDto>();
                }
            }

            return new GatePersonViewDto
            {
                UserId = g.User.Id,
                FullName = g.User?.FullName ?? "",
                Email = g.User?.Email ?? "",
                PhoneNumber = g.User?.PhoneNumber ?? "",
                Assignments = assignments,
                IsActive = g.IsActive
            };
        }).ToList();
    }


    public async Task<GatePersonDashboardDto> GetMyDashboardAsync(Guid gatePersonUserId)
    {
        // 1. Get GatePerson with User
        var gatePerson = await _context.GatePersons
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.UserId == gatePersonUserId && g.IsActive);

        if (gatePerson == null)
            throw new Exception("Gate person not found");

        // 2. Get Organizer
        var organizer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == gatePerson.CreatedByOrganizerId);

        // 3. Deserialize assignments
        List<Guid> assignedEventIds = new();

        if (!string.IsNullOrEmpty(gatePerson.AssignmentsJson))
        {
            var assignments = JsonSerializer.Deserialize<List<GatePersonAssignmentDto>>(
                gatePerson.AssignmentsJson);

            if (assignments != null)
            {
                assignedEventIds = assignments.Select(a => a.EventId).ToList();
            }
        }

        // 4. Load events
        List<GatePersonAssignedEventDto> events = new();

        if (assignedEventIds.Any())
        {
            events = await _context.Events
                .Where(e => assignedEventIds.Contains(e.Id))
                .Select(e => new GatePersonAssignedEventDto
                {
                    EventId = e.Id,
                    EventName = e.Title,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                })
                .ToListAsync();
        }

        return new GatePersonDashboardDto
        {
            GatePersonId = gatePerson.Id,
            FullName = gatePerson.User.FullName,
            Email = gatePerson.User.Email,
            OrganizerName = organizer?.FullName ?? "Unknown Organizer",
            HasAssignments = assignedEventIds.Any(),
            AssignedEvents = events
        };
    }


}
