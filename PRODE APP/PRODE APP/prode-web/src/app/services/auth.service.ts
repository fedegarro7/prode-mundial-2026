import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { AuthResponse } from '../models/auth-response.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/auth`;

  register(data: any): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${this.apiUrl}/register`,
        data
      )
      .pipe(
        tap(response => {

          localStorage.setItem(
            'token',
            response.token
          );

          localStorage.setItem(
            'user',
            JSON.stringify(response)
          );
        })
      );
  }

  login(data: any): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(
        `${this.apiUrl}/login`,
        data
      )
      .pipe(
        tap(response => {

          localStorage.setItem(
            'token',
            response.token
          );

          localStorage.setItem(
            'user',
            JSON.stringify(response)
          );
        })
      );
  }

  changePassword(data: {
    currentPassword: string;
    newPassword: string;
  }) {
    return this.http.post<void>(
      `${this.apiUrl}/change-password`,
      data
    );
  }

  forgotPassword(email: string) {
    return this.http.post<{
      message: string;
      developmentResetToken?: string | null;
    }>(
      `${this.apiUrl}/forgot-password`,
      { email }
    );
  }

  resetPassword(data: {
    email: string;
    token: string;
    newPassword: string;
  }) {
    return this.http.post<void>(
      `${this.apiUrl}/reset-password`,
      data
    );
  }

  logout() {

    localStorage.removeItem('token');

    localStorage.removeItem('user');
  }

  getToken() {
    return localStorage.getItem('token');
  }

  getUser() {

  const user = localStorage.getItem('user');

  if (!user) return null;

  return JSON.parse(user);
}

  isLoggedIn() {
    return !!this.getToken();
  }
}
