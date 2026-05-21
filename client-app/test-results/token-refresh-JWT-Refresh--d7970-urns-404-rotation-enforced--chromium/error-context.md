# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: token-refresh.spec.ts >> JWT Refresh-Token flow >> Re-using old refresh token after rotation returns 404 (rotation enforced)
- Location: e2e\tests\token-refresh.spec.ts:46:7

# Error details

```
Error: expect(received).toBe(expected) // Object.is equality

Expected: 404
Received: 200
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test'
  2  | import { ApiHelper, API_BASE, AuthTokens } from '../helpers/ApiHelper'
  3  | 
  4  | /**
  5  |  * E2E / integration test for the JWT refresh-token flow.
  6  |  *
  7  |  * Strategy: we test the refresh flow at the API level (via Playwright's
  8  |  * request context) rather than through the browser UI, because the silent
  9  |  * refresh is triggered automatically by the axios interceptor in the Vue
  10 |  * app and cannot easily be observed via the DOM.  These tests verify that:
  11 |  *   1. A valid JWT + refreshToken pair yields new tokens.
  12 |  *   2. The refresh token is rotated on every use (old token is invalidated).
  13 |  *   3. An invalid refresh token returns 404.
  14 |  *   4. Using the old (rotated) refresh token after rotation returns 404.
  15 |  */
  16 | 
  17 | const uniqueEmail = () => `e2e-refresh-${Date.now()}@test.com`
  18 | const PASSWORD = 'Test123!'
  19 | 
  20 | test.describe('JWT Refresh-Token flow', () => {
  21 |   let tokens: AuthTokens
  22 |   let api: ApiHelper
  23 | 
  24 |   test.beforeEach(async ({ request }) => {
  25 |     api = new ApiHelper(request)
  26 |     const email = uniqueEmail()
  27 |     tokens = await api.register(email, PASSWORD)
  28 |   })
  29 | 
  30 |   test('Valid token pair returns new JWT and rotated refresh token', async ({ request }) => {
  31 |     const refreshed = await api.refreshToken(tokens.jwt, tokens.refreshToken)
  32 | 
  33 |     expect(refreshed.jwt).toBeTruthy()
  34 |     expect(refreshed.refreshToken).toBeTruthy()
  35 |     // Rotation: new refresh token must differ from the old one
  36 |     expect(refreshed.refreshToken).not.toBe(tokens.refreshToken)
  37 |   })
  38 | 
  39 |   test('Invalid refresh token returns 404', async ({ request }) => {
  40 |     const res = await request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
  41 |       data: { jwt: tokens.jwt, refreshToken: 'totally-invalid-garbage' },
  42 |     })
  43 |     expect(res.status()).toBe(404)
  44 |   })
  45 | 
  46 |   test('Re-using old refresh token after rotation returns 404 (rotation enforced)', async ({
  47 |     request,
  48 |   }) => {
  49 |     const originalRefreshToken = tokens.refreshToken
  50 |     // First refresh — rotates the token
  51 |     await api.refreshToken(tokens.jwt, originalRefreshToken)
  52 | 
  53 |     // Second refresh with the OLD token must fail
  54 |     const res = await request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
  55 |       data: { jwt: tokens.jwt, refreshToken: originalRefreshToken },
  56 |     })
> 57 |     expect(res.status()).toBe(404)
     |                          ^ Error: expect(received).toBe(expected) // Object.is equality
  58 |   })
  59 | 
  60 |   test('After logout, refresh token is invalidated', async ({ request }) => {
  61 |     // Log out (invalidates the refresh token server-side)
  62 |     await api.logout(tokens.jwt, tokens.refreshToken)
  63 | 
  64 |     // Attempt to refresh with the now-invalidated token
  65 |     const res = await request.post(`${API_BASE}/api/v1/identity/account/refreshtokendata`, {
  66 |       data: { jwt: tokens.jwt, refreshToken: tokens.refreshToken },
  67 |     })
  68 |     expect(res.status()).toBe(404)
  69 |   })
  70 | })
  71 | 
```