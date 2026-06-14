import type { ChecklistItemDto } from "../types/ChecklistItemDto";
import type { CreateChecklistItemDto } from "../types/CreateChecklistItemDto";
import type { ResultDto } from "../types/shared/ResultDto";
import apiClient from "./apiClient";


export const checklistService = {
  async addItem(item: CreateChecklistItemDto): Promise<ResultDto<ChecklistItemDto>> {
    if (!item.title.trim()) {
      throw new Error('Item title cannot be empty.');
    }
    const response = await apiClient.post<ResultDto<ChecklistItemDto>>('/api/checklists', item);
    return response.data;
  },

  async getItems(tripId: string): Promise<ResultDto<ChecklistItemDto[]>> {
    const response = await apiClient.get<ResultDto<ChecklistItemDto[]>>(`/api/checklists/trip/${tripId}`);
    return response.data;
  },

  async toggleItem(tripId: string, itemId: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.put<ResultDto<boolean>>(`/api/checklists/trip/${tripId}/item/${itemId}/toggle`);
    return response.data;
  },

  async deleteItem(tripId: string, itemId: string): Promise<ResultDto<boolean>> {
    const response = await apiClient.delete<ResultDto<boolean>>(`/api/checklists/trip/${tripId}/item/${itemId}`);
    return response.data;
  }
};