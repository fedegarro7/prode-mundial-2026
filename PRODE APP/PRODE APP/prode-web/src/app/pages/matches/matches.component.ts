import { ChangeDetectorRef, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { Match } from '../../models/match.model';
import { MatchService } from '../../services/match.service';
import { PredictionService } from '../../services/prediction.service';
import { EsNamePipe } from '../../pipes/es-name.pipe';

interface MatchTab {
  key: string;
  label: string;
  shortLabel: string;
  type: 'group' | 'knockout';
  hasArgentina: boolean;
}

@Component({
  selector: 'app-matches',
  standalone: true,
  imports: [CommonModule, EsNamePipe],
  templateUrl: './matches.component.html',
  styleUrls: ['./matches.component.scss']
})
export class MatchesComponent implements OnInit, OnDestroy {

  private matchService = inject(MatchService);
  private predictionService = inject(PredictionService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  loading = true;
  activePhase: 'groups' | 'knockout' = 'groups';
  activeGroup = '';
  expandedPhase = '';

  groupTabs: MatchTab[] = [];
  knockoutPhases: MatchTab[] = [];
  groupMatches = new Map<string, Match[]>();
  knockoutMatches = new Map<string, Match[]>();

  savedIds = new Set<number>();
  savingIds = new Set<number>();
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';
  toastVisible = false;

  private toastTimer: ReturnType<typeof setTimeout> | null = null;
  readonly skeletons = [1, 2, 3, 4, 5, 6];

  private readonly GROUP_ORDER = [
    'Group A', 'Group B', 'Group C', 'Group D',
    'Group E', 'Group F', 'Group G', 'Group H',
    'Group I', 'Group J', 'Group K', 'Group L',
    'Group M', 'Group N', 'Group O', 'Group P'
  ];

  private readonly KNOCKOUT_ORDER = [
    'Round of 16', 'Quarter-finals',
    'Semi-finals', 'Match for third place', 'Final'
  ];

  private readonly KNOCKOUT_LABELS: Record<string, string> = {
    'Round of 16': 'Octavos de Final',
    'Quarter-finals': 'Cuartos de Final',
    'Semi-finals': 'Semifinales',
    'Match for third place': '3er y 4to Puesto',
    'Final': 'Gran Final 🏆'
  };

  ngOnInit(): void {
    this.loadMatches();
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => {
        if (e.urlAfterRedirects === '/matches') this.loadMatches();
      });
  }

  ngOnDestroy(): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  loadMatches(): void {
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
          if (m.myPrediction) this.savedIds.add(m.id);
        }

        this.buildGroups(mapped);
        this.loading = false;
        try { this.cdr.detectChanges(); } catch { /* SSR */ }
      },
      error: () => { this.loading = false; }
    });
  }

  private buildGroups(matches: Match[]): void {
    this.groupMatches.clear();
    this.knockoutMatches.clear();

    for (const m of matches) {
      const isGroup = !!(m.groupName?.trim());
      const key = isGroup ? m.groupName : (m.stage || 'Final');
      const map = isGroup ? this.groupMatches : this.knockoutMatches;
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(m);
    }

    this.groupTabs = [...this.groupMatches.keys()]
      .sort((a, b) => {
        const ia = this.GROUP_ORDER.indexOf(a);
        const ib = this.GROUP_ORDER.indexOf(b);
        return (ia < 0 ? 99 : ia) - (ib < 0 ? 99 : ib);
      })
      .map(k => ({
        key: k, label: k,
        shortLabel: k.replace('Group ', ''),
        type: 'group' as const,
        hasArgentina: this.groupMatches.get(k)!.some(m => this.isArgentina(m))
      }));

    this.knockoutPhases = [...this.knockoutMatches.keys()]
      .sort((a, b) => {
        const ia = this.KNOCKOUT_ORDER.indexOf(a);
        const ib = this.KNOCKOUT_ORDER.indexOf(b);
        return (ia < 0 ? 99 : ia) - (ib < 0 ? 99 : ib);
      })
      .map(k => ({
        key: k,
        label: this.KNOCKOUT_LABELS[k] || k,
        shortLabel: this.KNOCKOUT_LABELS[k] || k,
        type: 'knockout' as const,
        hasArgentina: (this.knockoutMatches.get(k) || []).some(m => this.isArgentina(m))
      }));

    const now = Date.now();

    // Default group: the one with the nearest upcoming match.
    // Falls back to the most recently-played group if all are done.
    if (!this.activeGroup && this.groupTabs.length) {
      let bestKey = '';
      let bestTs = Infinity;
      let latestDoneKey = '';
      let latestDoneTs = -Infinity;

      for (const tab of this.groupTabs) {
        for (const m of this.groupMatches.get(tab.key) ?? []) {
          const ts = new Date(m.matchDate).getTime();
          if (!m.isFinished && ts >= now && ts < bestTs) {
            bestTs = ts; bestKey = tab.key;
          }
          if (m.isFinished && ts > latestDoneTs) {
            latestDoneTs = ts; latestDoneKey = tab.key;
          }
        }
      }
      this.activeGroup = bestKey || latestDoneKey || this.groupTabs[0].key;
    }

    // Default knockout phase: nearest upcoming phase, else first.
    if (!this.expandedPhase && this.knockoutPhases.length) {
      let bestKey = '';
      let bestTs = Infinity;
      for (const phase of this.knockoutPhases) {
        for (const m of this.knockoutMatches.get(phase.key) ?? []) {
          const ts = new Date(m.matchDate).getTime();
          if (!m.isFinished && ts >= now && ts < bestTs) {
            bestTs = ts; bestKey = phase.key;
          }
        }
      }
      this.expandedPhase = bestKey || this.knockoutPhases[0].key;
    }

    // Auto-switch to knockout tab if all group matches are finished.
    const allGroupsDone = [...this.groupMatches.values()]
      .flat()
      .every(m => m.isFinished);
    if (allGroupsDone && this.knockoutPhases.length) {
      this.activePhase = 'knockout';
    }
  }

  get visibleGroupMatches(): Match[] {
    return this.groupMatches.get(this.activeGroup) ?? [];
  }

  setPhase(phase: 'groups' | 'knockout'): void {
    this.activePhase = phase;
    try { this.cdr.detectChanges(); } catch { /* SSR */ }
  }

  setGroup(key: string): void {
    this.activeGroup = key;
    try { this.cdr.detectChanges(); } catch { /* SSR */ }
  }

  toggleKnockout(key: string): void {
    this.expandedPhase = this.expandedPhase === key ? '' : key;
    try { this.cdr.detectChanges(); } catch { /* SSR */ }
  }

  isArgentina(match: Match): boolean {
    return (
      match.homeTeam?.name?.toLowerCase().includes('argentina') ||
      match.awayTeam?.name?.toLowerCase().includes('argentina') ||
      false
    );
  }

  isArgentineTeam(match: Match, side: 'home' | 'away'): boolean {
    const team = side === 'home' ? match.homeTeam : match.awayTeam;
    return team?.name?.toLowerCase().includes('argentina') ?? false;
  }

  isUnknownKnockout(match: Match): boolean {
    return !match.homeTeam && !match.awayTeam;
  }

  canPredict(match: Match): boolean {
    return !this.isMatchLocked(match);
  }

  isMatchLocked(match: Match): boolean {
    return match.predictionsLocked ||
      match.isFinished ||
      !match.homeTeam ||
      !match.awayTeam ||
      new Date(match.matchDate).getTime() <= Date.now();
  }

  lockReason(match: Match): string {
    if (!match.homeTeam || !match.awayTeam) return 'Equipos por definir';
    if (match.isFinished) return 'Partido finalizado';
    return 'Pronosticos cerrados';
  }

  closingLabel(match: Match): string {
    const diff = new Date(match.matchDate).getTime() - Date.now();
    if (diff <= 0) return 'Cerrado';

    const hours = Math.floor(diff / 36e5);
    const minutes = Math.floor((diff % 36e5) / 6e4);

    if (hours >= 24) return `Cierra en ${Math.floor(hours / 24)}d`;
    if (hours >= 1) return `Cierra en ${hours}h ${minutes}m`;
    return `Cierra en ${minutes}m`;
  }

  isSaved(match: Match): boolean {
    return this.savedIds.has(match.id);
  }

  isSaving(match: Match): boolean {
    return this.savingIds.has(match.id);
  }

  adjustScore(match: Match, side: 'home' | 'away', delta: number): void {
    if (side === 'home') {
      match.homePrediction = Math.max(0, (match.homePrediction ?? 0) + delta);
    } else {
      match.awayPrediction = Math.max(0, (match.awayPrediction ?? 0) + delta);
    }
    this.savedIds.delete(match.id);
    try { this.cdr.detectChanges(); } catch { /* SSR */ }
  }

  savePrediction(match: Match): void {
    if (!this.canPredict(match) || this.savingIds.has(match.id)) return;

    this.savingIds.add(match.id);
    try { this.cdr.detectChanges(); } catch { /* SSR */ }

    const data = {
      matchId: match.id,
      homeScorePrediction: match.homePrediction ?? 0,
      awayScorePrediction: match.awayPrediction ?? 0
    };

    this.predictionService.savePrediction(data).subscribe({
      next: () => {
        this.savingIds.delete(match.id);
        this.savedIds.add(match.id);
        match.myPrediction = {
          homeScorePrediction: data.homeScorePrediction,
          awayScorePrediction: data.awayScorePrediction,
          pointsEarned: 0
        };
        this.showToast('✓ Pronóstico guardado', 'success');
        try { this.cdr.detectChanges(); } catch { /* SSR */ }
      },
      error: () => {
        this.savingIds.delete(match.id);
        this.showToast('Error al guardar el pronóstico', 'error');
        try { this.cdr.detectChanges(); } catch { /* SSR */ }
      }
    });
  }

  showToast(message: string, type: 'success' | 'error'): void {
    this.toastMessage = message;
    this.toastType = type;
    this.toastVisible = true;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => {
      this.toastVisible = false;
      try { this.cdr.detectChanges(); } catch { /* SSR */ }
    }, 2500);
    try { this.cdr.detectChanges(); } catch { /* SSR */ }
  }

  getTeamName(match: Match, side: 'home' | 'away'): string {
    const team = side === 'home' ? match.homeTeam : match.awayTeam;
    const ph = side === 'home' ? match.homePlaceholder : match.awayPlaceholder;
    return team?.name || ph || 'TBD';
  }

  getTeamCode(match: Match, side: 'home' | 'away'): string {
    const ph = side === 'home' ? match.homePlaceholder : match.awayPlaceholder;
    return ph || '?';
  }
}
