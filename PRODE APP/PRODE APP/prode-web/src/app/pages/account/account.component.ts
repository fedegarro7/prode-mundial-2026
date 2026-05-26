import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './account.component.html',
  styleUrls: ['./account.component.scss']
})
export class AccountComponent {

  private auth = inject(AuthService);

  user = this.auth.getUser();

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';

  recoveryEmail = this.user?.email ?? '';
  resetToken = '';
  resetPasswordValue = '';

  message = '';
  error = '';
  devToken = '';
  loading = false;

  changePassword(): void {
    this.message = '';
    this.error = '';

    if (this.newPassword.length < 8) {
      this.error = 'La nueva contrasena debe tener al menos 8 caracteres.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Las contrasenas no coinciden.';
      return;
    }

    this.loading = true;
    this.auth.changePassword({
      currentPassword: this.currentPassword,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.message = 'Contrasena actualizada.';
      },
      error: (err) => {
        this.loading = false;
        this.error = this.readError(err, 'No pudimos cambiar la contrasena.');
      }
    });
  }

  requestRecovery(): void {
    this.message = '';
    this.error = '';
    this.devToken = '';
    this.loading = true;

    this.auth.forgotPassword(this.recoveryEmail).subscribe({
      next: (res) => {
        this.loading = false;
        this.message = res.message;
        this.devToken = res.developmentResetToken ?? '';
      },
      error: (err) => {
        this.loading = false;
        this.error = this.readError(err, 'No pudimos generar el token.');
      }
    });
  }

  resetPassword(): void {
    this.message = '';
    this.error = '';

    if (this.resetPasswordValue.length < 8) {
      this.error = 'La nueva contrasena debe tener al menos 8 caracteres.';
      return;
    }

    this.loading = true;
    this.auth.resetPassword({
      email: this.recoveryEmail,
      token: this.resetToken,
      newPassword: this.resetPasswordValue
    }).subscribe({
      next: () => {
        this.loading = false;
        this.resetToken = '';
        this.resetPasswordValue = '';
        this.message = 'Contrasena recuperada. Ya podes ingresar.';
      },
      error: (err) => {
        this.loading = false;
        this.error = this.readError(err, 'No pudimos recuperar la contrasena.');
      }
    });
  }

  private readError(err: any, fallback: string): string {
    return err?.error?.message ||
      err?.error?.title ||
      (typeof err?.error === 'string' ? err.error : null) ||
      fallback;
  }
}
