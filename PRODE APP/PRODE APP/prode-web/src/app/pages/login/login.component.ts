import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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

  private route = inject(ActivatedRoute);

  private cdr = inject(ChangeDetectorRef);

  email = '';

  password = '';

  recoveryEmail = '';

  resetToken = '';

  resetPasswordValue = '';

  mode: 'login' | 'recover' = 'login';

  error = '';

  message = '';

  isLoading = false;

  ngOnInit() {
    const email = this.route.snapshot.queryParamMap.get('email');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (email || token) {
      this.mode = 'recover';
      this.recoveryEmail = email ?? '';
      this.email = email ?? this.email;
      this.resetToken = token ?? '';
    }
  }

  login() {

  this.error = '';
  this.message = '';
  this.isLoading = true;

  const data = {
    email: this.email,
    password: this.password
  };

  this.authService.login(data)
    .subscribe({
     next: () => {
        this.router.navigate(['/matches']);
      },

      error: (err) => {

        this.isLoading = false;
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
    this.isLoading = true;

    this.authService.forgotPassword(this.recoveryEmail || this.email)
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          this.message = res.message;
          try { this.cdr.detectChanges(); } catch {}
        },
        error: (err) => {
          this.isLoading = false;
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
    this.isLoading = true;

    this.authService.resetPassword({
      email: this.recoveryEmail || this.email,
      token: this.resetToken,
      newPassword: this.resetPasswordValue
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.mode = 'login';
        this.password = '';
        this.resetToken = '';
        this.resetPasswordValue = '';
        this.message = 'Contraseña actualizada. Ya podes ingresar.';
        try { this.cdr.detectChanges(); } catch {}
      },
      error: (err) => {
        this.isLoading = false;
        this.error =
          err.error?.message ||
          (typeof err.error === 'string' ? err.error : null) ||
          'No se pudo recuperar la contraseña.';
        try { this.cdr.detectChanges(); } catch {}
      }
    });
  }
  }

