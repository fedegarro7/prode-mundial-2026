import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit,
  OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { Subject, timer } from 'rxjs';
import { takeUntil, filter, switchMap } from 'rxjs/operators';

import { StandingsService } from '../../services/standings.service';
import { GroupStanding } from '../../models/standing.model';
import { EsNamePipe } from '../../pipes/es-name.pipe';

/** Fast refresh during live matches (60 s). */
const POLL_LIVE_MS   = 60_000;
/** Slow refresh when no match is in progress (5 min). */
const POLL_IDLE_MS   = 300_000;

/**
 * Displays the FIFA 2026 World Cup group-stage standings as tables.
 * Polling adapts automatically: 60 s while a match is live, 5 min otherwise.
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
  private nextPoll$ = new Subject<void>();

  groups: GroupStanding[] = [];
  loading = true;
  error = false;
  lastUpdated: Date | null = null;

  ngOnInit(): void {
    this.schedulePoll(0);

    // Re-load whenever the user navigates back to this route.
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((e) => {
        if (e.urlAfterRedirects === '/standings') {
          this.nextPoll$.next();
          this.schedulePoll(0);
        }
      });
  }

  /** Schedules one fetch after `delayMs`, then re-schedules based on response. */
  private schedulePoll(delayMs: number): void {
    timer(delayMs)
      .pipe(
        switchMap(() => this.standingsService.getStandings()),
        takeUntil(this.destroy$),
        takeUntil(this.nextPoll$)
      )
      .subscribe({
        next: ({ standings, hasActiveMatches }) => {
          this.groups = standings;
          this.loading = false;
          this.error = false;
          this.lastUpdated = new Date();
          this.cdr.detectChanges();

          const next = hasActiveMatches ? POLL_LIVE_MS : POLL_IDLE_MS;
          this.schedulePoll(next);
        },
        error: () => {
          this.loading = false;
          this.error = true;
          this.cdr.detectChanges();
          // On error, retry after idle interval.
          this.schedulePoll(POLL_IDLE_MS);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.nextPoll$.complete();
  }
}



