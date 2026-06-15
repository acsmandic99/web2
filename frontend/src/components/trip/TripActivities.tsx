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
      setError(err.response?.data?.message || err.message || 'Failed to load activities.');
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
        await apiClient.put<ResultDto<boolean>>(`/api/activities/${editingId}`, { ...formData, tripId });
        setEditingId(null);
      } else {
        await apiClient.post<ResultDto<ActivityDto>>('/api/activities', { ...formData, tripId });
      }
      setFormData({ name: '', location: '', scheduledAt: '', price: 0, description: '', status: 0 });
      await fetchActivities();
      await onBudgetChange();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to save activity.');
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!formData.name.trim()) {
      setError('Activity name is required.');
      return;
    }
    if (!formData.location.trim()) {
      setError('Location is required.');
      return;
    }
    if (!formData.scheduledAt) {
      setError('Scheduled date and time are required.');
      return;
    }
    if (formData.price < 0) {
      setError('Price cannot be a negative value.');
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
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    setFormData({
      name: act.name,
      location: act.location,
      scheduledAt: `${year}-${month}-${day}T${hours}:${minutes}`,
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
      await apiClient.delete<ResultDto<boolean>>(`/api/activities/${id}`);
      await fetchActivities();
      await onBudgetChange();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to delete activity.');
    }
  };

  const getStatusLabel = (status: number) => {
    switch (status) {
      case 0: return { label: 'Planned', css: 'bg-gray-100 text-gray-800' };
      case 1: return { label: 'Reserved', css: 'bg-blue-100 text-blue-800' };
      case 2: return { label: 'Completed', css: 'bg-green-100 text-green-800' };
      case 3: return { label: 'Canceled', css: 'bg-red-100 text-red-800' };
      default: return { label: 'Unknown', css: 'bg-gray-100 text-gray-800' };
    }
  };

  const formatDateFormatted = (dateStr: string) => {
    const d = new Date(dateStr);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    return `${day}.${month}.${year}. ${hours}:${minutes}`;
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div className="lg:col-span-1 bg-white p-6 rounded-lg border border-gray-200 shadow-sm h-fit">
        <h3 className="text-lg font-bold text-gray-900 mb-4">
          {editingId ? 'Edit Activity' : 'Add Activity'}
        </h3>
        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Activity Name</label>
            <input type="text" name="name" value={formData.name} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Location</label>
            <input type="text" name="location" value={formData.location} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Scheduled Date & Time</label>
            <input type="datetime-local" name="scheduledAt" value={formData.scheduledAt} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Price (EUR)</label>
              <input type="number" name="price" value={formData.price} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Status</label>
              <select name="status" value={formData.status} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white">
                <option value={0}>Planned</option>
                <option value={1}>Reserved</option>
                <option value={2}>Completed</option>
                <option value={3}>Canceled</option>
              </select>
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Description</label>
            <textarea name="description" value={formData.description} onChange={handleChange} rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex space-x-2">
            {editingId && (
              <button type="button" onClick={handleCancelEdit} className="w-1/2 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium rounded-md text-sm transition-colors border cursor-pointer">
                Cancel
              </button>
            )}
            <button type="submit" className={`py-2 text-white font-medium rounded-md text-sm transition-colors shadow-sm cursor-pointer ${editingId ? 'w-1/2 bg-green-600 hover:bg-green-700' : 'w-full bg-blue-600 hover:bg-blue-700'}`}>
              {editingId ? 'Update' : 'Save Activity'}
            </button>
          </div>
        </form>
      </div>

      <div className="lg:col-span-2 space-y-4">
        {activities.length === 0 ? (
          <div className="bg-white p-6 rounded-lg border border-gray-200 text-center text-sm text-gray-500 shadow-sm">No scheduled activities yet.</div>
        ) : (
          activities.map((act) => {
            const status = getStatusLabel(act.status);
            return (
              <div key={act.id} className="bg-white p-5 rounded-lg border border-gray-200 shadow-sm flex justify-between items-start">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center space-x-2">
                    <h4 className="text-md font-bold text-gray-900">{act.name}</h4>
                    <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${status.css}`}>{status.label}</span>
                  </div>
                  <p className="text-xs text-gray-500 font-medium">{act.location} &bull; {formatDateFormatted(act.scheduledAt)}</p>
                  <p className="text-xs font-semibold text-blue-600 mt-1">Cost: {act.price} EUR</p>
                  {act.description && <p className="text-sm text-gray-600 mt-2 bg-gray-50 p-2 rounded border border-gray-100">{act.description}</p>}
                </div>
                <div className="flex space-x-3 ml-4">
                  <button onClick={() => handleEditClick(act)} className="text-xs font-medium text-blue-600 hover:text-blue-800 transition-colors cursor-pointer">Edit</button>
                  <button onClick={() => handleDelete(act.id)} className="text-xs font-medium text-red-600 hover:text-red-800 transition-colors cursor-pointer">Remove</button>
                </div>
              </div>
            );
          })
        )}
      </div>

      <BudgetWarningModal
        isOpen={showWarningModal}
        onClose={() => setShowBudgetWarningModal(false)}
        onConfirm={() => {
          setShowBudgetWarningModal(false);
          executeSaveActivity();
        }}
      />
    </div>
  );
};