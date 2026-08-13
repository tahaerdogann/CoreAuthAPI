using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rental.Business.Dtos
{
    public class RegisterDto : IValidatableObject
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        public string Surname { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }

        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        public string Password { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(PhoneNumber))
            {
                yield return new ValidationResult("E-posta veya telefon numarası zorunludur.", new[] { nameof(Email), nameof(PhoneNumber) });
            }
        }
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "E-posta veya telefon numarası boş olamaz.")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre boş olamaz.")]
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        public string Surname { get; set; } = string.Empty;

        [RegularExpression(@"^05\d{9}$", ErrorMessage = "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string? Email { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}