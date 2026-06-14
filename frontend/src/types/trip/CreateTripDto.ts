export interface CreateTripDto {
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  estimatedBudget: number;
  generalNotes: string;
}