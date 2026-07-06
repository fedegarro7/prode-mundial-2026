import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RoundExtraBonuses } from '../models/extra-bonus.model';

@Injectable({
  providedIn: 'root'
})
export class ExtraBonusService {
  private http = inject(HttpClient);

  getGroupExtraBonus(groupId: number, roundKey: string): Observable<RoundExtraBonuses> {
    return this.http.get<RoundExtraBonuses>(`/api/rankings/group/${groupId}/extra-bonus/${roundKey}`);
  }
}
