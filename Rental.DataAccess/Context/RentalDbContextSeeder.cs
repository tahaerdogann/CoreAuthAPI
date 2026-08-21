using Bogus;
using Rental.Entities.Entity;
using Rental.Entities.Enum;

namespace Rental.DataAccess.Context
{
    public static class RentalDbContextSeeder
    {
        public static void SeedCourts(RentalDbContext context, Guid ownerId)
        {
            var faker = new Faker("tr");

            var sportTypes = new[] { "Futbol", "Basketbol", "Tenis", "Voleybol" };
            var surfaceTypes = new[] { "Suni Çim", "Parke", "Toprak", "Akrilik", "Beton" };
            
            var courtsToSeed = new List<Court>();

            for (int i = 0; i < 50; i++)
            {
                var courtId = Guid.NewGuid();
                var name = $"{faker.Address.City()} {faker.Company.CompanyName()} Sahası";
                
                var court = new Court
                {
                    Id = courtId,
                    Name = name,
                    Slug = name.ToLower().Replace(" ", "-").Replace("ş", "s").Replace("ç", "c").Replace("ö", "o").Replace("ğ", "g").Replace("ü", "u").Replace("ı", "i") + "-" + Guid.NewGuid().ToString().Substring(0, 4),
                    City = faker.Address.City(),
                    District = faker.Address.County(),
                    Neighborhood = faker.Address.StreetName(),
                    AddressDetail = faker.Address.FullAddress(),
                    Description = faker.Lorem.Paragraph(),
                    SportType = faker.PickRandom(sportTypes),
                    SurfaceType = faker.PickRandom(surfaceTypes),
                    Amenities = (FacilityAmenities)faker.Random.Number(1, 15), 
                    RentalOptionsJson = "[]", 
                    Latitude = faker.Address.Latitude(36.0, 42.0), // Türkiye sınırları
                    Longitude = faker.Address.Longitude(26.0, 45.0),
                    IsActive = true,
                    IsPublished = true,
                    IsAutoApproveEnabled = faker.Random.Bool(),
                    OwnerId = ownerId
                };

                // Fotoğrafları üret
                for (int j = 0; j < 3; j++)
                {
                    court.Photos.Add(new CourtPhoto
                    {
                        Id = Guid.NewGuid(),
                        CourtId = courtId,
                        Url = $"https://picsum.photos/800/600?random={Guid.NewGuid()}",
                        PublicId = "", // Cloudinary silmede hata vermemesi için boş
                        IsCover = j == 0,
                        DisplayOrder = j,
                        UploadDate = DateTime.UtcNow
                    });
                }

                courtsToSeed.Add(court);
            }

            context.Courts.AddRange(courtsToSeed);
            context.SaveChanges();
        }
    }
}
