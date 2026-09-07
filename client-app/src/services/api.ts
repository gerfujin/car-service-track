import axios, { type AxiosInstance, type InternalAxiosRequestConfig, type AxiosResponse } from 'axios'
import type { JWTResponse, TokenRefreshInfo } from '@/types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5065'
const API_V1 = `${API_BASE_URL}/api/v1`

const JWT_KEY = 'cst_jwt'
const REFRESH_TOKEN_KEY = 'cst_refresh_token'
const USER_EMAIL_KEY = 'cst_user_email'

export const tokenStorage = {
  getJwt: (): string | null => localStorage.getItem(JWT_KEY),
  getRefreshToken: (): string | null => localStorage.getItem(REFRESH_TOKEN_KEY),
  getUserEmail: (): string | null => localStorage.getItem(USER_EMAIL_KEY),
  setTokens: (jwt: string, refreshToken: string, email?: string) => {
    localStorage.setItem(JWT_KEY, jwt)
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
    if (email) localStorage.setItem(USER_EMAIL_KEY, email)
  },
  // Swaps only the access token (e.g. after a profile update that re-issues the JWT with
  // fresh name claims). Never touches the refresh token.
  setJwt: (jwt: string) => localStorage.setItem(JWT_KEY, jwt),
  clearTokens: () => {
    localStorage.removeItem(JWT_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
    localStorage.removeItem(USER_EMAIL_KEY)
  }
}

const apiClient: AxiosInstance = axios.create({
  baseURL: API_V1
})

let isRefreshing = false
let failedQueue: Array<{ resolve: (value: string) => void; reject: (reason?: unknown) => void }> = []

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token!)
    }
  })
  failedQueue = []
}

apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const token = tokenStorage.getJwt()
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    },
    (error) => Promise.reject(error)
)

apiClient.interceptors.response.use(
    (response: AxiosResponse) => response,
    async (error) => {
      const originalRequest = error.config

      if (error.response?.status === 401 && !originalRequest._retry) {
        const refreshToken = tokenStorage.getRefreshToken()
        const jwt = tokenStorage.getJwt()

        if (!refreshToken || !jwt) {
          tokenStorage.clearTokens()
          window.location.href = '/login'
          return Promise.reject(error)
        }

        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject })
          }).then(token => {
            originalRequest.headers.Authorization = `Bearer ${token}`
            return apiClient(originalRequest)
          }).catch(err => Promise.reject(err))
        }

        originalRequest._retry = true
        isRefreshing = true

        try {
          const refreshData: TokenRefreshInfo = { jwt, refreshToken }
          const response = await axios.post<JWTResponse>(
              `${API_V1}/identity/account/refreshtokendata`,
              refreshData
          )
          const { jwt: newJwt, refreshToken: newRefreshToken } = response.data
          tokenStorage.setTokens(newJwt, newRefreshToken)
          processQueue(null, newJwt)
          originalRequest.headers.Authorization = `Bearer ${newJwt}`
          return apiClient(originalRequest)
        } catch (refreshError) {
          processQueue(refreshError, null)
          tokenStorage.clearTokens()
          window.location.href = '/login'
          return Promise.reject(refreshError)
        } finally {
          isRefreshing = false
        }
      }

      return Promise.reject(error)
    }
)

export default apiClient
export { API_V1, API_BASE_URL }
