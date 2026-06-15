import React, { useState } from 'react';
import apiClient from '../../services/apiClient';
import type { ResultDto } from '../../types/shared/ResultDto';

interface EditProfileModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const EditProfileModal: React.FC<EditProfileModalProps> = ({ isOpen, onClose }) => {
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    currentPassword: '',
    newPassword: ''
  });
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (!formData.username.trim() || !formData.email.trim() || !formData.currentPassword.trim()) {
      setError('Username, email and current password are required.');
      return;
    }

    try {
      setLoading(true);
      const response = await apiClient.put<ResultDto<boolean>>('/api/auth/profile', formData);
      if (response.data.isSuccess) {
        setSuccess('Profile updated successfully.');
        setFormData({ username: '', email: '', currentPassword: '', newPassword: '' });
      } else {
        setError(response.data.message || 'Failed to update profile.');
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
      <div className="relative bg-white rounded-xl shadow-2xl p-6 max-w-sm w-full border border-gray-100 z-10 animate-fade-in">
        <div className="flex justify-between items-center border-b pb-3 mb-4">
          <h3 className="text-lg font-bold text-gray-900">Manage Account Profile</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-500 text-xl font-semibold">&times;</button>
        </div>

        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}
        {success && <div className="mb-4 text-sm text-green-600 bg-green-50 p-2 rounded border border-green-200">{success}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">New Username</label>
            <input type="text" name="username" value={formData.username} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">New Email Address</label>
            <input type="email" name="email" value={formData.email} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Current Password</label>
            <input type="password" name="currentPassword" value={formData.currentPassword} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">New Password (Optional)</label>
            <input type="password" name="newPassword" value={formData.newPassword} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <button type="submit" disabled={loading} className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md text-sm transition-colors shadow-sm disabled:opacity-50 cursor-pointer">
            {loading ? 'Saving...' : 'Save Settings'}
          </button>
        </form>
      </div>
    </div>
  );
};