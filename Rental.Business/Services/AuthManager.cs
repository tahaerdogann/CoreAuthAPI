using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Enum;

namespace Rental.Business.Services
{
    public class AuthManager : IAuthService
    {
        private readonly RentalDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthManager(RentalDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ServiceResult RegisterUser(RegisterDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Email) && _context.Users.Any(u => u.Email == request.Email))
                return ServiceResult.Failure("Bu e-posta adresi zaten mevcut.");

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && _context.Users.Any(u => u.PhoneNumber == request.PhoneNumber))
                return ServiceResult.Failure("Bu telefon numarası zaten mevcut.");

            string hashedPassword = CreatePasswordHash(request.Password);

            var user = new User
            {
                Name = request.Name ?? "",
                Surname = request.Surname ?? "",
                PhoneNumber = request.PhoneNumber ?? "",
                Email = request.Email,
                PasswordHash = hashedPassword,
                Type = UserRoles.Customer,
                Status = UserStatus.Active,
                RecordUserCode = Guid.Empty
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            string token = CreateToken(user);
            return ServiceResult.Success(new { token = token });
        }

        public ServiceResult LoginUser(LoginDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Identifier || u.PhoneNumber == request.Identifier);
            if (user == null)
                return ServiceResult.Failure("E-posta/telefon veya şifre hatalı.");

            if (!VerifyPasswordHash(request.Password, user.PasswordHash))
                return ServiceResult.Failure("E-posta/telefon veya şifre hatalı.");

            string token = CreateToken(user);
            return ServiceResult.Success(new { token = token });
        }

        public ServiceResult UpdateProfile(UpdateProfileDto request, Guid userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return ServiceResult.Failure("Kullanıcı bulunamadı.");

            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
            {
                var emailExists = _context.Users.Any(u => u.Email == request.Email && u.Id != userId);
                if (emailExists)
                {
                    return ServiceResult.Failure("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
            {
                var phoneExists = _context.Users.Any(u => u.PhoneNumber == request.PhoneNumber && u.Id != userId);
                if (phoneExists)
                {
                    return ServiceResult.Failure("Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor.");
                }
            }

            user.Name = request.Name;
            user.Surname = request.Surname;
            user.PhoneNumber = request.PhoneNumber;

            _context.SaveChanges();
            
            string newToken = CreateToken(user);
            return ServiceResult.Success(new { message = "Profil başarıyla güncellendi.", token = newToken });
        }

        public ServiceResult ChangePassword(ChangePasswordDto request, Guid userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return ServiceResult.Failure("Kullanıcı bulunamadı.");

            if (!VerifyPasswordHash(request.CurrentPassword, user.PasswordHash))
                return ServiceResult.Failure("Mevcut şifreniz hatalı.");

            user.PasswordHash = CreatePasswordHash(request.NewPassword);
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Şifreniz başarıyla güncellendi." });
        }

        public bool CheckIdentifierExists(string identifier)
        {
            return _context.Users.Any(u => u.Email == identifier || u.PhoneNumber == identifier);
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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

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
    }
}
