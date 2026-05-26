import {
  ApplicationConfig
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

import { routes } from './app.routes';

import { authInterceptor } from './interceptors/auth.interceptor';

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
    )

  ]
};
