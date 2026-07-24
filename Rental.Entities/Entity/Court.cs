using System.ComponentModel.DataAnnotations.Schema; // Bunu en üste ekle

namespace Rental.Entities.Entity
{
    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        // Kuruş hassasiyeti eklendi (Virgülden sonra 2 hane)
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyPrice { get; set; }

        public bool IsActive { get; set; } = true;
        public int OwnerId { get; set; }
    }
}