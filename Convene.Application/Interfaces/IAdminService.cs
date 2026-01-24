using Convene.Application.DTOs.PagenationDtos;
using Convene.Application.DTOs.Requests;
using Convene.Application.DTOs.Responses;
using System;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IAdminService
    {
        // Organizer management
        Task<PaginatedResult<OrganizerPendingResponse>> GetPendingOrganizersAsync(PagedAndSortedRequest request);
        Task<bool> ApproveOrganizerAsync(Guid organizerId, string? adminNotes = null);
        Task<bool> RejectOrganizerAsync(Guid organizerId, string? adminNotes = null);
        Task<OrganizerDetailResponse?> GetOrganizerDetailAsync(Guid organizerId);

        // User management
        Task<PaginatedResult<UserListResponse>> GetAllUsersAsync(PagedAndSortedRequest request, string? role = null, string? status = null);
        Task<PaginatedResult<UserListResponse>> GetAllOrganizersAsync(PagedAndSortedRequest request, string? status = null);
        Task<PaginatedResult<UserListResponse>> SearchUsersAsync(PagedAndSortedRequest request, UserSearchRequest search);
        Task<AuthResponse> UpdateUserStatusAsync(UpdateUserStatusRequest request);
    }
}
