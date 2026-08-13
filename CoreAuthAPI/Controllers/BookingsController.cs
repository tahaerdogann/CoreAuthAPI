using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Kiralama için giriş yapmak zorunlu
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
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

        [HttpPost("create")]
        public IActionResult CreateBooking([FromBody] CreateBookingRequest request)
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.CreateBooking(request, userId.Value);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "İlgili seans bulunamadı.") return NotFound(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPost("external-booking")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CreateExternalBooking([FromBody] CreateExternalBookingRequest request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.CreateExternalBooking(request, userId.Value, userRole);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "İlgili seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahanın sahibi değilsiniz.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPut("courtslots/{slotId:guid}/maintenance")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult SetMaintenance(Guid slotId, [FromBody] SetMaintenanceRequest request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.SetMaintenance(slotId, request, userId.Value, userRole);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "İlgili seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahanın sahibi değilsiniz.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpGet("my-bookings")]
        public IActionResult GetMyBookings()
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.GetMyBookings(userId.Value);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("cancel/{bookingId:guid}")]
        public IActionResult CancelBooking(Guid bookingId)
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.CancelBooking(bookingId, userId.Value);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Kiralama kaydı bulunamadı.") return NotFound(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpGet("owner-booked-slots")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult GetOwnerBookedSlots([FromQuery] Guid? courtId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.GetOwnerBookedSlots(courtId, userId.Value, userRole);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("update-status/{bookingId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult UpdateBookingStatus(Guid bookingId, [FromBody] UpdateBookingStatusRequest request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.UpdateBookingStatus(bookingId, request, userId.Value, userRole);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Kiralama kaydı bulunamadı." || result.ErrorMessage == "Seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu işlem için yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }
    }
}
