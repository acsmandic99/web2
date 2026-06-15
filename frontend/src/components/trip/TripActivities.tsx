import React, { useState, useEffect } from 'react';
import apiClient from '../../services/apiClient';
import { BudgetWarningModal } from './BudgetWarningModal';
import type { ResultDto } from '../../types/shared/ResultDto';
import type { ActivityDto } from '../../types/activity/ActivityDto';

interface TripActivitiesProps {
  tripId: string;
  isBudgetExceeded: boolean;
  onBudgetChange: () => Promise<void>;
}

export const TripActivities: React.FC<TripActivitiesProps> = ({ tripId, isBudgetExceeded, onBudgetChange }) => {
  const [activities, setActivities] = useState<ActivityDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [showWarningModal, setShowBudgetWarningModal] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    location: '',
    scheduledAt: '',
    price: 0,
    description: '',
    status: 0
  });

  const fetchActivities = async () => {
    try {
      const response = await apiClient.get<ResultDto<ActivityDto[]>>(`/api/activities/trip/${tripId}`);
      if (response.data.isSuccess && response.data.data) {
        const sorted = [...response.data.data].sort(
          (a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime()
        );
        setActivities(sorted);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load activities.');
    }
  };

  useEffect(() => {
    fetchActivities();
  }, [tripId]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData({
      ...formData,
      [name]: name === 'price' || name === 'status' ? parseFloat(value) || 0 : value
    });
  };

  const executeSaveActivity = async () => {
    try {
      setError(null);
      if (editingId) {
        const res = await apiClient.put<ResultDto<boolean>>(`/api/activities/${editingId}`, { ...formData, tripId });
        if (!res.data.isSuccess) throw new Error(res.data.message);
        setEditingId(null);
      } else {
        const res = await apiClient.post<ResultDto<ActivityDto>>('/api/activities', { ...formData, tripId });
        if (!res.data.isSuccess) throw new Error(res.data.message);
      }
      
      setFormData({ name: '', location: '', scheduledAt: '', price: 0, description: '', status: 0 });
      await fetchActivities();
      await onBudgetChange();
    } catch (err: any) {
      setError(err.message || 'Failed to save activity.');
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.name.trim() || !formData.location.trim() || !formData.scheduledAt) {
      setError('All fields are required.');
      return;
    }

    if (!editingId && isBudgetExceeded && formData.price > 0) {
      setShowBudgetWarningModal(true);
    } else {
      executeSaveActivity();
    }
  };

  const handleEditClick = (act: ActivityDto) => {
    setEditingId(act.id);
    const d = new Date(act.scheduledAt);
    setFormData({
      name: act.name,
      location: act.location,
      scheduledAt: d.toISOString().slice(0, 16),
      price: act.price,
      description: act.description || '',
      status: act.status
    });
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setFormData({ name: '', location: '', scheduledAt: '', price: 0, description: '', status: 0 });
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await apiClient.delete<ResultDto<boolean>>(`/api/activities/${id}`);
      if (res.data.isSuccess) {
        await fetchActivities();
        await onBudgetChange();
      } else {
        setError(res.data.message);
      }
    } catch (err: any) {
      setError(err.message || 'Failed to delete activity.');
    }
  };



  const formatDateFormatted = (dateStr: string) => {
    return new Date(dateStr).toLocaleString('sr-RS', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div className="lg:col-span-1 bg-white p-6 rounded-lg border border-gray-200 shadow-sm h-fit">
        <h3 className="text-lg font-bold text-gray-900 mb-4">{editingId ? 'Edit Activity' : 'Add Activity'}</h3>
        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <input type="text" name="name" placeholder="Name" value={formData.name} onChange={handleChange} className="w-full px-3 py-2 border rounded-md text-sm" />
          <input type="text" name="location" placeholder="Location" value={formData.location} onChange={handleChange} className="w-full px-3 py-2 border rounded-md text-sm" />
          <input type="datetime-local" name="scheduledAt" value={formData.scheduledAt} onChange={handleChange} className="w-full px-3 py-2 border rounded-md text-sm" />
          <input type="number" name="price" placeholder="Price" value={formData.price} onChange={handleChange} className="w-full px-3 py-2 border rounded-md text-sm" />
          <select name="status" value={formData.status} onChange={handleChange} className="w-full px-3 py-2 border rounded-md text-sm bg-white">
            <option value={0}>Planned</option>
            <option value={1}>Reserved</option>
            <option value={2}>Completed</option>
            <option value={3}>Canceled</option>
          </select>
          <textarea name="description" placeholder="Description" value={formData.description} onChange={handleChange} className="w-full px-3 py-2 border rounded-md text-sm" />
          <div className="flex space-x-2">
            {editingId && <button type="button" onClick={handleCancelEdit} className="w-1/2 py-2 border rounded-md text-sm">Cancel</button>}
            <button type="submit" className={`py-2 text-white font-medium rounded-md text-sm ${editingId ? 'w-1/2 bg-green-600' : 'w-full bg-blue-600'}`}>
              {editingId ? 'Update' : 'Save'}
            </button>
          </div>
        </form>
      </div>

      <div className="lg:col-span-2 space-y-4">
        {activities.map((act) => (
          <div key={act.id} className="bg-white p-5 rounded-lg border border-gray-200 shadow-sm flex justify-between items-start">
            <div>
              <h4 className="font-bold">{act.name}</h4>
              <p className="text-xs text-gray-500">{act.location} • {formatDateFormatted(act.scheduledAt)}</p>
              <p className="text-xs font-semibold text-blue-600">Cost: {act.price} EUR</p>
            </div>
            <div className="flex space-x-2">
              <button onClick={() => handleEditClick(act)} className="text-blue-600 text-xs font-bold">Edit</button>
              <button onClick={() => handleDelete(act.id)} className="text-red-600 text-xs font-bold">Remove</button>
            </div>
          </div>
        ))}
      </div>
      <BudgetWarningModal isOpen={showWarningModal} onClose={() => setShowBudgetWarningModal(false)} onConfirm={() => { setShowBudgetWarningModal(false); executeSaveActivity(); }} />
    </div>
  );
};