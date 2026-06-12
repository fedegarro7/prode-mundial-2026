import {
  Component, OnInit, OnDestroy, inject, signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { GroupsService } from '../../services/groups.service';
import { Group, GroupRanking, JoinRequest, AdminGroup } from '../../models/group.model';
import { AuthService } from '../../services/auth.service';

type DetailTab = 'ranking' | 'requests' | 'invite';

/**
 * Groups page — accordion layout.
 * Each group is an expandable card; only one is open at a time.
 * Rankings and requests are loaded on demand when the card expands.
 */
@Component({
  selector: 'app-groups',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './groups.component.html',
  styleUrls: ['./groups.component.scss']
})
export class GroupsComponent implements OnInit, OnDestroy {

  private svc = inject(GroupsService);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  // ── Page state ──────────────────────────────────────────────────────────────
  groups   = signal<Group[]>([]);
  loading  = signal(true);
  error    = signal<string | null>(null);

  get isAdmin(): boolean { return !!this.auth.currentUser()?.isAdmin; }

  // ── Admin state ───────────────────────────────────────────────────────────
  adminGroups        = signal<AdminGroup[]>([]);
  adminLoading       = signal(false);
  adminError         = signal<string | null>(null);
  adminOpenGroupId   = signal<number | null>(null);

  get filteredAdminGroups(): AdminGroup[] {
    return this.adminGroups();
  }

  toggleAdminGroup(id: number): void {
    this.adminOpenGroupId.set(this.adminOpenGroupId() === id ? null : id);
  }

  // ── Accordion state ────────────────────────────────────────────────────────
  /** ID of the currently-open group accordion (null = all closed). */
  openGroupId = signal<number | null>(null);
  openTab     = signal<DetailTab>('ranking');

  /** Per-group cached data — loaded once per session per group. */
  rankingsMap = signal<Record<number, GroupRanking[]>>({});
  requestsMap = signal<Record<number, JoinRequest[]>>({});
  loadingMap  = signal<Record<number, boolean>>({});

  // ── Create form ────────────────────────────────────────────────────────────
  showCreateForm = signal(false);
  newGroupName = '';
  createLoading = signal(false);
  createError   = signal<string | null>(null);

  // ── Join form ──────────────────────────────────────────────────────────────
  showJoinForm = signal(false);
  joinCode = '';
  joinLoading = signal(false);
  joinMessage = signal<{ text: string; success: boolean } | null>(null);

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const inviteCode = this.route.snapshot.queryParamMap.get('join');
    if (inviteCode) {
      this.joinCode = inviteCode.toUpperCase();
      this.showJoinForm.set(true);
    }

    this.loadGroups();
    if (this.isAdmin) this.loadAdminGroups();
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  // ── Groups list ────────────────────────────────────────────────────────────

  loadGroups(): void {
    this.loading.set(true);
    this.error.set(null);
    this.svc.getMyGroups().pipe(takeUntil(this.destroy$)).subscribe({
      next: (gs) => { this.groups.set(gs); this.loading.set(false); },
      error: () => { this.error.set('No se pudieron cargar los grupos.'); this.loading.set(false); }
    });
  }

  loadAdminGroups(): void {
    this.adminLoading.set(true);
    this.adminError.set(null);
    this.svc.getAllGroupsAdmin().pipe(takeUntil(this.destroy$)).subscribe({
      next: (gs) => { this.adminGroups.set(gs); this.adminLoading.set(false); },
      error: () => { this.adminError.set('No se pudieron cargar todos los grupos.'); this.adminLoading.set(false); }
    });
  }

  // ── Accordion ──────────────────────────────────────────────────────────────

  toggleGroup(group: Group): void {
    if (this.openGroupId() === group.id) {
      this.openGroupId.set(null);   // collapse
    } else {
      this.openGroupId.set(group.id);
      this.openTab.set('ranking');
      this.ensureRankings(group.id, group);
    }
  }

  setTab(tab: DetailTab, group: Group): void {
    this.openTab.set(tab);
    if (tab === 'ranking')  this.ensureRankings(group.id, group);
    if (tab === 'requests') this.ensureRequests(group.id);
  }

  private setLoading(groupId: number, val: boolean): void {
    this.loadingMap.update(m => ({ ...m, [groupId]: val }));
  }

  private ensureRankings(groupId: number, group: Group): void {
    if (group.membershipStatus === 'Pending') return;   // can't fetch yet
    if (this.rankingsMap()[groupId]) return;            // already cached
    this.setLoading(groupId, true);
    this.svc.getRankings(groupId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (r) => { this.rankingsMap.update(m => ({ ...m, [groupId]: r })); this.setLoading(groupId, false); },
      error: () => { this.rankingsMap.update(m => ({ ...m, [groupId]: [] })); this.setLoading(groupId, false); }
    });
  }

  private ensureRequests(groupId: number): void {
    this.setLoading(groupId, true);
    this.svc.getRequests(groupId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (r) => { this.requestsMap.update(m => ({ ...m, [groupId]: r })); this.setLoading(groupId, false); },
      error: () => { this.requestsMap.update(m => ({ ...m, [groupId]: [] })); this.setLoading(groupId, false); }
    });
  }

  isLoading(groupId: number): boolean { return !!this.loadingMap()[groupId]; }
  rankingsFor(groupId: number): GroupRanking[] { return this.rankingsMap()[groupId] ?? []; }
  requestsFor(groupId: number): JoinRequest[] { return this.requestsMap()[groupId] ?? []; }

  // ── Create group ───────────────────────────────────────────────────────────

  submitCreate(): void {
    const name = this.newGroupName.trim();
    if (!name) { this.createError.set('Ingresá un nombre.'); return; }
    if (name.length > 50) { this.createError.set('Máximo 50 caracteres.'); return; }
    this.createLoading.set(true);
    this.createError.set(null);
    this.svc.create(name).pipe(takeUntil(this.destroy$)).subscribe({
      next: (g) => {
        this.groups.update(gs => [g, ...gs]);
        this.newGroupName = '';
        this.showCreateForm.set(false);
        this.createLoading.set(false);
        // Auto-expand the new group
        this.openGroupId.set(g.id);
        this.openTab.set('invite');
      },
      error: (err) => { this.createError.set(err?.error || 'No se pudo crear el grupo.'); this.createLoading.set(false); }
    });
  }

  // ── Join group ─────────────────────────────────────────────────────────────

  submitJoin(): void {
    const code = this.joinCode.trim().toUpperCase();
    if (code.length < 4) { this.joinMessage.set({ text: 'Ingresá un código válido.', success: false }); return; }
    this.joinLoading.set(true);
    this.joinMessage.set(null);
    this.svc.join(code).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.joinMessage.set({ text: `Solicitud enviada a "${res.groupName}". Esperá la aprobación del dueño.`, success: true });
        this.joinCode = '';
        this.joinLoading.set(false);
        this.loadGroups();
      },
      error: (err) => { this.joinMessage.set({ text: err?.error || 'No se pudo enviar la solicitud.', success: false }); this.joinLoading.set(false); }
    });
  }

  // ── Approve / Reject ───────────────────────────────────────────────────────

  approve(group: Group, userId: string): void {
    this.svc.approve(group.id, userId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.requestsMap.update(m => ({ ...m, [group.id]: (m[group.id] ?? []).filter(r => r.userId !== userId) }));
        this.rankingsMap.update(m => ({ ...m, [group.id]: undefined as any })); // invalidate cache
        this.groups.update(gs => gs.map(g => g.id === group.id ? { ...g, pendingRequestCount: g.pendingRequestCount - 1, memberCount: g.memberCount + 1 } : g));
      },
      error: () => alert('No se pudo aprobar la solicitud.')
    });
  }

  reject(group: Group, userId: string): void {
    this.svc.reject(group.id, userId).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.requestsMap.update(m => ({ ...m, [group.id]: (m[group.id] ?? []).filter(r => r.userId !== userId) }));
        this.groups.update(gs => gs.map(g => g.id === group.id ? { ...g, pendingRequestCount: g.pendingRequestCount - 1 } : g));
      },
      error: () => alert('No se pudo rechazar la solicitud.')
    });
  }

  // ── Leave / Delete ─────────────────────────────────────────────────────────

  leaveGroup(group: Group): void {
    if (!confirm(`¿Abandonar el grupo "${group.name}"?`)) return;
    this.svc.leave(group.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.groups.update(gs => gs.filter(g => g.id !== group.id)); if (this.openGroupId() === group.id) this.openGroupId.set(null); },
      error: (err) => alert(err?.error || 'No se pudo abandonar el grupo.')
    });
  }

  deleteGroup(group: Group): void {
    if (!confirm(`¿Eliminar el grupo "${group.name}"? Esta acción no se puede deshacer.`)) return;
    this.svc.delete(group.id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => { this.groups.update(gs => gs.filter(g => g.id !== group.id)); if (this.openGroupId() === group.id) this.openGroupId.set(null); },
      error: () => alert('No se pudo eliminar el grupo.')
    });
  }

  // ── WhatsApp share ─────────────────────────────────────────────────────────

  /**
   * Opens WhatsApp with a pre-filled invite message.
   * URL-encoded to prevent injection; opens in new tab with noopener for security.
   */
  shareWhatsApp(group: Group): void {
    const msg = encodeURIComponent(
      `¡Te invito a mi grupo "${group.name}" en Prode Mundial 2026! 🏆\n` +
      `Usá este link para sumarte: ${this.inviteLink(group)}\n` +
      `Código: *${group.inviteCode}*`
    );
    window.open(`https://wa.me/?text=${msg}`, '_blank', 'noopener,noreferrer');
  }

  copyCode(code: string): void { navigator.clipboard.writeText(code).catch(() => {}); }

  inviteLink(group: Group): string {
    return `${window.location.origin}/groups?join=${encodeURIComponent(group.inviteCode)}`;
  }

  copyInviteLink(group: Group): void {
    navigator.clipboard.writeText(this.inviteLink(group)).catch(() => {});
  }

  // ── Helpers for template ───────────────────────────────────────────────────
  trackById(_: number, item: { id: number }) { return item.id; }
  medalFor(pos: number): string { return pos === 1 ? '🥇' : pos === 2 ? '🥈' : pos === 3 ? '🥉' : String(pos); }

  /**
   * Calcula la posición real considerando empates.
   * Si múltiples jugadores tienen la misma puntuación, todos comparten la misma posición.
   * La siguiente posición se calcula como si todos los empatados ocuparan el mismo rango.
   * 
   * Ejemplo: [100, 100, 100, 95, 95, 90] → posiciones reales: [1, 1, 1, 4, 4, 6]
   */
  calculateRealPosition(entry: GroupRanking, rankings: GroupRanking[]): number {
    let realPos = 1;
    for (const ranking of rankings) {
      if (ranking.totalPoints > entry.totalPoints) {
        realPos++;
      }
    }
    return realPos;
  }

  /**
   * Determina si es la última posición (considerando empates).
   * Todos los jugadores con igual puntuación mínima son considerados últimos.
   */
  isLastPosition(entry: GroupRanking, rankings: GroupRanking[]): boolean {
    const minPoints = Math.min(...rankings.map(r => r.totalPoints));
    return entry.totalPoints === minPoints;
  }

  /**
   * Determina si es la primera posición (considerando empates).
   * Todos los jugadores con igual puntuación máxima son considerados primeros.
   */
  isFirstPosition(entry: GroupRanking, rankings: GroupRanking[]): boolean {
    const maxPoints = Math.max(...rankings.map(r => r.totalPoints));
    return entry.totalPoints === maxPoints;
  }

  titleFor(entry: GroupRanking, rankings: GroupRanking[]): string {
    if (rankings.length <= 1) return '';
    
    const realPos = this.calculateRealPosition(entry, rankings);
    const total = rankings.length;
    const isFirst = this.isFirstPosition(entry, rankings);
    const isLast = this.isLastPosition(entry, rankings);
    
    if (isFirst)                            return '👑 El Messi del grupo';
    if (realPos === 2 && total > 2)        return '🥈 Casi campeón';
    if (realPos === 3 && total > 3)        return '🥉 Tercer tiempo';
    if (realPos === 4 && total > 4)        return '😬 Casi podio';
    if (isLast && total > 3)               return '🪣 La madera del grupo';
    if (realPos === total - 1 && total > 4) return '😬 Penúltimo y con frío';
    return '';
  }
}

