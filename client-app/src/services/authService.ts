import axios from 'axios'
import type { LoginInfo, RegisterInfo, JWTResponse, LogoutInfo } from '@/types'
import { tokenStorage, API_V1 } from './api'
import apiClient from './api'

export const authService = {
  async login(data: LoginInfo): Promise<JWTResponse> {
    const response = await axios.post<JWTResponse>(
      `${API_V1}/identity/account/login`,
      data
    )
    return response.data
  },

  async register(data: RegisterInfo): Promise<JWTResponse> {
    const response = await axios.post<JWTResponse>(
      `${API_V1}/identity/account/register`,
      data
    )
    return response.data
  },

  async logout(refreshToken: string): Promise<void> {
    const logoutData: LogoutInfo = { refreshToken }
    try {
      await apiClient.post('/identity/account/logout', logoutData)
    } catch {
      // Ignore errors on logout - clear tokens anyway
    } finally {
      tokenStorage.clearTokens()
    }
  },

  // Extract email from JWT payload
  getEmailFromJwt(jwt: string): string | null {
    try {
      const payload = JSON.parse(atob(jwt.split('.')[1]))
      // ASP.NET Identity uses ClaimTypes.Email which maps to this claim
      return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
             payload['email'] ||
             payload['sub'] ||
             null
    } catch {
      return null
    }
  },

  isJwtExpired(jwt: string): boolean {
    try {
      const payload = JSON.parse(atob(jwt.split('.')[1]))
      const exp = payload.exp
      if (!exp) return true
      return Date.now() >= exp * 1000
    } catch {
      return true
    }
  }
}
