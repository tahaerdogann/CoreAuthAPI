using Rental.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema; // Bunu en üste ekle

namespace Rental.Entities.Entity
{
    public class CourtSlot : BaseEntity
    {
        public int CourtId { get; set; }
        public Court Court { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Kuruş hassasiyeti eklendi
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsBooked { get; set; } = false;
    }
}