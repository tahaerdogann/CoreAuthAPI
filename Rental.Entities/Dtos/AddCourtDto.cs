using Rental.Entities.Enum;

namespace Rental.Entities.Dtos
{
    public class AddCourtDto
    {
        public string Name { get; set; } = string.Empty;
        public string SportType { get; set; } = string.Empty;
        public string SurfaceType { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Neighborhood { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public decimal HourlyPrice { get; set; }
        public FacilityAmenities Amenities { get; set; }
        public string RentalOptionsJson { get; set; } = string.Empty;
    }
}
