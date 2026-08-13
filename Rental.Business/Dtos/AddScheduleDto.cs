using System;
using System.ComponentModel.DataAnnotations;

namespace Rental.Business.Dtos
{
    public class AddScheduleDto
    {
        [Required]
        public Guid CourtId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(15, 240, ErrorMessage = "Seans süresi 15 ile 240 dakika arasında olmalıdır.")]
        public int SessionDurationMinutes { get; set; }

        [Range(0, 120, ErrorMessage = "Tampon süre 0 ile 120 dakika arasında olmalıdır.")]
        public int BufferDurationMinutes { get; set; }

        [Required]
        public TimeSpan OpenTime { get; set; }

        [Required]
        public TimeSpan CloseTime { get; set; }

        [Range(1, 100000, ErrorMessage = "Geçerli bir taban fiyat giriniz.")]
        public decimal BasePrice { get; set; }

        [Range(1, 100000, ErrorMessage = "Geçerli bir prime-time fiyatı giriniz.")]
        public decimal PrimeTimePrice { get; set; }

        public TimeSpan PrimeTimeStart { get; set; }
        public TimeSpan PrimeTimeEnd { get; set; }

        // Gelişmiş Ayarlar
        public List<DayOfWeekConfig>? DaysConfig { get; set; }

        public bool IsAutoScheduleEnabled { get; set; } = false;
    }

    public class DayOfWeekConfig
    {
        public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday ... 6 = Saturday
        public bool IsActive { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
    }
}