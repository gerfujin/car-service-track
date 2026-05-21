import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright configuration for CarServiceTrack E2E tests.
 * Tests run against the local docker-compose stack:
 *   frontend  → http://localhost:81
 *   backend   → http://localhost:80
 */
export default defineConfig({
  testDir: './tests',
  /* Maximum time one test can run (30 s) */
  timeout: 30_000,
  /* Assert timeout */
  expect: { timeout: 5_000 },
  /* Run tests sequentially inside each file */
  fullyParallel: false,
  /* Fail the build on CI if you accidentally left test.only in the source */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 1 : 0,
  /* Reporter */
  reporter: process.env.CI
    ? [['junit', { outputFile: 'playwright-results.xml' }], ['list']]
    : [['html', { open: 'never' }]],

  use: {
    /* Base URL of the Vue app */
    baseURL: process.env.APP_BASE_URL ?? 'http://localhost:81',
    /* API base used in helpers */
    extraHTTPHeaders: {
      'Accept': 'application/json',
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
