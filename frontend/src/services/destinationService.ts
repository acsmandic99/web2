import type { ResultDto } from '../types/shared/ResultDto';
import type { DestinationDto } from '../types/destination/DestinationDto';
import type { CreateDestinationDto } from '../types/destination/CreateDestinationDto';
import apiClient from './apiClient';

const DESTINATION_PREFIX = import.meta.env.VITE_DESTINATION_PREFIX;

export const destinationService = {
  async getTripDestinations(tripId: string): Promise<ResultDto<DestinationDto[]>> {
    const response = await apiClient.get<ResultDto<DestinationDto[]>>(`/${DESTINATION_PREFIX}/trip/${tripId}`);
    return response.data;
  },

  async addDestination(destination: CreateDestinationDto): Promise<ResultDto<DestinationDto>> {
    if (!destination.name.trim() || !destination.location.trim()) {
      throw new Error('Name and location are required fields.');
    }
    const response = await apiClient.post<ResultDto<DestinationDto>>(`/${DESTINATION_PREFIX}`, destination);
    return response.data;
  },

  async deleteDestination(id: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.delete<ResultDto<boolean>>(`/${DESTINATION_PREFIX}/${id}`);
    return response.data;
  }
};