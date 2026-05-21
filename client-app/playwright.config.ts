import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright configuration for CarServiceTrack E2E tests.
 * Tests run against the local stack:
 *   frontend  → http://localhost:81
 *   backend   → http://localhost:80
 */
export default defineConfig({
  testDir: './e2e/tests',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI
    ? [['junit', { outputFile: 'playwright-results.xml' }], ['list']]
    : [['html', { open: 'never' }], ['list']],

  use: {
    baseURL: process.env.APP_BASE_URL ?? 'http://localhost:81',
    extraHTTPHeaders: {
      Accept: 'application/json',
    },
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
})
