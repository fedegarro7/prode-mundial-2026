import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RoundExtraBonuses } from '../models/extra-bonus.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ExtraBonusService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/rankings`;

  getGroupExtraBonus(groupId: number, roundKey: string): Observable<RoundExtraBonuses> {
    return this.http.get<RoundExtraBonuses>(`${this.apiUrl}/group/${groupId}/extra-bonus/${roundKey}`);
  }
}
