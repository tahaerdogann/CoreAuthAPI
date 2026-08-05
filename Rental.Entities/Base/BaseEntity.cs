namespace Rental.Entities.Base
{
    public abstract class BaseEntity
    {
        // Her tabloda mutlaka bir Id (Kimlik) olmalı
        public Guid Id { get; set; } = Guid.NewGuid();

        // standart izleme alanları
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public Guid RecordUserCode { get; set; }
    }
}