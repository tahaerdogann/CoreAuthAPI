// src/environments/environment.ts
export const environment = {
  production: false,
  // DİKKAT: Buradaki portu, Adım 1'de Swagger'da gördüğün GÜNCEL port ile değiştir!
  apiUrl: 'https://localhost:7284/api',
  cloudinary: {
    cloudName: 'elzcic8u',
    uploadPreset: 'rental_photos'
  },
  googleMapsApiKey: 'AIzaSyBNom8-fFnaT7657zfNWb0osk1EU0L0O68'
};
