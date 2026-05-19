import apiClient, { API_BASE_URL } from './api'
import type { RepairPhotoDto } from '@/types'

export const repairPhotoService = {
  /**
   * Get all photos for a service order.
   * Admin/Mechanic: any order. Owner: own order only (IDOR enforced server-side).
   */
  async getByOrder(orderId: string): Promise<RepairPhotoDto[]> {
    const response = await apiClient.get<RepairPhotoDto[]>('/RepairPhotos', {
      params: { serviceOrderId: orderId }
    })
    return response.data
  },

  /**
   * Get a single repair photo by ID.
   */
  async getById(id: string): Promise<RepairPhotoDto> {
    const response = await apiClient.get<RepairPhotoDto>(`/RepairPhotos/${id}`)
    return response.data
  },

  /**
   * Upload a new repair photo.
   * formData must contain: File, ServiceOrderId, Description
   */
  async upload(formData: FormData): Promise<RepairPhotoDto> {
    const response = await apiClient.post<RepairPhotoDto>('/RepairPhotos', formData)
    return response.data
  },

  /**
   * Delete a repair photo by ID.
   */
  async delete(id: string): Promise<void> {
    await apiClient.delete(`/RepairPhotos/${id}`)
  },

  /**
   * Build the full URL for displaying a photo.
   * photoUrl from backend is a relative path like "/uploads/repair-photos/{filename}".
   */
  getFullUrl(photoUrl: string): string {
    return `${API_BASE_URL}${photoUrl}`
  }
}