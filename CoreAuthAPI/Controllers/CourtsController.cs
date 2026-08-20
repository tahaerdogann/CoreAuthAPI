using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using System;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class CourtsController : ControllerBase
    {
        private readonly ICourtService _courtService;

        public CourtsController(ICourtService courtService)
        {
            _courtService = courtService;
        }

        private (Guid? userId, string userRole) GetUserContext()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (Guid.TryParse(userIdStr, out Guid userId))
            {
                return (userId, userRole ?? string.Empty);
            }
            return (null, userRole ?? string.Empty);
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult AddCourt([FromBody] AddCourtDto request)
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.AddCourt(request, userId.Value);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("my-courts")]
        public IActionResult GetMyCourts()
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.GetMyCourts(userId.Value);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult Search([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] string? sportTypes, [FromQuery] double? distance, [FromQuery] string? startDate, [FromQuery] string? endDate, [FromQuery] string? startTime, [FromQuery] string? endTime, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortBy = null)
        {
            var result = _courtService.Search(lat, lng, sportTypes, distance, startDate, endDate, startTime, endTime, minPrice, maxPrice, page, pageSize, sortBy);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public IActionResult GetCourtById(Guid id)
        {
            var (userId, _) = GetUserContext();
            var result = _courtService.GetCourtById(id, userId);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(new { message = result.ErrorMessage });
                return BadRequest(new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }

        [HttpGet("{slug}")]
        [AllowAnonymous]
        public IActionResult GetCourtBySlug(string slug)
        {
            var (userId, _) = GetUserContext();
            var result = _courtService.GetCourtBySlug(slug, userId);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(new { message = result.ErrorMessage });
                return BadRequest(new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }

        [HttpGet("slots/{courtId:guid}")]
        [AllowAnonymous]
        public IActionResult GetCourtSlots(Guid courtId)
        {
            var result = _courtService.GetCourtSlots(courtId);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("generate-schedule")]
        public IActionResult GenerateSchedule([FromBody] AddScheduleDto request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized(new { message = "Geçersiz kullanıcı kimliği." });

            var result = _courtService.GenerateSchedule(request, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(new { message = result.ErrorMessage });
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok!") return Unauthorized(new { message = result.ErrorMessage });
                return BadRequest(new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }

        [HttpPost("toggle-slot/{slotId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleSlot(Guid slotId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.ToggleSlot(slotId, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPost("{courtId:guid}/cancel-schedule")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CancelSchedule(Guid courtId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.CancelSchedule(courtId, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPost("{courtId:guid}/toggle-auto-schedule")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleAutoSchedule(Guid courtId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.ToggleAutoSchedule(courtId, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPost("{courtId:guid}/toggle-publish")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult TogglePublish(Guid courtId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.TogglePublish(courtId, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPost("{courtId:guid}/toggle-auto-approve")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleAutoApprove(Guid courtId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.ToggleAutoApprove(courtId, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult DeleteCourt(Guid id)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized(new { message = "Geçersiz kullanıcı kimliği." });

            var result = _courtService.DeleteCourt(id, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(new { message = result.ErrorMessage });
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(new { message = result.ErrorMessage });
                return BadRequest(new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult UpdateCourt(Guid id, [FromBody] AddCourtDto request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.UpdateCourt(id, request, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }

        [HttpDelete("{courtId:guid}/photos/{photoId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult DeleteCourtPhoto(Guid courtId, Guid photoId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Geçersiz kullanıcı kimliği.");

            var result = _courtService.DeleteCourtPhoto(courtId, photoId, userId.Value, userRole);
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Saha bulunamadı." || result.ErrorMessage == "Fotoğraf bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahada işlem yapma yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }
            return Ok(result.Data);
        }

        [HttpGet("get-upload-signature")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult GetUploadSignature()
        {
            var result = _courtService.GetUploadSignature();
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
        }
    }
}