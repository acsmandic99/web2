import type { ResultDto } from '../types/shared/ResultDto';
import type { ChecklistItemDto } from '../types/checklist/ChecklistItemDto';
import type { CreateChecklistItemDto } from '../types/checklist/CreateChecklistItemDto';
import apiClient from './apiClient';

const CHECKLIST_PREFIX = import.meta.env.VITE_CHECKLIST_PREFIX;

export const checklistService = {
  async getItems(tripId: string): Promise<ResultDto<ChecklistItemDto[]>> {
    const response = await apiClient.get<ResultDto<ChecklistItemDto[]>>(`/${CHECKLIST_PREFIX}/trip/${tripId}`);
    return response.data;
  },

  async addItem(item: CreateChecklistItemDto): Promise<ResultDto<ChecklistItemDto>> {
    if (!item.title.trim()) {
      throw new Error('Item title cannot be empty.');
    }
    const response = await apiClient.post<ResultDto<ChecklistItemDto>>(`/${CHECKLIST_PREFIX}`, item);
    return response.data;
  },

  async toggleItem(tripId: string, itemId: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.put<ResultDto<boolean>>(`/${CHECKLIST_PREFIX}/trip/${tripId}/item/${itemId}/toggle`);
    return response.data;
  },

  async deleteItem(tripId: string, itemId: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.delete<ResultDto<boolean>>(`/${CHECKLIST_PREFIX}/trip/${tripId}/item/${itemId}`);
    return response.data;
  }
};