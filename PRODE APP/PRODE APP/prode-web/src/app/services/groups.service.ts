import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Group, GroupRanking, JoinRequest, AdminGroup } from '../models/group.model';
import { environment } from '../../environments/environment';

/** Service for managing private prediction groups. */
@Injectable({ providedIn: 'root' })
export class GroupsService {

  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/groups`;

  /** Total pending join requests across all owned groups. Used by navbar badge. */
  pendingCount = signal(0);

  /** Returns all groups the current user is involved with. */
  getMyGroups(): Observable<Group[]> {
    return this.http.get<Group[]>(this.base).pipe(
      tap(groups => {
        const total = groups
          .filter(g => g.isOwner)
          .reduce((sum, g) => sum + g.pendingRequestCount, 0);
        this.pendingCount.set(total);
      })
    );
  }

  /** Returns a single group by ID. */
  getById(id: number): Observable<Group> {
    return this.http.get<Group>(`${this.base}/${id}`);
  }

  /** Creates a new group. */
  create(name: string): Observable<Group> {
    return this.http.post<Group>(this.base, { name });
  }

  /** Sends a join request via invite code (status: Pending until owner approves). */
  join(inviteCode: string): Observable<{ message: string; groupName: string }> {
    return this.http.post<{ message: string; groupName: string }>(`${this.base}/join`, { inviteCode });
  }

  /** Returns pending join requests for a group (owner only). */
  getRequests(groupId: number): Observable<JoinRequest[]> {
    return this.http.get<JoinRequest[]>(`${this.base}/${groupId}/requests`);
  }

  /** Approves a pending join request (owner only). */
  approve(groupId: number, userId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${groupId}/requests/${userId}/approve`, {});
  }

  /** Rejects a pending join request (owner only). */
  reject(groupId: number, userId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${groupId}/requests/${userId}/reject`, {});
  }

  /** Returns the ranking for all approved members of a group. */
  getRankings(groupId: number): Observable<GroupRanking[]> {
    return this.http.get<GroupRanking[]>(`${this.base}/${groupId}/rankings`);
  }

  /** Leaves a group (non-owners only). */
  leave(groupId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${groupId}/leave`);
  }

  /** Deletes a group (owners only). */
  delete(groupId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${groupId}`);
  }

  /** Returns all groups with members. Admin only. */
  getAllGroupsAdmin(): Observable<AdminGroup[]> {
    return this.http.get<AdminGroup[]>(`${this.base}/admin/all`);
  }
}
