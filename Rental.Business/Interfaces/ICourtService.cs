using System;
using Rental.Business.Dtos;

namespace Rental.Business.Interfaces
{
    public interface ICourtService
    {
        ServiceResult AddCourt(AddCourtDto request, Guid userId);
        ServiceResult GetMyCourts(Guid userId);
        ServiceResult Search(double? lat, double? lng, string? sportTypes, double? distance, string? startDate, string? endDate, string? startTime, string? endTime, decimal? minPrice, decimal? maxPrice, int page, int pageSize, string? sortBy);
        ServiceResult GetCourtById(Guid courtId, Guid? userId);
        ServiceResult GetCourtSlots(Guid courtId);
        ServiceResult GenerateSchedule(AddScheduleDto request, Guid userId, string userRole);
        ServiceResult ToggleSlot(Guid slotId, Guid userId, string userRole);
        ServiceResult CancelSchedule(Guid courtId, Guid userId, string userRole);
        ServiceResult ToggleAutoSchedule(Guid courtId, Guid userId, string userRole);
        ServiceResult TogglePublish(Guid courtId, Guid userId, string userRole);
        ServiceResult ToggleAutoApprove(Guid courtId, Guid userId, string userRole);
        ServiceResult DeleteCourt(Guid id, Guid userId, string userRole);
        ServiceResult UpdateCourt(Guid id, AddCourtDto request, Guid userId, string userRole);
        ServiceResult DeleteCourtPhoto(Guid courtId, Guid photoId, Guid userId, string userRole);
        ServiceResult GetUploadSignature();
    }
}
