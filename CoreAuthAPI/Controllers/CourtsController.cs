using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // İŞTE GERÇEK GÜVENLİK BURASI! Token'ı olmayan giremez.
    public class CourtsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public CourtsController(RentalDbContext context)
        {
            _context = context;
        }

        // 1. Tüm Sahaları Listeleme (GET)
        [HttpGet]
        public async Task<IActionResult> GetCourts()
        {
            var courts = await _context.Courts.ToListAsync();
            return Ok(courts);
        }

        // 2. Yeni Saha Ekleme (POST)
        [HttpPost]
        public async Task<IActionResult> AddCourt([FromBody] Court court)
        {
            await _context.Courts.AddAsync(court);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Saha başarıyla eklendi!" });
        }
    }
}