import apiClient from './api'
import type { ServiceOrderDto, ServiceOrderCreateDto, StatusHistoryEntry } from '@/types'

export const orderService = {
  async getAll(): Promise<ServiceOrderDto[]> {
    const response = await apiClient.get<ServiceOrderDto[]>('/serviceorders')
    return response.data
  },

  async getById(id: string): Promise<ServiceOrderDto> {
    const response = await apiClient.get<ServiceOrderDto>(`/serviceorders/${id}`)
    return response.data
  },

  async create(data: ServiceOrderCreateDto): Promise<ServiceOrderDto> {
    const response = await apiClient.post<ServiceOrderDto>('/serviceorders', data)
    return response.data
  },

  async getStatusHistory(id: string): Promise<StatusHistoryEntry[]> {
    const response = await apiClient.get<StatusHistoryEntry[]>(`/serviceorders/${id}/status-history`)
    return response.data
  },

  // Admin/Mechanic only: update order status
  async updateStatus(id: string, status: string, notes?: string): Promise<void> {
    await apiClient.patch(`/serviceorders/${id}/status`, { status, notes })
  }
}
