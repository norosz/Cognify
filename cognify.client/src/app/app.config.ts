import { ApplicationConfig, provideZoneChangeDetection, inject, provideAppInitializer, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { NgxEchartsModule } from 'ngx-echarts';

function initializeApp(): Promise<void> | void {
  const authService = inject(AuthService);
  const token = authService.getToken();

  if (!token) {
    return;
  }

  // Convert Observable to Promise to satisfy the void | Promise<void> signature if needed, 
  // or return the observable if the types allow (Angular 19 supports Observable<unknown>).
  // However, explicit subscription or promise conversion is safer for "void" return types if logic is simple.
  // Actually provideAppInitializer supports Observable. Let's return valid Observable.

  return new Promise((resolve) => {
    authService.validateToken().pipe(
      catchError(() => {
        // Error handled in service (logout)
        return of(null);
      })
    ).subscribe(() => resolve());
  });
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    provideAppInitializer(initializeApp),
    importProvidersFrom(NgxEchartsModule.forRoot({ echarts: () => import('echarts') }))
  ]
};
