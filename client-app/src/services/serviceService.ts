import apiClient from './api'
import type { ServiceDto } from '@/types'

export interface ServiceUpsertDto {
  name: string
  description: string
  basePrice: number
  estimatedTimeMinutes: number
}

export const serviceService = {
  async getAll(): Promise<ServiceDto[]> {
    const response = await apiClient.get<ServiceDto[]>('/Services')
    return response.data
  },

  async getById(id: string): Promise<ServiceDto> {
    const response = await apiClient.get<ServiceDto>(`/Services/${id}`)
    return response.data
  },

  async create(data: ServiceUpsertDto): Promise<ServiceDto> {
    const response = await apiClient.post<ServiceDto>('/Services', data)
    return response.data
  },

  async update(id: string, data: ServiceUpsertDto): Promise<void> {
    await apiClient.put(`/Services/${id}`, data)
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/Services/${id}`)
  }
}
