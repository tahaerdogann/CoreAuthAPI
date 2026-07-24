using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Enum;
using CoreAuthAPI.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RentalDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(RentalDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public IActionResult Register(UserDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("Bu email adresi zaten mevcut.");

            // 1. Gelen şifreyi güvenli bir şekilde kriptoluyoruz (Hash)
            string hashedPassword = CreatePasswordHash(request.Password);

            // Dinamik DTO verilerini kullanıyoruz
            var user = new User
            {
                Name = request.Name,
                Surname = request.Surname,
                PhoneNumber = request.PhoneNumber ?? "", // null gelirse boş string at
                Email = request.Email ?? "",
                PasswordHash = hashedPassword,
                Type = UserRoles.Customer,
                Status = UserStatus.Active,
                RecordUserCode = 1
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // DÜZ METİN YERİNE TOKEN DÖNÜYORUZ (Otomatik Login için)
            string token = CreateToken(user);
            return Ok(new { token = token });


        }

        [HttpPost("login")]
        public IActionResult Login(UserDto request)
        {
            // 1. E-posta sistemde var mı?
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
                return BadRequest("E-posta veya şifre hatalı.");

            // 2. GİRİŞ GÜVENLİĞİ: Adamın girdiği şifreyle veritabanındaki kriptolu şifre eşleşiyor mu?
            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
                return BadRequest("E-posta veya şifre hatalı.");

            // 3. Her şey doğruysa Token üret
            string token = CreateToken(user);
            return Ok(new { Token = token });
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Type.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("Jwt:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                issuer: _configuration.GetSection("Jwt:Issuer").Value,
                audience: _configuration.GetSection("Jwt:Audience").Value,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        // --- YARDIMCI GÜVENLİK (HASH) METOTLARI ---
        private string CreatePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var computedHash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return computedHash == storedHash;
            }
        }

        [HttpGet("check-email")]
        public IActionResult CheckEmail([FromQuery] string email)
        {
            bool exists = _context.Users.Any(u => u.Email == email);
            return Ok(new { exists });
        }
    }
}