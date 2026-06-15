import React, { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { tripService } from '../../services/tripService';

export const ClaimShare: React.FC = () => {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const hasCalled = useRef(false);

  useEffect(() => {
    const processClaim = async () => {
      if (!token || hasCalled.current) return;
      try {
        hasCalled.current = true;
        const result = await tripService.claimShareToken(token);
        if (result.isSuccess) {
          navigate('/dashboard');
        } else {
          setError(result.message || 'The token is invalid or has already been claimed.');
        }
      } catch (err: any) {
        setError(err.response?.data?.message || err.message || 'Failed to process invitation.');
      }
    };

    processClaim();
  }, [token, navigate]);

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white p-8 rounded-xl shadow-md border max-w-sm w-full text-center">
        {error ? (
          <div>
            <div className="w-12 h-12 bg-red-100 text-red-600 rounded-full flex items-center justify-center mx-auto mb-4 text-xl font-bold">!</div>
            <h3 className="text-lg font-bold text-gray-900 mb-2">Invitation Error</h3>
            <p className="text-sm text-gray-500 mb-6">{error}</p>
            <button onClick={() => navigate('/dashboard')} className="w-full py-2 bg-gray-800 text-white font-medium rounded-md text-sm hover:bg-gray-900 transition-colors cursor-pointer">Go to Dashboard</button>
          </div>
        ) : (
          <div>
            <div className="w-12 h-12 bg-blue-50 text-blue-600 rounded-full flex items-center justify-center mx-auto mb-4 text-xl animate-spin border-2 border-t-transparent border-blue-600"></div>
            <h3 className="text-lg font-bold text-gray-900 mb-2">Processing Invitation</h3>
            <p className="text-sm text-gray-500">Adding you to the travel plan, please hold on...</p>
          </div>
        )}
      </div>
    </div>
  );
};