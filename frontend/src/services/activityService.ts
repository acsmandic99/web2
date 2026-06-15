import type { ResultDto } from '../types/shared/ResultDto';
import type { ActivityDto } from '../types/activity/ActivityDto';
import type { CreateActivityDto } from '../types/activity/CreateActivityDto';
import apiClient from './apiClient';

const ACTIVITY_PREFIX = import.meta.env.VITE_ACTIVITY_PREFIX;

export const activityService = {
  async getTripActivities(tripId: string): Promise<ResultDto<ActivityDto[]>> {
    const response = await apiClient.get<ResultDto<ActivityDto[]>>(`/${ACTIVITY_PREFIX}/trip/${tripId}`);
    return response.data;
  },

  async addActivity(activity: CreateActivityDto): Promise<ResultDto<ActivityDto>> {
    if (!activity.name.trim() || !activity.location.trim() || !activity.scheduledAt) {
      throw new Error('Name, location, and schedule time are required fields.');
    }
    if (activity.price < 0) {
      throw new Error('Price cannot be a negative value.');
    }
    const response = await apiClient.post<ResultDto<ActivityDto>>(`/${ACTIVITY_PREFIX}`, activity);
    return response.data;
  },

  async deleteActivity(id: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.delete<ResultDto<boolean>>(`/${ACTIVITY_PREFIX}/${id}`);
    return response.data;
  }
};