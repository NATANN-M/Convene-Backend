using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Scanner;
using Convene.Domain.Entities;

namespace Convene.Application.Interfaces
{
    public interface ITicketScanService
    {
        Task<ScanTicketResponseDto> ScanAsync(ScanTicketRequestDto request,Guid gatePersonId);

        Task<List<TicketScanLog>> GetScanLogByuserID(Guid gatePersonId);

        Task<PaginatedResult<TicketScanLogResponseDto>> GetEventScanLogsAsync(
    Guid organizerUserId,
    Guid eventId,
    PagedAndSortedRequest request,
    DateTime? from ,
    DateTime? to );

        Task<ScanSummaryResponseDto> GetScanSummaryAsync(Guid organizerUserId, Guid eventId);

        Task<List<GatePersonScanHistoryDto>> GetGatePersonRecentScansAsync(Guid gatePersonId);


    }
}
