import { Stadium, Team } from './match.model';

export interface Prediction {
  id: number;
  matchId: number;
  homeTeam?: Team | null;
  homePlaceholder: string;
  awayTeam?: Team | null;
  awayPlaceholder: string;
  homeScorePrediction: number;
  awayScorePrediction: number;
  pointsEarned: number;
}

export interface PendingPrediction {
  matchId: number;
  matchNumber?: number;
  homeTeam?: Team | null;
  homePlaceholder: string;
  awayTeam?: Team | null;
  awayPlaceholder: string;
  matchDate: string;
  stage: string;
  groupName: string;
  stadium: Stadium;
}

export interface PredictionHistory {
  matchId: number;
  matchNumber?: number;
  homeTeam?: Team | null;
  homePlaceholder: string;
  awayTeam?: Team | null;
  awayPlaceholder: string;
  matchDate: string;
  stage: string;
  homeScorePrediction: number;
  awayScorePrediction: number;
  homeScore?: number | null;
  awayScore?: number | null;
  pointsEarned: number;
}

export interface MyDashboard {
  totalPredictions: number;
  pendingPredictions: number;
  totalPoints: number;
  globalPosition?: number | null;
  approvedGroups: number;
  nextPendingPrediction?: PendingPrediction | null;
}
