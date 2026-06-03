import { ChangeDetectorRef, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { Match } from '../../models/match.model';
import { MatchService } from '../../services/match.service';
import { PredictionService } from '../../services/prediction.service';
import { EsNamePipe } from '../../pipes/es-name.pipe';

export interface DayGroup {
  label: string;
  date: Date;
  matches: Match[];
}

@Component({
  selector: 'app-my-predictions',
  standalone: true,
  imports: [CommonModule, EsNamePipe],
  templateUrl: './my-predictions.component.html',
  styleUrls: ['./my-predictions.component.scss']
})
export class MyPredictionsComponent implements OnInit, OnDestroy {

  private matchService = inject(MatchService);
  private predictionService = inject(PredictionService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  loading = true;
  dayGroups: DayGroup[] = [];
  totalMatches = 0;
  predictedCount = 0;

  savedIds = new Set<number>();
  savingIds = new Set<number>();
  collapsedDays = new Set<string>();
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';
  toastVisible = false;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.load();
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => {
        if (e.urlAfterRedirects === '/my-predictions') this.load();
      });
  }

  ngOnDestroy(): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  load(): void {
    this.loading = true;
    this.matchService.getUpcomingMatches().subscribe({
      next: (res) => {
        const mapped = res.map(m => ({
          ...m,
          homePrediction: m.myPrediction?.homeScorePrediction ?? 0,
          awayPrediction: m.myPrediction?.awayScorePrediction ?? 0
        }));

        this.savedIds.clear();
        for (const m of mapped) {
          if (m.myPrediction && !m.isFinished) this.savedIds.add(m.id);
        }

        const predictable = mapped.filter(m => !m.isFinished);
        this.totalMatches = predictable.length;
        this.predictedCount = this.savedIds.size;
        this.dayGroups = this.buildDayGroups(mapped);
        this.initCollapsedDays();
        this.loading = false;
        try { this.cdr.detectChanges(); } catch {}
      },
      error: () => { this.loading = false; try { this.cdr.detectChanges(); } catch {} }
    });
  }

  private buildDayGroups(matches: Match[]): DayGroup[] {
    const map = new Map<string, Match[]>();
    for (const m of matches) {
      const d = new Date(m.matchDate);
      const key = `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(m);
    }
    return [...map.entries()]
      .sort((a, b) => {
        const da = new Date(a[1][0].matchDate);
        const db = new Date(b[1][0].matchDate);
        return da.getTime() - db.getTime();
      })
      .map(([, matches]) => {
        const d = new Date(matches[0].matchDate);
        return {
          label: d.toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }),
          date: d,
          matches
        };
      });
  }

  private initCollapsedDays(): void {
    this.collapsedDays.clear();
    for (const day of this.dayGroups) {
      // Auto-collapse if ALL matches in the day are finished, or ALL predictable ones are already saved
      const allDone = day.matches.every(m => m.isFinished);
      const allSaved = day.matches.every(m => !this.canPredict(m) || this.savedIds.has(m.id));
      if (allDone || allSaved) {
        this.collapsedDays.add(day.label);
      }
    }
  }

  isDayCollapsed(label: string): boolean { return this.collapsedDays.has(label); }

  toggleDay(label: string): void {
    if (this.collapsedDays.has(label)) {
      this.collapsedDays.delete(label);
    } else {
      this.collapsedDays.add(label);
    }
    try { this.cdr.detectChanges(); } catch {}
  }

  getTeamName(match: Match, side: 'home' | 'away'): string {
    const team = side === 'home' ? match.homeTeam : match.awayTeam;
    const ph = side === 'home' ? match.homePlaceholder : match.awayPlaceholder;
    return team?.name || ph || 'TBD';
  }

  canPredict(match: Match): boolean {
    return !match.predictionsLocked &&
      !match.isFinished &&
      !!match.homeTeam &&
      !!match.awayTeam &&
      new Date(match.matchDate).getTime() > Date.now();
  }

  isSaved(match: Match): boolean { return this.savedIds.has(match.id); }
  isSaving(match: Match): boolean { return this.savingIds.has(match.id); }

  adjustScore(match: Match, side: 'home' | 'away', delta: number): void {
    if (side === 'home') {
      match.homePrediction = Math.max(0, (match.homePrediction ?? 0) + delta);
    } else {
      match.awayPrediction = Math.max(0, (match.awayPrediction ?? 0) + delta);
    }
    this.savedIds.delete(match.id);
    try { this.cdr.detectChanges(); } catch {}
  }

  savePrediction(match: Match): void {
    if (!this.canPredict(match) || this.savingIds.has(match.id)) return;

    this.savingIds.add(match.id);
    try { this.cdr.detectChanges(); } catch {}

    const data = {
      matchId: match.id,
      homeScorePrediction: match.homePrediction ?? 0,
      awayScorePrediction: match.awayPrediction ?? 0
    };

    this.predictionService.savePrediction(data).subscribe({
      next: () => {
        this.savingIds.delete(match.id);
        const wasNew = !this.savedIds.has(match.id);
        this.savedIds.add(match.id);
        if (wasNew) this.predictedCount++;
        match.myPrediction = {
          homeScorePrediction: data.homeScorePrediction,
          awayScorePrediction: data.awayScorePrediction,
          pointsEarned: 0
        };
        this.showToast('✓ Pronóstico guardado', 'success');
        try { this.cdr.detectChanges(); } catch {}
      },
      error: () => {
        this.savingIds.delete(match.id);
        this.showToast('Error al guardar', 'error');
        try { this.cdr.detectChanges(); } catch {}
      }
    });
  }

  get progressPercent(): number {
    return this.totalMatches > 0
      ? Math.round((this.predictedCount / this.totalMatches) * 100)
      : 0;
  }

  get pendingCount(): number {
    return this.totalMatches - this.predictedCount;
  }

  getDayRating(day: DayGroup): { label: string; cls: string } | null {
    const finished = day.matches.filter(m => m.isFinished && m.myPrediction);
    if (finished.length === 0) return null;
    const earned = finished.reduce((sum, m) => sum + (m.myPrediction?.pointsEarned ?? 0), 0);
    const max = finished.length * 3;
    if (earned === max)        return { label: '🧠 Genio de la predicción',       cls: 'rating-genius' };
    if (earned >= max / 2)    return { label: '👍 Bastante bien',                 cls: 'rating-good' };
    if (earned > 0)           return { label: '😐 Pudo estar mejor',              cls: 'rating-meh' };
    return                           { label: '💀 Pronósticos para el olvido',   cls: 'rating-bad' };
  }

  showToast(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    this.toastVisible = true;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => {
      this.toastVisible = false;
      try { this.cdr.detectChanges(); } catch {}
    }, 2500);
  }
}
