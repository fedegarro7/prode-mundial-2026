import {
  APP_INITIALIZER,
  ApplicationConfig,
  inject
} from '@angular/core';

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

function initAuth() {
  const auth = inject(AuthService);

  return () => auth.me().pipe(catchError(() => of(null)));
}

export const appConfig: ApplicationConfig = {

  providers: [

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
