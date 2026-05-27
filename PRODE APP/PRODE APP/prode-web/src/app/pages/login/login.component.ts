import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  private authService = inject(AuthService);

  private router = inject(Router);

  private cdr = inject(ChangeDetectorRef);

  email = '';

  password = '';

  recoveryEmail = '';

  resetToken = '';

  resetPasswordValue = '';

  mode: 'login' | 'recover' = 'login';

  error = '';

  message = '';

  devToken = '';

  login() {

  this.error = '';
  this.message = '';

  const data = {
    email: this.email,
    password: this.password
  };

  this.authService.login(data)
    .subscribe({
     next: (response) => {

  localStorage.setItem(
    'user',
    JSON.stringify(response)
  );

  this.router.navigate(['/matches']);
},

      error: (err) => {

        console.error(err);

        if (err.status === 401) {
          this.error = 'Contraseña incorrecta o usuario inexistente.';
        } else if (err.status === 0) {
          this.error = 'Error de conexión. Verificá que el servidor esté disponible.';
        } else {
          this.error =
            err.error?.message ||
            err.error?.title ||
            (typeof err.error === 'string' ? err.error : null) ||
            'Ocurrió un error inesperado. Intentá de nuevo.';
        }

        try { this.cdr.detectChanges(); } catch {}
      }
    });
}

  requestRecovery() {
    this.error = '';
    this.message = '';
    this.devToken = '';

    this.authService.forgotPassword(this.recoveryEmail || this.email)
      .subscribe({
        next: (res) => {
          this.message = res.message;
          this.devToken = res.developmentResetToken ?? '';
          try { this.cdr.detectChanges(); } catch {}
        },
        error: (err) => {
          this.error =
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : null) ||
            'No se pudo generar la recuperacion.';
          try { this.cdr.detectChanges(); } catch {}
        }
      });
  }

  resetPassword() {
    this.error = '';
    this.message = '';

    this.authService.resetPassword({
      email: this.recoveryEmail || this.email,
      token: this.resetToken,
      newPassword: this.resetPasswordValue
    }).subscribe({
      next: () => {
        this.mode = 'login';
        this.password = '';
        this.resetToken = '';
        this.resetPasswordValue = '';
        this.message = 'Contrasena actualizada. Ya podes ingresar.';
        try { this.cdr.detectChanges(); } catch {}
      },
      error: (err) => {
        this.error =
          err.error?.message ||
          (typeof err.error === 'string' ? err.error : null) ||
          'No se pudo recuperar la contrasena.';
        try { this.cdr.detectChanges(); } catch {}
      }
    });
  }
  }

