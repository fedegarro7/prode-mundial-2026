import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

export interface RoundSummary {
  roundKey: string;
  roundLabel: string;
  basePoints: number;
  bombMatch: { matchId: number; homeTeam: string; awayTeam: string } | null;
  awards: { awardType: string; awardLabel: string; winners: string[]; pointsAwarded: number }[];
}

@Injectable({
  providedIn: 'root'
})
export class RankingService {

  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/rankings`;

  getRanking() {
    return this.http.get<any[]>(this.apiUrl);
  }

  getRoundSummary() {
    return this.http.get<RoundSummary>(`${this.apiUrl}/round-summary`);
  }
}
