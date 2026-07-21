namespace Rental.Entities.Base
{
    public abstract class BaseEntity
    {
        // Her tabloda mutlaka bir Id (Kimlik) olmalı
        public int Id { get; set; }

        // standart izleme alanları
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public int RecordUserCode { get; set; }
    }
}