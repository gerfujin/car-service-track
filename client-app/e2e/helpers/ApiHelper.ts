import { APIRequestContext } from '@playwright/test'

export const API_BASE = process.env.API_BASE_URL ?? 'http://localhost:80'

export interface AuthTokens {
  jwt: string
  refreshToken: string
}

/**
 * Low-level helper that calls the backend REST API directly —
 * used in beforeEach/afterEach to seed and clean up test state
 * without going through the browser UI.
 */
export class ApiHelper {
  constructor(private readonly request: APIRequestContext) {}

  // ── Identity ──────────────────────────────────────────────────────────────

  async register(
    email: string,
    password: string,
    firstname = 'Test',
    lastname = 'User',
  ): Promise<AuthTokens> {
    const res = await this.request.post(`${API_BASE}/api/v1/identity/account/register`, {
      data: { email, password, firstname, lastname },
    })
    if (!res.ok()) {
      throw new Error(`register failed: ${res.status()} ${await res.text()}`)
    }
    return res.json()
  }

  async login(email: string, password: string): Promise<AuthTokens> {
    const res = await this.request.post(`${API_BASE}/api/v1/identity/account/login`, {
      data: { email, password },
    })
    if (!res.ok()) {
      throw new Error(`login failed: ${res.status()} ${await res.text()}`)
    }
    return res.json()
  }

  async logout(jwt: string, refreshToken: string): Promise<void> {
    await this.request.post(`${API_BASE}/api/v1/identity/account/logout`, {
      data: { refreshToken },
      headers: { Authorization: `Bearer ${jwt}` },
    })
  }

  async refreshToken(jwt: string, refreshToken: string): Promise<AuthTokens> {
    const res = await this.request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
      data: { jwt, refreshToken },
    })
    if (!res.ok()) {
      throw new Error(`refresh failed: ${res.status()} ${await res.text()}`)
    }
    return res.json()
  }

  // ── Vehicles ──────────────────────────────────────────────────────────────

  async createVehicle(jwt: string, data: Record<string, unknown>): Promise<{ id: string }> {
    const res = await this.request.post(`${API_BASE}/api/v1/vehicles`, {
      data,
      headers: { Authorization: `Bearer ${jwt}` },
    })
    if (!res.ok()) {
      throw new Error(`createVehicle failed: ${res.status()} ${await res.text()}`)
    }
    return res.json()
  }

  async deleteVehicle(jwt: string, vehicleId: string): Promise<void> {
    await this.request.delete(`${API_BASE}/api/v1/vehicles/${vehicleId}`, {
      headers: { Authorization: `Bearer ${jwt}` },
    })
  }

  async getVehicles(jwt: string): Promise<Array<{ id: string }>> {
    const res = await this.request.get(`${API_BASE}/api/v1/vehicles`, {
      headers: { Authorization: `Bearer ${jwt}` },
    })
    return res.json()
  }

  // ── Workshops ─────────────────────────────────────────────────────────────

  async getWorkshops(jwt: string): Promise<Array<{ id: string; name: string }>> {
    const res = await this.request.get(`${API_BASE}/api/v1/workshops`, {
      headers: { Authorization: `Bearer ${jwt}` },
    })
    return res.json()
  }

  // ── Service Orders ────────────────────────────────────────────────────────

  async createOrder(
    jwt: string,
    vehicleId: string,
    workshopId: string,
    description = 'E2E test order',
  ): Promise<{ id: string }> {
    const res = await this.request.post(`${API_BASE}/api/v1/serviceorders`, {
      data: { vehicleId, workshopId, description },
      headers: { Authorization: `Bearer ${jwt}` },
    })
    if (!res.ok()) {
      throw new Error(`createOrder failed: ${res.status()} ${await res.text()}`)
    }
    return res.json()
  }

  async getOrders(jwt: string): Promise<Array<{ id: string }>> {
    const res = await this.request.get(`${API_BASE}/api/v1/serviceorders`, {
      headers: { Authorization: `Bearer ${jwt}` },
    })
    return res.json()
  }

  // ── Payments ──────────────────────────────────────────────────────────────

  async getPayments(jwt: string): Promise<Array<{ id: string; serviceOrderId: string }>> {
    const res = await this.request.get(`${API_BASE}/api/v1/payments`, {
      headers: { Authorization: `Bearer ${jwt}` },
    })
    return res.json()
  }
}
