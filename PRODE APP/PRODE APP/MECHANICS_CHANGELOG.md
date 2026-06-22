# Mecánicas Prode Mundial 2026 — Changelog

> Última actualización: 2026-06-22

---

## Resumen ejecutivo

A partir de los **Dieciseisavos de Final**, el sistema de puntos escala con nuevas mecánicas.  
La Fase de Grupos mantiene el sistema clásico sin cambios.

---

## Sistema de puntos por fase

| Fase | Resultado correcto | Marcador exacto | Mecánicas extra |
|---|---|---|---|
| **Grupos** | 1 pt | 3 pts | ❌ Ninguna |
| **Dieciseisavos** | 1 pt | 4 pts | ✅ Todas |
| **Octavos** | 1 pt | 5 pts | ✅ Todas |
| **Cuartos** | 1 pt | 7 pts | ✅ Todas |
| **Semifinal** | 1 pt | 10 pts | ✅ Todas |
| **Final / 3er puesto** | 1 pt | 15 pts | ✅ Todas |

---

## Mecánicas habilitadas desde Dieciseisavos

### 🧢 Capitán (+5 pts)
- Elegís **una selección** antes de que arranquen los dieciseisavos.
- Si acertás el **resultado** (no necesariamente exacto) de cualquier partido de esa selección en fase eliminatoria, sumás **+5 pts extra**.
- Se puede cambiar hasta que el primer partido de dieciseisavos se bloquee.
- No aplica a partidos de la Fase de Grupos.

### 💣 Partido Bomba (×2 base)
- En cada ronda eliminatoria, **un partido es sorteado** como Partido Bomba.
- Si acertás el **marcador exacto**, los puntos base se duplican.
- El reveal ocurre cuando **todos los pronósticos de la ronda están cerrados** (todos los partidos ya arrancaron).
- Ejemplo en Cuartos (base 7): exacto → **14 pts**.

### 🥇 Gol de Oro (×3 base)
- Una vez por ronda, elegís **un partido** como Gol de Oro antes de que arranque.
- Si acertás el **marcador exacto**, los puntos base se triplican.
- Si el partido también es Bomba: los multiplicadores se suman → **×4 la base**.
- Ejemplo en Cuartos (base 7): exacto + Gol de Oro → 21 pts · con Bomba → **28 pts**.

### 🎯 Francotirador (+5 pts bonus)
- Una vez por ronda, elegís un partido como objetivo.
- Si ese partido **se define por penales** (`WasDecidedByPenalties = true`), recibís +5 pts al finalizar la ronda.

### 🔮 Oráculo (pts variables)
- Antes de cada ronda, predecís:
  - Cuántos partidos irán a **tiempo extra** (empate al 90')
  - Cuántos se definirán **por penales**
- El jugador más cercano gana puntos bonus al finalizar la ronda.
- En caso de empate, todos los acertadores reciben el premio completo.

---

## Escenarios de puntos máximos

| Ronda | Solo exacto | + Bomba | + Gol de Oro | + Bomba + GdO | + Capitán |
|---|---|---|---|---|---|
| Grupos | 3 | — | — | — | — |
| R32 | 4 | 8 | 12 | 16 | +5 |
| R16 | 5 | 10 | 15 | 20 | +5 |
| Cuartos | 7 | 14 | 21 | 28 | +5 |
| Semifinal | 10 | 20 | 30 | 40 | +5 |
| Final | 15 | 30 | 45 | 60 | +5 |

> Máximo teórico partido: **65 pts** (Final, exacto + Bomba + Gol de Oro + Capitán)

---

## Pronósticos y alargue / penales

- Los pronósticos se comparan contra el **marcador oficial final**, incluyendo tiempo suplementario.
- Si el partido va a penales (empate después del alargue, e.g. 1-1), el marcador oficial es **1-1**. Pronosticar 1-1 = marcador exacto.
- Los penales **no suman goles** al marcador — solo activan el Francotirador.
- En la Fase de Grupos **no hay alargue**. Empate al 90' = resultado final.

---

## Bugs corregidos (2026-06-22)

| # | Tipo | Descripción | Archivo |
|---|---|---|---|
| 1 | 🐛 Backend | El bonus de Capitán (+5 pts) se aplicaba también a partidos de Fase de Grupos | `ScoreRecalculationService.cs` |
| 2 | 🐛 Frontend | El badge 🧢 CAPITÁN aparecía en la sección de Fase de Grupos | `matches.component.html` |
| 3 | 🐛 Backend | `SetResult` no persistía `WasDecidedByPenalties` aunque el DTO lo traía | `MatchesController.cs` |
| 4 | 🎨 CSS | `.rules-hero-images { display: none }` referenciaba una clase removida del HTML | `news.component.scss` |
| 5 | 📱 Mobile | `chiqui-standalone` tenía `height: 680px` fijo sin breakpoint mobile | `news.component.scss` |

---

## Migraciones aplicadas

| Migración | Fecha | Tablas/Columnas agregadas |
|---|---|---|
| `AddMechanicsSupport` | 2026-06-22 | `GoldenGoalPicks`, `BombMatches`, `CaptainPicks`, `SharpShooterPredictions`, `OraclePredictions`, `RoundAwards`, columnas `WasDecidedByPenalties` / `BasePointsEarned` / `MultiplierBonusPoints` / `CaptainBonusPoints` |

---

## Estado del sistema de DB (Neon — plan Launch)

**Consultas adicionales por mecánicas (solo fase eliminatoria):**
- `GoldenGoalPicks` por partido → 1 query cuando se carga resultado
- `CaptainPicks` por partido → 1 query cuando se carga resultado
- `SharpShooterPredictions` por ronda → 1 query al finalizar ronda
- `OraclePredictions` por ronda → 1 query al finalizar ronda
- `BombMatches` por ronda → 1 query al inicio de ronda

**Polling existente:**
- Background service FIFA sync: cada **5 minutos** (configurable via `FixtureSync:ScoreSyncIntervalMinutes`)
- Navbar Argentina fixtures: cada **5 minutos** por usuario activo
- Standings: cada **60 segundos** mientras el usuario está en esa pantalla

**Recomendación**: El standings polling a 60s es el más agresivo. Durante días sin partidos en vivo, considerar aumentarlo a 5 min o hacerlo condicional a si hay partidos activos. Para el Mundial con ~4 partidos/día no es un problema significativo.

---

## Deploy checklist

- [ ] `dotnet ef database update` (migración ya aplicada en dev, verificar producción)
- [ ] Reiniciar Angular dev server para que detecte imágenes nuevas en `src/assets/images/`
- [ ] Verificar `FixtureSync:ScoreSyncIntervalMinutes` en `appsettings.json` de producción
