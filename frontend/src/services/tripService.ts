import type { ResultDto } from '../types/shared/ResultDto';
import type { CreateTripDto } from '../types/trip/CreateTripDto';
import type { TripDto } from '../types/trip/TripDto';
import apiClient from './apiClient';

const TRIP_PREFIX = 'api/trips';

export const tripService = {
  async getUserTrips(userId: string): Promise<ResultDto<TripDto[]>> {
    const response = await apiClient.get<ResultDto<TripDto[]>>(`/${TRIP_PREFIX}/user/${userId}`);
    return response.data;
  },

  async getTripById(id: string): Promise<ResultDto<TripDto>> {
    const response = await apiClient.get<ResultDto<TripDto>>(`/${TRIP_PREFIX}/${id}`);
    return response.data;
  },

  async createTrip(trip: CreateTripDto): Promise<ResultDto<TripDto>> {
    if (trip.estimatedBudget < 0) {
      throw new Error('Budget cannot be a negative value.');
    }
    if (new Date(trip.startDate) > new Date(trip.endDate)) {
      throw new Error('End date cannot be scheduled before the start date.');
    }
    const response = await apiClient.post<ResultDto<TripDto>>(`/${TRIP_PREFIX}`, trip);
    return response.data;
  },

  async updateTrip(id: string, trip: CreateTripDto): Promise<ResultDto<TripDto>> {
    if (trip.estimatedBudget < 0) {
      throw new Error('Budget cannot be a negative value.');
    }
    if (new Date(trip.startDate) > new Date(trip.endDate)) {
      throw new Error('End date cannot be scheduled before the start date.');
    }
    const response = await apiClient.put<ResultDto<TripDto>>(`/${TRIP_PREFIX}/${id}`, trip);
    return response.data;
  },

  async deleteTrip(id: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.delete<ResultDto<boolean>>(`/${TRIP_PREFIX}/${id}`);
    return response.data;
  },

  async shareTrip(id: string, accessLevel: number): Promise<ResultDto<any>> {
    const response = await apiClient.post<ResultDto<any>>(`/${TRIP_PREFIX}/${id}/share?accessLevel=${accessLevel}`);
    return response.data;
  },

  async claimShareToken(token: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.post<ResultDto<boolean>>(`/${TRIP_PREFIX}/share/claim/${token}`);
    return response.data;
  }
};