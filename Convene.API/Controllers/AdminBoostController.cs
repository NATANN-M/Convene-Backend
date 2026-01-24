using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Convene.Application.DTOs.Boosts;
using Convene.Application.DTOs.Boosts.AdminBoostLevel;
using Convene.Application.Interfaces;

namespace Convene.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminBoostController : ControllerBase
    {
        private readonly IBoostService _boostService;

        public AdminBoostController(IBoostService boostService)
        {
            _boostService = boostService;
        }

        [HttpGet("Get-All-Boost-Levels")]
        public async Task<IActionResult> GetAll()
        {
            var boosts = await _boostService.GetAllBoostLevelsAsync();
            return Ok(boosts);
        }

        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetById(Guid id)
        //{
        //    var boost = await _boostService.GetBoostLevelByIdAsync(id);
        //    return Ok(boost);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] CreateBoostLevelDto dto)
        //{
        //    var boost = await _boostService.CreateBoostLevelAsync(dto);
        //    return Ok(boost);
        //}

        [HttpPut("Update-Boost-Level")]
        public async Task<IActionResult> Update([FromBody] UpdateBoostLevelDto dto)
        {
            var boost = await _boostService.UpdateBoostLevelAsync(dto);
            return Ok(boost);
        }

        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    var success = await _boostService.DeleteBoostLevelAsync(id);
        //    return Ok(new { Success = success });
        //}

        [HttpPost("BoostLevel-UpdateStatus/{BoostLevelId}")]
        public async Task<IActionResult> Activate(Guid BoostLevelId ,bool IsActive)
        {
            var success = await _boostService.SetBoostLevelStatusAsync(BoostLevelId,IsActive);
            return Ok(new { Success = success });
        }

       
    }
}
