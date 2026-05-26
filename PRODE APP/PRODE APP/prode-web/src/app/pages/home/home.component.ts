import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

import { MyDashboard, PendingPrediction, PredictionHistory } from '../../models/prediction.model';
import { AuthService } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { PredictionService } from '../../services/prediction.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {

  private auth = inject(AuthService);
  private dashboardService = inject(DashboardService);
  private predictionService = inject(PredictionService);

  user = this.auth.getUser();
  dashboard: MyDashboard | null = null;
  pending: PendingPrediction[] = [];
  history: PredictionHistory[] = [];
  loading = false;

  readonly rules = [
    { points: 3, label: 'Resultado exacto', detail: 'Acertaste goles de ambos equipos.' },
    { points: 1, label: 'Signo correcto', detail: 'Acertaste ganador o empate.' },
    { points: 0, label: 'Sin acierto', detail: 'El partido fue para otro lado.' }
  ];

  ngOnInit(): void {
    if (!this.auth.isLoggedIn()) return;

    this.loading = true;

    this.dashboardService.getMine().subscribe({
      next: (res) => {
        this.dashboard = res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });

    this.predictionService.getPending(5).subscribe({
      next: (res) => this.pending = res,
      error: () => this.pending = []
    });

    this.predictionService.getHistory(5).subscribe({
      next: (res) => this.history = res,
      error: () => this.history = []
    });
  }

  teamName(match: PendingPrediction | PredictionHistory, side: 'home' | 'away'): string {
    const team = side === 'home' ? match.homeTeam : match.awayTeam;
    const placeholder = side === 'home' ? match.homePlaceholder : match.awayPlaceholder;
    return team?.name || placeholder || 'Por definir';
  }

  teamCode(match: PendingPrediction | PredictionHistory, side: 'home' | 'away'): string {
    const team = side === 'home' ? match.homeTeam : match.awayTeam;
    const placeholder = side === 'home' ? match.homePlaceholder : match.awayPlaceholder;
    return team?.code || placeholder || 'TBD';
  }
}
