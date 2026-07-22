using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // 1. Herkesin girebileceği açık bir endpoint
        [HttpGet("public")]
        public IActionResult GetPublicData()
        {
            return Ok("Bu veriyi herkes görebilir, token'a gerek yok.");
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
    }
}