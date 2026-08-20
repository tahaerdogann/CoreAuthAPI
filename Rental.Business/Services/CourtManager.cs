using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Enum;

namespace Rental.Business.Services
{
    public class CourtManager : ICourtService
    {
        private readonly RentalDbContext _context;
        private readonly IConfiguration _configuration;

        public CourtManager(RentalDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public ServiceResult AddCourt(AddCourtDto request, Guid userId)
        {
            if (request.Photos != null && request.Photos.Count > 16)
                return ServiceResult.Failure("Bir sahaya en fazla 16 fotoğraf eklenebilir.");

            var court = new Court
            {
                Name = request.Name,
                SportType = request.SportType,
                SurfaceType = request.SurfaceType,
                City = request.City,
                District = request.District,
                Neighborhood = request.Neighborhood,
                AddressDetail = request.AddressDetail,
                Description = request.Description,
                Amenities = request.Amenities,
                RentalOptionsJson = request.RentalOptionsJson ?? "{}",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                OwnerId = userId,
                Slug = GenerateUniqueSlug(request.Name),
                IsActive = true,
                IsPublished = request.IsPublished,
                Photos = request.Photos?.Select(p => new CourtPhoto 
                {
                    Url = p.Url,
                    PublicId = p.PublicId,
                    IsCover = p.IsCover,
                    DisplayOrder = p.DisplayOrder
                }).ToList() ?? new List<CourtPhoto>()
            };

            _context.Courts.Add(court);
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Saha başarıyla eklendi!" });
        }

        public ServiceResult GetMyCourts(Guid userId)
        {
            List<Court> myCourts = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .Where(c => c.OwnerId == userId && c.IsActive).ToList();

            return ServiceResult.Success(myCourts);
        }

        public ServiceResult Search(double? lat, double? lng, string? sportTypes, double? distance, string? startDate, string? endDate, string? startTime, string? endTime, decimal? minPrice, decimal? maxPrice, int page, int pageSize, string? sortBy)
        {
            DateTime? parsedStartDate = null;
            if (DateTime.TryParse(startDate, out var sd)) parsedStartDate = sd;

            DateTime? parsedEndDate = null;
            if (DateTime.TryParse(endDate, out var ed)) parsedEndDate = ed;

            TimeSpan? parsedStartTime = null;
            if (TimeSpan.TryParse(startTime, out var st)) parsedStartTime = st;

            TimeSpan? parsedEndTime = null;
            if (TimeSpan.TryParse(endTime, out var et)) parsedEndTime = et;

            var query = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .Where(c => c.IsActive && c.IsPublished && _context.CourtSlots.Any(s => 
                    s.CourtId == c.Id && 
                    s.StartTime >= DateTime.Now &&
                    s.Status == SlotStatus.Available &&
                    (!parsedStartDate.HasValue || s.StartTime.Date >= parsedStartDate.Value.Date) &&
                    (!parsedEndDate.HasValue || s.StartTime.Date <= parsedEndDate.Value.Date) &&
                    (!parsedStartTime.HasValue || s.StartTime.TimeOfDay >= parsedStartTime.Value) &&
                    (!parsedEndTime.HasValue || s.StartTime.TimeOfDay <= parsedEndTime.Value) &&
                    (!minPrice.HasValue || s.Price >= minPrice.Value) &&
                    (!maxPrice.HasValue || s.Price <= maxPrice.Value)
                ));

            if (!string.IsNullOrEmpty(sportTypes))
            {
                var types = sportTypes.Split(',').Select(t => t.Trim()).ToList();
                query = query.Where(c => types.Contains(c.SportType));
            }

            var dbResults = query.Select(c => new {
                c.Id,
                c.Slug,
                c.Name,
                c.SportType,
                c.SurfaceType,
                c.City,
                c.District,
                c.Neighborhood,
                c.AddressDetail,
                c.Description,
                HourlyPrice = _context.CourtSlots
                                .Where(s => s.CourtId == c.Id && s.Status == SlotStatus.Available && s.StartTime >= DateTime.Now)
                                .Min(s => (decimal?)s.Price) ?? 0,
                c.Amenities,
                c.RentalOptionsJson,
                c.Latitude,
                c.Longitude,
                c.OwnerId,
                c.IsActive,
                c.IsPublished,
                Photos = c.Photos.OrderBy(p => p.DisplayOrder).Select(p => p.Url).ToList()
            }).ToList();

            var finalResults = dbResults.Select(r => new {
                r.Id,
                r.Slug,
                r.Name,
                r.SportType,
                r.SurfaceType,
                r.City,
                r.District,
                r.Neighborhood,
                r.AddressDetail,
                r.Description,
                r.HourlyPrice,
                r.Amenities,
                r.RentalOptionsJson,
                r.Latitude,
                r.Longitude,
                r.OwnerId,
                r.IsActive,
                r.IsPublished,
                r.Photos,
                DistanceKm = (lat.HasValue && lng.HasValue && r.Latitude.HasValue && r.Longitude.HasValue) 
                             ? CalculateDistance(lat.Value, lng.Value, r.Latitude.Value, r.Longitude.Value) 
                             : (double?)null
            }).AsEnumerable();

            if (lat.HasValue && lng.HasValue)
            {
                if (distance.HasValue)
                {
                    finalResults = finalResults.Where(r => r.DistanceKm <= distance.Value);
                }
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "price_asc":
                        finalResults = finalResults.OrderBy(r => r.HourlyPrice);
                        break;
                    case "price_desc":
                        finalResults = finalResults.OrderByDescending(r => r.HourlyPrice);
                        break;
                    case "distance_asc":
                        finalResults = finalResults.OrderBy(r => r.DistanceKm ?? double.MaxValue);
                        break;
                    default:
                        if (lat.HasValue && lng.HasValue)
                            finalResults = finalResults.OrderBy(r => r.DistanceKm ?? double.MaxValue);
                        break;
                }
            }
            else
            {
                if (lat.HasValue && lng.HasValue)
                {
                    finalResults = finalResults.OrderBy(r => r.DistanceKm ?? double.MaxValue);
                }
            }

            var totalCount = finalResults.Count();
            var pagedResults = finalResults.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return ServiceResult.Success(new { TotalCount = totalCount, Items = pagedResults });
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; 
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public ServiceResult GetCourtById(Guid courtId, Guid? userId)
        {
            var court = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .FirstOrDefault(c => c.Id == courtId);
                
            if (court == null)
                return ServiceResult.Failure("Saha bulunamadı.");

            if (!court.IsActive || !court.IsPublished)
            {
                if (!userId.HasValue || court.OwnerId != userId.Value)
                {
                    return ServiceResult.Failure("Bu saha sistemden kaldırılmış veya henüz yayında değil.");
                }
            }

            return ServiceResult.Success(court);
        }

        public ServiceResult GetCourtSlots(Guid courtId)
        {
            var slots = _context.CourtSlots
                .Where(s => s.CourtId == courtId && s.StartTime >= DateTime.Now)
                .OrderBy(s => s.StartTime)
                .ToList();

            return ServiceResult.Success(slots);
        }

        public ServiceResult GenerateSchedule(AddScheduleDto request, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == request.CourtId);

            if (court == null)
                return ServiceResult.Failure("Saha bulunamadı.");
                
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok!");

            var schedule = new CourtSchedule
            {
                CourtId = request.CourtId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                SessionDurationMinutes = request.SessionDurationMinutes,
                BufferDurationMinutes = request.BufferDurationMinutes,
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                BasePrice = request.BasePrice,
                PrimeTimePrice = request.PrimeTimePrice,
                PrimeTimeStart = request.PrimeTimeStart,
                PrimeTimeEnd = request.PrimeTimeEnd,
                RecordDate = DateTime.Now,
                RecordUserCode = userId,
                AdvancedRulesJson = request.DaysConfig != null ? JsonSerializer.Serialize(request.DaysConfig) : string.Empty,
                IsAutoScheduleEnabled = request.IsAutoScheduleEnabled
            };

            _context.CourtSchedules.Add(schedule);

            var existingSlots = _context.CourtSlots
                .Where(s => s.CourtId == request.CourtId && s.StartTime >= request.StartDate && s.StartTime <= request.EndDate.AddDays(1))
                .ToList();

            var newSlots = new List<CourtSlot>();
            var conflictErrors = new List<string>();

            for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
            {
                var dayOfWeek = (int)date.DayOfWeek;
                var dayConfig = request.DaysConfig?.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
                
                if (dayConfig != null && !dayConfig.IsActive)
                    continue; 
                
                var currentOpenTime = dayConfig != null ? dayConfig.OpenTime : request.OpenTime;
                var currentCloseTime = dayConfig != null ? dayConfig.CloseTime : request.CloseTime;
                
                var currentTime = currentOpenTime;

                while (currentTime.Add(TimeSpan.FromMinutes(request.SessionDurationMinutes)) <= currentCloseTime)
                {
                    var slotStartTime = date.Add(currentTime);
                    var slotEndTime = slotStartTime.AddMinutes(request.SessionDurationMinutes);

                    var hasConflict = existingSlots.Any(s => s.StartTime < slotEndTime && s.EndTime > slotStartTime);

                    if (hasConflict)
                    {
                        conflictErrors.Add($"{slotStartTime:ddd dd.MM.yyyy HH:mm} - {slotEndTime:HH:mm} arası çakışma nedeniyle üretilemedi.");
                    }
                    else
                    {
                        bool isPrimeTime = currentTime >= request.PrimeTimeStart && currentTime < request.PrimeTimeEnd;
                        decimal currentPrice = isPrimeTime ? request.PrimeTimePrice : request.BasePrice;

                        newSlots.Add(new CourtSlot
                        {
                            CourtId = request.CourtId,
                            StartTime = slotStartTime,
                            EndTime = slotEndTime,
                            Price = currentPrice,
                            Status = SlotStatus.Available
                        });
                    }

                    currentTime = currentTime.Add(TimeSpan.FromMinutes(request.SessionDurationMinutes + request.BufferDurationMinutes));
                }
            }

            if (newSlots.Any())
            {
                _context.CourtSlots.AddRange(newSlots);
                _context.SaveChanges();
            }
            else if (conflictErrors.Any())
            {
                return ServiceResult.Failure("Çakışmalar nedeniyle hiçbir yeni seans üretilemedi.");
            }

            return ServiceResult.Success(new { 
                message = $"{newSlots.Count} adet yeni seans başarıyla üretildi",
                errors = conflictErrors 
            });
        }

        public ServiceResult ToggleSlot(Guid slotId, Guid userId, string userRole)
        {
            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null) return ServiceResult.Failure("Seans bulunamadı.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            if (userRole != "Admin" && court?.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            slot.Status = slot.Status == SlotStatus.Available ? SlotStatus.Booked : SlotStatus.Available;
            if (slot.Status == SlotStatus.Available) 
            {
                slot.RenterId = null; 
            }

            _context.SaveChanges();
            return ServiceResult.Success(new { message = "Seans durumu başarıyla güncellendi.", isBooked = slot.Status != SlotStatus.Available });
        }

        public ServiceResult CancelSchedule(Guid courtId, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            var unbookedSlots = _context.CourtSlots.Where(s => s.CourtId == courtId && s.Status == SlotStatus.Available).ToList();
            _context.CourtSlots.RemoveRange(unbookedSlots);
            
            var activeSchedules = _context.CourtSchedules.Where(s => s.CourtId == courtId).ToList();
            foreach (var sched in activeSchedules)
            {
                sched.IsAutoScheduleEnabled = false;
            }

            _context.SaveChanges();
            return ServiceResult.Success(new { message = $"Takvimlendirme iptal edildi. {unbookedSlots.Count} adet boş seans silindi." });
        }

        public ServiceResult ToggleAutoSchedule(Guid courtId, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            var latestSchedule = _context.CourtSchedules.Where(s => s.CourtId == courtId).OrderByDescending(s => s.Id).FirstOrDefault();
            if (latestSchedule == null) return ServiceResult.Failure("Bu sahaya ait bir takvim kuralı bulunamadı.");

            latestSchedule.IsAutoScheduleEnabled = !latestSchedule.IsAutoScheduleEnabled;
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Otomatik uzatma durumu güncellendi.", isAutoScheduleEnabled = latestSchedule.IsAutoScheduleEnabled });
        }

        public ServiceResult TogglePublish(Guid courtId, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            court.IsPublished = !court.IsPublished;
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Sahanın yayın durumu güncellendi.", isPublished = court.IsPublished });
        }

        public ServiceResult ToggleAutoApprove(Guid courtId, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            court.IsAutoApproveEnabled = !court.IsAutoApproveEnabled;
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Otomatik onay durumu güncellendi.", isAutoApproveEnabled = court.IsAutoApproveEnabled });
        }

        public ServiceResult DeleteCourt(Guid id, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == id);
            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            bool hasFutureBookedSlots = _context.CourtSlots.Any(s => s.CourtId == id && s.Status != SlotStatus.Available && s.StartTime >= DateTime.Now);
            if (hasFutureBookedSlots)
                return ServiceResult.Failure("Bu sahanın gelecekte aktif (dolu) rezervasyonları bulunduğu için silinemez!");

            court.IsActive = false;

            var futureUnbookedSlots = _context.CourtSlots
                .Where(s => s.CourtId == id && s.Status == SlotStatus.Available && s.StartTime >= DateTime.Now)
                .ToList();
            _context.CourtSlots.RemoveRange(futureUnbookedSlots);

            var schedules = _context.CourtSchedules.Where(s => s.CourtId == id).ToList();
            foreach (var sched in schedules)
            {
                sched.IsAutoScheduleEnabled = false;
            }

            _context.SaveChanges();
            return ServiceResult.Success(new { message = "Saha pasife alındı ve gelecekteki boş seanslar temizlendi." });
        }

        public ServiceResult UpdateCourt(Guid id, AddCourtDto request, Guid userId, string userRole)
        {
            if (request.Photos != null && request.Photos.Count > 16)
                return ServiceResult.Failure("Bir sahaya en fazla 16 fotoğraf eklenebilir.");

            var court = _context.Courts
                .Include(c => c.Photos)
                .FirstOrDefault(c => c.Id == id);

            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            court.Name = request.Name;
            court.Slug = GenerateUniqueSlug(request.Name, court.Id);
            court.SportType = request.SportType;
            court.SurfaceType = request.SurfaceType;
            court.City = request.City;
            court.District = request.District;
            court.Neighborhood = request.Neighborhood;
            court.AddressDetail = request.AddressDetail;
            court.Description = request.Description;
            court.Amenities = request.Amenities;
            court.RentalOptionsJson = request.RentalOptionsJson ?? "{}";
            court.Latitude = request.Latitude;
            court.Longitude = request.Longitude;
            court.IsPublished = request.IsPublished;

            if (request.Photos != null && request.Photos.Any())
            {
                var coverPublicId = request.Photos.FirstOrDefault(p => p.IsCover)?.PublicId;

                _context.CourtPhotos
                    .Where(p => p.CourtId == court.Id && p.IsCover && p.PublicId != coverPublicId)
                    .ExecuteUpdate(s => s.SetProperty(p => p.IsCover, false));

                if (!string.IsNullOrEmpty(coverPublicId))
                {
                    _context.CourtPhotos
                        .Where(p => p.CourtId == court.Id && p.PublicId == coverPublicId && !p.IsCover)
                        .ExecuteUpdate(s => s.SetProperty(p => p.IsCover, true));
                }

                foreach (var photoDto in request.Photos)
                {
                    var existingPhoto = court.Photos.FirstOrDefault(p => p.PublicId == photoDto.PublicId);
                    if (existingPhoto == null)
                    {
                        _context.CourtPhotos.Add(new CourtPhoto
                        {
                            CourtId = court.Id,
                            Url = photoDto.Url,
                            PublicId = photoDto.PublicId,
                            IsCover = photoDto.IsCover,
                            DisplayOrder = photoDto.DisplayOrder
                        });
                    }
                    else
                    {
                        if (existingPhoto.DisplayOrder != photoDto.DisplayOrder)
                        {
                            existingPhoto.DisplayOrder = photoDto.DisplayOrder;
                        }
                    }
                }
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.FirstOrDefault();
                if (entry != null)
                {
                    string entityName = entry.Entity.GetType().Name;
                    return ServiceResult.Failure($"Eşzamanlılık hatası! Varlık: {entityName}.");
                }
                return ServiceResult.Failure("Kayıt güncellenirken eşzamanlılık hatası oluştu. Varlık: Bilinmiyor.");
            }

            return ServiceResult.Success(new { message = "Saha bilgileri başarıyla güncellendi!" });
        }

        public ServiceResult DeleteCourtPhoto(Guid courtId, Guid photoId, Guid userId, string userRole)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null) return ServiceResult.Failure("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId != userId)
                return ServiceResult.Failure("Bu sahada işlem yapma yetkiniz yok.");

            var photo = _context.CourtPhotos.FirstOrDefault(p => p.Id == photoId && p.CourtId == courtId);
            if (photo == null) return ServiceResult.Failure("Fotoğraf bulunamadı.");

            if (!string.IsNullOrEmpty(photo.PublicId))
            {
                var cloudName = _configuration["Cloudinary:CloudName"];
                var apiKey = _configuration["Cloudinary:ApiKey"];
                var apiSecret = _configuration["Cloudinary:ApiSecret"];

                if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
                {
                    Account account = new Account(cloudName, apiKey, apiSecret);
                    Cloudinary cloudinary = new Cloudinary(account);
                    
                    var delParams = new DeletionParams(photo.PublicId);
                    cloudinary.Destroy(delParams);
                }
            }

            _context.CourtPhotos.Remove(photo);
            _context.SaveChanges();

            return ServiceResult.Success(new { message = "Fotoğraf başarıyla silindi." });
        }

        public ServiceResult GetUploadSignature()
        {
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                return ServiceResult.Failure("Cloudinary ayarları eksik.");

            var timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            
            var parameters = new SortedDictionary<string, object>
            {
                { "folder", "courts" },
                { "timestamp", timestamp }
            };

            Account account = new Account(cloudName, apiKey, apiSecret);
            Cloudinary cloudinary = new Cloudinary(account);
            
            var signature = cloudinary.Api.SignParameters(parameters);

            return ServiceResult.Success(new
            {
                signature,
                timestamp,
                apiKey,
                cloudName,
                folder = "courts"
            });
        }
        public ServiceResult GetCourtBySlug(string slug, Guid? userId)
        {
            var court = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .FirstOrDefault(c => c.Slug == slug);
                
            if (court == null)
                return ServiceResult.Failure("Saha bulunamadı.");

            if (!court.IsActive || !court.IsPublished)
            {
                if (!userId.HasValue || court.OwnerId != userId.Value)
                {
                    return ServiceResult.Failure("Bu saha sistemden kaldırılmış veya henüz yayında değil.");
                }
            }

            return ServiceResult.Success(court);
        }

        public ServiceResult FixExistingSlugs()
        {
            var courts = _context.Courts.ToList();
            int updated = 0;
            foreach (var court in courts)
            {
                if (Guid.TryParse(court.Slug, out _) || string.IsNullOrEmpty(court.Slug))
                {
                    court.Slug = GenerateUniqueSlug(court.Name, court.Id);
                    updated++;
                }
            }

            if (updated > 0)
            {
                _context.SaveChanges();
            }

            return ServiceResult.Success(new { message = $"{updated} adet sahanın slug değeri güncellendi." });
        }

        private string GenerateUniqueSlug(string name, Guid? excludeCourtId = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return Guid.NewGuid().ToString();

            var charMap = new Dictionary<char, char> {
                {'ç', 'c'}, {'ğ', 'g'}, {'ı', 'i'}, {'ö', 'o'}, {'ş', 's'}, {'ü', 'u'},
                {'Ç', 'c'}, {'Ğ', 'g'}, {'İ', 'i'}, {'Ö', 'o'}, {'Ş', 's'}, {'Ü', 'u'}
            };

            var slug = new System.Text.StringBuilder(name.Length);
            foreach (var c in name.ToLowerInvariant())
            {
                if (charMap.TryGetValue(c, out char mapped))
                    slug.Append(mapped);
                else
                    slug.Append(c);
            }

            var cleanSlug = System.Text.RegularExpressions.Regex.Replace(slug.ToString(), @"[^a-z0-9\s-]", "");
            cleanSlug = System.Text.RegularExpressions.Regex.Replace(cleanSlug, @"\s+", "-").Trim('-');
            cleanSlug = System.Text.RegularExpressions.Regex.Replace(cleanSlug, @"-+", "-");

            if (string.IsNullOrEmpty(cleanSlug))
                cleanSlug = Guid.NewGuid().ToString();

            var finalSlug = cleanSlug;
            int counter = 1;

            while (true)
            {
                bool exists = _context.Courts.Any(c => c.Slug == finalSlug && c.Id != excludeCourtId);
                if (!exists)
                    break;
                
                finalSlug = $"{cleanSlug}-{counter}";
                counter++;
            }

            return finalSlug;
        }
    }
}
