using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Event;
using Convene.Application.Interfaces;
using Convene.Infrastructure.Services;
using System.Threading.Tasks;

namespace Convene.API.Controllers
{
    [ApiController]
    [Route("api/event-categories")]
    public class EventCategoryController : ControllerBase
    {
        private readonly IEventCategoryService _categoryService;

        public EventCategoryController(IEventCategoryService categoryService)
        {
            _categoryService = categoryService;
        }


        // ================= ADD CATEGORY (ADMIN ONLY) =================
        [HttpPost("add-category")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> AddCategory([FromBody] EventCategoryCreateDto dto)
        {
            var category = await _categoryService.AddCategoryAsync(dto);
            return Ok(category);
        }
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPatch("Update-Category")]
   public async Task<IActionResult> Updatecatagory(EventCategoryCreateDto request,Guid CategoryId)
        {
            var category = await _categoryService.UpdateCategoryAsync(request, CategoryId);

            return Ok(category);

        }
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("Delete-Category")]
        public async Task<bool> deletecategory(Guid CategoryId)
        {
            return await _categoryService.DeleteCategoryAsync(CategoryId);


        }
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("categories-with-count")]
        public async Task<IActionResult> GetCategoriesWithEventCount()
        {
            var result = await _categoryService.GetAllCategoriesWithEventCountAsync();
            return Ok(result);
        }



        // ================= GET ALL CATEGORIES =================
        [HttpGet("get-all-categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

    }
}
