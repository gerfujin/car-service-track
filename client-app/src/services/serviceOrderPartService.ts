import apiClient from './api'
import type { ServiceOrderPartDto, ServiceOrderPartCreateDto, ServiceOrderPartUpdateDto } from '@/types'

export const serviceOrderPartService = {
  async getByOrder(orderId: string): Promise<ServiceOrderPartDto[]> {
    const response = await apiClient.get<ServiceOrderPartDto[]>(`/ServiceOrderParts?serviceOrderId=${orderId}`)
    return response.data
  },

  async getById(id: string): Promise<ServiceOrderPartDto> {
    const response = await apiClient.get<ServiceOrderPartDto>(`/ServiceOrderParts/${id}`)
    return response.data
  },

  async create(dto: ServiceOrderPartCreateDto): Promise<ServiceOrderPartDto> {
    const response = await apiClient.post<ServiceOrderPartDto>('/ServiceOrderParts', dto)
    return response.data
  },

  async update(id: string, dto: ServiceOrderPartUpdateDto): Promise<void> {
    await apiClient.put(`/ServiceOrderParts/${id}`, dto)
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/ServiceOrderParts/${id}`)
  }
}

