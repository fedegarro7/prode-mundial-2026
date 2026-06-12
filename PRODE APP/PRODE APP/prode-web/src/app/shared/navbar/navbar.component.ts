import { ChangeDetectorRef, Component, HostListener, inject, OnDestroy, NgZone, PLATFORM_ID } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { filter } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { NavigationService } from '../../services/navigation.service';
import { GroupsService } from '../../services/groups.service';
import { MatchService } from '../../services/match.service';
import { Match, Team } from '../../models/match.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnDestroy {

  authService               = inject(AuthService);
  private router            = inject(Router);
  private navigationService = inject(NavigationService);
  private ngZone            = inject(NgZone);
  private cdr               = inject(ChangeDetectorRef);
  private platformId        = inject(PLATFORM_ID);
  private matchService      = inject(MatchService);
  groupsService             = inject(GroupsService);

  user: any = null;
  menuOpen     = false;
  dropdownOpen = false;

  countdown = '';
  matchStarted = false;
  private countdownInterval: ReturnType<typeof setInterval> | null = null;
  private fixtureRefreshInterval: ReturnType<typeof setInterval> | null = null;
  private argentinaMatches: Match[] = [];
  private fixturesLoaded = false;

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
      this.loadArgentinaFixtures();
      this.ngZone.runOutsideAngular(() => {
        this.countdownInterval = setInterval(() => {
          this.updateCountdown();
          this.cdr.detectChanges();
        }, 1000);
        this.fixtureRefreshInterval = setInterval(() => {
          this.loadArgentinaFixtures();
        }, 300_000);
      });
    }
  }

  private updateCountdown(): void {
    if (!this.fixturesLoaded) return;

    const now = Date.now();
    const nextMatch = this.argentinaMatches.find(match =>
      new Date(match.matchDate).getTime() > now
    );

    if (!nextMatch) {
      const liveMatch = this.argentinaMatches.find(match => {
        const kickoff = new Date(match.matchDate).getTime();
        return !match.isFinished && kickoff <= now && now - kickoff <= 18_000_000;
      });

      this.matchStarted = !!liveMatch;
      this.countdown = liveMatch ? 'En juego' : 'Fixture a confirmar';
      return;
    }

    this.matchStarted = false;

    const diff = new Date(nextMatch.matchDate).getTime() - now;
    const d = Math.floor(diff / 86_400_000);
    const h = Math.floor((diff % 86_400_000) / 3_600_000);
    const m = Math.floor((diff % 3_600_000) / 60_000);
    const s = Math.floor((diff % 60_000) / 1_000);
    const separator = '\u00b7';
    this.countdown = `${d}d ${separator} ${this.pad(h)}h ${separator} ${this.pad(m)}m ${separator} ${this.pad(s)}s`;
  }

  private loadArgentinaFixtures(): void {
    this.matchService.getMatches().subscribe({
      next: matches => {
        this.argentinaMatches = matches
          .filter(match => this.isArgentinaMatch(match))
          .sort((a, b) =>
            new Date(a.matchDate).getTime() - new Date(b.matchDate).getTime()
          );
        this.fixturesLoaded = true;
        this.updateCountdown();
        try { this.cdr.detectChanges(); } catch { /* SSR */ }
      },
      error: () => {
        this.fixturesLoaded = true;
        this.matchStarted = false;
        this.countdown = 'Fixture a confirmar';
        try { this.cdr.detectChanges(); } catch { /* SSR */ }
      }
    });
  }

  private isArgentinaMatch(match: Match): boolean {
    return (
      this.isArgentinaTeam(match.homeTeam) ||
      this.isArgentinaTeam(match.awayTeam)
    );
  }

  private isArgentinaTeam(team?: Team | null): boolean {
    return (
      team?.code?.toUpperCase() === 'ARG' ||
      team?.name?.toLowerCase().includes('argentina') ||
      false
    );
  }

  private pad(n: number): string { return n.toString().padStart(2, '0'); }

  ngOnDestroy(): void {
    if (this.countdownInterval) clearInterval(this.countdownInterval);
    if (this.fixtureRefreshInterval) clearInterval(this.fixtureRefreshInterval);
  }

  loadUser() { this.user = this.authService.currentUser(); }

  logout() {
    this.authService.logout();
    this.user         = null;
    this.menuOpen     = false;
    this.dropdownOpen = false;
    this.router.navigate(['/login']);
  }

  notifyRoute(route: string) { this.navigationService.notify(route); }

  toggleMenu()     { this.menuOpen     = !this.menuOpen; }
  closeMenu()      { this.menuOpen     = false; }
  toggleDropdown() { this.dropdownOpen = !this.dropdownOpen; }
  closeDropdown()  { this.dropdownOpen = false; }

  get userInitials(): string {
    const name = this.authService.currentUser()?.name;
    if (!name) return '?';
    return name
      .split(' ')
      .filter((w: string) => w.length > 0)
      .slice(0, 2)
      .map((w: string) => w[0].toUpperCase())
      .join('');
  }

  get firstName(): string {
    return this.authService.currentUser()?.name?.split(' ')[0] ?? '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!(event.target as HTMLElement).closest('.user-chip')) {
      this.dropdownOpen = false;
    }
  }
}
