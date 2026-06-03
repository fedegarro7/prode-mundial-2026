import {
  APP_INITIALIZER,
  ApplicationConfig,
  inject,
  LOCALE_ID
} from '@angular/core';

import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';

import {
  provideRouter,
  withEnabledBlockingInitialNavigation,
  withRouterConfig
} from '@angular/router';

import {
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';

import { catchError, of } from 'rxjs';

import { routes } from './app.routes';

import { authInterceptor } from './interceptors/auth.interceptor';
import { AuthService } from './services/auth.service';

registerLocaleData(localeEs);

function initAuth() {
  const auth = inject(AuthService);

  return () => auth.me().pipe(catchError(() => of(null)));
}

export const appConfig: ApplicationConfig = {

  providers: [

    { provide: LOCALE_ID, useValue: 'es' },

    provideRouter(
      routes,
      withEnabledBlockingInitialNavigation(),
      withRouterConfig({ onSameUrlNavigation: 'reload' })
    ),

    provideHttpClient(
      withInterceptors([
        authInterceptor
      ])
    ),

    {
      provide: APP_INITIALIZER,
      useFactory: initAuth,
      multi: true
    }

  ]
};
