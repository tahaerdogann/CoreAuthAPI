using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rental.DataAccess.Context;
using Rental.Entities.Dtos;
using Rental.Entities.Entity;

namespace CoreAuthAPI.Services
{
    public class AutoScheduleWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoScheduleWorker> _logger;

        public AutoScheduleWorker(IServiceProvider serviceProvider, ILogger<AutoScheduleWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                // Gece 00:00'da çalışmasını istiyoruz, ama demo için veya test için daha sık da çalışabilir.
                // Biz günde 1 kez çalışacak şekilde ayarlayalım. (Gerçekte her dakika kontrol edip 00:00 mı diye bakabiliriz)
                
                if (now.Hour == 0 && now.Minute == 1) // Gece 00:01'de çalıştır (Günde 1 kez)
                {
                    _logger.LogInformation("AutoScheduleWorker: Gece uzatma işlemi başlatılıyor...");
                    await ExtendSchedulesAsync();
                }

                try
                {
                    // Her 1 dakikada bir saati kontrol et
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Uygulama kapanırken token iptal edildiğinde buraya düşer, güvenlice çıkış yap.
                    break;
                }
            }
        }

        private async Task ExtendSchedulesAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
                
                // Otomatik uzatması açık olan takvimleri bul
                var activeSchedules = context.CourtSchedules.Where(s => s.IsAutoScheduleEnabled).ToList();
                
                foreach (var schedule in activeSchedules)
                {
                    try
                    {
                        // Sahanın şu anki en son üretilmiş slotunu bul
                        var lastSlot = context.CourtSlots
                            .Where(s => s.CourtId == schedule.CourtId)
                            .OrderByDescending(s => s.EndTime)
                            .FirstOrDefault();

                        DateTime targetDate;
                        if (lastSlot != null && lastSlot.EndTime.Date >= DateTime.Now.Date)
                        {
                            // En son slotun tarihinden bir sonraki gün için üret
                            targetDate = lastSlot.EndTime.Date.AddDays(1);
                        }
                        else
                        {
                            // Eğer hiç slot yoksa bugünden itibaren başla
                            targetDate = DateTime.Now.Date;
                        }

                        // Sadece 1 gün ileri uzatalım
                        GenerateSlotsForDate(context, schedule, targetDate);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"AutoScheduleWorker: Saha ID {schedule.CourtId} uzatılırken hata oluştu.");
                    }
                }
                
                await context.SaveChangesAsync();
            }
        }

        private void GenerateSlotsForDate(RentalDbContext context, CourtSchedule schedule, DateTime date)
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
                return; // Bu gün kapalı

            var currentOpenTime = dayConfig != null ? dayConfig.OpenTime : schedule.OpenTime;
            var currentCloseTime = dayConfig != null ? dayConfig.CloseTime : schedule.CloseTime;
            
            var currentTime = currentOpenTime;

            var existingSlots = context.CourtSlots
                .Where(s => s.CourtId == schedule.CourtId && s.StartTime >= date && s.StartTime < date.AddDays(1))
                .ToList();

            var newSlots = new List<CourtSlot>();

            while (currentTime.Add(TimeSpan.FromMinutes(schedule.SessionDurationMinutes)) <= currentCloseTime)
            {
                var slotStartTime = date.Add(currentTime);
                var slotEndTime = slotStartTime.AddMinutes(schedule.SessionDurationMinutes);

                // Çakışma kontrolü
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
                        IsBooked = false
                    });
                }

                currentTime = currentTime.Add(TimeSpan.FromMinutes(schedule.SessionDurationMinutes + schedule.BufferDurationMinutes));
            }

            if (newSlots.Any())
            {
                context.CourtSlots.AddRange(newSlots);
            }
        }
    }
}
