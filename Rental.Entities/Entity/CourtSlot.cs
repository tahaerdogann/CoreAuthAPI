using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rental.Entities.Entity
{
    public class CourtSlot
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourtId { get; set; } // Hangi sahaya ait olduğu
        public DateTime StartTime { get; set; } // Kiralama başlangıç saati
        public DateTime EndTime { get; set; } // Kiralama bitiş saati

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Bu saat diliminin ücreti

        public bool IsBooked { get; set; } = false; // Biri kiraladı mı? (Dolu/Boş)
        public Guid? RenterId { get; set; } // Kim kiraladı? (Henüz boşsa null)
    }
}