import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { RankingService, RoundSummary } from '../../services/ranking.service';
import { MechanicsService } from '../../services/mechanics.service';
import { ROUND_LABELS } from '../../models/mechanics.model';

@Component({
  selector: 'app-rankings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rankings.component.html',
  styleUrls: ['./rankings.component.scss']
})
export class RankingsComponent implements OnInit {

  private rankingService = inject(RankingService);
  readonly mechanics = inject(MechanicsService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  rankings: any[] = [];
  loading = true;
  roundSummary: RoundSummary | null = null;
  roundSummaryOpen = false;

  ngOnInit(): void {
    this.loadRankings();
    this.mechanics.load().subscribe();

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        if (e.urlAfterRedirects === '/rankings') {
          this.loadRankings();
          this.mechanics.load().subscribe();
        }
      });
  }

  loadRankings(): void {
    this.loading = true;

    this.rankingService.getRanking().subscribe({
      next: (response) => {
        this.rankings = response;
        this.loading = false;
        try { this.cdr.detectChanges(); } catch {}
      },
      error: () => {
        this.loading = false;
        try { this.cdr.detectChanges(); } catch {}
      }
    });

    this.rankingService.getRoundSummary().subscribe({
      next: (s) => {
        this.roundSummary = s;
        try { this.cdr.detectChanges(); } catch {}
      },
      error: () => { /* non-critical */ }
    });
  }

  toggleRoundSummary(): void {
    this.roundSummaryOpen = !this.roundSummaryOpen;
    try { this.cdr.detectChanges(); } catch {}
  }

  get captainName(): string {
    return this.mechanics.state()?.captain?.teamName ?? '';
  }

  get goldenGoalRound(): string {
    const ggs = this.mechanics.state()?.goldenGoals ?? [];
    if (!ggs.length) return '';
    const rk = ggs[ggs.length - 1].roundKey;
    return ROUND_LABELS[rk] ?? rk;
  }

  get hasOracle(): boolean {
    return (this.mechanics.state()?.oraclePredictions ?? []).length > 0;
  }
}
