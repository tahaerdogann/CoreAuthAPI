using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Kiralama için giriş yapmak zorunlu
    public class BookingsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public BookingsController(RentalDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public IActionResult CreateBooking([FromBody] CreateBookingRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized("Kullanıcı kimliği alınamadı.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == request.SlotId);
            if (slot == null)
                return NotFound("İlgili seans bulunamadı.");

            if (slot.IsBooked)
                return BadRequest("Bu seans zaten kiralanmış.");

            // Kiralama işlemini kaydet
            var booking = new Booking
            {
                CourtSlotId = slot.Id,
                CustomerId = userId,
                IsCancelled = false,
                RecordDate = DateTime.Now,
                RecordUserCode = userId
            };

            // Slotu güncelle
            slot.IsBooked = true;
            slot.RenterId = userId;

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return Ok(new { message = "Kiralama işlemi başarıyla gerçekleştirildi!", bookingId = booking.Id });
        }
    }

    public class CreateBookingRequest
    {
        public int SlotId { get; set; }
    }
}
