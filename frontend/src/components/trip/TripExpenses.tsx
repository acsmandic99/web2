import React, { useState, useEffect } from 'react';
import apiClient from '../../services/apiClient';
import { BudgetWarningModal } from './BudgetWarningModal';
import type { ResultDto } from '../../types/shared/ResultDto';
import type { ExpenseDto } from '../../types/expense/ExpenseDto';
import type { BudgetSummaryDto } from '../../types/expense/BudgetSummaryDto';

interface TripExpensesProps {
  tripId: string;
  budgetSummary: BudgetSummaryDto | null;
  onBudgetChange: () => Promise<void>;
}

export const TripExpenses: React.FC<TripExpensesProps> = ({ tripId, budgetSummary, onBudgetChange }) => {
  const [expenses, setExpenses] = useState<ExpenseDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [showWarningModal, setShowBudgetWarningModal] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    title: '',
    category: 0,
    amount: 0,
    incurredAt: '',
    description: ''
  });

  const fetchExpenses = async () => {
    try {
      const response = await apiClient.get<ResultDto<ExpenseDto[]>>(`/api/expenses/trip/${tripId}`);
      if (response.data.isSuccess && response.data.data) {
        const sorted = [...response.data.data].sort(
          (a, b) => new Date(a.incurredAt).getTime() - new Date(b.incurredAt).getTime()
        );
        setExpenses(sorted);
      }
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load expenses.');
    }
  };

  useEffect(() => {
    fetchExpenses();
  }, [tripId]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: name === 'amount' || name === 'category' ? parseFloat(value) || 0 : value
    });
  };

  const executeSaveExpense = async () => {
    try {
      setError(null);
      if (editingId) {
        await apiClient.put<ResultDto<ExpenseDto>>(`/api/expenses/${editingId}`, { ...formData, tripId });
        setEditingId(null);
      } else {
        await apiClient.post<ResultDto<ExpenseDto>>('/api/expenses', { ...formData, tripId });
      }
      setFormData({ title: '', category: 0, amount: 0, incurredAt: '', description: '' });
      await fetchExpenses();
      await onBudgetChange();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to save expense.');
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.title.trim()) {
      setError('Expense title is required.');
      return;
    }
    if (formData.amount <= 0) {
      setError('Amount must be greater than 0.');
      return;
    }
    if (!formData.incurredAt) {
      setError('Date incurred is required.');
      return;
    }

    const isExceeded = budgetSummary ? budgetSummary.remainingBudget < 0 : false;
    if (!editingId && isExceeded && formData.amount > 0) {
      setShowBudgetWarningModal(true);
    } else {
      executeSaveExpense();
    }
  };

  const handleEditClick = (exp: ExpenseDto) => {
    setEditingId(exp.id);
    const d = new Date(exp.incurredAt);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    setFormData({
      title: exp.title,
      category: exp.category,
      amount: exp.amount,
      incurredAt: `${year}-${month}-${day}`,
      description: exp.description || ''
    });
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setFormData({ title: '', category: 0, amount: 0, incurredAt: '', description: '' });
  };

  const handleDelete = async (id: string) => {
    try {
      await apiClient.delete<ResultDto<boolean>>(`/api/expenses/${id}`);
      await fetchExpenses();
      await onBudgetChange();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to delete expense.');
    }
  };

  const getCategoryLabel = (cat: number) => {
    const categories = ['Transportation', 'Accommodation', 'Food', 'Tickets', 'Shopping', 'Other', 'Activity'];
    return categories[cat] || 'Other';
  };

  const formatDateFormatted = (dateStr: string) => {
    const d = new Date(dateStr);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}.${month}.${year}.`;
  };

  const isExceeded = budgetSummary ? budgetSummary.remainingBudget < 0 : false;

  return (
    <div className="space-y-6">
      {budgetSummary && (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-3">
          <div className="bg-white overflow-hidden shadow rounded-lg border border-gray-200 p-5">
            <dt className="text-sm font-medium text-gray-500 truncate">Estimated Budget</dt>
            <dd className="mt-1 text-2xl font-semibold text-gray-900">{budgetSummary.estimatedBudget} EUR</dd>
          </div>
          <div className="bg-white overflow-hidden shadow rounded-lg border border-gray-200 p-5">
            <dt className="text-sm font-medium text-gray-500 truncate">Total Spent</dt>
            <dd className="mt-1 text-2xl font-semibold text-red-600">{budgetSummary.totalSpent} EUR</dd>
          </div>
          <div className={`overflow-hidden shadow rounded-lg border p-5 transition-colors ${isExceeded ? 'bg-red-600 border-red-700 text-white' : 'bg-white border-gray-200 text-gray-900'}`}>
            <dt className={`text-sm font-medium truncate ${isExceeded ? 'text-red-100' : 'text-gray-500'}`}>Remaining Budget</dt>
            <dd className="mt-1 text-2xl font-semibold">{budgetSummary.remainingBudget} EUR</dd>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1 bg-white p-6 rounded-lg border border-gray-200 shadow-sm h-fit">
          <h3 className="text-lg font-bold text-gray-900 mb-4">
            {editingId ? 'Edit Expense' : 'Record Expense'}
          </h3>
          {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Expense Title</label>
              <input type="text" name="title" value={formData.title} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Amount (EUR)</label>
                <input type="number" name="amount" value={formData.amount} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Category</label>
                <select name="category" value={formData.category} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white">
                  <option value={0}>Transportation</option>
                  <option value={1}>Accommodation</option>
                  <option value={2}>Food</option>
                  <option value={3}>Tickets</option>
                  <option value={4}>Shopping</option>
                  <option value={5}>Other</option>
                  <option value={6}>Activity</option>
                </select>
              </div>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Date Incurred</label>
              <input type="date" name="incurredAt" value={formData.incurredAt} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Description</label>
              <textarea name="description" value={formData.description} onChange={handleChange} rows={2} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div className="flex space-x-2">
              {editingId && (
                <button type="button" onClick={handleCancelEdit} className="w-1/2 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium rounded-md text-sm transition-colors border cursor-pointer">
                  Cancel
                </button>
              )}
              <button type="submit" className={`py-2 text-white font-medium rounded-md text-sm transition-colors shadow-sm cursor-pointer ${editingId ? 'w-1/2 bg-green-600 hover:bg-green-700' : 'w-full bg-blue-600 hover:bg-blue-700'}`}>
                {editingId ? 'Update' : 'Add Expense'}
              </button>
            </div>
          </form>
        </div>

        <div className="lg:col-span-2 space-y-4">
          {expenses.length === 0 ? (
            <div className="bg-white p-6 rounded-lg border border-gray-200 text-center text-sm text-gray-500 shadow-sm">No expenses logged yet.</div>
          ) : (
            expenses.map((exp) => (
              <div key={exp.id} className="bg-white p-5 rounded-lg border border-gray-200 shadow-sm flex justify-between items-center">
                <div>
                  <h4 className="text-md font-bold text-gray-900">{exp.title}</h4>
                  <p className="text-xs text-gray-500 font-medium">
                    {getCategoryLabel(exp.category)} &bull; {formatDateFormatted(exp.incurredAt)}
                  </p>
                  {exp.description && <p className="text-sm text-gray-600 mt-1">{exp.description}</p>}
                </div>
                <div className="flex items-center space-x-4">
                  <span className={`text-md font-bold ${exp.category === 6 ? 'text-purple-600' : 'text-red-600'}`}>
                    {exp.amount} EUR
                  </span>
                  <div className="flex space-x-3">
                    <button onClick={() => handleEditClick(exp)} className="text-xs font-medium text-blue-600 hover:text-blue-800 transition-colors cursor-pointer">Edit</button>
                    <button onClick={() => handleDelete(exp.id)} className="text-xs font-medium text-red-600 hover:text-red-800 transition-colors cursor-pointer">Delete</button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      <BudgetWarningModal
        isOpen={showWarningModal}
        onClose={() => setShowBudgetWarningModal(false)}
        onConfirm={() => {
          setShowBudgetWarningModal(false);
          executeSaveExpense();
        }}
      />
    </div>
  );
};