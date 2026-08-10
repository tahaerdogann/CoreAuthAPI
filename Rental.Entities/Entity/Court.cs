using Rental.Entities.Enum;
namespace Rental.Entities.Entity
{
    public class Court
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Neighborhood { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string SportType { get; set; } = string.Empty;
        public string SurfaceType { get; set; } = string.Empty;

        public FacilityAmenities Amenities { get; set; } = FacilityAmenities.None;
        public string RentalOptionsJson { get; set; } = string.Empty;
        
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPublished { get; set; } = false; // Müşterilere görünüp görünmeme durumu
        public Guid OwnerId { get; set; }

        public ICollection<CourtPhoto> Photos { get; set; } = new List<CourtPhoto>();
    }
}