using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Dtos;
using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // PROFESYONEL DOKUNUŞ: Sisteme giriş yapmamış kimse bu API'ye ulaşamaz.
    public class CourtsController : ControllerBase
    {
        private readonly RentalDbContext _context;
        private readonly IConfiguration _configuration;

        public CourtsController(RentalDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult AddCourt([FromBody] AddCourtDto request)
        {
            if (request.Photos != null && request.Photos.Count > 16)
                return BadRequest(new { message = "Bir sahaya en fazla 16 fotoğraf eklenebilir." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

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
                IsActive = true,
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

            return Ok(new { message = "Saha başarıyla eklendi!" });
        }

        [HttpGet("my-courts")]
        public IActionResult GetMyCourts()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            List<Court> myCourts = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .Where(c => c.OwnerId == userId && c.IsActive).ToList();

            return Ok(myCourts);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult Search([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] string? sportType)
        {
            // Sadece aktif olan ve gelecekte en az 1 slotu olan sahaları getir
            var query = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .Where(c => c.IsActive && _context.CourtSlots.Any(s => s.CourtId == c.Id && s.StartTime >= DateTime.Now));

            if (!string.IsNullOrEmpty(sportType))
            {
                query = query.Where(c => c.SportType == sportType);
            }

            var dbResults = query.Select(c => new {
                c.Id,
                c.Name,
                c.SportType,
                c.SurfaceType,
                c.City,
                c.District,
                c.Neighborhood,
                c.AddressDetail,
                c.Description,
                HourlyPrice = _context.CourtSlots
                                .Where(s => s.CourtId == c.Id && !s.IsBooked && s.StartTime >= DateTime.Now)
                                .Min(s => (decimal?)s.Price) ?? 0,
                c.Amenities,
                c.RentalOptionsJson,
                c.Latitude,
                c.Longitude,
                c.OwnerId,
                c.IsActive,
                Photos = c.Photos.OrderBy(p => p.DisplayOrder).Select(p => p.Url).ToList()
            }).ToList();

            var finalResults = dbResults.Select(r => new {
                r.Id,
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
                r.Photos,
                DistanceKm = (lat.HasValue && lng.HasValue && r.Latitude.HasValue && r.Longitude.HasValue) 
                             ? CalculateDistance(lat.Value, lng.Value, r.Latitude.Value, r.Longitude.Value) 
                             : (double?)null
            });

            if (lat.HasValue && lng.HasValue)
            {
                finalResults = finalResults.OrderBy(r => r.DistanceKm ?? double.MaxValue);
            }

            return Ok(finalResults.ToList());
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
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

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public IActionResult GetCourtById(Guid id)
        {
            var court = _context.Courts
                .Include(c => c.Photos.OrderBy(p => p.DisplayOrder))
                .FirstOrDefault(c => c.Id == id);
                
            if (court == null)
                return NotFound(new { message = "Saha bulunamadı." });

            if (!court.IsActive)
                return BadRequest(new { message = "Bu saha sistemden kaldırılmıştır ve artık kiralanamaz." });

            return Ok(court);
        }

        [HttpGet("slots/{courtId:guid}")]
        [AllowAnonymous]
        public IActionResult GetCourtSlots(Guid courtId)
        {
            // PROFESYONEL DOKUNUŞ: Geçmişteki slotları getirme, sadece bugünden sonrasını getir ve tarihe göre sırala.
            var slots = _context.CourtSlots
                .Where(s => s.CourtId == courtId && s.StartTime >= DateTime.Now)
                .OrderBy(s => s.StartTime)
                .ToList();

            return Ok(slots);
        }

        [HttpPost("generate-schedule")]
        public IActionResult GenerateSchedule([FromBody] AddScheduleDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var court = _context.Courts.FirstOrDefault(c => c.Id == request.CourtId);

            if (court == null)
                return NotFound(new { message = "Saha bulunamadı." });
                
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized(new { message = "Bu sahada işlem yapma yetkiniz yok!" });

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
                RecordUserCode = Guid.Parse(userIdStr!),
                AdvancedRulesJson = request.DaysConfig != null ? JsonSerializer.Serialize(request.DaysConfig) : string.Empty,
                IsAutoScheduleEnabled = request.IsAutoScheduleEnabled
            };

            _context.CourtSchedules.Add(schedule);

            // Mevcut tüm slotları hafızaya alıyoruz (Çakışma kontrolü için)
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

                    // ÇAKIŞMA KONTROLÜ: Yeni slotun başlangıcı, mevcutlardan birinin bitişinden önce VE yeni slotun bitişi mevcutlardan birinin başlangıcından sonra mı?
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
                            IsBooked = false
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
                return BadRequest(new { message = "Çakışmalar nedeniyle hiçbir yeni seans üretilemedi.", errors = conflictErrors });
            }

            return Ok(new { 
                message = $"{newSlots.Count} adet yeni seans başarıyla üretildi",
                errors = conflictErrors 
            });
        }

        [HttpPost("toggle-slot/{slotId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleSlot(Guid slotId)
        {
            var slot = _context.CourtSlots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null) return NotFound("Seans bulunamadı.");

            var court = _context.Courts.FirstOrDefault(c => c.Id == slot.CourtId);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole != "Admin" && court?.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            slot.IsBooked = !slot.IsBooked;
            if (!slot.IsBooked) 
            {
                slot.RenterId = null; 
            }

            _context.SaveChanges();
            return Ok(new { message = "Seans durumu başarıyla güncellendi.", isBooked = slot.IsBooked });
        }

        [HttpPost("{courtId:guid}/cancel-schedule")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CancelSchedule(Guid courtId)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (court == null) return NotFound("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            var unbookedSlots = _context.CourtSlots.Where(s => s.CourtId == courtId && !s.IsBooked).ToList();
            _context.CourtSlots.RemoveRange(unbookedSlots);
            
            // Otomatik uzatmayı da kapatalım ki arkadan tekrar üretmesin
            var activeSchedules = _context.CourtSchedules.Where(s => s.CourtId == courtId).ToList();
            foreach (var sched in activeSchedules)
            {
                sched.IsAutoScheduleEnabled = false;
            }

            _context.SaveChanges();
            return Ok(new { message = $"Takvimlendirme iptal edildi. {unbookedSlots.Count} adet boş seans silindi." });
        }

        [HttpPost("{courtId:guid}/toggle-auto-schedule")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleAutoSchedule(Guid courtId)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (court == null) return NotFound("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            var latestSchedule = _context.CourtSchedules.Where(s => s.CourtId == courtId).OrderByDescending(s => s.Id).FirstOrDefault();
            if (latestSchedule == null) return BadRequest("Bu sahaya ait bir takvim kuralı bulunamadı.");

            latestSchedule.IsAutoScheduleEnabled = !latestSchedule.IsAutoScheduleEnabled;
            _context.SaveChanges();

            return Ok(new { message = "Otomatik uzatma durumu güncellendi.", isAutoScheduleEnabled = latestSchedule.IsAutoScheduleEnabled });
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult DeleteCourt(Guid id)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == id);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (court == null) return NotFound("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            // Sadece gelecekteki dolu rezervasyonları kontrol et
            bool hasFutureBookedSlots = _context.CourtSlots.Any(s => s.CourtId == id && s.IsBooked && s.StartTime >= DateTime.Now);
            if (hasFutureBookedSlots)
                return BadRequest("Bu sahanın gelecekte aktif (dolu) rezervasyonları bulunduğu için silinemez!");

            // Sahayı pasife al (Soft Delete)
            court.IsActive = false;

            // Gelecekteki BOŞ seansları sil (çünkü saha artık kiralanamaz)
            var futureUnbookedSlots = _context.CourtSlots
                .Where(s => s.CourtId == id && !s.IsBooked && s.StartTime >= DateTime.Now)
                .ToList();
            _context.CourtSlots.RemoveRange(futureUnbookedSlots);

            // Otomatik uzatmayı kapat
            var schedules = _context.CourtSchedules.Where(s => s.CourtId == id).ToList();
            foreach (var sched in schedules)
            {
                sched.IsAutoScheduleEnabled = false;
            }

            _context.SaveChanges();
            return Ok(new { message = "Saha pasife alındı ve gelecekteki boş seanslar temizlendi." });
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult UpdateCourt(Guid id, [FromBody] AddCourtDto request)
        {
            if (request.Photos != null && request.Photos.Count > 16)
                return BadRequest(new { message = "Bir sahaya en fazla 16 fotoğraf eklenebilir." });

            var court = _context.Courts
                .Include(c => c.Photos)
                .FirstOrDefault(c => c.Id == id);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (court == null) return NotFound("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            court.Name = request.Name;
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

            if (request.Photos != null && request.Photos.Any())
            {
                var coverPublicId = request.Photos.FirstOrDefault(p => p.IsCover)?.PublicId;

                // 1. EF Core Change Tracker problemlerini aşmak için doğrudan veritabanında güncelleme yapalım.
                _context.CourtPhotos
                    .Where(p => p.CourtId == court.Id && p.IsCover && p.PublicId != coverPublicId)
                    .ExecuteUpdate(s => s.SetProperty(p => p.IsCover, false));

                if (!string.IsNullOrEmpty(coverPublicId))
                {
                    _context.CourtPhotos
                        .Where(p => p.CourtId == court.Id && p.PublicId == coverPublicId && !p.IsCover)
                        .ExecuteUpdate(s => s.SetProperty(p => p.IsCover, true));
                }

                // 2. Fotoğrafları güncelle veya yeni ekle
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
                        // Sadece sırasını güncelle (IsCover işlemi yukarıda ExecuteUpdate ile yapıldı)
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
                    string details = $"State: {entry.State}. ";
                    
                    if (entry.Entity is CourtPhoto cp)
                    {
                        var original = entry.Property("IsCover").OriginalValue;
                        var current = entry.Property("IsCover").CurrentValue;
                        var modProps = string.Join(", ", entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name));
                        details += $"PhotoId: {cp.Id}, PublicId: {cp.PublicId}, IsCover (Orig: {original}, Cur: {current}). ModProps: {modProps}";
                    }

                    return BadRequest(new { message = $"Eşzamanlılık hatası! Varlık: {entityName}. Detay: {details}" });
                }
                return BadRequest(new { message = "Kayıt güncellenirken eşzamanlılık hatası oluştu. Varlık: Bilinmiyor." });
            }

            return Ok(new { message = "Saha bilgileri başarıyla güncellendi!" });
        }

        [HttpDelete("{courtId:guid}/photos/{photoId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult DeleteCourtPhoto(Guid courtId, Guid photoId)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == courtId);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (court == null) return NotFound("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            var photo = _context.CourtPhotos.FirstOrDefault(p => p.Id == photoId && p.CourtId == courtId);
            if (photo == null) return NotFound("Fotoğraf bulunamadı.");

            // 1. Cloudinary'den sil
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

            // 2. DB'den sil
            _context.CourtPhotos.Remove(photo);
            _context.SaveChanges();

            return Ok(new { message = "Fotoğraf başarıyla silindi." });
        }

        [HttpGet("get-upload-signature")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult GetUploadSignature()
        {
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                return BadRequest(new { message = "Cloudinary ayarları eksik." });

            var timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            
            // Cloudinary imza (signature) iin parametrelerin alfabetik srada olmas ARTTIR!
            // Eski CloudinaryDotNet srmleri Dictionary'yi otomatik sralamad iin SortedDictionary kullanyoruz.
            var parameters = new SortedDictionary<string, object>
            {
                { "folder", "courts" },
                { "timestamp", timestamp }
            };

            Account account = new Account(cloudName, apiKey, apiSecret);
            Cloudinary cloudinary = new Cloudinary(account);
            
            // İmzayı oluştur (Sadece yetkili API'miz bunu yapabilir)
            var signature = cloudinary.Api.SignParameters(parameters);

            return Ok(new
            {
                signature,
                timestamp,
                apiKey,
                cloudName,
                folder = "courts"
            });
        }
    }
}