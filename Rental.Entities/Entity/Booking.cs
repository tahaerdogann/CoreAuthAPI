using Rental.Entities.Base;

namespace Rental.Entities.Entity
{
    public class Booking : BaseEntity
    {
        public int CourtSlotId { get; set; }
        public CourtSlot CourtSlot { get; set; } // Hangi saat dilimi kiralandı?

        public int CustomerId { get; set; }
        public User Customer { get; set; } // Kiralayan müşteri kim?

        public bool IsCancelled { get; set; } = false; // Owner iptal ederse burası True olacak
    }
}