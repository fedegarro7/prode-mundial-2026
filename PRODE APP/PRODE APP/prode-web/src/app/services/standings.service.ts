import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GroupStanding } from '../models/standing.model';
import { environment } from '../../environments/environment';

/**
 * Service for fetching group standings computed from match results.
 */
@Injectable({ providedIn: 'root' })
export class StandingsService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/standings`;

  /**
   * Returns all group standings.
   * No authentication is required — standings are public.
   */
  getStandings(): Observable<GroupStanding[]> {
    return this.http.get<GroupStanding[]>(this.apiUrl);
  }
}
