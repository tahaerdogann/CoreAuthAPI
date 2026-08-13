using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using Rental.Business.Interfaces;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        private Guid? GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out Guid userId))
            {
                return userId;
            }
            return null;
        }

        [HttpPost("toggle/{courtId:guid}")]
        public IActionResult ToggleFavorite(Guid courtId)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _favoriteService.ToggleFavorite(courtId, userId.Value);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Kort bulunamadı.") return NotFound(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpGet("my-favorites")]
        public IActionResult GetMyFavorites()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _favoriteService.GetMyFavorites(userId.Value);
            
            if (!result.IsSuccess)
            {
                return StatusCode(500, new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }
        
        [HttpGet("check/{courtId:guid}")]
        [AllowAnonymous] // Changed to AllowAnonymous since logic supports null user
        public IActionResult CheckFavorite(Guid courtId)
        {
            var userId = GetUserId();
            var result = _favoriteService.CheckFavorite(courtId, userId);
            return Ok(result.Data);
        }
    }
}
