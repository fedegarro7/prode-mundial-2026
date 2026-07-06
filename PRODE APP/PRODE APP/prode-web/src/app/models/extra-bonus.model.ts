export interface ExtraBonusDetails {
  userId: string;
  userName: string;
  goldenGoal?: GoldenGoalBonus;
  captain?: CaptainBonus;
  sharpShooter: SharpShooterBonus[];
  oracleDraws?: OracleBonus;
  oraclePenalties?: OracleBonus;
  totalExtraPoints: number;
  isRoundKing: boolean;
}

export interface GoldenGoalBonus {
  matchId: number;
  matchDescription: string;
  pointsEarned: number;
}

export interface CaptainBonus {
  teamId: number;
  teamName: string;
  matches: CaptainMatchContribution[];
  pointsEarned: number;
}

export interface CaptainMatchContribution {
  matchId: number;
  matchDescription: string;
  pointsEarned: number;
}

export interface SharpShooterBonus {
  matchId: number;
  matchDescription: string;
  pointsEarned: number;
}

export interface OracleBonus {
  category: string; // "Empates" or "Penales"
  prediction: number;
  actual: number;
  distance: number;
  pointsEarned: number;
  isWinner: boolean;
}

export interface RoundExtraBonuses {
  roundKey: string;
  roundLabel: string;
  users: ExtraBonusDetails[];
}
