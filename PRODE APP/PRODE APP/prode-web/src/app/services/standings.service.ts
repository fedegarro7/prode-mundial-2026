import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GroupStanding } from '../models/standing.model';
import { environment } from '../../environments/environment';

export interface StandingsResponse {
  standings: GroupStanding[];
  hasActiveMatches: boolean;
}

/**
 * Service for fetching group standings computed from match results.
 */
@Injectable({ providedIn: 'root' })
export class StandingsService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/standings`;

  /**
   * Returns all group standings plus a flag indicating if any match is currently live.
   * No authentication is required — standings are public.
   */
  getStandings(): Observable<StandingsResponse> {
    return this.http.get<StandingsResponse>(this.apiUrl);
  }
}
