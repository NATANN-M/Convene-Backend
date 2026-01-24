using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Services
{
    public class EventCategoryService : IEventCategoryService
    {
        private readonly ConveneDbContext _context;

        public EventCategoryService(ConveneDbContext context)
        {
            _context = context;
        }

        public async Task<EventCategoryResponseDto> AddCategoryAsync(EventCategoryCreateDto dto)
        {
            var category = new EventCategory
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.EventCategories.AddAsync(category);
            await _context.SaveChangesAsync();

            return new EventCategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }
        public async Task<List<EventCategoryResponseDto>> GetAllCategoriesAsync()
        {
            return await _context.EventCategories
                .Select(c => new EventCategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync(); 
        }

        public async Task<EventCategoryResponseDto> UpdateCategoryAsync(EventCategoryCreateDto request, Guid catagoryId)
        {
            var catagroryExist = await _context.EventCategories.FindAsync(catagoryId);

            if (catagroryExist == null)
            {
                throw new KeyNotFoundException("catagory with this id not found");
            }

            if (catagroryExist.IsDefault)
            {

                throw new InvalidOperationException("Can not update system/Default Catagories");
            }

            catagroryExist.Name = request.Name;
            catagroryExist.Description = request.Description;
            catagroryExist.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new EventCategoryResponseDto
            {

                Id = catagroryExist.Id,
                Name=catagroryExist.Name,
                Description=catagroryExist.Description
            };
        }

        public async Task<bool> DeleteCategoryAsync(Guid categoryId)
        {
            var category = await _context.EventCategories
                .Include(c => c.Events) // Include events to check if category is in use
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                throw new KeyNotFoundException($"Category with ID {categoryId} not found.");
            }

            // Check if category is a system/default category
            if (category.IsDefault)
            {
                throw new InvalidOperationException("Cannot delete system/default categories.");
            }

            // Check if category has associated events
            if (category.Events != null && category.Events.Any())
            {
                throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it has {category.Events.Count} associated event(s).");
            }

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<EventCategoryWithCountDto>> GetAllCategoriesWithEventCountAsync()
        {
            return await _context.EventCategories
                .Select(c => new EventCategoryWithCountDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    EventCount = c.Events.Count() 
                })
                .ToListAsync();
        }


    }
}

