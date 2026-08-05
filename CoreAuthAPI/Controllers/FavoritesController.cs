using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Microsoft.EntityFrameworkCore;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public FavoritesController(RentalDbContext context)
        {
            _context = context;
        }

        [HttpPost("toggle/{courtId:guid}")]
        public IActionResult ToggleFavorite(Guid courtId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null)
                return NotFound("Kort bulunamadı.");

            var existingFavorite = _context.UserFavoriteCourts.FirstOrDefault(f => f.UserId == userId && f.CourtId == courtId);

            if (existingFavorite != null)
            {
                // Zaten favorilerde var, demek ki kaldırmak istiyor.
                _context.UserFavoriteCourts.Remove(existingFavorite);
                _context.SaveChanges();
                return Ok(new { message = "Kort favorilerden çıkarıldı.", isFavorite = false });
            }
            else
            {
                // Favorilerde yok, ekleyelim.
                var newFavorite = new UserFavoriteCourt
                {
                    UserId = userId,
                    CourtId = courtId
                };
                _context.UserFavoriteCourts.Add(newFavorite);
                _context.SaveChanges();
                return Ok(new { message = "Kort favorilere eklendi.", isFavorite = true });
            }
        }

        [HttpGet("my-favorites")]
        public IActionResult GetMyFavorites()
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                    return Unauthorized("Geçersiz kullanıcı kimliği.");

                var favorites = _context.UserFavoriteCourts
                    .Include(f => f.Court)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.AddedAt)
                    .Select(f => new 
                    {
                        f.CourtId,
                        f.AddedAt,
                        Court = new 
                        {
                            f.Court!.Id,
                            f.Court.Name,
                            f.Court.City,
                            f.Court.District,
                            f.Court.SportType,
                            f.Court.SurfaceType,
                            f.Court.IsActive
                        }
                    })
                    .ToList();

                return Ok(favorites);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Sunucu hatası oluştu", error = ex.Message, inner = ex.InnerException?.Message, stackTrace = ex.StackTrace });
            }
        }
        
        [HttpGet("check/{courtId:guid}")]
        public IActionResult CheckFavorite(Guid courtId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Ok(new { isFavorite = false }); // Hata dönmeye gerek yok, belki anonim kullanıcı bakıyor. Ama Controller [Authorize]! 
            
            var isFavorite = _context.UserFavoriteCourts.Any(f => f.UserId == userId && f.CourtId == courtId);
            return Ok(new { isFavorite });
        }
    }
}
