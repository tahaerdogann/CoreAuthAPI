using Rental.Entities.Base;
using Rental.Entities.Enum;

namespace Rental.Entities.Entity
{
    // User sınıfı BaseEntity'den miras alır (Id, RecordDate, RecordUserCode otomatik gelir)
    public class User : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // Enum'larımızı kullanıyoruz
        public UserRoles Type { get; set; } = UserRoles.Customer;
        public UserStatus Status { get; set; } = UserStatus.Active;
    }
}