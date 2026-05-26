import { Component, inject } from '@angular/core';
import { RouterLink, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {

  private authService = inject(AuthService);

  private router = inject(Router);

  private navigationService = inject(NavigationService);

  user: any = null;

  constructor() {

    this.loadUser();

    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd)
      )
      .subscribe(() => {
        this.loadUser();
      });
  }

  loadUser() {
    this.user = this.authService.getUser();
  }

  logout() {

    this.authService.logout();

    this.user = null;

    this.router.navigate(['/login']);
  }

  /**
   * Notify interested pages that a navbar link was clicked.
   */
  notifyRoute(route: string) {
    // Emit event for subscribers; navigation will be handled by routerLink.
    this.navigationService.notify(route);
  }
}
