/** Represents a private prediction group. */
export interface Group {
  id: number;
  name: string;
  inviteCode: string;
  ownerName: string;
  isOwner: boolean;
  memberCount: number;
  pendingRequestCount: number;
  /** "Owner" | "Approved" | "Pending" | "None" */
  membershipStatus: string;
  createdAt: string;
}

/** A pending join request (only visible to the group owner). */
export interface JoinRequest {
  userId: string;
  userName: string;
  requestedAt: string;
}

/** Ranking entry inside a private group. */
export interface GroupRanking {
  userId: string;
  userName: string;
  totalPoints: number;
  position: number;
  isCurrentUser: boolean;
}

/** Member entry in the admin group view. */
export interface AdminGroupMember {
  userId: string;
  userName: string;
  /** "Approved" | "Pending" */
  status: string;
}

/** Full group details visible to admin users. */
export interface AdminGroup {
  id: number;
  name: string;
  inviteCode: string;
  ownerName: string;
  memberCount: number;
  pendingRequestCount: number;
  createdAt: string;
  members: AdminGroupMember[];
}
