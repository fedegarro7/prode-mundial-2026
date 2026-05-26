import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { RankingService } from '../../services/ranking.service';

@Component({
  selector: 'app-rankings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rankings.component.html',
  styleUrls: ['./rankings.component.scss']
})
export class RankingsComponent implements OnInit {

  private rankingService = inject(RankingService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  rankings: any[] = [];
  loading = true;

  ngOnInit(): void {
    this.loadRankings();

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe((e) => {
        if (e.urlAfterRedirects === '/rankings') {
          this.loadRankings();
        }
      });
  }

  loadRankings(): void {
    this.loading = true;

    this.rankingService.getRanking().subscribe({
      next: (response) => {
        this.rankings = response;
        this.loading = false;
        try { this.cdr.detectChanges(); } catch {}
      },
      error: () => {
        this.loading = false;
        try { this.cdr.detectChanges(); } catch {}
      }
    });
  }
}
