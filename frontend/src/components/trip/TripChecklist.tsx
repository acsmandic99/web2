import React, { useState, useEffect } from 'react';
import { checklistService } from '../../services/checklistService';
import type { ChecklistItemDto } from '../../types/checklist/ChecklistItemDto';

interface TripChecklistProps {
  tripId: string;
}

export const TripChecklist: React.FC<TripChecklistProps> = ({ tripId }) => {
  const [items, setItems] = useState<ChecklistItemDto[]>([]);
  const [newTitle, setNewTitle] = useState('');
  const [error, setError] = useState<string | null>(null);

  const fetchItems = async () => {
    try {
      const result = await checklistService.getItems(tripId);
      if (result.isSuccess && result.data) {
        setItems(result.data);
      }
    } catch (err: any) {
      setError(err.message);
    }
  };

  useEffect(() => {
    fetchItems();
  }, [tripId]);

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitle.trim()) return;
    try {
      setError(null);
      await checklistService.addItem({ title: newTitle, tripId });
      setNewTitle('');
      fetchItems();
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleToggle = async (itemId: string) => {
    try {
      await checklistService.toggleItem(tripId, itemId);
      fetchItems();
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleDelete = async (itemId: string) => {
    try {
      await checklistService.deleteItem(tripId, itemId);
      fetchItems();
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <div className="bg-white p-6 rounded-lg border border-gray-200 shadow-sm">
      <h3 className="text-lg font-bold text-gray-900 mb-2">My Private Packing List</h3>
      <p className="text-xs text-gray-500 mb-4">Items inside this list are strictly confidential and visible only to you.</p>

      {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}

      <form onSubmit={handleAddItem} className="flex space-x-2 mb-6">
        <input
          type="text"
          value={newTitle}
          onChange={(e) => setNewTitle(e.target.value)}
          placeholder="e.g., Passport, Charger, Underwear"
          className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
        />
        <button type="submit" className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md text-sm transition-colors">
          Add
        </button>
      </form>

      {items.length === 0 ? (
        <p className="text-sm text-gray-500 text-center py-4">Your packing list is empty.</p>
      ) : (
        <ul className="divide-y divide-gray-100">
          {items.map((item) => (
            <li key={item.id} className="flex items-center justify-between py-3">
              <div className="flex items-center space-x-3">
                <input
                  type="checkbox"
                  checked={item.isCompleted}
                  onChange={() => handleToggle(item.id)}
                  className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                />
                <span className={`text-sm ${item.isCompleted ? 'line-through text-gray-400' : 'text-gray-700'}`}>
                  {item.title}
                </span>
              </div>
              <button onClick={() => handleDelete(item.id)} className="text-xs font-medium text-red-600 hover:text-red-800 transition-colors">
                Delete
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};