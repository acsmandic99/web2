import React, { useState, useEffect } from 'react';
import { activityService } from '../../services/activityService';
import { expenseService } from '../../services/expenseService';
import type { ActivityDto } from '../../types/activity/ActivityDto';
import type { ExpenseDto } from '../../types/expense/ExpenseDto';

interface TripOverviewProps {
  tripId: string;
  startDate: string;
  endDate: string;
  setActiveTab: (tab: 'overview' | 'destinations' | 'activities' | 'expenses' | 'checklist') => void;
}

export const TripOverview: React.FC<TripOverviewProps> = ({ tripId, startDate, endDate, setActiveTab }) => {
  const [activities, setActivities] = useState<ActivityDto[]>([]);
  const [expenses, setExpenses] = useState<ExpenseDto[]>([]);
  const [currentPage, setCurrentPage] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchAllData = async () => {
      try {
        const [actResult, expResult] = await Promise.all([
          activityService.getTripActivities(tripId),
          expenseService.getTripExpenses(tripId)
        ]);

        if (actResult.isSuccess && actResult.data) setActivities(actResult.data);
        if (expResult.isSuccess && expResult.data) setExpenses(expResult.data);
      } catch (err) {
      } {
        setLoading(false);
      }
    };
    fetchAllData();
  }, [tripId]);

  if (loading) return <div className="text-center text-sm text-gray-500 py-6">Loading timeline overview...</div>;

  const start = new Date(startDate);
  start.setHours(0, 0, 0, 0);
  const end = new Date(endDate);
  end.setHours(0, 0, 0, 0);
  
  const totalDays = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1;

  const daysArray = Array.from({ length: totalDays }, (_, index) => {
    const currentDaysDate = new Date(start);
    currentDaysDate.setDate(start.getDate() + index);
    currentDaysDate.setHours(0, 0, 0, 0);
    return currentDaysDate;
  });

  const daysPerPage = 5;
  const totalPages = Math.ceil(totalDays / daysPerPage);
  const startIndex = currentPage * daysPerPage;
  const visibleDays = daysArray.slice(startIndex, startIndex + daysPerPage);

  const getItemsForDate = (date: Date) => {
    const dateString = date.toDateString();

    const dayActivities = activities.filter(a => {
      const aDate = new Date(a.scheduledAt);
      aDate.setHours(0, 0, 0, 0);
      return aDate.toDateString() === dateString;
    });

    const dayExpenses = expenses.filter(e => {
      const eDate = new Date(e.incurredAt);
      eDate.setHours(0, 0, 0, 0);
      return eDate.toDateString() === dateString;
    });

    return { dayActivities, dayExpenses };
  };

  const formatDateFormatted = (date: Date) => {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}.`;
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center bg-white p-4 rounded-xl border border-gray-200 shadow-xs">
        <span className="text-sm font-medium text-gray-600">
          Showing days {startIndex + 1} - {Math.min(startIndex + daysPerPage, totalDays)} of {totalDays}
        </span>
        <div className="flex space-x-2">
          <button
            disabled={currentPage === 0}
            onClick={() => setCurrentPage(prev => prev - 1)}
            className="px-3 py-1.5 bg-gray-100 hover:bg-gray-200 text-gray-800 font-bold text-sm rounded-lg transition-colors disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            &larr; Previous 5 Days
          </button>
          <button
            disabled={currentPage >= totalPages - 1}
            onClick={() => setCurrentPage(prev => prev + 1)}
            className="px-3 py-1.5 bg-gray-100 hover:bg-gray-200 text-gray-800 font-bold text-sm rounded-lg transition-colors disabled:opacity-40 disabled:cursor-not-allowed cursor-pointer"
          >
            Next 5 Days &rarr;
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
        {visibleDays.map((date, index) => {
          const dayNumber = startIndex + index + 1;
          const { dayActivities, dayExpenses } = getItemsForDate(date);

          return (
            <div key={dayNumber} className="bg-white rounded-xl border border-gray-200 shadow-xs p-4 flex flex-col h-full min-h-[350px]">
              <div className="border-b border-gray-100 pb-2 mb-3">
                <h4 className="text-md font-black text-blue-600">Day {dayNumber}</h4>
                <p className="text-xs font-medium text-gray-400">{formatDateFormatted(date)}</p>
              </div>

              <div className="space-y-4 flex-1 overflow-y-auto">
                <div>
                  <h5 className="text-[11px] font-bold uppercase tracking-wider text-gray-400 mb-1">Activities</h5>
                  {dayActivities.length === 0 ? (
                    <p className="text-xs text-gray-400 italic">None</p>
                  ) : (
                    dayActivities.map(a => (
                      <button
                        key={a.id}
                        onClick={() => setActiveTab('activities')}
                        className="w-full text-left text-xs border-l-2 border-blue-500 pl-1.5 py-0.5 mb-1 bg-gray-50/50 hover:bg-blue-50 rounded-r transition-colors block cursor-pointer"
                      >
                        <p className="font-bold text-gray-800 truncate">{a.name}</p>
                        <p className="text-[10px] text-gray-400">{new Date(a.scheduledAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</p>
                      </button>
                    ))
                  )}
                </div>

                <div>
                  <h5 className="text-[11px] font-bold uppercase tracking-wider text-gray-400 mb-1">Expenses</h5>
                  {dayExpenses.length === 0 ? (
                    <p className="text-xs text-gray-400 italic">None</p>
                  ) : (
                    dayExpenses.map(e => (
                      <button
                        key={e.id}
                        onClick={() => setActiveTab('expenses')}
                        className="w-full flex justify-between items-center text-xs py-1 px-1 rounded hover:bg-red-50 text-left transition-colors cursor-pointer block"
                      >
                        <span className="text-gray-600 truncate mr-1 font-medium">&bull; {e.title}</span>
                        <span className="font-bold text-red-600 shrink-0">{e.amount}€</span>
                      </button>
                    ))
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};