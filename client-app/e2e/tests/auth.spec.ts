import { test, expect } from '@playwright/test'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { NavPage } from '../pages/NavPage'
import { ApiHelper } from '../helpers/ApiHelper'

/**
 * E2E tests for authentication flows:
 *   - Register → auto-login → logout
 *   - Login with valid credentials → redirect to home
 *   - Login with wrong password → error message
 *   - Logout → redirected to /login
 *   - Unauthenticated navigation to protected route → redirect to /login
 */

const uniqueEmail = () => `e2e-auth-${Date.now()}@test.com`
const PASSWORD = 'Test123!'

test.describe('Authentication', () => {
  let loginPage: LoginPage
  let registerPage: RegisterPage
  let nav: NavPage

  test.beforeEach(({ page }) => {
    loginPage = new LoginPage(page)
    registerPage = new RegisterPage(page)
    nav = new NavPage(page)
  })

  // ── Register ──────────────────────────────────────────────────────────────

  test('Register with new email redirects to home and shows logout button', async ({ page }) => {
    const email = uniqueEmail()
    await registerPage.register(email, PASSWORD, 'Alice', 'E2E')

    // After registration the app auto-logs in and redirects to /
    await page.waitForURL('/', { timeout: 10_000 })
    await nav.expectLoggedIn()
  })

  test('Register with already-taken email shows error', async ({ page, request }) => {
    // Pre-register via API so we have a known-taken email
    const api = new ApiHelper(request)
    const email = uniqueEmail()
    await api.register(email, PASSWORD)

    await registerPage.register(email, PASSWORD)
    await registerPage.expectErrorVisible()
  })

  // ── Login ─────────────────────────────────────────────────────────────────

  test('Login with valid credentials redirects to home', async ({ page, request }) => {
    const api = new ApiHelper(request)
    const email = uniqueEmail()
    await api.register(email, PASSWORD)

    await loginPage.login(email, PASSWORD)
    await page.waitForURL('/', { timeout: 10_000 })
    await nav.expectLoggedIn()
  })

  test('Login with wrong password shows error message', async ({ page, request }) => {
    const api = new ApiHelper(request)
    const email = uniqueEmail()
    await api.register(email, PASSWORD)

    await loginPage.login(email, 'WrongPassword!')
    await loginPage.expectErrorVisible()
  })

  test('Login with non-existent email shows error message', async ({ page }) => {
    await loginPage.login('nobody-exists@test.com', PASSWORD)
    await loginPage.expectErrorVisible()
  })

  // ── Logout ────────────────────────────────────────────────────────────────

  test('Logout redirects to /login and hides protected nav links', async ({ page, request }) => {
    const api = new ApiHelper(request)
    const email = uniqueEmail()
    await api.register(email, PASSWORD)

    // Log in via UI
    await loginPage.login(email, PASSWORD)
    await page.waitForURL('/', { timeout: 10_000 })

    // Logout
    await nav.logout()
    await page.waitForURL('/login', { timeout: 10_000 })
    await nav.expectLoggedOut()
  })

  // ── Route guard ───────────────────────────────────────────────────────────

  test('Navigating to /vehicles without auth redirects to /login', async ({ page }) => {
    await page.goto('/vehicles')
    await page.waitForURL(/\/login/, { timeout: 10_000 })
  })

  test('Navigating to /orders without auth redirects to /login', async ({ page }) => {
    await page.goto('/orders')
    await page.waitForURL(/\/login/, { timeout: 10_000 })
  })
})
