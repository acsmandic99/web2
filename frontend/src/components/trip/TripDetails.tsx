import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { tripService } from '../../services/tripService';
import { expenseService } from '../../services/expenseService';
import { TripDestinations } from './TripDestinations';
import { TripActivities } from './TripActivities';
import { TripExpenses } from './TripExpenses';
import { TripChecklist } from './TripChecklist';
import { TripOverview } from './TripOverview';
import { TripCollaborators } from './TripCollaborators';
import { ShareTripModal } from './ShareTripModal';
import { EditTripModal } from './EditTripModal';
import type { TripDto } from '../../types/trip/TripDto';
import type { BudgetSummaryDto } from '../../types/expense/BudgetSummaryDto';

export const TripDetails: React.FC = () => {
  const { tripId } = useParams<{ tripId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [trip, setTrip] = useState<TripDto | null>(null);
  const [budgetSummary, setBudgetSummary] = useState<BudgetSummaryDto | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'destinations' | 'activities' | 'expenses' | 'checklist' | 'collaborators'>('overview');
  const [isShareModalOpen, setIsShareModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchTripDetails = async () => {
    if (!tripId) return;
    try {
      const result = await tripService.getTripById(tripId);
      if (result.isSuccess && result.data) {
        setTrip(result.data);
        await fetchBudget();
      } else {
        setError(result.message);
      }
    } catch (err: any) {
      setError(err.message || 'Access denied to this trip.');
    } finally {
      setLoading(false);
    }
  };

  const fetchBudget = async () => {
    if (!tripId) return;
    try {
      const result = await expenseService.getBudgetSummary(tripId);
      if (result.isSuccess && result.data) {
        setBudgetSummary(result.data);
      }
    } catch (err) {}
  };

  useEffect(() => {
    fetchTripDetails();
  }, [tripId]);

  const handleDeleteTrip = async () => {
    if (!trip || !window.confirm('Are you sure you want to permanently delete this trip plan?')) return;
    try {
      const result = await tripService.deleteTrip(trip.id);
      if (result.isSuccess) {
        navigate('/dashboard');
      }
    } catch (err) {}
  };

  if (loading) return <div className="min-h-screen flex items-center justify-center text-gray-500">Loading details...</div>;
  if (error || !trip) return <div className="min-h-screen flex items-center justify-center text-red-600">{error || 'Trip plan not found.'}</div>;

  const isBudgetExceeded = budgetSummary ? budgetSummary.remainingBudget < 0 : false;
  const isOwner = user?.id === trip.userId;

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}.${month}.${year}.`;
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 py-6 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex justify-between items-start">
          <div className="space-y-1">
            <button onClick={() => navigate('/dashboard')} className="text-sm font-bold text-blue-600 hover:text-blue-800 mb-2 block">&larr; Back to Dashboard</button>
            <div className="flex items-center space-x-3">
              <h1 className="text-3xl font-black text-gray-900 tracking-tight">{trip.title}</h1>
              {isOwner && (
                <>
                  <button onClick={() => setIsEditModalOpen(true)} className="text-xs font-bold text-blue-600 hover:text-blue-800 cursor-pointer">Edit</button>
                  <button onClick={handleDeleteTrip} className="text-xs font-bold text-red-600 hover:text-red-800 cursor-pointer">Delete</button>
                </>
              )}
            </div>
            <p className="text-xs font-semibold text-gray-400 bg-gray-100 px-2 py-1 rounded-md w-fit">
              Duration: {formatDate(trip.startDate)} - {formatDate(trip.endDate)}
            </p>
            <p className="text-sm text-gray-500 pt-1">{trip.description}</p>
          </div>
          
          <div className="flex items-center space-x-3">
            {budgetSummary && (
              <div className={`p-4 rounded-xl border text-right shadow-sm transition-colors min-w-[140px] ${isBudgetExceeded ? 'bg-red-600 border-red-700 text-white' : 'bg-green-50 border-green-200 text-gray-900'}`}>
                <span className={`text-[10px] font-bold block uppercase tracking-wide ${isBudgetExceeded ? 'text-red-100' : 'text-gray-500'}`}>Remaining Budget</span>
                <span className="text-xl font-black">
                  {budgetSummary.remainingBudget} EUR
                </span>
              </div>
            )}
          </div>
        </div>
      </header>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-6">
        <div className="border-b border-gray-200 mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between pb-4 sm:pb-0 gap-4">
          <nav className="-mb-px flex space-x-8">
            <button onClick={() => setActiveTab('overview')} className={`pb-4 px-1 border-b-2 font-bold text-sm transition-colors cursor-pointer ${activeTab === 'overview' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}>Overview</button>
            <button onClick={() => setActiveTab('destinations')} className={`pb-4 px-1 border-b-2 font-bold text-sm transition-colors cursor-pointer ${activeTab === 'destinations' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}>Destinations</button>
            <button onClick={() => setActiveTab('activities')} className={`pb-4 px-1 border-b-2 font-bold text-sm transition-colors cursor-pointer ${activeTab === 'activities' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}>Activities</button>
            <button onClick={() => setActiveTab('expenses')} className={`pb-4 px-1 border-b-2 font-bold text-sm transition-colors cursor-pointer ${activeTab === 'expenses' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}>Expenses & Budget</button>
            <button onClick={() => setActiveTab('checklist')} className={`pb-4 px-1 border-b-2 font-bold text-sm transition-colors cursor-pointer ${activeTab === 'checklist' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}>Packing List</button>
            {isOwner && (
              <button onClick={() => setActiveTab('collaborators')} className={`pb-4 px-1 border-b-2 font-bold text-sm transition-colors cursor-pointer ${activeTab === 'collaborators' ? 'border-blue-500 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'}`}>Collaborators</button>
            )}
          </nav>
          
          <button
            onClick={() => setIsShareModalOpen(true)}
            className="sm:mb-3 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs rounded-xl shadow-md transition-colors cursor-pointer"
          >
            Share Trip
          </button>
        </div>

        {activeTab === 'overview' && <TripOverview tripId={trip.id} startDate={trip.startDate} endDate={trip.endDate} setActiveTab={setActiveTab} />}
        {activeTab === 'destinations' && <TripDestinations tripId={trip.id} />}
        {activeTab === 'activities' && <TripActivities tripId={trip.id} isBudgetExceeded={isBudgetExceeded} onBudgetChange={fetchBudget} />}
        {activeTab === 'expenses' && <TripExpenses tripId={trip.id} budgetSummary={budgetSummary} onBudgetChange={fetchBudget} />}
        {activeTab === 'checklist' && <TripChecklist tripId={trip.id} />}
        {activeTab === 'collaborators' && isOwner && <TripCollaborators tripId={trip.id} />}
      </div>

      <ShareTripModal isOpen={isShareModalOpen} tripId={trip.id} onClose={() => setIsShareModalOpen(false)} />
      <EditTripModal isOpen={isEditModalOpen} trip={trip} onClose={() => setIsEditModalOpen(false)} onTripUpdated={fetchTripDetails} />
    </div>
  );
};