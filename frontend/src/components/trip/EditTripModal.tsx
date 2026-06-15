import React, { useState, useEffect } from 'react';
import apiClient from '../../services/apiClient';
import { tripService } from '../../services/tripService';
import type { ResultDto } from '../../types/shared/ResultDto';
import type { TripDto } from '../../types/trip/TripDto';
import type { DestinationDto } from '../../types/destination/DestinationDto';
import type { ActivityDto } from '../../types/activity/ActivityDto';
import type { ExpenseDto } from '../../types/expense/ExpenseDto';

interface EditTripModalProps {
  isOpen: boolean;
  trip: TripDto;
  onClose: () => void;
  onTripUpdated: () => void;
}

export const EditTripModal: React.FC<EditTripModalProps> = ({ isOpen, trip, onClose, onTripUpdated }) => {
  const [formData, setFormData] = useState({
    title: trip.title,
    description: trip.description,
    startDate: trip.startDate.split('T')[0],
    endDate: trip.endDate.split('T')[0],
    estimatedBudget: trip.estimatedBudget,
    generalNotes: trip.generalNotes || ''
  });
  const [destinations, setDestinations] = useState<DestinationDto[]>([]);
  const [activities, setActivities] = useState<ActivityDto[]>([]);
  const [expenses, setExpenses] = useState<ExpenseDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      const fetchTripData = async () => {
        try {
          const [destRes, actRes, expRes] = await Promise.all([
            apiClient.get<ResultDto<DestinationDto[]>>(`/api/destinations/trip/${trip.id}`),
            apiClient.get<ResultDto<ActivityDto[]>>(`/api/activities/trip/${trip.id}`),
            apiClient.get<ResultDto<ExpenseDto[]>>(`/api/expenses/trip/${trip.id}`)
          ]);
          if (destRes.data.isSuccess && destRes.data.data) setDestinations(destRes.data.data);
          if (actRes.data.isSuccess && actRes.data.data) setActivities(actRes.data.data);
          if (expRes.data.isSuccess && expRes.data.data) setExpenses(expRes.data.data);
        } catch (err) {}
      };
      fetchTripData();
    }
  }, [isOpen, trip.id]);

  if (!isOpen) return null;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: name === 'estimatedBudget' ? parseFloat(value) || 0 : value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!formData.title.trim() || !formData.description.trim() || !formData.startDate || !formData.endDate) {
      setError('All core fields are required.');
      return;
    }

    const newStart = new Date(formData.startDate);
    newStart.setHours(0, 0, 0, 0);
    const newEnd = new Date(formData.endDate);
    newEnd.setHours(0, 0, 0, 0);

    if (newStart > newEnd) {
      setError('End date cannot be scheduled before the start date.');
      return;
    }

    for (const dest of destinations) {
      const arrDate = new Date(dest.arrivalDate);
      arrDate.setHours(0, 0, 0, 0);
      const depDate = new Date(dest.departureDate);
      depDate.setHours(0, 0, 0, 0);
      if (arrDate < newStart || depDate > newEnd) {
        setError(`Cannot change dates. Destination '${dest.name}' falls outside the new trip range. Please adjust or remove it first.`);
        return;
      }
    }

    for (const act of activities) {
      const actDate = new Date(act.scheduledAt);
      actDate.setHours(0, 0, 0, 0);
      if (actDate < newStart || actDate > newEnd) {
        setError(`Cannot change dates. Activity '${act.name}' is scheduled outside the new trip range. Please remove it first.`);
        return;
      }
    }

    for (const exp of expenses) {
      const expDate = new Date(exp.incurredAt);
      expDate.setHours(0, 0, 0, 0);
      if (expDate < newStart || expDate > newEnd) {
        setError(`Cannot change dates. Expense '${exp.title}' is recorded outside the new trip range. Please remove it first.`);
        return;
      }
    }

    try {
      setLoading(true);
      const result = await tripService.updateTrip(trip.id, formData);
      if (result.isSuccess) {
        onTripUpdated();
        onClose();
      } else {
        setError(result.message || 'Failed to update trip.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'An error occurred.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose}></div>
      <div className="relative bg-white rounded-xl shadow-2xl p-6 max-w-md w-full border border-gray-100 z-10 animate-fade-in">
        <div className="flex justify-between items-center border-b pb-3 mb-4">
          <h3 className="text-lg font-bold text-gray-900">Edit Trip Information</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-500 text-xl font-semibold cursor-pointer">&times;</button>
        </div>

        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Trip Title</label>
            <input type="text" name="title" value={formData.title} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Description</label>
            <textarea name="description" value={formData.description} onChange={handleChange} rows={2} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
              <input type="date" name="startDate" value={formData.startDate} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
              <input type="date" name="endDate" value={formData.endDate} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Estimated Budget (EUR)</label>
            <input type="number" name="estimatedBudget" value={formData.estimatedBudget} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <button type="submit" disabled={loading} className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold text-sm rounded-md transition-colors shadow-sm disabled:opacity-50 cursor-pointer">
            {loading ? 'Updating...' : 'Update Trip Plan'}
          </button>
        </form>
      </div>
    </div>
  );
};