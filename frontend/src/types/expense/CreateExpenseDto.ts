export interface CreateExpenseDto {
  title: string;
  category: number;
  amount: number;
  incurredAt: string;
  description: string;
  tripId: string;
}