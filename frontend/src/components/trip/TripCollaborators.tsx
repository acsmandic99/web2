import React, { useState, useEffect } from 'react';
import apiClient from '../../services/apiClient';
import type { ResultDto } from '../../types/shared/ResultDto';

interface TripCollaboratorsProps {
  tripId: string;
}

interface Collaborator {
  userId: string;
  username: string;
  email: string;
  accessLevel: string;
}

export const TripCollaborators: React.FC<TripCollaboratorsProps> = ({ tripId }) => {
  const [collaborators, setCollaborators] = useState<Collaborator[]>([]);
  const [pendingLevels, setPendingLevels] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchCollaborators = async () => {
    try {
      const response = await apiClient.get<ResultDto<Collaborator[]>>(`/api/trips/${tripId}/collaborators`);
      if (response.data.isSuccess && response.data.data) {
        setCollaborators(response.data.data);
        setPendingLevels({});
      } else {
        setError(response.data.message || 'Failed to load collaborators.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'An error occurred.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCollaborators();
  }, [tripId]);

  const handleDropdownChange = (userId: string, value: string) => {
    setPendingLevels((prev) => ({
      ...prev,
      [userId]: value,
    }));
  };

  const handleUpdatePermission = async (userId: string) => {
    const newLevel = pendingLevels[userId];
    if (!newLevel) return;

    try {
      setError(null);
      const response = await apiClient.put<ResultDto<boolean>>(`/api/trips/${tripId}/collaborators/${userId}?accessLevel=${newLevel}`);
      if (response.data.isSuccess) {
        await fetchCollaborators();
      } else {
        setError(response.data.message || 'Failed to update permission.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update permission.');
    }
  };

  const handleRevokePermission = async (userId: string) => {
    try {
      setError(null);
      const response = await apiClient.delete<ResultDto<boolean>>(`/api/trips/${tripId}/collaborators/${userId}`);
      if (response.data.isSuccess) {
        await fetchCollaborators();
      } else {
        setError(response.data.message || 'Failed to revoke access.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to revoke access.');
    }
  };

  if (loading) return <div className="text-center text-sm text-gray-500 py-6">Loading collaborators...</div>;

  return (
    <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm max-w-2xl">
      <div className="border-b border-gray-100 pb-3 mb-4">
        <h3 className="text-lg font-bold text-gray-900">Manage Trip Access</h3>
        <p className="text-xs text-gray-500">View and adjust permissions for users who joined via share link.</p>
      </div>

      {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}

      {collaborators.length === 0 ? (
        <p className="text-sm text-gray-400 italic py-4">No collaborators have joined this trip yet.</p>
      ) : (
        <div className="space-y-3">
          {collaborators.map((col) => {
            const currentPending = pendingLevels[col.userId];
            const hasChanged = currentPending !== undefined && currentPending !== col.accessLevel;

            return (
              <div key={col.userId} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200 shadow-xs">
                <div className="min-w-0 flex-1 mr-4">
                  <p className="text-sm font-bold text-gray-900 truncate">{col.username}</p>
                  <p className="text-xs text-gray-400 truncate">{col.email}</p>
                </div>
                <div className="flex items-center space-x-2 shrink-0">
                  <select
                        value={currentPending !== undefined ? currentPending : col.accessLevel}
                        onChange={(e) => handleDropdownChange(col.userId, e.target.value)}
                        className="px-2.5 py-1.5 border border-gray-300 rounded-lg text-xs bg-white focus:outline-none focus:ring-2 focus:ring-blue-500 font-medium"
                        >
                        <option value="Viewer">Viewer</option>
                        <option value="Editor">Editor</option>
                    </select>

                  {hasChanged && (
                    <button
                      onClick={() => handleUpdatePermission(col.userId)}
                      className="px-2.5 py-1.5 bg-green-600 hover:bg-green-700 text-white font-bold text-xs rounded-lg transition-colors cursor-pointer"
                    >
                      Confirm
                    </button>
                  )}

                  <button
                    onClick={() => handleRevokePermission(col.userId)}
                    className="px-3 py-1.5 bg-red-50 hover:bg-red-100 text-red-600 font-bold text-xs rounded-lg border border-red-200 transition-colors cursor-pointer"
                  >
                    Revoke
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};