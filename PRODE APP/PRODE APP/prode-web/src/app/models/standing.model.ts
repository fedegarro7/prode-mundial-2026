export interface StandingEntry {
  position: number;
  teamId: number;
  teamName: string;
  teamCode: string;
  flagUrl: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
  qualifiesAsThird?: boolean;
}

export interface GroupStanding {
  groupName: string;
  entries: StandingEntry[];
}
