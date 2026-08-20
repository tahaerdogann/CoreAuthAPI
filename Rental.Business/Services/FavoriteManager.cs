using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;

namespace Rental.Business.Services
{
    public class FavoriteManager : IFavoriteService
    {
        private readonly RentalDbContext _context;

        public FavoriteManager(RentalDbContext context)
        {
            _context = context;
        }

        public ServiceResult ToggleFavorite(Guid courtId, Guid userId)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null)
                return ServiceResult.Failure("Kort bulunamadı.");

            var existingFavorite = _context.UserFavoriteCourts.FirstOrDefault(f => f.UserId == userId && f.CourtId == courtId);

            if (existingFavorite != null)
            {
                _context.UserFavoriteCourts.Remove(existingFavorite);
                _context.SaveChanges();
                return ServiceResult.Success(new { message = "Kort favorilerden çıkarıldı.", isFavorite = false });
            }
            else
            {
                var newFavorite = new UserFavoriteCourt
                {
                    UserId = userId,
                    CourtId = courtId
                };
                _context.UserFavoriteCourts.Add(newFavorite);
                _context.SaveChanges();
                return ServiceResult.Success(new { message = "Kort favorilere eklendi.", isFavorite = true });
            }
        }

        public ServiceResult GetMyFavorites(Guid userId)
        {
            try
            {
                var favorites = _context.UserFavoriteCourts
                    .Include(f => f.Court)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.AddedAt)
                    .Select(f => new 
                    {
                        f.CourtId,
                        f.AddedAt,
                        Court = new 
                        {
                            f.Court!.Id,
                            f.Court.Slug,
                            f.Court.Name,
                            f.Court.City,
                            f.Court.District,
                            f.Court.SportType,
                            f.Court.SurfaceType,
                            f.Court.IsActive
                        }
                    })
                    .ToList();

                return ServiceResult.Success(favorites);
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"Sunucu hatası oluştu: {ex.Message}");
            }
        }

        public ServiceResult CheckFavorite(Guid courtId, Guid? userId)
        {
            if (!userId.HasValue)
                return ServiceResult.Success(new { isFavorite = false });
                
            var isFavorite = _context.UserFavoriteCourts.Any(f => f.UserId == userId.Value && f.CourtId == courtId);
            return ServiceResult.Success(new { isFavorite });
        }
    }
}
