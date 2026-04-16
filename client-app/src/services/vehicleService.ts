import apiClient from './api'
import type { VehicleDto, VehicleCreateDto } from '@/types'

export const vehicleService = {
  async getAll(): Promise<VehicleDto[]> {
    const response = await apiClient.get<VehicleDto[]>('/vehicles')
    return response.data
  },

  async getById(id: string): Promise<VehicleDto> {
    const response = await apiClient.get<VehicleDto>(`/vehicles/${id}`)
    return response.data
  },

  async create(data: VehicleCreateDto): Promise<VehicleDto> {
    const response = await apiClient.post<VehicleDto>('/vehicles', data)
    return response.data
  },

  async update(id: string, data: VehicleCreateDto): Promise<void> {
    await apiClient.put(`/vehicles/${id}`, data)
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/vehicles/${id}`)
  }
}
