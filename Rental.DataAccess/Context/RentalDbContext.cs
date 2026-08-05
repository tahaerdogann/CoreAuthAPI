using Microsoft.EntityFrameworkCore;
using Rental.Entities.Entity;

namespace Rental.DataAccess.Context
{
    public class RentalDbContext : DbContext
    {
        public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        // YENİ EKLENEN: Sahalar tablomuz
        public DbSet<Court> Courts { get; set; }
        public DbSet<CourtSlot> CourtSlots { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CourtSchedule> CourtSchedules { get; set; }
        public DbSet<UserFavoriteCourt> UserFavoriteCourts { get; set; }
        public DbSet<CourtPhoto> CourtPhotos { get; set; }
    }
}