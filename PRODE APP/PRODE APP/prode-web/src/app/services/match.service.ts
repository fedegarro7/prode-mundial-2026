import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Match } from '../models/match.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MatchService {

  private http = inject(HttpClient);

  private apiUrl =
    `${environment.apiUrl}/matches`;

  getUpcomingMatches(): Observable<Match[]> {

    return this.http.get<Match[]>(
      `${this.apiUrl}/upcoming`
    );
  }
}
