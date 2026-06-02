import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { AuthResponse } from '../models/auth-response.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/auth`;

  currentUser = signal<AuthResponse | null>(this.getUser());

  register(data: any): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, data)
      .pipe(tap(response => this.setUser(response)));
  }

  login(data: any): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, data)
      .pipe(tap(response => this.setUser(response)));
  }

  me(): Observable<AuthResponse> {
    return this.http
      .get<AuthResponse>(`${this.apiUrl}/me`)
      .pipe(tap(response => this.setUser(response)));
  }

  updateName(name: string): Observable<AuthResponse> {
    return this.http
      .put<AuthResponse>(`${this.apiUrl}/name`, { name })
      .pipe(tap(response => this.setUser(response)));
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
    this.http.post<void>(`${this.apiUrl}/logout`, {}).subscribe({
      error: () => undefined
    });

    this.clearUser();
  }

  getUser(): AuthResponse | null {
    if (!this.hasSessionStorage()) return null;

    const user = sessionStorage.getItem('user');

    if (!user) return null;

    try {
      return JSON.parse(user) as AuthResponse;
    } catch {
      sessionStorage.removeItem('user');
      return null;
    }
  }

  isLoggedIn() {
    return !!this.currentUser();
  }

  clearUser() {
    if (this.hasSessionStorage()) {
      sessionStorage.removeItem('user');
    }

    this.currentUser.set(null);
  }

  private setUser(response: AuthResponse) {
    if (this.hasSessionStorage()) {
      sessionStorage.setItem(
        'user',
        JSON.stringify(response)
      );
    }

    this.currentUser.set(response);
  }

  private hasSessionStorage() {
    return typeof window !== 'undefined' && !!window.sessionStorage;
  }
}
