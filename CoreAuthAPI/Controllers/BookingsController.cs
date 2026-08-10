using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Microsoft.EntityFrameworkCore;

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
                Status = Rental.Entities.Enum.BookingStatus.Approved,
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
                    Status = bs.b.Status
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
            if (booking.Status == Rental.Entities.Enum.BookingStatus.Cancelled) return BadRequest("Bu kayıt zaten iptal edilmiş.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == booking.CourtSlotId);
            if (slot != null)
            {
                slot.IsBooked = false;
                slot.RenterId = null;
            }
            
            booking.Status = Rental.Entities.Enum.BookingStatus.Cancelled;
            _context.SaveChanges();

            return Ok(new { message = "Seans başarıyla iptal edildi." });
        }

        [HttpGet("owner-booked-slots")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult GetOwnerBookedSlots([FromQuery] Guid? courtId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = from s in _context.CourtSlots
                        join c in _context.Courts on s.CourtId equals c.Id
                        where s.IsBooked
                        select new { s, c };

            if (userRole != "Admin")
            {
                query = query.Where(x => x.c.OwnerId.ToString() == userIdStr);
            }

            if (courtId.HasValue)
            {
                query = query.Where(x => x.c.Id == courtId.Value);
            }

            var slots = query
                .OrderByDescending(x => x.s.StartTime)
                .Select(x => new
                {
                    SlotId = x.s.Id,
                    CourtId = x.s.CourtId,
                    CourtName = x.c.Name,
                    StartTime = x.s.StartTime,
                    EndTime = x.s.EndTime,
                    Price = x.s.Price,
                    IsManualClose = x.s.RenterId == null,
                    BookingId = _context.Bookings.Where(b => b.CourtSlotId == x.s.Id).Select(b => (Guid?)b.Id).FirstOrDefault(),
                    Status = _context.Bookings.Where(b => b.CourtSlotId == x.s.Id).Select(b => (int?)b.Status).FirstOrDefault(),
                    CustomerName = _context.Users.Where(u => u.Id == x.s.RenterId).Select(u => u.Name + " " + u.Surname).FirstOrDefault(),
                    CustomerPhone = _context.Users.Where(u => u.Id == x.s.RenterId).Select(u => u.PhoneNumber).FirstOrDefault()
                }).ToList();

            // Geçmişte kalmış ve hala "Pending" (0) statüsünde olan rezervasyonları listeden çıkarıyoruz (veya iptal edilmiş sayıyoruz)
            var now = DateTime.Now;
            slots = slots.Where(s => !(s.Status == 0 && s.StartTime < now)).ToList();

            return Ok(slots);
        }

        [HttpPost("update-status/{bookingId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult UpdateBookingStatus(Guid bookingId, [FromBody] UpdateBookingStatusRequest request)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return NotFound("Kiralama kaydı bulunamadı.");

            var slotInfo = (from s in _context.CourtSlots
                            join c in _context.Courts on s.CourtId equals c.Id
                            where s.Id == booking.CourtSlotId
                            select new { s, c }).FirstOrDefault();
            
            if (slotInfo == null) return NotFound("Seans bulunamadı.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin" && slotInfo.c.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu işlem için yetkiniz yok.");

            booking.Status = request.Status;

            if (request.Status == Rental.Entities.Enum.BookingStatus.Cancelled)
            {
                slotInfo.s.IsBooked = false;
                slotInfo.s.RenterId = null;
            }
            else if (request.Status == Rental.Entities.Enum.BookingStatus.Approved)
            {
                slotInfo.s.IsBooked = true;
                slotInfo.s.RenterId = booking.CustomerId;
            }

            _context.SaveChanges();
            return Ok(new { message = "Rezervasyon durumu güncellendi." });
        }
    }

    public class UpdateBookingStatusRequest
    {
        public Rental.Entities.Enum.BookingStatus Status { get; set; }
    }

    public class CreateBookingRequest
    {
        public Guid SlotId { get; set; }
    }
}
