import apiClient from './api'
import type { WorkshopDto } from '@/types'

export const workshopService = {
  async getAll(): Promise<WorkshopDto[]> {
    const response = await apiClient.get<WorkshopDto[]>('/workshops')
    return response.data
  }
}
