using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Enum;

namespace Rental.Business.Services
{
    public class BookingManager : IBookingService
    {
        private readonly RentalDbContext _context;

        public BookingManager(RentalDbContext context)
        {
            _context = context;
        }

        public ServiceResult CreateBooking(CreateBookingRequest request, Guid userId)
        {
            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == request.SlotId);
            if (slot == null)
                return ServiceResult.Failure("İlgili seans bulunamadı.");

            if (slot.Status != SlotStatus.Available)
                return ServiceResult.Failure("Bu seans zaten kiralanmış veya bakıma alınmış.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            if (court == null || !court.IsPublished)
                return ServiceResult.Failure("Bu saha şu anda yayında olmadığı için kiralama yapılamaz.");

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

            return ServiceResult.Success(new { message = "Kiralama işlemi başarıyla gerçekleştirildi!", bookingId = booking.Id });
        }

        public ServiceResult CreateExternalBooking(CreateExternalBookingRequest request, Guid userId, string userRole)
        {
            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == request.SlotId);
            if (slot == null) return ServiceResult.Failure("İlgili seans bulunamadı.");
            if (slot.Status != SlotStatus.Available) return ServiceResult.Failure("Bu seans zaten kiralanmış veya bakıma alınmış.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            if (userRole != "Admin" && court?.OwnerId != userId)
                return ServiceResult.Failure("Bu sahanın sahibi değilsiniz.");

            var booking = new Booking
            {
                CourtSlotId = slot.Id,
                CustomerId = null,
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

            return ServiceResult.Success(new { message = "Dışarıdan kiralama işlemi başarıyla eklendi!", bookingId = booking.Id });
        }

        public ServiceResult SetMaintenance(Guid slotId, SetMaintenanceRequest request, Guid userId, string userRole)
        {
            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null) return ServiceResult.Failure("İlgili seans bulunamadı.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            if (userRole != "Admin" && court?.OwnerId != userId)
                return ServiceResult.Failure("Bu sahanın sahibi değilsiniz.");

            if (slot.Status == SlotStatus.Booked)
                return ServiceResult.Failure("Kiralı olan bir seansı doğrudan bakıma alamazsınız. Önce kiralamayı iptal edin.");

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
            return ServiceResult.Success(new { message = request.IsMaintenance ? "Seans bakıma alındı." : "Seans bakımdan çıkarıldı, kiralanabilir." });
        }

        public ServiceResult GetMyBookings(Guid userId)
        {
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

            return ServiceResult.Success(bookings);
        }

        public ServiceResult CancelBooking(Guid bookingId, Guid userId)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId && b.CustomerId == userId);
            if (booking == null) return ServiceResult.Failure("Kiralama kaydı bulunamadı.");
            if (booking.Status == BookingStatus.Cancelled) return ServiceResult.Failure("Bu kayıt zaten iptal edilmiş.");
            if (booking.Status == BookingStatus.Completed) return ServiceResult.Failure("Tamamlanmış seanslar iptal edilemez.");

            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == booking.CourtSlotId);
            if (slot != null)
            {
                slot.Status = SlotStatus.Available;
                slot.RenterId = null;
            }
            
            booking.Status = BookingStatus.Cancelled;
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Seans başarıyla iptal edildi." });
        }

        public ServiceResult GetOwnerBookedSlots(Guid? courtId, Guid userId, string userRole)
        {
            var query = from s in _context.CourtSlots
                        join c in _context.Courts on s.CourtId equals c.Id
                        where s.Status != SlotStatus.Available
                        select new { s, c };

            if (userRole != "Admin")
            {
                query = query.Where(x => x.c.OwnerId == userId);
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

            var now = DateTime.Now;
            slots = slots.Where(s => !(s.Status == 0 && s.StartTime < now)).ToList();

            return ServiceResult.Success(slots);
        }

        public ServiceResult UpdateBookingStatus(Guid bookingId, UpdateBookingStatusRequest request, Guid userId, string userRole)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking == null) return ServiceResult.Failure("Kiralama kaydı bulunamadı.");

            var slotInfo = (from s in _context.CourtSlots
                            join c in _context.Courts on s.CourtId equals c.Id
                            where s.Id == booking.CourtSlotId
                            select new { s, c }).FirstOrDefault();
            
            if (slotInfo == null) return ServiceResult.Failure("Seans bulunamadı.");

            if (userRole != "Admin" && slotInfo.c.OwnerId != userId)
                return ServiceResult.Failure("Bu işlem için yetkiniz yok.");

            if (booking.Status == BookingStatus.Completed)
                return ServiceResult.Failure("Tamamlanmış seanslar üzerinde işlem yapılamaz.");

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
            return ServiceResult.Success(new { message = "Rezervasyon durumu güncellendi." });
        }
    }
}
