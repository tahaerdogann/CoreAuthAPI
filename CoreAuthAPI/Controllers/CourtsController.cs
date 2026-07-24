using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using System.Security.Claims; // Bu kütüphane Token içindeki claim'leri okumak için şart

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public CourtsController(RentalDbContext context)
        {
            _context = context;
        }

        // 1. Tüm Sahaları Listeleme (AÇIK PLATFORM - Herkes görebilir, Authorize yok!)
        [HttpGet]
        public async Task<IActionResult> GetCourts()
        {
            var courts = await _context.Courts.ToListAsync();
            return Ok(courts);
        }

        // 2. Yeni Saha Ekleme (SADECE OWNER VE ADMIN EKLİYEBİLİR)
        [HttpPost]
        [Authorize(Roles = "Owner,Admin")] // Mükemmel güvenlik! Customer buraya istek atarsa 403 Forbidden yer.
        public async Task<IActionResult> AddCourt([FromBody] Court court)
        {
            // Token'ın içinden giriş yapan kişinin ID'sini çekiyoruz
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized("Kullanıcı kimliği doğrulanamadı.");

            // Sahanın sahibini (OwnerId) otomatik olarak giriş yapan kişi yapıyoruz
            court.OwnerId = int.Parse(userIdString);

            await _context.Courts.AddAsync(court);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Saha başarıyla eklendi!" });
        }
    }
}