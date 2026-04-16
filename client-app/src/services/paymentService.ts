import apiClient from './api'
import type { PaymentDto } from '@/types'

export const paymentService = {
  async getAll(): Promise<PaymentDto[]> {
    const response = await apiClient.get<PaymentDto[]>('/payments')
    return response.data
  },

  async getById(id: string): Promise<PaymentDto> {
    const response = await apiClient.get<PaymentDto>(`/payments/${id}`)
    return response.data
  },

  async getByOrderId(serviceOrderId: string): Promise<PaymentDto | null> {
    try {
      const response = await apiClient.get<PaymentDto>(`/payments/by-order/${serviceOrderId}`)
      return response.data
    } catch {
      return null
    }
  }
}
