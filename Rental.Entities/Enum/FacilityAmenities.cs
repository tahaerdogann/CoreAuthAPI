using System;

namespace Rental.Entities.Enum
{
    [Flags]
    public enum FacilityAmenities
    {
        None = 0,
        Restroom = 1,
        Cafeteria = 2,
        DisabledAccess = 4,
        ChangingRoom = 8,
        WiFi = 16,
        Shower = 32,
        Locker = 64,
        Grandstand = 128,
        AirConditioning = 256,
        PrayerRoom = 512,
        Lighting = 1024
    }
}