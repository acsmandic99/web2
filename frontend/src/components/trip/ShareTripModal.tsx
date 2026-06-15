import React, { useState } from 'react';
import { tripService } from '../../services/tripService';

interface ShareTripModalProps {
  isOpen: boolean;
  tripId: string;
  onClose: () => void;
}

export const ShareTripModal: React.FC<ShareTripModalProps> = ({ isOpen, tripId, onClose }) => {
  const [accessLevel, setAccessLevel] = useState<number>(0);
  const [generatedLink, setGeneratedLink] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);

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

  const handleCopy = () => {
    if (!generatedLink) return;
    navigator.clipboard.writeText(generatedLink);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose}></div>
      <div className="relative bg-white rounded-xl shadow-2xl p-6 max-w-sm w-full border border-gray-100 z-10 animate-fade-in">
        <div className="flex justify-between items-center border-b pb-3 mb-4">
          <h3 className="text-lg font-bold text-gray-900">Invite Collaborators</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-500 text-xl font-semibold">&times;</button>
        </div>

        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}

        <form onSubmit={handleGenerateLink} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Permission Level</label>
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
            className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md text-sm transition-colors shadow-sm disabled:opacity-50"
          >
            {loading ? 'Generating...' : 'Generate Invitation Link'}
          </button>
        </form>

        {generatedLink && (
          <div className="mt-6 bg-gray-50 p-4 rounded-lg border border-gray-200 space-y-4 text-center">
            <div className="text-left space-y-2">
              <label className="block text-xs font-bold text-gray-600 uppercase tracking-wide">Share this link</label>
              <div className="flex space-x-2">
                <input
                  type="text"
                  readOnly
                  value={generatedLink}
                  className="flex-1 bg-white px-2 py-1.5 border rounded text-xs text-gray-600 outline-none select-all"
                />
                <button
                  onClick={handleCopy}
                  className={`px-3 py-1.5 font-semibold text-xs rounded text-white shadow-xs transition-colors ${copied ? 'bg-green-600' : 'bg-gray-800 hover:bg-gray-900'}`}
                >
                  {copied ? 'Copied!' : 'Copy'}
                </button>
              </div>
            </div>

            <div className="flex flex-col items-center justify-center pt-3 border-t border-gray-200/60">
              <label className="block text-xs font-bold text-gray-600 uppercase tracking-wide mb-2">Or scan QR Code</label>
              <div className="bg-white p-3 rounded-xl border border-gray-200 shadow-xs">
                <img
                  src={`https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodeURIComponent(generatedLink)}`}
                  alt="Trip Share QR Code"
                  className="w-[150px] h-[150px]"
                />
              </div>
            </div>
            
            <p className="text-[10px] text-gray-400 italic">Anyone with this link or QR code will be granted access to your plan.</p>
          </div>
        )}
      </div>
    </div>
  );
};