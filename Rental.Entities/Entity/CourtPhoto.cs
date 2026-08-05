using System.ComponentModel.DataAnnotations.Schema;

namespace Rental.Entities.Entity
{
    public class CourtPhoto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CourtId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty; // Cloudinary'deki silme işlemi için gerekli ID
        public bool IsCover { get; set; } = false;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourtId))]
        public Court? Court { get; set; }
    }
}
