import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { MechanicsState, RoundContext } from '../models/mechanics.model';

@Injectable({ providedIn: 'root' })
export class MechanicsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mechanics`;

  state = signal<MechanicsState | null>(null);

  load() {
    return this.http.get<MechanicsState>(this.base).pipe(
      tap(s => this.state.set(s))
    );
  }

  selectCaptain(teamId: number) {
    return this.http.post(this.base + '/captain', { teamId }).pipe(
      tap(() => this.load().subscribe())
    );
  }

  selectGoldenGoal(matchId: number) {
    return this.http.post(this.base + '/golden-goal', { matchId }).pipe(
      tap(() => this.load().subscribe())
    );
  }

  selectSharpShooter(matchId: number) {
    return this.http.post(this.base + '/sharp-shooter', { matchId }).pipe(
      tap(() => this.load().subscribe())
    );
  }

  submitOracle(roundKey: string, draws: number, penalties: number) {
    return this.http.post(this.base + '/oracle', {
      roundKey,
      drawsAfterNinetyPrediction: draws,
      penaltyShootoutsPrediction: penalties,
    }).pipe(tap(() => this.load().subscribe()));
  }

  /** Whether the user has used golden goal for a given round */
  hasGoldenGoalFor(roundKey: string): boolean {
    return this.state()?.goldenGoals.some(g => g.roundKey === roundKey) ?? false;
  }

  /** Which matchId has golden goal for this round (or null) */
  goldenGoalMatchFor(roundKey: string): number | null {
    return this.state()?.goldenGoals.find(g => g.roundKey === roundKey)?.matchId ?? null;
  }

  /** Whether the user has a sharpshooter pick for a given round */
  hasSharpShooterFor(roundKey: string): boolean {
    return this.state()?.sharpShooters.some(s => s.roundKey === roundKey) ?? false;
  }

  sharpShooterMatchFor(roundKey: string): number | null {
    return this.state()?.sharpShooters.find(s => s.roundKey === roundKey)?.matchId ?? null;
  }

  hasOracleFor(roundKey: string): boolean {
    return this.state()?.oraclePredictions.some(o => o.roundKey === roundKey) ?? false;
  }

  getRoundContext() {
    return this.http.get<RoundContext>(this.base + '/round-context');
  }
}
