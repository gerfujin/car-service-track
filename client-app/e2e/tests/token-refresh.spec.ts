import { test, expect } from '@playwright/test'
import { ApiHelper, API_BASE, AuthTokens } from '../helpers/ApiHelper'

/**
 * E2E / integration test for the JWT refresh-token flow.
 *
 * Strategy: we test the refresh flow at the API level (via Playwright's
 * request context) rather than through the browser UI, because the silent
 * refresh is triggered automatically by the axios interceptor in the Vue
 * app and cannot easily be observed via the DOM.  These tests verify that:
 *   1. A valid JWT + refreshToken pair yields new tokens.
 *   2. The refresh token is rotated on every use (old token is invalidated).
 *   3. An invalid refresh token returns 404.
 *   4. Using the old (rotated) refresh token after rotation returns 404.
 */

const uniqueEmail = () => `e2e-refresh-${Date.now()}@test.com`
const PASSWORD = 'Test123!'

test.describe('JWT Refresh-Token flow', () => {
  let tokens: AuthTokens
  let api: ApiHelper

  test.beforeEach(async ({ request }) => {
    api = new ApiHelper(request)
    const email = uniqueEmail()
    tokens = await api.register(email, PASSWORD)
  })

  test('Valid token pair returns new JWT and rotated refresh token', async ({ request }) => {
    const refreshed = await api.refreshToken(tokens.jwt, tokens.refreshToken)

    expect(refreshed.jwt).toBeTruthy()
    expect(refreshed.refreshToken).toBeTruthy()
    // Rotation: new refresh token must differ from the old one
    expect(refreshed.refreshToken).not.toBe(tokens.refreshToken)
  })

  test('Invalid refresh token returns 404', async ({ request }) => {
    const res = await request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
      data: { jwt: tokens.jwt, refreshToken: 'totally-invalid-garbage' },
    })
    expect(res.status()).toBe(404)
  })

  test('Re-using old refresh token after rotation returns 404 (rotation enforced)', async ({
    request,
  }) => {
    const originalRefreshToken = tokens.refreshToken
    // First refresh — rotates the token
    await api.refreshToken(tokens.jwt, originalRefreshToken)

    // Second refresh with the OLD token must fail
    const res = await request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
      data: { jwt: tokens.jwt, refreshToken: originalRefreshToken },
    })
    expect(res.status()).toBe(404)
  })

  test('After logout, refresh token is invalidated', async ({ request }) => {
    // Log out (invalidates the refresh token server-side)
    await api.logout(tokens.jwt, tokens.refreshToken)

    // Attempt to refresh with the now-invalidated token
    const res = await request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
      data: { jwt: tokens.jwt, refreshToken: tokens.refreshToken },
    })
    expect(res.status()).toBe(404)
  })
})
