import { Component, OnInit, OnDestroy, signal, inject, afterNextRender } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NewsService, NewsItem } from '../../services/news.service';
import { environment } from '../../../environments/environment';

export interface StatCard {
  icon: string;
  category: string;
  value: string;
  label: string;
  detail: string;
  accent: string;
}

const STATS: StatCard[] = [
  { icon: '🌍', category: 'Historia',      value: '23',        label: 'Ediciones del Mundial',           detail: 'Desde Uruguay 1930 hasta el próximo USA-Canadá-México 2026', accent: '#d4af37' },
  { icon: '🏆', category: 'Campeones',     value: 'Brasil',    label: 'Selección más ganadora',          detail: '5 copas del mundo: 1958, 1962, 1970, 1994 y 2002', accent: '#22C55E' },
  { icon: '⚽', category: 'Goleadores',    value: '18',        label: 'Goles de Lionel Andres Messi',         detail: 'Máximo goleador histórico de los mundiales en 6 torneos (2006-2026)', accent: '#3B82F6' },
  { icon: '🥇', category: 'Actualidad',    value: 'Argentina', label: 'Actual campeona del mundo',       detail: 'Tercer título en Qatar 2022, capitaneada por Lionel Messi', accent: '#60A5FA' },
  { icon: '📊', category: 'Récords',       value: '5.38',      label: 'Goles por partido (récord)',      detail: 'Suiza 1954: 140 goles en 26 partidos — el mundial más goleador de la historia', accent: '#F97316' },
  { icon: '👑', category: 'Leyendas',      value: 'Messi',     label: '6 mundiales jugados',             detail: '13 goles en 26 partidos — uno de los máximos goleadores históricos', accent: '#FACC15' },
  { icon: '🌎', category: 'Mundial 2026',  value: '48',        label: 'Selecciones en USA 2026',         detail: 'El torneo más grande de la historia con 104 partidos en 3 países', accent: '#8B5CF6' },
  { icon: '🎯', category: 'Récords',       value: '13',        label: 'Goles de Just Fontaine (1958)',   detail: 'Francia 1958 — récord en un solo torneo que nadie pudo superar en 68 años', accent: '#EF4444' },
  { icon: '🏟️', category: 'Historia',      value: '1930',      label: 'El primer Mundial',               detail: 'Uruguay organizó y ganó la primera Copa del Mundo con 13 selecciones', accent: '#6366F1' },
  { icon: '💫', category: 'Leyendas',      value: 'Maradona',  label: 'El gol del siglo',                detail: 'Diego Maradona anotó el mejor gol de la historia ante Inglaterra en México 1986', accent: '#F59E0B' },
  { icon: '🇩🇪', category: 'Campeones',    value: '4',         label: 'Títulos de Alemania',             detail: 'Campeón en 1954, 1974, 1990 y 2014 — una de las dos naciones con 4 copas', accent: '#9CA3AF' },
  { icon: '⚡', category: 'Goleadores',    value: 'Mbappé',    label: '12 goles en 3 mundiales',         detail: 'Con 27 años, ya es el séptimo máximo goleador histórico de la Copa del Mundo', accent: '#1D4ED8' },
  { icon: '📈', category: 'Récords',       value: '172',       label: 'Goles en Qatar 2022',             detail: 'Récord absoluto de goles en un solo torneo, con promedio de 2.69 por partido', accent: '#059669' },
  { icon: '🏅', category: 'Curiosidades',  value: '8',         label: 'Países campeones del mundo',      detail: 'Solo Brasil, Alemania, Italia, Argentina, Francia, Uruguay, España e Inglaterra', accent: '#DC2626' },
  { icon: '🦸', category: 'Leyendas',      value: 'Pelé',      label: 'Tres mundiales ganados',          detail: 'Único jugador en ganar 3 Copas del Mundo (1958, 1962, 1970) con Brasil', accent: '#FBBF24' },
];

@Component({
  selector: 'app-news',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './news.component.html',
  styleUrls: ['./news.component.scss']
})
export class NewsComponent implements OnInit, OnDestroy {
  private svc = inject(NewsService);
  private readonly apiBaseUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  news     = signal<NewsItem[]>([]);
  loading  = signal(true);
  error    = signal<string | null>(null);
  readonly portals      = this.svc.portals;
  readonly skeletons    = [1, 2, 3, 4, 5, 6];
  readonly brokenImages = new Set<string>();
  installGuideOpen      = signal(false);

  readonly stats       = STATS;
  currentStat          = signal(0);
  private carouselTimer?: ReturnType<typeof setInterval>;
  private touchStartX: number | null = null;
  private touchStartY: number | null = null;

  constructor() {
    afterNextRender(() => {
      this.startCarousel();
    });
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    clearInterval(this.carouselTimer);
  }

  prevStat(): void {
    this.currentStat.update(i => (i - 1 + STATS.length) % STATS.length);
    this.resetCarousel();
  }

  nextStat(): void {
    this.currentStat.update(i => (i + 1) % STATS.length);
    this.resetCarousel();
  }

  goToStat(index: number): void {
    this.currentStat.set(index);
    this.resetCarousel();
  }

  onCarouselTouchStart(event: TouchEvent): void {
    const touch = event.touches[0];
    this.touchStartX = touch.clientX;
    this.touchStartY = touch.clientY;
  }

  onCarouselTouchEnd(event: TouchEvent): void {
    if (this.touchStartX === null || this.touchStartY === null) return;

    const touch = event.changedTouches[0];
    const deltaX = touch.clientX - this.touchStartX;
    const deltaY = touch.clientY - this.touchStartY;

    // Trigger swipe only for intentional horizontal gestures.
    if (Math.abs(deltaX) > 40 && Math.abs(deltaX) > Math.abs(deltaY)) {
      if (deltaX < 0) this.nextStat();
      else this.prevStat();
    }

    this.touchStartX = null;
    this.touchStartY = null;
  }

  private startCarousel(): void {
    this.carouselTimer = setInterval(() => {
      this.currentStat.update(i => (i + 1) % STATS.length);
    }, 4500);
  }

  private resetCarousel(): void {
    clearInterval(this.carouselTimer);
    this.startCarousel();
  }

  onImageError(url: string): void {
    this.brokenImages.add(url);
  }

  toggleInstallGuide(): void {
    this.installGuideOpen.update((value) => !value);
  }

  resolveImageUrl(url: string): string {
    if (url.startsWith('/api/')) {
      return `${this.apiBaseUrl}${url}`;
    }

    return url;
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getNews().subscribe({
      next: (items) => { this.news.set(items); this.loading.set(false); },
      error: () => { this.error.set('No se pudieron cargar las noticias.'); this.loading.set(false); }
    });
  }

  timeAgo(dateStr: string): string {
    const diff = (Date.now() - new Date(dateStr).getTime()) / 1000;
    if (diff < 120)    return 'hace un momento';
    if (diff < 3600)   return `hace ${Math.floor(diff / 60)} min`;
    if (diff < 86400)  return `hace ${Math.floor(diff / 3600)}h`;
    if (diff < 172800) return 'ayer';
    return `hace ${Math.floor(diff / 86400)} días`;
  }

  placeholderGradient(item: NewsItem): string {
    return `linear-gradient(135deg, ${item.sourceColor}33 0%, rgba(2,8,24,0.8) 100%)`;
  }
}

