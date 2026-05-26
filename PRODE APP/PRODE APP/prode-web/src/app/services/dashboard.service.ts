import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../environments/environment';
import { MyDashboard } from '../models/prediction.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {

  private http = inject(HttpClient);

  private apiUrl = `${environment.apiUrl}/dashboard`;

  getMine(): Observable<MyDashboard> {
    return this.http.get<MyDashboard>(`${this.apiUrl}/me`);
  }
}
