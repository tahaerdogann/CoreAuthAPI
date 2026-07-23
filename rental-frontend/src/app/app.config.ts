import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
// HTTP ve Interceptor yeteneklerini import ediyoruz
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './interceptors/auth-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    // Angular'a diyoruz ki: İnternete çıkarken benim bu görevlimi de araya koy!
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
