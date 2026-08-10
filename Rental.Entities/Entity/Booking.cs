using Rental.Entities.Base;

namespace Rental.Entities.Entity
{
    public class Booking : BaseEntity
    {
        public Guid CourtSlotId { get; set; }
        public CourtSlot CourtSlot { get; set; } = null!; // Hangi saat dilimi kiralandı?

        public Guid CustomerId { get; set; }
        public User Customer { get; set; } = null!; // Kiralayan müşteri kim?

        public Rental.Entities.Enum.BookingStatus Status { get; set; } = Rental.Entities.Enum.BookingStatus.Pending; // Rezervasyon durumu
    }
}