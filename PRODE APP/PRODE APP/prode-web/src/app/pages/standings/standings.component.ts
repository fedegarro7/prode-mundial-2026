import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit,
  OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, interval } from 'rxjs';
import { startWith, switchMap, takeUntil, filter } from 'rxjs/operators';

import { StandingsService } from '../../services/standings.service';
import { GroupStanding } from '../../models/standing.model';
import { EsNamePipe } from '../../pipes/es-name.pipe';

/** Refresh interval in milliseconds (60 s). */
const POLL_INTERVAL_MS = 60_000;

/**
 * Displays the FIFA 2026 World Cup group-stage standings as tables.
 * Data is computed server-side from finished match results and auto-refreshes
 * every 60 seconds so standings stay current as matches progress.
 */
@Component({
  selector: 'app-standings',
  standalone: true,
  imports: [CommonModule, EsNamePipe],
  templateUrl: './standings.component.html',
  styleUrls: ['./standings.component.scss']
})
export class StandingsComponent implements OnInit, OnDestroy {

  private standingsService = inject(StandingsService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  private destroy$ = new Subject<void>();

  groups: GroupStanding[] = [];
  loading = true;
  error = false;
  lastUpdated: Date | null = null;

  ngOnInit(): void {
    this.loadStandings();

    // Re-load whenever the user navigates back to this route.
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((e) => {
        if (e.urlAfterRedirects === '/standings') {
          this.loadStandings();
        }
      });
  }

  loadStandings(): void {
    this.loading = true;
    this.error = false;
    this.cdr.detectChanges();

    // Fetch immediately, then poll every POLL_INTERVAL_MS.
    interval(POLL_INTERVAL_MS)
      .pipe(
        startWith(0),
        switchMap(() => this.standingsService.getStandings()),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (data) => {
          this.groups = data;
          this.loading = false;
          this.error = false;
          this.lastUpdated = new Date();
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
          this.error = true;
          this.cdr.detectChanges();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}


