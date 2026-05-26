import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RankingService {

  private http = inject(HttpClient);

  private apiUrl =
    `${environment.apiUrl}/rankings`;

  getRanking() {

    return this.http.get<any[]>(
      this.apiUrl
    );
  }
}
