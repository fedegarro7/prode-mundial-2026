import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * NavigationService
 * Simple event bus to notify components about navigation clicks
 */
@Injectable({ providedIn: 'root' })
export class NavigationService {

  private navSubject = new Subject<string>();

  nav$ = this.navSubject.asObservable();

  notify(route: string) {
    this.navSubject.next(route);
  }

}
