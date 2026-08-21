using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly Rental.DataAccess.Context.RentalDbContext _context;

        public TestController(Rental.DataAccess.Context.RentalDbContext context)
        {
            _context = context;
        }

        // 1. Herkesin girebileceği açık bir endpoint
        [HttpGet("public")]
        public IActionResult GetPublicData()
        {
            return Ok("Bu veriyi herkes görebilir, token'a gerek yok.");
        }

        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _context.Users.Select(u => new { u.Id, u.Email, u.Name }).ToList();
            return Ok(users);
        }

        // 2. Sadece geçerli bir token'ı olan (Giriş yapmış) herkesin görebileceği endpoint
        [Authorize]
        [HttpGet("protected")]
        public IActionResult GetProtectedData()
        {
            // Token içindeki emaili (Claim) okuyoruz
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            return Ok($"Hoş geldin! Bu veriyi sadece giriş yapanlar görebilir. Emailin: {userEmail}");
        }

        // 3. Sadece rolü "Admin" olan giriş yapmış kişilerin görebileceği endpoint
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult GetAdminData()
        {
            return Ok("Tebrikler Patron! Bu çok gizli veriyi sadece Adminler görebilir.");
        }

        // 4. Sadece rolü "Customer" (Müşteri) olanların görebileceği endpoint
        [Authorize(Roles = "Customer")]
        [HttpGet("customer-only")]
        public IActionResult GetCustomerData()
        {
            return Ok("Merhaba Değerli Müşterimiz! Sadece müşterilerimiz bu alanı görebilir.");
        }

        // 5. Veritabanına 50 adet sahte saha ve fotoğraflarını ekler
        [Authorize]
        [HttpPost("seed-courts")]
        public IActionResult SeedCourts()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdStr, out Guid userId))
                {
                    return Unauthorized("Geçerli bir kullanıcı bulunamadı. Lütfen tekrar giriş yapın.");
                }

                Rental.DataAccess.Context.RentalDbContextSeeder.SeedCourts(_context, userId);
                return Ok(new { message = "50 adet test sahası ve fotoğrafları başarıyla oluşturuldu." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bir hata oluştu: " + ex.Message });
            }
        }
    }
}