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
using Microsoft.AspNetCore.Authorization;

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
        public IActionResult Register(RegisterDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Email) && _context.Users.Any(u => u.Email == request.Email))
                return BadRequest(new { title = "Bu e-posta adresi zaten mevcut." });

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && _context.Users.Any(u => u.PhoneNumber == request.PhoneNumber))
                return BadRequest(new { title = "Bu telefon numarası zaten mevcut." });

            // 1. Gelen şifreyi güvenli bir şekilde kriptoluyoruz (Hash)
            string hashedPassword = CreatePasswordHash(request.Password);

            // Dinamik DTO verilerini kullanıyoruz
            var user = new User
            {
                Name = request.Name ?? "",
                Surname = request.Surname ?? "",
                PhoneNumber = request.PhoneNumber ?? "", // null gelirse boş string at
                Email = request.Email,
                PasswordHash = hashedPassword,
                Type = UserRoles.Customer,
                Status = UserStatus.Active,
                RecordUserCode = Guid.Empty
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // DÜZ METİN YERİNE TOKEN DÖNÜYORUZ (Otomatik Login için)
            string token = CreateToken(user);
            return Ok(new { token = token });


        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto request)
        {
            // 1. Kullanıcı (e-posta veya telefon ile) sistemde var mı?
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Identifier || u.PhoneNumber == request.Identifier);
            if (user == null)
                return BadRequest("E-posta/telefon veya şifre hatalı.");

            // 2. GİRİŞ GÜVENLİĞİ: Adamın girdiği şifreyle veritabanındaki kriptolu şifre eşleşiyor mu?
            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
                return BadRequest("E-posta/telefon veya şifre hatalı.");

            // 3. Her şey doğruysa Token üret
            string token = CreateToken(user);
            return Ok(new { Token = token });
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? ""),
                new Claim(ClaimTypes.Role, user.Type.ToString()),
                new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}")
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

        // --- GÜVENLİ KİMLİK DOĞRULAMA (ME) ---
        [Authorize] // Sadece geçerli ve imzası doğru token'a sahip olanlar girebilir
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            // [Authorize] sayesinde User nesnesi kesinlikle güvenilirdir.
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var phoneNumber = User.FindFirst(ClaimTypes.MobilePhone)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;

            int role = 2; // Default Customer
            if (Enum.TryParse(roleStr, true, out UserRoles parsedRole))
            {
                role = (int)parsedRole;
            }

            string roleText = role switch
            {
                1 => "Admin",
                3 => "İşletmeci",
                _ => "Müşteri"
            };

            return Ok(new
            {
                email = email,
                phoneNumber = phoneNumber,
                name = name,
                role = role.ToString(),
                roleText = roleText
            });
        }

        [HttpPut("update-profile")]
        [Authorize]
        public IActionResult UpdateProfile(UpdateProfileDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Check if email is being changed and if it's already in use
            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
            {
                var emailExists = _context.Users.Any(u => u.Email == request.Email && u.Id != userId);
                if (emailExists)
                {
                    return BadRequest(new { title = "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor." });
                }
                user.Email = request.Email;
            }

            // Check if phone number is being changed and if it's already in use
            if (!string.IsNullOrEmpty(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
            {
                var phoneExists = _context.Users.Any(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                if (phoneExists)
                {
                    return BadRequest(new { title = "Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor." });
                }
            }

            user.Name = request.Name;
            user.Surname = request.Surname;
            user.PhoneNumber = request.PhoneNumber;

            _context.SaveChanges();
            
            // Profil değiştiği için yeni bilgileri barındıran güncel bir token üretiyoruz
            string newToken = CreateToken(user);
            
            return Ok(new { message = "Profil başarıyla güncellendi.", token = newToken });
        }

        [HttpPut("change-password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            if (!VerifyPasswordHash(request.CurrentPassword, user.PasswordHash))
                return BadRequest("Mevcut şifreniz hatalı.");

            user.PasswordHash = CreatePasswordHash(request.NewPassword);
            _context.SaveChanges();

            return Ok(new { message = "Şifreniz başarıyla güncellendi." });
        }

        [HttpGet("check-identifier")]
        public IActionResult CheckIdentifier([FromQuery] string identifier)
        {
            bool exists = _context.Users.Any(u => u.Email == identifier || u.PhoneNumber == identifier);
            return Ok(new { exists });
        }
    }
}