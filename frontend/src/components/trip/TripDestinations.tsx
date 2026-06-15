import React, { useState, useEffect } from 'react';
import { destinationService } from '../../services/destinationService';
import type { DestinationDto } from '../../types/destination/DestinationDto';

interface TripDestinationsProps {
  tripId: string;
}

export const TripDestinations: React.FC<TripDestinationsProps> = ({ tripId }) => {
  const [destinations, setDestinations] = useState<DestinationDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    location: '',
    arrivalDate: '',
    departureDate: '',
    notes: ''
  });

  const fetchDestinations = async () => {
    try {
      const result = await destinationService.getTripDestinations(tripId);
      if (result.isSuccess && result.data) {
        setDestinations(result.data);
      }
    } catch (err: any) {
      setError(err.message);
    }
  };

  useEffect(() => {
    fetchDestinations();
  }, [tripId]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleAdd = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setError(null);

      if (!formData.name.trim() || !formData.location.trim() || !formData.arrivalDate || !formData.departureDate) {
        setError('All fields except notes are required.');
        return;
      }

      if (new Date(formData.arrivalDate) > new Date(formData.departureDate)) {
        setError('Departure date cannot be scheduled before the arrival date.');
        return;
      }

      await destinationService.addDestination({
        ...formData,
        tripId
      });
      
      setFormData({ name: '', location: '', arrivalDate: '', departureDate: '', notes: '' });
      fetchDestinations();
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      await destinationService.deleteDestination(id);
      fetchDestinations();
    } catch (err: any) {
      setError(err.message);
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div className="lg:col-span-1 bg-white p-6 rounded-lg border border-gray-200 shadow-sm h-fit">
        <h3 className="text-lg font-bold text-gray-900 mb-4">Add Destination</h3>
        {error && <div className="mb-4 text-sm text-red-600 bg-red-50 p-2 rounded border border-red-200">{error}</div>}
        <form onSubmit={handleAdd} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Destination Name</label>
            <input type="text" name="name" value={formData.name} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Location / City</label>
            <input type="text" name="location" value={formData.location} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Arrival Date</label>
              <input type="date" name="arrivalDate" value={formData.arrivalDate} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Departure Date</label>
              <input type="date" name="departureDate" value={formData.departureDate} onChange={handleChange} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
            <textarea name="notes" value={formData.notes} onChange={handleChange} rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <button type="submit" className="w-full py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-md text-sm transition-colors shadow-sm">Save Destination</button>
        </form>
      </div>

      <div className="lg:col-span-2 space-y-4">
        {destinations.length === 0 ? (
          <div className="bg-white p-6 rounded-lg border border-gray-200 text-center text-sm text-gray-500 shadow-sm">No destinations mapped out yet.</div>
        ) : (
          destinations.map((dest) => (
            <div key={dest.id} className="bg-white p-5 rounded-lg border border-gray-200 shadow-sm flex justify-between items-start">
              <div className="flex-1 min-w-0">
                <h4 className="text-md font-bold text-gray-900">{dest.name}</h4>
                <p className="text-xs text-gray-500 font-medium">{dest.location}</p>
                <p className="text-xs text-blue-600 font-semibold mt-1">
                  Stay: {new Date(dest.arrivalDate).toLocaleDateString()} - {new Date(dest.departureDate).toLocaleDateString()}
                </p>
                {dest.notes && <p className="text-sm text-gray-600 mt-2 bg-gray-50 p-2 rounded border border-gray-100">{dest.notes}</p>}
              </div>
              <button onClick={() => handleDelete(dest.id)} className="text-xs font-medium text-red-600 hover:text-red-800 transition-colors ml-4">Remove</button>
            </div>
          ))
        )}
      </div>
    </div>
  );
};