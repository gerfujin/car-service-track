import apiClient from './api'
import type { ProfileDto, ProfileUpdateDto, ProfileUpdateResponseDto } from '@/types'

export const profileService = {
  async getProfile(): Promise<ProfileDto> {
    const response = await apiClient.get<ProfileDto>('/profile')
    return response.data
  },

  async updateProfile(data: ProfileUpdateDto): Promise<ProfileUpdateResponseDto> {
    const response = await apiClient.put<ProfileUpdateResponseDto>('/profile', data)
    return response.data
  }
}
