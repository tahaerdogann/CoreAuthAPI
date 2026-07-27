namespace Rental.Entities.Entity
{
    public class RentalOptions
    {
        public RentItem Krampon { get; set; } = new RentItem();
        public RentItem Ayakkabi { get; set; } = new RentItem();
        public RentItem Yelek { get; set; } = new RentItem();
        public RentItem Kaleci { get; set; } = new RentItem();
        public RentItem Hakem { get; set; } = new RentItem();
    }

    public class RentItem
    {
        public bool IsActive { get; set; } = false;
        public int AvailableCount { get; set; } = 0;
        public decimal UnitPrice { get; set; } = 0;
    }
}