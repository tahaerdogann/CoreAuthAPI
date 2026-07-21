using Microsoft.EntityFrameworkCore;
using Rental.Entities.Entity;

namespace Rental.DataAccess.Context
{
    // DbContext'ten miras alarak bunun bir veritabanı köprüsü olduğunu belirtiyoruz
    public class RentalDbContext : DbContext
    {
        // Dışarıdan (API'den) gönderilecek bağlantı ayarlarını (Connection String) içeri alan kurucu metot
        public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options)
        {
        }

        // Veritabanındaki "Users" tablomuz
        public DbSet<User> Users { get; set; }
    }
}