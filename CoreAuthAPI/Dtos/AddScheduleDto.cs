using System;
using System.ComponentModel.DataAnnotations;

namespace Rental.Entities.Dtos
{
    public class AddScheduleDto
    {
        [Required]
        public int CourtId { get; set; }

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
    }
}