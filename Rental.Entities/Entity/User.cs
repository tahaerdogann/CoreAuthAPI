using Rental.Entities.Base;
using Rental.Entities.Enum;

namespace Rental.Entities.Entity
{
    public class User : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // GÜVENLİK İÇİN EKLENEN SÜTUN: Şifreyi şifrelenmiş metin olarak tutacak
        public string PasswordHash { get; set; } = string.Empty;

        public UserRoles Type { get; set; } = UserRoles.Customer;
        public UserStatus Status { get; set; } = UserStatus.Active;
    }
}