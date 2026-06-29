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
import { MatchService } from '../../services/match.service';
import { GroupStanding } from '../../models/standing.model';
import { Match } from '../../models/match.model';
import { EsNamePipe } from '../../pipes/es-name.pipe';

/** Fast refresh during live matches (60 s). */
const POLL_LIVE_MS   = 300_000;
/** Slow refresh when no match is in progress (5 min). */
const POLL_IDLE_MS   = 300_000;

export interface BracketRound {
  key: string;
  label: string;
  matches: Match[];
}

/**
 * Displays the FIFA 2026 World Cup group-stage standings as tables
 * plus the knockout-stage bracket below.
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
  private matchService     = inject(MatchService);
  private router           = inject(Router);
  private cdr              = inject(ChangeDetectorRef);
  private destroy$         = new Subject<void>();
  private nextPoll$        = new Subject<void>();

  groups: GroupStanding[] = [];
  loading = true;
  error = false;
  lastUpdated: Date | null = null;

  bracketRounds: BracketRound[] = [];
  bracketLoading = true;
  thirdPlaceMatch: Match | null = null;

  ngOnInit(): void {
    this.schedulePoll(0);
    this.loadBracket();

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
          this.loadBracket();
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

  /** Fetches all matches and organises knockout rounds into the bracket. */
  private loadBracket(): void {
    this.matchService.getMatches()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (matches) => {
          this.bracketRounds = this.organizeBracket(matches);
          this.bracketLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.bracketLoading = false;
          this.cdr.detectChanges();
        }
      });
  }

  private organizeBracket(matches: Match[]): BracketRound[] {
    const ko = matches.filter(m => !m.groupName);

    const rounds: BracketRound[] = [
      { key: 'r32', label: '16VOS',  matches: [] },
      { key: 'r16', label: '8VOS',   matches: [] },
      { key: 'qf',  label: '4TOS',   matches: [] },
      { key: 'sf',  label: 'SEMIS',  matches: [] },
      { key: 'fn',  label: 'FINAL',  matches: [] },
    ];

    // 3rd-place match is shown separately below the bracket
    this.thirdPlaceMatch = ko
      .filter(m => this.isThirdPlace(m))
      .sort((a, b) => new Date(a.matchDate).getTime() - new Date(b.matchDate).getTime())[0] ?? null;

    for (const m of ko) {
      if (this.isThirdPlace(m)) continue; // handled separately

      const s = m.stage.toUpperCase();
      if      (s.includes('ROUND OF 32') || s.includes('DIECISEIS')) rounds[0].matches.push(m);
      else if (s.includes('ROUND OF 16') || s.includes('OCTAV'))     rounds[1].matches.push(m);
      else if (s.includes('QUARTER')     || s.includes('CUART'))     rounds[2].matches.push(m);
      else if (s.includes('SEMI'))                                    rounds[3].matches.push(m);
      else                                                            rounds[4].matches.push(m);
    }

    for (const r of rounds) {
      r.matches.sort((a, b) =>
        new Date(a.matchDate).getTime() - new Date(b.matchDate).getTime()
      );
    }

    return rounds.filter(r => r.matches.length > 0);
  }

  /** Height in px of a single "slot" in the bracket column for the given round. */
  slotHeight(key: string): number {
    const map: Record<string, number> = { r32: 64, r16: 128, qf: 256, sf: 512, fn: 1024 };
    return map[key] ?? 64;
  }

  isHomeWinner(m: Match): boolean {
    if (!m.isFinished || m.homeScore == null || m.awayScore == null) return false;
    return m.homeScore > m.awayScore;
  }

  isAwayWinner(m: Match): boolean {
    if (!m.isFinished || m.homeScore == null || m.awayScore == null) return false;
    return m.awayScore > m.homeScore;
  }

  isThirdPlace(m: Match): boolean {
    const s = m.stage.toUpperCase();
    return s.includes('THIRD') || s.includes('TERCER') || s.includes('PLAY-OFF');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.nextPoll$.complete();
  }
}


