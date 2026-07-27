using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Rental.DataAccess.Context;
using Rental.Entities.Entity;
using Rental.Entities.Dtos; // DTO'nun olduğu namespace'i eklemeyi unutma

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

        [HttpGet("my-courts")]
        public IActionResult GetMyCourts()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized("Geçersiz kullanıcı kimliği.");

            var myCourts = _context.Courts
                .Where(c => c.OwnerId == userId)
                .ToList();

            return Ok(myCourts);
        }

        [HttpGet("slots/{courtId}")]
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
            // 1. GÜVENLİK (Yetki Kontrolü): Bu saha gerçekten işlem yapan adama mı ait?
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var court = _context.Courts.FirstOrDefault(c => c.Id == request.CourtId);

            if (court == null || court.OwnerId.ToString() != userIdStr)
                return Unauthorized(new { message = "Bu sahada işlem yapma yetkiniz yok!" });

            // 2. Kural Setini Veritabanına Kaydet
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
                RecordUserCode = int.Parse(userIdStr!)
            };

            _context.CourtSchedules.Add(schedule);

            // 3. ÇAKIŞMA ÖNLEYİCİ (Conflict Prevention)
            // Daha önce bu tarihler arasında açılmış olan slotların saatlerini hafızaya alıyoruz ki aynı saatleri tekrar üretmeyelim.
            var existingSlots = _context.CourtSlots
                .Where(s => s.CourtId == request.CourtId && s.StartTime >= request.StartDate && s.StartTime <= request.EndDate.AddDays(1))
                .Select(s => s.StartTime)
                .ToHashSet();

            var newSlots = new List<CourtSlot>();

            // 4. TAKVİM ÜRETİM MOTORU
            for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
            {
                var currentTime = request.OpenTime;

                while (currentTime.Add(TimeSpan.FromMinutes(request.SessionDurationMinutes)) <= request.CloseTime)
                {
                    var slotStartTime = date.Add(currentTime);
                    var slotEndTime = slotStartTime.AddMinutes(request.SessionDurationMinutes);

                    // Eğer bu saat daha önce üretilmediyse listeye ekle
                    if (!existingSlots.Contains(slotStartTime))
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

                    // Sonraki seansa geç (Maç süresi + Temizlik/Tampon süre)
                    currentTime = currentTime.Add(TimeSpan.FromMinutes(request.SessionDurationMinutes + request.BufferDurationMinutes));
                }
            }

            // 5. TOPLU KAYIT (Bulk Insert)
            if (newSlots.Any())
            {
                _context.CourtSlots.AddRange(newSlots);
            }

            _context.SaveChanges();

            return Ok(new { message = $"Sistem {newSlots.Count} adet yeni seansı çakışmasız olarak başarıyla üretti!" });
        }
    }
}