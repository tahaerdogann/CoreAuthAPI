using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rental.Entities.Entity
{
    public class CourtSchedule
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Court")]
        public int CourtId { get; set; }
        // public virtual Court Court { get; set; } // Court sınıfınla bağlantı kurması için (Navigation Property)

        // 📅 1. KURAL: Planın Geçerlilik Takvimi
        public DateTime StartDate { get; set; } // Örn: 21 Temmuz
        public DateTime EndDate { get; set; }   // Örn: 21 Ekim

        // ⏱️ 2. KURAL: Seans ve Tampon Süreleri
        public int SessionDurationMinutes { get; set; } // Maç süresi (Örn: 60 dakika)
        public int BufferDurationMinutes { get; set; }  // Temizlik/Hazırlık boşluğu (Örn: 15 dakika)

        // 🕒 3. KURAL: Günlük Mesai Saatleri (İlk aşama için her güne standart yapıyoruz)
        public TimeSpan OpenTime { get; set; }  // Açılış (Örn: 10:00)
        public TimeSpan CloseTime { get; set; } // Kapanış (Örn: 23:59)

        // 💰 4. KURAL: Dinamik Fiyatlandırma ve Prime-Time
        public decimal BasePrice { get; set; }      // Gündüz (Ölü saat) fiyatı (Örn: 500 ₺)
        public decimal PrimeTimePrice { get; set; } // Akşam (Altın saat) fiyatı (Örn: 1000 ₺)

        public TimeSpan PrimeTimeStart { get; set; } // Prime-Time Başlangıcı (Örn: 18:00)
        public TimeSpan PrimeTimeEnd { get; set; }   // Prime-Time Bitişi (Örn: 23:59)

        // 🔒 Sistem Kayıt Bilgileri
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public int RecordUserCode { get; set; }
    }
}