using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

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

            if (slot.Status != SlotStatus.Available)
                return BadRequest("Bu seans zaten kiralanmış veya bakıma alınmış.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            if (court == null || !court.IsPublished)
                return BadRequest("Bu saha şu anda yayında olmadığı için kiralama yapılamaz.");

            var isAutoApprove = court.IsAutoApproveEnabled;

            var booking = new Booking
            {
                CourtSlotId = slot.Id,
                CustomerId = userId,
                Status = isAutoApprove ? BookingStatus.Approved : BookingStatus.Pending,
                RecordDate = DateTime.Now,
                RecordUserCode = userId
            };

            slot.Status = SlotStatus.Booked;
            slot.RenterId = userId;

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return Ok(new { message = "Kiralama işlemi başarıyla gerçekleştirildi!", bookingId = booking.Id });
        }

        [HttpPost("external-booking")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CreateExternalBooking([FromBody] CreateExternalBookingRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Kullanıcı kimliği alınamadı.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == request.SlotId);
            if (slot == null) return NotFound("İlgili seans bulunamadı.");
            if (slot.Status != SlotStatus.Available) return BadRequest("Bu seans zaten kiralanmış veya bakıma alınmış.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin" && court?.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahanın sahibi değilsiniz.");

            var booking = new Booking
            {
                CourtSlotId = slot.Id,
                CustomerId = null, // Dışarıdan kiralama
                ExternalCustomerName = request.ExternalCustomerName,
                ExternalCustomerPhone = request.ExternalCustomerPhone,
                Status = BookingStatus.Approved,
                RecordDate = DateTime.Now,
                RecordUserCode = userId
            };

            slot.Status = SlotStatus.Booked;
            slot.RenterId = null;

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return Ok(new { message = "Dışarıdan kiralama işlemi başarıyla eklendi!", bookingId = booking.Id });
        }

        [HttpPut("courtslots/{slotId:guid}/maintenance")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult SetMaintenance(Guid slotId, [FromBody] SetMaintenanceRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Kullanıcı kimliği alınamadı.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null) return NotFound("İlgili seans bulunamadı.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin" && court?.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahanın sahibi değilsiniz.");

            if (slot.Status == SlotStatus.Booked)
                return BadRequest("Kiralı olan bir seansı doğrudan bakıma alamazsınız. Önce kiralamayı iptal edin.");

            if (request.IsMaintenance)
            {
                slot.Status = SlotStatus.Maintenance;
                slot.MaintenanceNote = request.Note;
            }
            else
            {
                slot.Status = SlotStatus.Available;
                slot.MaintenanceNote = null;
            }

            _context.SaveChanges();
            return Ok(new { message = request.IsMaintenance ? "Seans bakıma alındı." : "Seans bakımdan çıkarıldı, kiralanabilir." });
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
            if (booking.Status == BookingStatus.Cancelled) return BadRequest("Bu kayıt zaten iptal edilmiş.");
            if (booking.Status == BookingStatus.Completed) return BadRequest("Tamamlanmış seanslar iptal edilemez.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == booking.CourtSlotId);
            if (slot != null)
            {
                slot.Status = SlotStatus.Available;
                slot.RenterId = null;
            }
            
            booking.Status = BookingStatus.Cancelled;
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
                        where s.Status != SlotStatus.Available
                        select new { s, c };

            if (userRole != "Admin")
            {
                query = query.Where(x => x.c.OwnerId.ToString() == userIdStr);
            }

            if (courtId.HasValue)
            {
                query = query.Where(x => x.c.Id == courtId.Value);
            }

            var slotsInfo = query.OrderByDescending(x => x.s.StartTime).ToList();

            var slots = slotsInfo
                .Select(x => {
                    var booking = _context.Bookings.FirstOrDefault(b => b.CourtSlotId == x.s.Id);
                    var customerName = booking?.ExternalCustomerName ?? _context.Users.Where(u => u.Id == x.s.RenterId).Select(u => u.Name + " " + u.Surname).FirstOrDefault();
                    var customerPhone = booking?.ExternalCustomerPhone ?? _context.Users.Where(u => u.Id == x.s.RenterId).Select(u => u.PhoneNumber).FirstOrDefault();

                    return new
                    {
                        SlotId = x.s.Id,
                        CourtId = x.s.CourtId,
                        CourtName = x.c.Name,
                        StartTime = x.s.StartTime,
                        EndTime = x.s.EndTime,
                        Price = x.s.Price,
                        SlotStatus = x.s.Status.ToString(),
                        MaintenanceNote = x.s.MaintenanceNote,
                        IsManualClose = x.s.Status == SlotStatus.Maintenance || (x.s.Status == SlotStatus.Booked && x.s.RenterId == null),
                        BookingId = booking?.Id,
                        Status = booking != null ? (int?)booking.Status : null,
                        CustomerName = customerName,
                        CustomerPhone = customerPhone
                    };
                }).ToList();

            // Geçmişte kalmış ve hala "Pending" (0) statüsünde olan rezervasyonları listeden çıkarıyoruz
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

            if (booking.Status == BookingStatus.Completed)
                return BadRequest("Tamamlanmış seanslar üzerinde işlem yapılamaz.");

            booking.Status = request.Status;

            if (request.Status == BookingStatus.Cancelled)
            {
                slotInfo.s.Status = SlotStatus.Available;
                slotInfo.s.RenterId = null;
            }
            else if (request.Status == BookingStatus.Approved)
            {
                slotInfo.s.Status = SlotStatus.Booked;
                slotInfo.s.RenterId = booking.CustomerId;
            }

            _context.SaveChanges();
            return Ok(new { message = "Rezervasyon durumu güncellendi." });
        }
    }

    public class UpdateBookingStatusRequest
    {
        public BookingStatus Status { get; set; }
    }

    public class CreateBookingRequest
    {
        public Guid SlotId { get; set; }
    }

    public class CreateExternalBookingRequest
    {
        public Guid SlotId { get; set; }
        public string? ExternalCustomerName { get; set; }
        public string? ExternalCustomerPhone { get; set; }
    }

    public class SetMaintenanceRequest
    {
        public bool IsMaintenance { get; set; }
        public string? Note { get; set; }
    }
}
