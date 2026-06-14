export interface CreateActivityDto {
  name: string;
  location: string;
  scheduledAt: string;
  price: number;
  description: string;
  status: number;
  tripId: string;
}