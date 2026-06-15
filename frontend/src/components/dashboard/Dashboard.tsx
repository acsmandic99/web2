import React, { useState, useEffect } from 'react';
import { useAuth } from '../../context/AuthContext';
import { tripService } from '../../services/tripService';
import { CreateTripModal } from './CreateTripModal';
import { EditProfileModal } from './EditProfileModal';
import { useNavigate } from 'react-router-dom';
import type { TripDto } from '../../types/trip/TripDto';

export const Dashboard: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [trips, setTrips] = useState<TripDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [isProfileModalOpen, setIsProfileModalOpen] = useState<boolean>(false);

  const fetchTrips = async () => {
    if (!user?.id) return;
    try {
      setLoading(true);
      setError(null);
      const result = await tripService.getUserTrips(user.id);
      if (result.isSuccess && result.data) {
        setTrips(result.data);
      } else {
        setError(result.message || 'Failed to load travel plans.');
      }
    } catch (err: any) {
      setError(err.message || 'An error occurred while fetching your data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTrips();
  }, [user?.id]);

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}.${month}.${year}.`;
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <span className="text-xl font-bold text-blue-600 tracking-wide">
                TravelPlanner
              </span>
            </div>
            <div className="flex items-center space-x-4">
              <button 
                onClick={() => setIsProfileModalOpen(true)}
                className="text-right hover:bg-gray-50 p-1.5 rounded-lg transition-colors cursor-pointer"
              >
                <p className="text-sm font-bold text-gray-700">{user?.username}</p>
                <p className="text-xs text-gray-500">{user?.email}</p>
              </button>
              <button
                onClick={logout}
                className="ml-4 py-1.5 px-3.5 bg-red-50 text-red-600 hover:bg-red-100 font-medium rounded-md text-sm transition-colors border border-red-200 cursor-pointer"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div className="md:flex md:items-center md:justify-between mb-8">
          <div className="flex-1 min-w-0">
            <h2 className="text-2xl font-bold leading-7 text-gray-900 sm:text-3xl sm:truncate">
              My Travel Plans
            </h2>
            <p className="mt-1 text-sm text-gray-500">
              Manage your personal trips or create new itineraries.
            </p>
          </div>
          <div className="mt-4 flex md:mt-0 md:ml-4">
            <button
              type="button"
              onClick={() => setIsModalOpen(true)}
              className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors cursor-pointer"
            >
              Create New Trip
            </button>
          </div>
        </div>

        {error && (
          <div className="mb-6 p-4 bg-red-100 border border-red-400 text-red-700 rounded-md">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex justify-center items-center py-12">
            <div className="text-lg font-medium text-gray-500">Loading your trips...</div>
          </div>
        ) : trips.length === 0 ? (
          <div className="bg-white overflow-hidden shadow rounded-lg border border-gray-200">
            <div className="px-4 py-12 text-center sm:px-6">
              <p className="text-sm text-gray-500 mb-4">
                No trips found. Start by creating your first travel plan!
              </p>
              <button
                onClick={() => setIsModalOpen(true)}
                className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-md text-blue-700 bg-blue-100 hover:bg-blue-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 cursor-pointer"
              >
                Add First Trip
              </button>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {trips.map((trip) => (
              <div
                key={trip.id}
                className="bg-white overflow-hidden shadow rounded-lg border border-gray-200 hover:shadow-md transition-shadow flex flex-col justify-between"
              >
                <div className="p-5">
                  <h3 className="text-lg font-bold text-gray-900 truncate">{trip.title}</h3>
                  <p className="text-xs text-gray-500 mt-1">
                    {formatDate(trip.startDate)} - {formatDate(trip.endDate)}
                  </p>
                  <p className="text-sm text-gray-600 mt-3 line-clamp-3">{trip.description}</p>
                </div>
                <div className="bg-gray-50 px-5 py-3 border-t border-gray-200 flex justify-between items-center">
                  <span className="text-xs font-semibold text-gray-500">
                    Budget: <span className="text-blue-600">${trip.estimatedBudget}</span>
                  </span>
                  <button 
                    onClick={() => navigate(`/trip/${trip.id}`)}
                    className="text-xs font-bold text-blue-600 hover:text-blue-800 cursor-pointer"
                  >
                    View Details &rarr;
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>

      <CreateTripModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onTripCreated={fetchTrips}
      />

      <EditProfileModal 
        isOpen={isProfileModalOpen}
        onClose={() => setIsProfileModalOpen(false)}
      />
    </div>
  );
};