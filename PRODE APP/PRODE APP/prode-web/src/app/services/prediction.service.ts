import { HttpClient }
  from '@angular/common/http';

import {
  inject,
  Injectable
} from '@angular/core';

import { Observable }
  from 'rxjs';

import { Prediction }
  from '../models/prediction.model';

import {
  PendingPrediction,
  PredictionHistory
} from '../models/prediction.model';

import { environment }
  from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PredictionService {

  private http =
    inject(HttpClient);

  private apiUrl =
    `${environment.apiUrl}/predictions`;

  getMine():
    Observable<Prediction[]> {

    return this.http.get<Prediction[]>(
      `${this.apiUrl}/mine`
    );

  }

  savePrediction(data: any) {

    return this.http.post(
      this.apiUrl,
      data
    );

  }

  getPending(limit = 8):
    Observable<PendingPrediction[]> {

    return this.http.get<PendingPrediction[]>(
      `${this.apiUrl}/pending?limit=${limit}`
    );

  }

  getHistory(limit = 20):
    Observable<PredictionHistory[]> {

    return this.http.get<PredictionHistory[]>(
      `${this.apiUrl}/history?limit=${limit}`
    );

  }

}
