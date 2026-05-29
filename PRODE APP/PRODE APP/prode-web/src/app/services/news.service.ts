import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface NewsItem {
  title: string;
  description: string;
  link: string;
  source: string;
  sourceUrl: string;
  sourceColor: string;
  publishedAt: string;
  imageUrl: string | null;
}

// Portal metadata for the UI chips (matches backend sources order)
const PORTALS = [
  { name: 'Olé',        url: 'https://www.ole.com.ar',          color: '#E8000D' },
  { name: 'TyC Sports', url: 'https://www.tycsports.com',        color: '#0057A8' },
  { name: 'Infobae',    url: 'https://www.infobae.com/deportes', color: '#E40000' },
  { name: 'AS',         url: 'https://argentina.as.com',         color: '#D0021B' },
  { name: 'Marca',      url: 'https://www.marca.com',            color: '#F5A623' },
];

@Injectable({ providedIn: 'root' })
export class NewsService {
  private http = inject(HttpClient);

  readonly portals = PORTALS;

  getNews(): Observable<NewsItem[]> {
    return this.http.get<NewsItem[]>(`${environment.apiUrl}/news`);
  }
}

