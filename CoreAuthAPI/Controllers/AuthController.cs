using CoreAuthAPI.Data;
using CoreAuthAPI.Dtos;
using CoreAuthAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        // Veritabanı ve appsettings.json dosyasına erişim köprülerini kuruyoruz
        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(UserDto request)
        {
            // Kullanıcı adı daha önce alınmış mı kontrolü
            if (_context.Users.Any(u => u.Username == request.Username))
                return BadRequest("Bu kullanıcı adı zaten mevcut.");

            var user = new User
            {
                Username = request.Username,
                // Şifreyi açıkça değil, BCrypt ile kriptolayarak kaydediyoruz
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "Customer" // Varsayılan rol
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("Kullanıcı başarıyla kaydedildi.");
        }

        [HttpPost("login")]
        public IActionResult Login(UserDto request)
        {
            // Veritabanından kullanıcıyı bul
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
            if (user == null)
                return BadRequest("Kullanıcı bulunamadı.");

            // Girilen şifre ile veritabanındaki kriptolu şifre eşleşiyor mu kontrolü
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return BadRequest("Hatalı şifre.");

            // Şifre doğruysa JWT Token üret ve gönder
            string token = CreateToken(user);
            return Ok(new { Token = token });
        }

        // JWT Token Üretme Motoru
        private string CreateToken(User user)
        {
            // Token'ın içine kullanıcının adını ve rolünü (kimlik kartı bilgileri) gömüyoruz
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // appsettings.json'daki gizli anahtarımızı alıyoruz
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("Jwt:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("Jwt:Issuer").Value,
                audience: _configuration.GetSection("Jwt:Audience").Value,
                claims: claims,
                expires: DateTime.Now.AddDays(1), // Token 1 gün geçerli olsun
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}