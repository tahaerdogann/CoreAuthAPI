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
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
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

        [HttpGet("my-bookings")]
        public IActionResult GetMyBookings()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Kullanıcı kimliği alınamadı.");

            var bookings = _context.Bookings
                .Where(b => b.CustomerId == userId)
                .Join(_context.CourtSlots, b => b.CourtSlotId, s => s.Id, (b, s) => new { b, s })
                .Join(_context.Courts, bs => bs.s.CourtId, c => c.Id, (bs, c) => new 
                {
                    BookingId = bs.b.Id,
                    CourtId = c.Id,
                    CourtName = c.Name,
                    StartTime = bs.s.StartTime,
                    EndTime = bs.s.EndTime,
                    Price = bs.s.Price,
                    IsCancelled = bs.b.IsCancelled
                })
                .OrderBy(x => x.StartTime)
                .ToList();

            return Ok(bookings);
        }

        [HttpPost("cancel/{bookingId:guid}")]
        public IActionResult CancelBooking(Guid bookingId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Kullanıcı kimliği alınamadı.");

            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId && b.CustomerId == userId);
            if (booking == null) return NotFound("Kiralama kaydı bulunamadı.");
            if (booking.IsCancelled) return BadRequest("Bu kayıt zaten iptal edilmiş.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == booking.CourtSlotId);
            if (slot != null)
            {
                slot.IsBooked = false;
                slot.RenterId = null;
            }
            
            booking.IsCancelled = true;
            _context.SaveChanges();

            return Ok(new { message = "Seans başarıyla iptal edildi." });
        }
    }

    public class CreateBookingRequest
    {
        public Guid SlotId { get; set; }
    }
}
