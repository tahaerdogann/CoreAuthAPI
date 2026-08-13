using System;
using Rental.Entities.Enum;

namespace Rental.Business.Dtos
{
    public class UpdateBookingStatusRequest
    {
        public BookingStatus Status { get; set; }
    }

    public class CreateBookingRequest
    {
        public Guid SlotId { get; set; }
    }

    public class CreateExternalBookingRequest
    {
        public Guid SlotId { get; set; }
        public string? ExternalCustomerName { get; set; }
        public string? ExternalCustomerPhone { get; set; }
    }

    public class SetMaintenanceRequest
    {
        public bool IsMaintenance { get; set; }
        public string? Note { get; set; }
    }
}
