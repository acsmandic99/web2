import type { ResultDto } from '../types/shared/ResultDto';
import type { ExpenseDto } from '../types/expense/ExpenseDto';
import type { CreateExpenseDto } from '../types/expense/CreateExpenseDto';
import type { BudgetSummaryDto } from '../types/expense/BudgetSummaryDto';
import apiClient from './apiClient';

const EXPENSE_PREFIX = import.meta.env.VITE_EXPENSE_PREFIX;

export const expenseService = {
  async getTripExpenses(tripId: string): Promise<ResultDto<ExpenseDto[]>> {
    const response = await apiClient.get<ResultDto<ExpenseDto[]>>(`/${EXPENSE_PREFIX}/trip/${tripId}`);
    return response.data;
  },

  async getBudgetSummary(tripId: string): Promise<ResultDto<BudgetSummaryDto>> {
    const response = await apiClient.get<ResultDto<BudgetSummaryDto>>(`/${EXPENSE_PREFIX}/trip/${tripId}/summary`);
    return response.data;
  },

  async addExpense(expense: CreateExpenseDto): Promise<ResultDto<ExpenseDto>> {
    if (!expense.title.trim() || !expense.incurredAt) {
      throw new Error('Title and incurrence date are required fields.');
    }
    if (expense.amount < 0) {
      throw new Error('Amount cannot be a negative value.');
    }
    const response = await apiClient.post<ResultDto<ExpenseDto>>(`/${EXPENSE_PREFIX}`, expense);
    return response.data;
  },

  async deleteExpense(id: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.delete<ResultDto<boolean>>(`/${EXPENSE_PREFIX}/${id}`);
    return response.data;
  }
};