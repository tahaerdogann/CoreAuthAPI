using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rental.Business.Interfaces;

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
                
                if (now.Hour == 0 && now.Minute == 1) 
                {
                    _logger.LogInformation("AutoScheduleWorker: Gece uzatma işlemi başlatılıyor...");
                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobsService = scope.ServiceProvider.GetRequiredService<IBackgroundJobsService>();
                        await jobsService.ExtendSchedulesAsync();
                    }
                }

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobsService = scope.ServiceProvider.GetRequiredService<IBackgroundJobsService>();
                        await jobsService.UpdateBookingStatusesAsync();
                    }
                    
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AutoScheduleWorker çalışırken beklenmeyen bir hata oluştu.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}
