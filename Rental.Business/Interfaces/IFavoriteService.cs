using System;
using Rental.Business.Dtos;

namespace Rental.Business.Interfaces
{
    public interface IFavoriteService
    {
        ServiceResult ToggleFavorite(Guid courtId, Guid userId);
        ServiceResult GetMyFavorites(Guid userId);
        ServiceResult CheckFavorite(Guid courtId, Guid? userId);
    }
}
