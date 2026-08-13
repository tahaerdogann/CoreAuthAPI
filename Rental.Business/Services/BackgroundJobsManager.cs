using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;

namespace Rental.Business.Services
{
    public class BackgroundJobsManager : IBackgroundJobsService
    {
        private readonly RentalDbContext _context;
        private readonly ILogger<BackgroundJobsManager> _logger;

        public BackgroundJobsManager(RentalDbContext context, ILogger<BackgroundJobsManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExtendSchedulesAsync()
        {
            var activeSchedules = await _context.CourtSchedules.Where(s => s.IsAutoScheduleEnabled).ToListAsync();
            
            foreach (var schedule in activeSchedules)
            {
                try
                {
                    var lastSlot = await _context.CourtSlots
                        .Where(s => s.CourtId == schedule.CourtId)
                        .OrderByDescending(s => s.EndTime)
                        .FirstOrDefaultAsync();

                    DateTime targetDate;
                    if (lastSlot != null && lastSlot.EndTime.Date >= DateTime.Now.Date)
                    {
                        targetDate = lastSlot.EndTime.Date.AddDays(1);
                    }
                    else
                    {
                        targetDate = DateTime.Now.Date;
                    }

                    GenerateSlotsForDate(schedule, targetDate);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"BackgroundJobsManager: Saha ID {schedule.CourtId} uzatılırken hata oluştu.");
                }
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBookingStatusesAsync()
        {
            var now = DateTime.Now;

            var bookingsToUpdate = await _context.Bookings
                .Where(b => (b.Status == Rental.Entities.Enum.BookingStatus.Pending || b.Status == Rental.Entities.Enum.BookingStatus.Approved))
                .Join(_context.CourtSlots, b => b.CourtSlotId, s => s.Id, (b, s) => new { Booking = b, Slot = s })
                .Where(bs => bs.Slot.StartTime <= now)
                .ToListAsync();

            foreach (var item in bookingsToUpdate)
            {
                if (item.Booking.Status == Rental.Entities.Enum.BookingStatus.Pending)
                {
                    item.Booking.Status = Rental.Entities.Enum.BookingStatus.Cancelled;
                    item.Slot.Status = Rental.Entities.Enum.SlotStatus.Available;
                    item.Slot.RenterId = null;
                }
                else if (item.Booking.Status == Rental.Entities.Enum.BookingStatus.Approved)
                {
                    item.Booking.Status = Rental.Entities.Enum.BookingStatus.Completed;
                }
            }

            if (bookingsToUpdate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private void GenerateSlotsForDate(CourtSchedule schedule, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            List<DayOfWeekConfig>? daysConfig = null;
            
            if (!string.IsNullOrEmpty(schedule.AdvancedRulesJson))
            {
                try { daysConfig = JsonSerializer.Deserialize<List<DayOfWeekConfig>>(schedule.AdvancedRulesJson); }
                catch { }
            }

            var dayConfig = daysConfig?.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
            if (dayConfig != null && !dayConfig.IsActive)
                return;

            var currentOpenTime = dayConfig != null ? dayConfig.OpenTime : schedule.OpenTime;
            var currentCloseTime = dayConfig != null ? dayConfig.CloseTime : schedule.CloseTime;
            
            var currentTime = currentOpenTime;

            var existingSlots = _context.CourtSlots
                .Where(s => s.CourtId == schedule.CourtId && s.StartTime >= date && s.StartTime < date.AddDays(1))
                .ToList();

            var newSlots = new List<CourtSlot>();

            while (currentTime.Add(TimeSpan.FromMinutes(schedule.SessionDurationMinutes)) <= currentCloseTime)
            {
                var slotStartTime = date.Add(currentTime);
                var slotEndTime = slotStartTime.AddMinutes(schedule.SessionDurationMinutes);

                var hasConflict = existingSlots.Any(s => s.StartTime < slotEndTime && s.EndTime > slotStartTime);

                if (!hasConflict)
                {
                    bool isPrimeTime = currentTime >= schedule.PrimeTimeStart && currentTime < schedule.PrimeTimeEnd;
                    decimal currentPrice = isPrimeTime ? schedule.PrimeTimePrice : schedule.BasePrice;

                    newSlots.Add(new CourtSlot
                    {
                        CourtId = schedule.CourtId,
                        StartTime = slotStartTime,
                        EndTime = slotEndTime,
                        Price = currentPrice,
                        Status = Rental.Entities.Enum.SlotStatus.Available
                    });
                }

                currentTime = currentTime.Add(TimeSpan.FromMinutes(schedule.SessionDurationMinutes + schedule.BufferDurationMinutes));
            }

            if (newSlots.Any())
            {
                _context.CourtSlots.AddRange(newSlots);
            }
        }
    }
}
