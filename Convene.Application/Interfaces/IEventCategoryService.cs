using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Convene.Application.DTOs.Event;

namespace Convene.Application.Interfaces
{
    public interface IEventCategoryService
    {
        Task<EventCategoryResponseDto> AddCategoryAsync(EventCategoryCreateDto dto);

        Task<List<EventCategoryResponseDto>> GetAllCategoriesAsync();

        Task<EventCategoryResponseDto> UpdateCategoryAsync(EventCategoryCreateDto request, Guid categoryId);
        Task<bool> DeleteCategoryAsync(Guid categoryId);

        Task<List<EventCategoryWithCountDto>> GetAllCategoriesWithEventCountAsync();

    }
}
