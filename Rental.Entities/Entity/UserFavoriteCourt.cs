using System;

namespace Rental.Entities.Entity
{
    public class UserFavoriteCourt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid CourtId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation property for Court, User could be added if needed, but not strictly necessary for simple FK operations
        public virtual Court? Court { get; set; }
    }
}
