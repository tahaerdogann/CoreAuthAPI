using Rental.Entities.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rental.Entities.Entity
{
    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string City { get; set; }
        public string District { get; set; }
        public string Neighborhood { get; set; }
        public string AddressDetail { get; set; }

        public string SportType { get; set; }
        public string SurfaceType { get; set; }

        public FacilityAmenities Amenities { get; set; } = FacilityAmenities.None;
        public string RentalOptionsJson { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyPrice { get; set; }

        public bool IsActive { get; set; } = true;
        public int OwnerId { get; set; }
    }
}