import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authService } from '@/services/authService'
import { tokenStorage } from '@/services/api'
import type { LoginInfo, RegisterInfo } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  const jwt = ref<string | null>(tokenStorage.getJwt())
  const refreshToken = ref<string | null>(tokenStorage.getRefreshToken())
  const userEmail = ref<string | null>(tokenStorage.getUserEmail())

  const isAuthenticated = computed(() => {
    if (!jwt.value) return false
    return !authService.isJwtExpired(jwt.value)
  })

  const displayName = computed(() => userEmail.value || 'User')

  // Decode role(s) from JWT payload
  // ASP.NET Identity uses ClaimTypes.Role which maps to this URI
  const userRoles = computed((): string[] => {
    if (!jwt.value) return []
    try {
      const payload = JSON.parse(atob(jwt.value.split('.')[1]))
      const roleKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
      const raw = payload[roleKey] || payload['role'] || payload['roles']
      if (!raw) return []
      if (Array.isArray(raw)) return raw.map((r: string) => r.toLowerCase())
      return [String(raw).toLowerCase()]
    } catch {
      return []
    }
  })

  const isAdmin = computed(() => userRoles.value.includes('admin'))
  const isMechanic = computed(() => userRoles.value.includes('mechanic'))
  const isClient = computed(() => userRoles.value.includes('client'))

  function restoreFromStorage() {
    const storedJwt = tokenStorage.getJwt()
    const storedRefresh = tokenStorage.getRefreshToken()
    const storedEmail = tokenStorage.getUserEmail()

    if (storedJwt && !authService.isJwtExpired(storedJwt)) {
      jwt.value = storedJwt
      refreshToken.value = storedRefresh
      userEmail.value = storedEmail
    } else {
      tokenStorage.clearTokens()
      jwt.value = null
      refreshToken.value = null
      userEmail.value = null
    }
  }

  async function login(data: LoginInfo) {
    const response = await authService.login(data)
    const email = authService.getEmailFromJwt(response.jwt) || data.email
    tokenStorage.setTokens(response.jwt, response.refreshToken, email)
    jwt.value = response.jwt
    refreshToken.value = response.refreshToken
    userEmail.value = email
    return response
  }

  async function register(data: RegisterInfo) {
    const response = await authService.register(data)
    const email = authService.getEmailFromJwt(response.jwt) || data.email
    tokenStorage.setTokens(response.jwt, response.refreshToken, email)
    jwt.value = response.jwt
    refreshToken.value = response.refreshToken
    userEmail.value = email
    return response
  }

  async function logout() {
    if (refreshToken.value) {
      await authService.logout(refreshToken.value)
    }
    jwt.value = null
    refreshToken.value = null
    userEmail.value = null
    tokenStorage.clearTokens()
  }

  function updateTokens(newJwt: string, newRefreshToken: string) {
    jwt.value = newJwt
    refreshToken.value = newRefreshToken
    tokenStorage.setTokens(newJwt, newRefreshToken, userEmail.value || undefined)
  }

  return {
    jwt,
    refreshToken,
    userEmail,
    isAuthenticated,
    displayName,
    userRoles,
    isAdmin,
    isMechanic,
    isClient,
    restoreFromStorage,
    login,
    register,
    logout,
    updateTokens
  }
})
