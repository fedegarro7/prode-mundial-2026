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
 private readonly rankTitles = [
  // Muy cerca de la cima
  '🚀 Keep pushing',
  '⭐ Más cerca de la cima que del fondo',
  '🔥 Metele que se te van',
  '💪 Luchador digno',
  '🎯 Vas afinando la puntería',
  '⚡ A un paso de sorprender',
  '📈 En clara recuperación',
  '🥊 Nunca bajás los brazos',
  '🌱 Hay potencial escondido',
  '🚦 Todavía estás en carrera',
  '🤌 Surge valentía',
  '🕺 Fulbo champagne',
  '😎 Perfil bajo, puntos altos',
  '👀 La estás viendo',
  '📈 Paso a paso, como Mostaza',
  '🚶 Sin hacer ruido',
  '🛣️ Por la banquina, pero avanzando',
  '🧠 Hay método en la locura',
  '🎯 No será lindo, pero suma',
  '🥐 Facturando puntitos',
  '🚂 Despacito y sin frenos',

  // Mitad superior
  '🧩 Te falta una pieza',
  '🛠️ Ajustando la estrategia',
  '⚽ Todavía no le encontraste la vuelta',
  '😅 La próxima metele ganas',
  '🎺 Haciendo ruido desde abajo',
  '🧱 Construyendo la remontada',
  '🏕️ Acampando en mitad de tabla',
  '🧉 Ruido de mate',
  '🧭 Buscando el rumbo',
  '🌊 A contracorriente',
  '🎭 Impredecible hasta para vos',
  '🎯 Encontrando el ritmo',
  '📊 Más consistente que la fecha pasada',
  '⚙️ Ajustando los últimos detalles',
  '🥾 Todavía queda campeonato',
  '📣 No te descuides que venís ahí',
  '🚂 Agarrando impulso',
  '🚬 No era penal',
  '📺 Lo vi por TikTok',
  '🕺 Fulbo champagne',
  '🏃 Corriendo de atrás',
  '🤌 Falta picardía',
  '🧉 Tomando mate y especulando',
  '🧾 Haciendo cuentas como contador de barrio',
  '🏕️ Modo camping en mitad de tabla',
  '🥟 Hoy jugaste para el empate',
  '🎤 Muchachos, ahora nos volvimos a ilusionar',

  // Mitad inferior
  '🤦 El VAR tampoco te ayudó',
  '🌧️ Día complicado',
  '🫠 Te faltó VAR',
  '📉 Hoy no era tu día',
  '📡 Señal perdida con los resultados',
  '🔍 El resultado estaba en otro partido',
  '📚 Mucho por aprender del fixture',
  '🎢 Más irregular que la fase de grupos',
  '🤷 Inexplicable',
  '📺 ¿Viste los partidos?',
  '🫣 Anulo mufa',
  '📡 Señal perdida',
  '🤷 Elijo creer',
  '🪄 Fe le sobra, aciertos no tanto',
  '📉 Se cayó el sistema',
  '😶 Hoy no conectaste una',
  '🚧 Obra en construcción',
  '🥵 Fecha para archivar',
  '🪫 Te quedaste sin batería',
  '🚨 Entraste dormido al partido',

  // Fondo de tabla
  '🎲 Apostaste a cualquier cosa',
  '🥵 Fecha para olvidar',
  '😬 Mejor olvidemos esta fecha',
  '🚑 Necesitás una remontada',
  '🏃 Corriendo desde atrás',
  '🕳️ Cerca del sótano',
  '🎰 Te jugaste todo al azar',
  '🎪 Espectáculo garantizado, aciertos opcionales',
  '🆘 Situación crítica'
];
private readonly championTitles = [
  '👑 El Messi del grupo',
  '🐐 GOAT de los pronósticos',
  '🏆 Dueño absoluto de la tabla',
  '🧠 Oráculo del fútbol',
  '⚡ Maestro de las predicciones',
  '🎯 Francotirador del resultado',
  '🚀 Imparable esta fecha',
  '🌟 Leyenda del prode',
  '🔥 En modo campeón',
  '📖 Escribiendo la historia del grupo',
  '🐐 El Diego te mira orgulloso',
  '🧠 La Scaloneta de los pronósticos',
  '⭐ Campeón del pueblo',
  '🔥 Modo Qatar 2022',
  '🎯 Más preciso que Julián',
  '⚽ Tocá de primera, fenómeno',
  '🚀 Elijo creer... y acertar',
  '👑 Dueño de la redonda',
  '🧉 Cebando mates desde la punta',
  '🏆 Fulbo champagne',
];

private readonly lastPlaceTitles = [
  '💀 Experto en errarle',
  '🪵 La madera del grupo',
  '🫥 Desaparecido en acción',
  '🚨 Emergencia futbolística',
  '🕯️ Que alguien rece por tus pronósticos',
  '📉 Caída libre sin escalas',
  '🌋 Todo salió mal',
  '🛟 Necesitás un milagro mundialista',
  '🚑 Pidieron asistencia desde el fondo',
  '☠️ Prohibido mostrar esta tabla',
  '🫠 Masterclass de errarle',
  '📉 Más perdido que turco en la neblina',
  '🛟 Necesitás una épica',
  '📺 Estabas viendo otro deporte',
  '🚑 Llamen al DT',
  '🪦 Acá yace una fecha',
  '☠️ Viniste a participar',
  '🎰 Jugaste al azar y salió mal',
];
private hash(value: string): number {
  let hash = 0;

  for (let i = 0; i < value.length; i++) {
    hash = ((hash << 5) - hash) + value.charCodeAt(i);
    hash |= 0;
  }

  return Math.abs(hash);
}
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
  private getTieIndex(
  entry: GroupRanking,
  rankings: GroupRanking[]
): number {

  const tiedPlayers = rankings
    .filter(r => r.totalPoints === entry.totalPoints)
    .sort((a, b) => a.userId.localeCompare(b.userId));

  return tiedPlayers.findIndex(
    r => r.userId === entry.userId
  );
}

titleFor(entry: GroupRanking, rankings: GroupRanking[]): string {
  if (rankings.length <= 1) return '';

  const realPos = this.calculateRealPosition(entry, rankings);
  const total = rankings.length;

  const seed = rankings.reduce(
    (sum, r) => sum + r.totalPoints,
    0
  );

  // ─────────────────────────────────────────────
  // Campeones
  // ─────────────────────────────────────────────

  if (this.isFirstPosition(entry, rankings)) {
    const tieIndex = this.getTieIndex(
  entry,
  rankings
);

const championIndex =
  (seed + tieIndex)
  % this.championTitles.length;

    return this.championTitles[championIndex];
  }

  // ─────────────────────────────────────────────
  // Últimos
  // ─────────────────────────────────────────────

  if (this.isLastPosition(entry, rankings)) {
   const tieIndex = this.getTieIndex(
  entry,
  rankings
);

const lastIndex =
  (seed + tieIndex)
  % this.lastPlaceTitles.length;

    return this.lastPlaceTitles[lastIndex];
  }

  // ─────────────────────────────────────────────
  // Intermedios
  // ─────────────────────────────────────────────

  const middlePositions = total - 2;

  // Grupo de 3 personas
  if (middlePositions === 1) {
    const idx =
      (seed + this.hash(entry.userId))
      % this.rankTitles.length;

    return this.rankTitles[idx];
  }

  const variation = seed % 5;

  const middleRank = realPos - 2;

  const baseIndex =
    (middleRank * (this.rankTitles.length - 1))
    / (middlePositions - 1);

  const adjustedIndex = Math.round(
    Math.max(
      0,
      Math.min(
        this.rankTitles.length - 1,
        baseIndex + variation
      )
    )
  );

  const tiedPlayers = rankings.filter(
  r => r.totalPoints === entry.totalPoints
);

const tieIndex = this.getTieIndex(
  entry,
  rankings
);

const jitter =
  tiedPlayers.length > 1
    ? tieIndex
    : this.hash(entry.userId) % 3;

const finalIndex = Math.min(
  this.rankTitles.length - 1,
  adjustedIndex + jitter
);

  return this.rankTitles[finalIndex];
}
}

