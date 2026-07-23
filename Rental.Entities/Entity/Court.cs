namespace Rental.Entities.Entity
{
    public class Court
    {
        public int Id { get; set; }

        // Sahanın Adı (Örn: "Yıldızlar Halısaha", "Kapalı Basketbol Kortu")
        public string Name { get; set; }

        // Saha Tipi (Örn: "Halısaha", "Basketbol", "Tenis")
        public string Type { get; set; }

        // Saatlik Kiralama Ücreti
        public decimal HourlyPrice { get; set; }

        // Saha şu an kiralamaya açık mı? (Bakımda vs. olabilir)
        public bool IsActive { get; set; } = true;
    }
}