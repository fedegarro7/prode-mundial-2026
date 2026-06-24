import { ChangeDetectorRef, Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { catchError, of } from 'rxjs';

import { MechanicsService } from '../../services/mechanics.service';
import { RoundContext, RoundInfo } from '../../models/mechanics.model';

@Component({
  selector: 'app-mecanicas',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './mecanicas.component.html',
  styleUrls: ['./mecanicas.component.scss']
})
export class MecanicasComponent implements OnInit, OnDestroy {

  readonly mechanics = inject(MechanicsService);
  private cdr = inject(ChangeDetectorRef);

  loading = true;
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  /**
   * Polling window: starts once the last group match finishes.
   * 28 Jun 01:00 ART = 28 Jun 04:00 UTC (Argentina is UTC-3).
   * Runs every 2 min, stops automatically when all 16 R32 matches are confirmed.
   */
  private readonly POLL_START_UTC = new Date('2026-06-28T04:00:00Z').getTime();
  private readonly REFRESH_MS = 120_000;
  roundContext = signal<RoundContext | null>(null);
  activeRoundKey = signal<string>('');

  // ── Captain ──────────────────────────────────────────────────────────────
  selectedCaptainId: number | null = null;
  savingCaptain = false;

  // ── Per-round picks (keyed by roundKey) ──────────────────────────────────
  selectedGoldenGoal: Record<string, number | null> = {};
  selectedSharpShooter: Record<string, number | null> = {};
  oracleDraws: Record<string, number> = {};
  oraclePenalties: Record<string, number> = {};

  savingRound: Record<string, boolean> = {};

  // ── Toast ─────────────────────────────────────────────────────────────────
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';
  toastVisible = false;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadAll();
    this.scheduleRefresh();
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  /** Silent background refresh of round context — no loading spinner */
  private refreshContext(): void {
    this.mechanics.getRoundContext().pipe(catchError(() => of(null))).subscribe(ctx => {
      if (ctx) {
        this.roundContext.set(ctx);
        this.initSelections(ctx);
        if (!this.activeRoundKey()) {
          const firstUnlocked = ctx.rounds.find(r => !r.isLocked) ?? ctx.rounds[0];
          if (firstUnlocked) this.activeRoundKey.set(firstUnlocked.roundKey);
        }
        this.cdr.detectChanges();
        // Mission accomplished — all R32 matches confirmed, stop polling
        if (this.isR32FullyConfirmed(ctx)) {
          this.stopRefresh();
        }
      }
    });
  }

  /**
   * Only start polling after the last group match finishes (28 Jun 04:00 UTC).
   * No point querying the DB before that — groups haven't ended yet.
   */
  private scheduleRefresh(): void {
    if (Date.now() < this.POLL_START_UTC) return;
    // Already past the window — check if we even need to poll
    const ctx = this.roundContext();
    if (ctx && this.isR32FullyConfirmed(ctx)) return;
    this.refreshTimer = setInterval(() => this.refreshContext(), this.REFRESH_MS);
  }

  private stopRefresh(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  /** All 16 R32 matches confirmed = dropdowns are ready = no more polling needed */
  private isR32FullyConfirmed(ctx: RoundContext): boolean {
    const r32 = ctx.rounds.find(r => r.roundKey === 'ROUND_OF_32');
    return (r32?.matches.length ?? 0) >= 16;
  }

  private loadAll(): void {
    this.loading = true;
    forkJoin({
      state: this.mechanics.load(),
      context: this.mechanics.getRoundContext().pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ context }) => {
        // state is set via tap inside mechanics.load()
        this.selectedCaptainId = this.mechanics.state()?.captain?.teamId ?? null;
        if (context) {
          this.roundContext.set(context);
          this.initSelections(context);
          const firstUnlocked = context.rounds.find(r => !r.isLocked) ?? context.rounds[0];
          if (firstUnlocked) this.activeRoundKey.set(firstUnlocked.roundKey);
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private initSelections(ctx: RoundContext): void {
    const state = this.mechanics.state();
    // captain is already initialized in loadAll() from state directly

    for (const round of ctx.rounds) {
      const rk = round.roundKey;

      const gg = state?.goldenGoals.find(g => g.roundKey === rk);
      this.selectedGoldenGoal[rk] = gg?.matchId ?? null;

      const ss = state?.sharpShooters.find(s => s.roundKey === rk);
      this.selectedSharpShooter[rk] = ss?.matchId ?? null;

      const oracle = state?.oraclePredictions.find(o => o.roundKey === rk);
      this.oracleDraws[rk] = oracle?.drawsAfterNinetyPrediction ?? 0;
      this.oraclePenalties[rk] = oracle?.penaltyShootoutsPrediction ?? 0;
    }
  }

  selectRound(roundKey: string): void {
    this.activeRoundKey.set(roundKey);
  }

  get activeRound(): RoundInfo | null {
    return this.roundContext()?.rounds.find(r => r.roundKey === this.activeRoundKey()) ?? null;
  }

  // ── Captain ──────────────────────────────────────────────────────────────
  get captainLocked(): boolean {
    return this.roundContext()?.isCaptainLocked ?? false;
  }

  get captainTeamName(): string | null {
    return this.mechanics.state()?.captain?.teamName ?? null;
  }

  saveCaptain(): void {
    if (!this.selectedCaptainId) return;
    this.savingCaptain = true;
    this.mechanics.selectCaptain(this.selectedCaptainId).subscribe({
      next: () => {
        this.showToast('¡Capitán guardado! 🦅', 'success');
        this.savingCaptain = false;
      },
      error: (err) => {
        this.showToast(err.error?.message ?? 'Error al guardar capitán', 'error');
        this.savingCaptain = false;
      }
    });
  }

  // ── Gol de Oro ───────────────────────────────────────────────────────────
  saveGoldenGoal(roundKey: string): void {
    const matchId = this.selectedGoldenGoal[roundKey];
    if (!matchId) return;
    this.savingRound[roundKey + '_gg'] = true;
    this.mechanics.selectGoldenGoal(matchId).subscribe({
      next: () => {
        this.showToast('¡Gol de Oro guardado! ⚽🏅', 'success');
        this.savingRound[roundKey + '_gg'] = false;
      },
      error: (err) => {
        this.showToast(err.error?.message ?? 'Error al guardar Gol de Oro', 'error');
        this.savingRound[roundKey + '_gg'] = false;
      }
    });
  }

  goldenGoalSaved(roundKey: string): boolean {
    const savedId = this.mechanics.goldenGoalMatchFor(roundKey);
    return savedId !== null && savedId === this.selectedGoldenGoal[roundKey];
  }

  // ── Francotirador ────────────────────────────────────────────────────────
  saveSharpShooter(roundKey: string): void {
    const matchId = this.selectedSharpShooter[roundKey];
    if (!matchId) return;
    this.savingRound[roundKey + '_ss'] = true;
    this.mechanics.selectSharpShooter(matchId).subscribe({
      next: () => {
        this.showToast('¡Francotirador guardado! 🎯', 'success');
        this.savingRound[roundKey + '_ss'] = false;
      },
      error: (err) => {
        this.showToast(err.error?.message ?? 'Error al guardar Francotirador', 'error');
        this.savingRound[roundKey + '_ss'] = false;
      }
    });
  }

  sharpShooterSaved(roundKey: string): boolean {
    const savedId = this.mechanics.sharpShooterMatchFor(roundKey);
    return savedId !== null && savedId === this.selectedSharpShooter[roundKey];
  }

  // ── Oráculo ───────────────────────────────────────────────────────────────
  getOracleDraws(rk: string): number { return this.oracleDraws[rk] ?? 0; }
  getOraclePenalties(rk: string): number { return this.oraclePenalties[rk] ?? 0; }

  incrementDraws(rk: string, max: number): void {
    const cur = this.oracleDraws[rk] ?? 0;
    if (cur < max) this.oracleDraws[rk] = cur + 1;
  }

  decrementDraws(rk: string): void {
    const cur = this.oracleDraws[rk] ?? 0;
    if (cur > 0) this.oracleDraws[rk] = cur - 1;
  }

  incrementPenalties(rk: string, max: number): void {
    const cur = this.oraclePenalties[rk] ?? 0;
    if (cur < max) this.oraclePenalties[rk] = cur + 1;
  }

  decrementPenalties(rk: string): void {
    const cur = this.oraclePenalties[rk] ?? 0;
    if (cur > 0) this.oraclePenalties[rk] = cur - 1;
  }

  oracleSaved(roundKey: string): boolean {
    const oracle = this.mechanics.state()?.oraclePredictions.find(o => o.roundKey === roundKey);
    return oracle != null
      && oracle.drawsAfterNinetyPrediction === (this.oracleDraws[roundKey] ?? 0)
      && oracle.penaltyShootoutsPrediction === (this.oraclePenalties[roundKey] ?? 0);
  }

  saveOracle(roundKey: string, matchCount: number): void {
    const draws = this.oracleDraws[roundKey] ?? 0;
    const penalties = this.oraclePenalties[roundKey] ?? 0;
    if (draws > matchCount || penalties > matchCount) return;
    this.savingRound[roundKey + '_oracle'] = true;
    this.mechanics.submitOracle(roundKey, draws, penalties).subscribe({
      next: () => {
        this.showToast('¡Oráculo guardado! 🔮', 'success');
        this.savingRound[roundKey + '_oracle'] = false;
      },
      error: (err) => {
        this.showToast(err.error?.message ?? 'Error al guardar Oráculo', 'error');
        this.savingRound[roundKey + '_oracle'] = false;
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  formatDate(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleString('es-AR', {
      weekday: 'short', day: '2-digit', month: 'short',
      hour: '2-digit', minute: '2-digit', timeZone: 'America/Argentina/Buenos_Aires'
    });
  }

  trackByRoundKey(_: number, r: RoundInfo): string { return r.roundKey; }

  private showToast(message: string, type: 'success' | 'error'): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastMessage = message;
    this.toastType = type;
    this.toastVisible = true;
    this.toastTimer = setTimeout(() => { this.toastVisible = false; }, 3500);
  }
}
