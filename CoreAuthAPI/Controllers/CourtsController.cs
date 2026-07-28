using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Dtos;
using System.Text.Json;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // PROFESYONEL DOKUNUŞ: Sisteme giriş yapmamış kimse bu API'ye ulaşamaz.
    public class CourtsController : ControllerBase
    {
        private readonly RentalDbContext _context;

        public CourtsController(RentalDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult AddCourt([FromBody] AddCourtDto request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
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
                HourlyPrice = request.HourlyPrice,
                Amenities = request.Amenities,
                RentalOptionsJson = request.RentalOptionsJson ?? "{}",
                OwnerId = userId,
                IsActive = true
            };

            _context.Courts.Add(court);
            _context.SaveChanges();

            return Ok(new { message = "Saha başarıyla eklendi!" });
        }

        [HttpGet("my-courts")]
        public IActionResult GetMyCourts()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var role = User.FindFirstValue(ClaimTypes.Role);
            List<Court> myCourts;
            
            if (role == "Admin") 
            {
                myCourts = _context.Courts.ToList();
            }
            else 
            {
                myCourts = _context.Courts.Where(c => c.OwnerId == userId).ToList();
            }

            return Ok(myCourts);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public IActionResult Search([FromQuery] string? city, [FromQuery] string? sportType)
        {
            // Sadece aktif olan ve gelecekte en az 1 slotu olan sahaları getir
            var query = _context.Courts.Where(c => c.IsActive && _context.CourtSlots.Any(s => s.CourtId == c.Id && s.StartTime >= DateTime.Now));

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(c => c.City.Contains(city) || c.District.Contains(city) || c.Neighborhood.Contains(city));
            }

            if (!string.IsNullOrEmpty(sportType))
            {
                query = query.Where(c => c.SportType == sportType);
            }

            var results = query.ToList();
            return Ok(results);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetCourtById(int id)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == id && c.IsActive);
            if (court == null)
                return NotFound("Saha bulunamadı.");

            return Ok(court);
        }

        [HttpGet("slots/{courtId}")]
        [AllowAnonymous]
        public IActionResult GetCourtSlots(int courtId)
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
                RecordUserCode = int.Parse(userIdStr!),
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
                message = $"Sistem {newSlots.Count} adet yeni seansı başarıyla üretti!",
                errors = conflictErrors 
            });
        }

        [HttpPost("toggle-slot/{slotId}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleSlot(int slotId)
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

        [HttpPost("{courtId}/cancel-schedule")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CancelSchedule(int courtId)
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

        [HttpPost("{courtId}/toggle-auto-schedule")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult ToggleAutoSchedule(int courtId)
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

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult DeleteCourt(int id)
        {
            var court = _context.Courts.FirstOrDefault(c => c.Id == id);
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (court == null) return NotFound("Saha bulunamadı.");
            if (userRole != "Admin" && court.OwnerId.ToString() != userIdStr)
                return Unauthorized("Bu sahada işlem yapma yetkiniz yok.");

            bool hasBookedSlots = _context.CourtSlots.Any(s => s.CourtId == id && s.IsBooked);
            if (hasBookedSlots)
                return BadRequest("Bu sahanın aktif (dolu) rezervasyonları bulunduğu için silinemez!");

            var slots = _context.CourtSlots.Where(s => s.CourtId == id).ToList();
            var schedules = _context.CourtSchedules.Where(s => s.CourtId == id).ToList();

            _context.CourtSlots.RemoveRange(slots);
            _context.CourtSchedules.RemoveRange(schedules);
            _context.Courts.Remove(court);
            
            _context.SaveChanges();
            return Ok(new { message = "Saha ve tüm boş takvimleri başarıyla silindi." });
        }
    }
}