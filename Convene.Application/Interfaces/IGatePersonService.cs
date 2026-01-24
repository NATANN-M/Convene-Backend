using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Convene.Application.DTOs.GatePerson;

public interface IGatePersonService
{
    Task<List<AssignmentEventDto>> GetEventsForAssignmentAsync(Guid organizerId);

    Task<GatePersonViewDto> CreateGatePersonAsync(Guid organizerId, GatePersonCreateUpdateDto dto);

    Task<GatePersonViewDto> UpdateGatePersonAsync(Guid id, GatePersonCreateUpdateDto dto);

    Task<bool> DeleteGatePersonAsync(Guid id, bool hardDelete = false);

    Task<List<GatePersonViewDto>> GetAllGatePersonsAsync(Guid organizerId);

    Task<GatePersonDashboardDto> GetMyDashboardAsync(Guid gatePersonUserId);
}
