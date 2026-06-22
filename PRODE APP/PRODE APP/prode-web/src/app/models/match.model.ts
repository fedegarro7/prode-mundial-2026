export interface Team {
  id: number;
  fifaId: string;
  name: string;
  code: string;
  flagUrl: string;
  group: string;
}

export interface Stadium {
  id: number;
  fifaId: string;
  name: string;
  city: string;
  country: string;
}

export interface MyPrediction {
  homeScorePrediction: number;
  awayScorePrediction: number;
  pointsEarned: number;
  basePointsEarned?: number;
  multiplierBonusPoints?: number;
  captainBonusPoints?: number;
}

export interface GroupPredictionParticipant {
  userId: string;
  userName: string;
  isCurrentUser: boolean;
  hasPrediction: boolean;
  homeScorePrediction?: number | null;
  awayScorePrediction?: number | null;
  pointsEarned: number;
}

export interface MatchGroupPredictions {
  groupId: number;
  groupName: string;
  participants: GroupPredictionParticipant[];
}

export interface Match {
  id: number;
  fifaId: string;
  matchNumber?: number;
  homeTeam?: Team | null;
  homePlaceholder: string;
  awayTeam?: Team | null;
  awayPlaceholder: string;
  matchDate: string;
  stage: string;
  groupName: string;
  stadium: Stadium;
  homeScore?: number;
  awayScore?: number;
  isFinished: boolean;
  predictionsLocked: boolean;
  myPrediction?: MyPrediction | null;
  groupPredictions?: MatchGroupPredictions[];
  homePrediction?: number | null;
  awayPrediction?: number | null;
  isBombMatch?: boolean;
  wasDecidedByPenalties?: boolean;
}
