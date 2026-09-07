import { defineStore } from 'pinia'
import { ref } from 'vue'
import { profileService } from '@/services/profileService'
import { useAuthStore } from '@/stores/auth'
import type { ProfileDto, ProfileUpdateDto } from '@/types'

export const useProfileStore = defineStore('profile', () => {
  const profile = ref<ProfileDto | null>(null)
  const loading = ref(false)
  const saving = ref(false)

  async function loadProfile() {
    loading.value = true
    try {
      profile.value = await profileService.getProfile()
    } finally {
      loading.value = false
    }
  }

  async function saveProfile(data: ProfileUpdateDto): Promise<ProfileDto> {
    saving.value = true
    try {
      const response = await profileService.updateProfile(data)
      profile.value = response.profile
      // Swap only the access token through the auth store's dedicated helper — the refresh
      // token slot is untouched, and displayName/userName re-decode automatically.
      useAuthStore().updateJwt(response.jwt)
      return response.profile
    } finally {
      saving.value = false
    }
  }

  return {
    profile,
    loading,
    saving,
    loadProfile,
    saveProfile
  }
})
