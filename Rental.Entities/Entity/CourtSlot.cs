using System;
using System.ComponentModel.DataAnnotations.Schema;
using Rental.Entities.Enum;

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

        public SlotStatus Status { get; set; } = SlotStatus.Available; // Slot durumu (Boş, Kiralandı, Bakımda)
        public string? MaintenanceNote { get; set; } // Bakım notu
        public Guid? RenterId { get; set; } // Kim kiraladı? (Henüz boşsa null)
    }
}