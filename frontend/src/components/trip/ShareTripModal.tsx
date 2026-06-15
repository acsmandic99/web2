import React, { useState, useEffect } from 'react';
import apiClient from '../../services/apiClient';
import { tripService } from '../../services/tripService';
import type { ResultDto } from '../../types/shared/ResultDto';

interface ShareTripModalProps {
  isOpen: boolean;
  tripId: string;
  onClose: () => void;
}

interface Collaborator {
  userId: string;
  username: string;
  email: string;
  accessLevel: string;
}

export const ShareTripModal: React.FC<ShareTripModalProps> = ({ isOpen, tripId, onClose }) => {
  const [accessLevel, setAccessLevel] = useState<number>(0);
  const [generatedLink, setGeneratedLink] = useState<string | null>(null);
  const [collaborators, setCollaborators] = useState<Collaborator[]>([]);
  const [pendingLevels, setPendingLevels] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);

  const fetchCollaborators = async () => {
    try {
      const response = await apiClient.get<ResultDto<Collaborator[]>>(`/api/trips/${tripId}/collaborators`);
      if (response.data.isSuccess && response.data.data) {
        setCollaborators(response.data.data);
        setPendingLevels({});
      }
    } catch (err) {}
  };

  useEffect(() => {
    if (isOpen) {
      fetchCollaborators();
      setGeneratedLink(null);
      setError(null);
    }
  }, [isOpen, tripId]);

  if (!isOpen) return null;

  const handleGenerateLink = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setError(null);
      setLoading(true);
      setCopied(false);
      
      const result = await tripService.shareTrip(tripId, accessLevel);
      if (result.isSuccess && result.data) {
        const token = result.data.token;
        const fullLink = `${window.location.origin}/share/claim/${token}`;
        setGeneratedLink(fullLink);
      } else {
        setError(result.message || 'Failed to generate token.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'An error occurred.');
    } finally {
      setLoading(false);
    }
  };

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
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to revoke access.');
    }
  };

  const handleCopy = () => {
    if (!generatedLink) return;
    navigator.clipboard.writeText(generatedLink);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose}></div>
      <div className="relative bg-white rounded-xl shadow-2xl p-6 max-w-md w-full border border-gray-100 z-10 max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center border-b pb-3 mb-4">
          <h3 className="text-lg font-bold text-gray-900">Trip Sharing & Collaborators</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-500 text-xl font-semibold cursor-pointer">&times;</button>
        </div>

        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}

        <form onSubmit={handleGenerateLink} className="space-y-4 border-b pb-5 mb-5">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Create New Invite Link</label>
            <select
              value={accessLevel}
              onChange={(e) => setAccessLevel(parseInt(e.target.value))}
              className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
            >
              <option value={0}>Viewer (Read Only)</option>
              <option value={1}>Editor (Can Modify)</option>
            </select>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold text-sm rounded-md transition-colors shadow-sm disabled:opacity-50 cursor-pointer"
          >
            {loading ? 'Generating...' : 'Generate Invitation Link'}
          </button>
        </form>

        {generatedLink && (
          <div className="mb-6 bg-gray-50 p-4 rounded-lg border border-gray-200 space-y-4 text-center">
            <div className="text-left space-y-2">
              <label className="block text-xs font-bold text-gray-600 uppercase tracking-wide">Share this link</label>
              <div className="flex space-x-2">
                <input type="text" readOnly value={generatedLink} className="flex-1 bg-white px-2 py-1.5 border rounded text-xs text-gray-600 outline-none select-all" />
                <button onClick={handleCopy} className={`px-3 py-1.5 font-semibold text-xs rounded text-white shadow-xs transition-colors ${copied ? 'bg-green-600' : 'bg-gray-800 hover:bg-gray-900'}`}>{copied ? 'Copied!' : 'Copy'}</button>
              </div>
            </div>
            <div className="flex flex-col items-center justify-center pt-2 border-t border-gray-200/60">
              <div className="bg-white p-2 rounded-xl border border-gray-200 shadow-xs">
                <img src={`https://api.qrserver.com/v1/create-qr-code/?size=120x150&data=${encodeURIComponent(generatedLink)}`} alt="QR Code" className="w-[120px] h-[120px]" />
              </div>
            </div>
          </div>
        )}

        <div>
          <h4 className="text-xs font-bold text-gray-600 uppercase tracking-wide mb-3">Users with Access</h4>
          {collaborators.length === 0 ? (
            <p className="text-xs text-gray-400 italic">This trip is currently private.</p>
          ) : (
            <div className="space-y-3 max-h-48 overflow-y-auto pr-1">
              {collaborators.map((col) => {
                const currentPending = pendingLevels[col.userId];
                const hasChanged = currentPending !== undefined && currentPending !== col.accessLevel;

                return (
                  <div key={col.userId} className="flex items-center justify-between p-2.5 bg-gray-50 rounded-lg border border-gray-100 text-sm">
                    <div className="min-w-0 flex-1 mr-3">
                      <p className="font-bold text-gray-800 truncate">{col.username}</p>
                      <p className="text-[11px] text-gray-400 truncate">{col.email}</p>
                    </div>
                    <div className="flex items-center space-x-1.5 shrink-0">
                      <select
                            value={currentPending !== undefined ? currentPending : col.accessLevel}
                            onChange={(e) => handleDropdownChange(col.userId, e.target.value)}
                            className="px-2 py-1 border border-gray-300 rounded text-xs bg-white focus:outline-none"
                            >
                            <option value="Viewer">Viewer</option>
                            <option value="Editor">Editor</option>
                        </select>

                      {hasChanged && (
                        <button
                          onClick={() => handleUpdatePermission(col.userId)}
                          className="px-2 py-1 bg-green-600 hover:bg-green-700 text-white font-bold text-xs rounded transition-colors cursor-pointer"
                        >
                          ✓
                        </button>
                      )}

                      <button
                        onClick={() => handleRevokePermission(col.userId)}
                        className="text-xs font-bold text-red-600 hover:text-red-800 transition-colors cursor-pointer ml-1"
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
      </div>
    </div>
  );
};