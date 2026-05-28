import { ChangeDetectorRef, Component, HostListener, inject, OnDestroy, NgZone, PLATFORM_ID } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';

// Partido inaugural: 11 jun 2026 a las 21:00 UTC
const FIRST_MATCH = new Date('2026-06-11T21:00:00Z');

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnDestroy {

  private authService       = inject(AuthService);
  private router            = inject(Router);
  private navigationService = inject(NavigationService);
  private ngZone            = inject(NgZone);
  private cdr               = inject(ChangeDetectorRef);
  private platformId        = inject(PLATFORM_ID);

  user: any = null;
  menuOpen     = false;
  dropdownOpen = false;

  countdown = '';
  matchStarted = false;
  private countdownInterval: ReturnType<typeof setInterval> | null = null;

  constructor() {
    this.loadUser();
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.loadUser();
        this.menuOpen     = false;
        this.dropdownOpen = false;
      });

    if (isPlatformBrowser(this.platformId)) {
      this.updateCountdown();
      this.ngZone.runOutsideAngular(() => {
        this.countdownInterval = setInterval(() => {
          this.updateCountdown();
          this.cdr.detectChanges();
        }, 1000);
      });
    }
  }

  private updateCountdown(): void {
    const diff = FIRST_MATCH.getTime() - Date.now();
    if (diff <= 0) {
      this.matchStarted = true;
      this.countdown = '¡Arrancó el Mundial!';
      if (this.countdownInterval) {
        clearInterval(this.countdownInterval);
        this.countdownInterval = null;
      }
      return;
    }
    const d = Math.floor(diff / 86_400_000);
    const h = Math.floor((diff % 86_400_000) / 3_600_000);
    const m = Math.floor((diff % 3_600_000) / 60_000);
    const s = Math.floor((diff % 60_000) / 1_000);
    this.countdown = `${d}d · ${this.pad(h)}h · ${this.pad(m)}m · ${this.pad(s)}s`;
  }

  private pad(n: number): string { return n.toString().padStart(2, '0'); }

  ngOnDestroy(): void {
    if (this.countdownInterval) clearInterval(this.countdownInterval);
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

