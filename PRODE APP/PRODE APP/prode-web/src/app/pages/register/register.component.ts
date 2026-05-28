import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {

  private auth = inject(AuthService);

  private router = inject(Router);

  private cdr = inject(ChangeDetectorRef);

  name = '';

  email = '';

  password = '';

  error = '';

  showPassword = false;

  isLoading = false;

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  register() {

    this.error = '';

    if (this.password.length < 8) {
      this.error = 'La contraseña debe tener al menos 8 caracteres.';
      return;
    }

    this.isLoading = true;

    this.auth.register({
      name: this.name,
      email: this.email,
      password: this.password
    })
    .subscribe({

      next: (response) => {

        console.log(response);

        localStorage.setItem(
          'user',
          JSON.stringify(response)
        );

        this.router.navigate(['/matches']);

      },

      error: (error: any) => {

        this.isLoading = false;
        console.error(error);

        if (error.status === 0) {
          this.error = 'Error de conexión. Verificá que el servidor esté disponible.';
        } else if (error.status === 400) {
          this.error =
            error.error?.message ||
            error.error?.title ||
            (typeof error.error === 'string' ? error.error : null) ||
            'Datos inválidos. Revisá los campos e intentá de nuevo.';
        } else if (error.status === 409) {
          this.error = 'Ya existe una cuenta con ese email.';
        } else {
          this.error =
            error.error?.message ||
            (typeof error.error === 'string' ? error.error : null) ||
            'Ocurrió un error inesperado. Intentá de nuevo.';
        }

        try { this.cdr.detectChanges(); } catch {}

      }

    });

  }

}
