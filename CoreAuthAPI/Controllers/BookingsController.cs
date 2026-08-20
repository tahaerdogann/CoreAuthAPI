using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using Rental.Business.Dtos;
using Rental.Business.Interfaces;

namespace CoreAuthAPI.Controllers
{
    [Route("api/[controller]")] //[controller] shortcut'ı ile eğer bu kodu yazdığın sınıfın adı BookingsController ise, bu sınıfın rotası "api/bookings" olur.
    [ApiController]  

    /*  Bu etiket sayesinde frontend'den gelen veriler (DTO) hatalı veya eksikse .NET senin 
        yerine kontrol edip otomatik olarak 400 Bad Request döner, kodun içine hiç girmez */

    [Authorize] // Kiralama için giriş yapmak zorunlu. [Authorize] -> Program.cs -> AddAuthentication() -> AddJwtBearer() ile birlikte çalışır.
    public class BookingsController : ControllerBase // create a public class then inherit ControllerBase to create a Web API controller 

    /* ControllerBase sınıfı, Web API controller'ları için temel işlevselliği sağlar.
     * neden controller kuanmıyoruz? Çünkü controller sınıfı, MVC (Model-View-Controller) 
     * deseninde kullanılır ve View (görünüm) ile ilgili işlevsellik içerir. Bizim amacımız 
     * sadece API endpoint'leri oluşturmak, yani veri almak ve göndermek. 
     * Bu yüzden ControllerBase kullanıyoruz. */  
    
    {
        //private readonly kullanma sebebimiz, bu alanın sadece sınıf içinde kullanılmasını ve dışarıdan değiştirilememesini sağlamak. readonly ise, bu alanın sadece constructor içinde atanabileceğini ve sonrasında değiştirilemeyeceğini belirtir.
        private readonly  IBookingService _bookingService; // Dependency Injection ile IBookingService'i alıyoruz. Bu sayede servis katmanındaki iş mantığını controller'da kullanabiliriz.

        /* C#'ta statik (static) olmayan metotları, doğrudan bir tipin ismi üzerinden çağıramazsın. Yani kodun aşağısına inip IBookingService.CreateBooking()
         * yazarsan, derleyici sana şu hatayı fırlatır: “An object reference is required for the non-static field, method, or property.” 
         * (Statik olmayan bir metodu çağırabilmek için gerçek bir nesne referansına ihtiyacın var.) */

        // Constructor Injection: IBookingService'i controller'a enjekte ediyoruz. Bu sayede controller, servis katmanındaki iş mantığını kullanabilir.
        public BookingsController (IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // Kullanıcının kimliğini ve rolünü almak için bir yardımcı metot oluşturuyoruz. Bu metot, JWT token'ından kullanıcı bilgilerini çeker.
        // 
        private (Guid? userId, string userRole) GetUserContext()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier); // JWT token'ında NameIdentifier claim'i, kullanıcının benzersiz kimliğini (ID) temsil eder. Bu ID, genellikle veritabanındaki kullanıcı kaydının primary key'idir.
            var userRole = User.FindFirstValue(ClaimTypes.Role); // JWT token'ında Role claim'i, kullanıcının rolünü temsil eder. Bu rol, kullanıcının sistemdeki yetkilerini belirler (örneğin Admin, Owner, Customer).
            if (Guid.TryParse(userIdStr, out Guid userId)) // userIdStr'yi Guid tipine çevirmeye çalışıyoruz. Eğer başarılı olursa, userId değişkenine atanır ve true döner.
            {
                return (userId, userRole ?? string.Empty); // Eğer userRole null ise, boş string döneriz. Bu sayede null referans hatalarından kaçınırız.
            }
            return (null, userRole ?? string.Empty);   // Eğer userIdStr geçerli bir Guid değilse, null döneriz. Bu durumda kullanıcı kimliği alınamamış demektir.
        }

        [HttpPost("create")]
        // Endpoint URL'si: api/bookings/create 
        // Bu endpoint, kullanıcıların yeni bir kiralama oluşturmasını sağlar. Kullanıcı, kiralama bilgilerini JSON formatında gönderir ve sistem bu bilgileri işleyerek yeni bir kiralama kaydı oluşturur.
        public IActionResult CreateBooking([FromBody] CreateBookingRequest request) 
        //IactionResult, .NET Core'da bir HTTP yanıtını temsil eden bir arayüzdür. Bu arayüz, farklı türde yanıtlar döndürebilmemizi sağlar (örneğin Ok, NotFound, BadRequest vb.).
        {
            var (userId, _) = GetUserContext(); // Kullanıcının kimliğini ve rolünü alıyoruz. Burada rolü kullanmayacağız, bu yüzden "_" ile ignore ediyoruz.
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.CreateBooking(request, userId.Value);

            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "İlgili seans bulunamadı.") return NotFound(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPost("external-booking")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult CreateExternalBooking([FromBody] CreateExternalBookingRequest request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.CreateExternalBooking(request, userId.Value, userRole);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "İlgili seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahanın sahibi değilsiniz.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpPut("courtslots/{slotId:guid}/maintenance")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult SetMaintenance(Guid slotId, [FromBody] SetMaintenanceRequest request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.SetMaintenance(slotId, request, userId.Value, userRole);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "İlgili seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu sahanın sahibi değilsiniz.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpGet("my-bookings")]
        public IActionResult GetMyBookings()
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.GetMyBookings(userId.Value);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("cancel/{bookingId:guid}")]
        public IActionResult CancelBooking(Guid bookingId)
        {
            var (userId, _) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.CancelBooking(bookingId, userId.Value);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Kiralama kaydı bulunamadı.") return NotFound(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }

        [HttpGet("owner-booked-slots")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult GetOwnerBookedSlots([FromQuery] Guid? courtId)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.GetOwnerBookedSlots(courtId, userId.Value, userRole);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
        }

        [HttpPost("update-status/{bookingId:guid}")]
        [Authorize(Roles = "Admin,Owner")]
        public IActionResult UpdateBookingStatus(Guid bookingId, [FromBody] UpdateBookingStatusRequest request)
        {
            var (userId, userRole) = GetUserContext();
            if (!userId.HasValue) return Unauthorized("Kullanıcı kimliği alınamadı.");

            var result = _bookingService.UpdateBookingStatus(bookingId, request, userId.Value, userRole);
            
            if (!result.IsSuccess)
            {
                if (result.ErrorMessage == "Kiralama kaydı bulunamadı." || result.ErrorMessage == "Seans bulunamadı.") return NotFound(result.ErrorMessage);
                if (result.ErrorMessage == "Bu işlem için yetkiniz yok.") return Unauthorized(result.ErrorMessage);
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.Data);
        }
    }
}
