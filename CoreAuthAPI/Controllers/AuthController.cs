using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto request)
        {
            var result = _authService.RegisterUser(request);
            if (result.IsSuccess) return Ok(result.Data);
            return BadRequest(new { title = result.ErrorMessage });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto request)
        {
            var result = _authService.LoginUser(request);
            if (result.IsSuccess) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [Authorize] 
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var phoneNumber = User.FindFirst(ClaimTypes.MobilePhone)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;

            int role = 2; // Default Customer
            if (Enum.TryParse(roleStr, true, out Rental.Entities.Enum.UserRoles parsedRole))
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

            var result = _authService.UpdateProfile(request, userId);
            if (result.IsSuccess) return Ok(result.Data);
            return BadRequest(new { title = result.ErrorMessage });
        }

        [HttpPut("change-password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _authService.ChangePassword(request, userId);
            if (result.IsSuccess) return Ok(result.Data);
            return BadRequest(result.ErrorMessage);
        }

        [HttpGet("check-identifier")]
        public IActionResult CheckIdentifier([FromQuery] string identifier)
        {
            bool exists = _authService.CheckIdentifierExists(identifier);
            return Ok(new { exists });
        }
    }
}