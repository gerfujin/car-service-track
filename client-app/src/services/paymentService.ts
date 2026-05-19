import apiClient from './api'
import type { PaymentDto, PaymentCreateDto } from '@/types'

export const paymentService = {
  async getAll(filters?: { serviceOrderId?: string }): Promise<PaymentDto[]> {
    const params: Record<string, string> = {}
    if (filters?.serviceOrderId) params.serviceOrderId = filters.serviceOrderId
    const response = await apiClient.get<PaymentDto[]>('/payments', { params })
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
  },

  async create(data: PaymentCreateDto): Promise<PaymentDto> {
    const response = await apiClient.post<PaymentDto>('/payments', data)
    return response.data
  },

  async pay(id: string): Promise<void> {
    await apiClient.patch(`/payments/${id}/pay`)
  }
}
