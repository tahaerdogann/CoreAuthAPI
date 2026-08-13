using System;
using Rental.Business.Dtos;

namespace Rental.Business.Interfaces
{
    public interface IBookingService
    {
        ServiceResult CreateBooking(CreateBookingRequest request, Guid userId);
        ServiceResult CreateExternalBooking(CreateExternalBookingRequest request, Guid userId, string userRole);
        ServiceResult SetMaintenance(Guid slotId, SetMaintenanceRequest request, Guid userId, string userRole);
        ServiceResult GetMyBookings(Guid userId);
        ServiceResult CancelBooking(Guid bookingId, Guid userId);
        ServiceResult GetOwnerBookedSlots(Guid? courtId, Guid userId, string userRole);
        ServiceResult UpdateBookingStatus(Guid bookingId, UpdateBookingStatusRequest request, Guid userId, string userRole);
    }
}
