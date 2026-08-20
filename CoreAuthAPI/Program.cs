using Rental.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Rental.Business.Interfaces;
using Rental.Business.Services;

// WebApplicationBuilder nesnesi, uygulamanın yapılandırmasını ve servislerini yönetmek için kullanılır
WebApplicationBuilder builder = WebApplication.CreateBuilder(args); 

// Veritabanı bağlantımızı sisteme tanıtıyoruz
builder.Services.AddDbContext<RentalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); //connection string'i appsettings.json dosyasından alıyoruz

// 1. CORS Ayarları (Angular'ın API'ye erişebilmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // Angular projen varsayılan olarak 4200 portunda çalışır
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader() //means JWT header section
              .AllowAnyMethod(); //means can GET, POST, DELETE on the db
    });
});

// 2. JWT Kimlik Doğrulama (Authentication) Ayarları
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
// Ve app.UseCors("AllowAll"); satırı ekli olmalı.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Eğer JSON'a çevirirken aynı veriye tekrar denk gelirsen döngüye girme, onu görmezden gel ve çeviriyi bitir.
        /* Court entity'si içinde photos entities'i var, photos entity'si içinde de court entity'si var. Biz de
           arada DTO kullanmadan direkt entity'leri JSON'a çevirdik (tembellikten). Bu yüzde    n JSON'a çevirirken döngüye giriyor ve hata veriyordu.
        DEZAVANTAJI: json'a çevirirken bazı veriler eksik kalabilir, çünkü döngüye giren veriler görmezden geliniyor. Bu yüzden DTO kullanmak daha sağlıklı olur.
        */
    });
builder.Services.AddHostedService<CoreAuthAPI.Services.AutoScheduleWorker>();
// AutoScheduleWorker'ı arka planda çalışacak bir servis olarak ekliyoruz

/* 
DEZAVANTAJLARI

1.  Server Kapanırsa bekçi servis de kapanır, arka planda yapması gereken işleri yapamaz.
    Bu yüzden arka planda çalışacak servisler için Windows Service veya Linux Daemon gibi çözümler kullanmalı
2.  Çoklu Sunucucu ortamında, her sunucu kendi bekçi servisini çalıştırır ve bu da çakışmalara yol açabilir. 
    Bu yüzden arka planda çalışacak servisler için merkezi bir görev yöneticisi veya kuyruk sistemi kullanmalı

*/

// Servisleri Dependency Injection (DI) ile sisteme tanıtıyoruz
builder.Services.AddScoped<IAuthService, AuthManager>(); //AddScoped: Her HTTP isteği için yeni bir örnek oluşturur ve aynı istek boyunca aynı örneği kullanır. Bu, genellikle veri tabanı bağlamları veya kullanıcıya özgü hizmetler için uygundur.
builder.Services.AddScoped<ICourtService, CourtManager>();
builder.Services.AddScoped<IFavoriteService, FavoriteManager>();
builder.Services.AddScoped<IBookingService, BookingManager>();
builder.Services.AddScoped<IBackgroundJobsService, BackgroundJobsManager>();
builder.Services.AddOpenApi();

/*  NEDEN FARKLI SERVİS EKLEME METOTLARI VAR?
 *  çünkü her servis farklı ömür (lifetime) yönetimine ihtiyaç duyabilir. Örneğin:
AddSingleton: Uygulama boyunca tek bir örnek oluşturur ve tüm isteklerde aynı örneği kullanır. Bu, genellikle konfigürasyon veya önbellekleme gibi durumlar için uygundur.
AddTransient: Her istekte yeni bir örnek oluşturur. Bu, genellikle kısa ömürlü hizmetler için uygundur.
*/
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 3. CORS'u Devreye Alıyoruz (Sıralama çok önemli, Auth'tan önce olmalı!)
app.UseCors("AllowAngular");

// 4. Authentication (Kimlik Doğrulama) Middleware'i ekleniyor 
// DİKKAT: UseAuthentication() mutlaka UseAuthorization()'dan ÖNCE yazılmalıdır!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();