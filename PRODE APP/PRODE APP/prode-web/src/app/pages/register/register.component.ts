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

  register() {

    this.error = '';

    if (this.password.length < 8) {
      this.error = 'La contrasena debe tener al menos 8 caracteres.';
      return;
    }

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

        console.error(error);

        this.error =
          error.error?.message ||
          error.error?.title ||
          (typeof error.error === 'string' ? error.error : null) ||
          error.statusText ||
          'Error de conexión. Verificá que el servidor esté disponible.';

        try { this.cdr.detectChanges(); } catch {}

      }

    });

  }

}
