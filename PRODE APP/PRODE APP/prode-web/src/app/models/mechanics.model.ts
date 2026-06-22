export interface CaptainPick {
  teamId: number;
  teamName: string;
  isLocked: boolean;
}

export interface GoldenGoalPick {
  roundKey: string;
  matchId: number;
}

export interface SharpShooterPick {
  roundKey: string;
  matchId: number;
  pointsAwarded: number;
}

export interface OraclePrediction {
  roundKey: string;
  drawsAfterNinetyPrediction: number;
  penaltyShootoutsPrediction: number;
}

export interface MechanicsState {
  captain: CaptainPick | null;
  goldenGoals: GoldenGoalPick[];
  sharpShooters: SharpShooterPick[];
  oraclePredictions: OraclePrediction[];
}

/** Rounds used across UI */
export const ROUND_KEYS = {
  GROUP_STAGE: 'GROUP_STAGE',
  ROUND_OF_32: 'ROUND_OF_32',
  ROUND_OF_16: 'ROUND_OF_16',
  QUARTER_FINALS: 'QUARTER_FINALS',
  SEMI_FINALS: 'SEMI_FINALS',
  FINAL_ROUND: 'FINAL_ROUND',
} as const;

export type RoundKey = (typeof ROUND_KEYS)[keyof typeof ROUND_KEYS];

export const ROUND_LABELS: Record<string, string> = {
  GROUP_STAGE: 'Fase de Grupos',
  ROUND_OF_32: 'Dieciseisavos de Final',
  ROUND_OF_16: 'Octavos de Final',
  QUARTER_FINALS: 'Cuartos de Final',
  SEMI_FINALS: 'Semifinales',
  FINAL_ROUND: 'Ronda Final',
};

export const BASE_POINTS: Record<string, number> = {
  GROUP_STAGE: 3,
  ROUND_OF_32: 4,
  ROUND_OF_16: 5,
  QUARTER_FINALS: 7,
  SEMI_FINALS: 10,
  FINAL_ROUND: 12, // 15 for the final itself
};
