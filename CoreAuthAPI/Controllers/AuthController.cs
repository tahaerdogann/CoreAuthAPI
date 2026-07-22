using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Enum;
using CoreAuthAPI.Dtos;
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
        private readonly RentalDbContext _context;
        private readonly IConfiguration _configuration;

        // yeni katmandaki RentalDbContext'i kullanıyoruz
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

            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                // diğer alanları şimdilik varsayılan dolduruyoruz
                Name = "Stajyer",
                Surname = "Kral",
                PhoneNumber = "5550000000",
                Type = UserRoles.Customer,
                Status = UserStatus.Active,
                RecordUserCode = 1
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("Kullanıcı başarıyla kaydedildi.");
        }

        [HttpPost("login")]
        public IActionResult Login(UserDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
                return BadRequest("Kullanıcı bulunamadı.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return BadRequest("Hatalı şifre.");

            string token = CreateToken(user);
            return Ok(new { Token = token });
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Type.ToString()) // Enum'u string olarak token'a gömüyoruz
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
    }
}