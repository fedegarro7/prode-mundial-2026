import { Component, HostListener, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {

  private authService    = inject(AuthService);
  private router         = inject(Router);
  private navigationService = inject(NavigationService);

  user: any = null;
  menuOpen     = false;
  dropdownOpen = false;

  constructor() {
    this.loadUser();
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.loadUser();
        this.menuOpen     = false;
        this.dropdownOpen = false;
      });
  }

  loadUser() { this.user = this.authService.getUser(); }

  logout() {
    this.authService.logout();
    this.user        = null;
    this.menuOpen    = false;
    this.dropdownOpen = false;
    this.router.navigate(['/login']);
  }

  notifyRoute(route: string) { this.navigationService.notify(route); }

  toggleMenu()    { this.menuOpen     = !this.menuOpen; }
  closeMenu()     { this.menuOpen     = false; }
  toggleDropdown(){ this.dropdownOpen = !this.dropdownOpen; }
  closeDropdown() { this.dropdownOpen = false; }

  get userInitials(): string {
    if (!this.user?.name) return '?';
    return this.user.name
      .split(' ')
      .filter((w: string) => w.length > 0)
      .slice(0, 2)
      .map((w: string) => w[0].toUpperCase())
      .join('');
  }

  get firstName(): string {
    return this.user?.name?.split(' ')[0] ?? '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.user-chip')) {
      this.dropdownOpen = false;
    }
  }
}

