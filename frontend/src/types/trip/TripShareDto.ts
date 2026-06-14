export interface TripShareDto {
  id: string;
  tripId: string;
  token: string;
  accessLevel: number;
  claimedByUserId: string | null;
}