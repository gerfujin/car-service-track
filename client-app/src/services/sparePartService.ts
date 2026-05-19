import apiClient from './api'
import type { SparePartDto, SparePartCreateDto, SparePartUpdateDto } from '@/types'

export const sparePartService = {
  async getAll(): Promise<SparePartDto[]> {
    const response = await apiClient.get<SparePartDto[]>('/SpareParts')
    return response.data
  },

  async getById(id: string): Promise<SparePartDto> {
    const response = await apiClient.get<SparePartDto>(`/SpareParts/${id}`)
    return response.data
  },

  async create(dto: SparePartCreateDto): Promise<SparePartDto> {
    const response = await apiClient.post<SparePartDto>('/SpareParts', dto)
    return response.data
  },

  async update(id: string, dto: SparePartUpdateDto): Promise<void> {
    await apiClient.put(`/SpareParts/${id}`, dto)
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/SpareParts/${id}`)
  }
}

