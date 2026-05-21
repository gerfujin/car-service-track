import { test, expect } from '@playwright/test'
import { LoginPage } from '../pages/LoginPage'
import { VehiclesPage } from '../pages/VehiclesPage'
import { ApiHelper } from '../helpers/ApiHelper'

/**
 * E2E tests for the Vehicle CRUD flows.
 * Each test registers a fresh user via the API to guarantee isolation.
 *
 * Covered scenarios:
 *   - Create vehicle via UI → appears in list
 *   - View vehicle detail
 *   - Edit vehicle make via UI
 *   - Delete vehicle via UI → removed from list
 *   - Unauthenticated access → redirected to /login
 */

const uniqueEmail = () => `e2e-vehicles-${Date.now()}@test.com`
const PASSWORD = 'Test123!'

/** A valid 17-char VIN that passes the front-end pattern */
const TEST_VIN = 'JH4KA7650MC000001'

test.describe('Vehicle CRUD', () => {
  let vehiclesPage: VehiclesPage
  let loginPage: LoginPage
  let api: ApiHelper
  let userEmail: string
  let jwt: string

  test.beforeEach(async ({ page, request }) => {
    api = new ApiHelper(request)
    loginPage = new LoginPage(page)
    vehiclesPage = new VehiclesPage(page)

    // Register + login a fresh client user for each test
    userEmail = uniqueEmail()
    const tokens = await api.register(userEmail, PASSWORD, 'Vehicle', 'Tester')
    jwt = tokens.jwt

    // Log in through the UI so cookies / localStorage are set
    await loginPage.login(userEmail, PASSWORD)
    await page.waitForURL('/', { timeout: 10_000 })
  })

  // ── Create ────────────────────────────────────────────────────────────────

  test('Create vehicle via UI → appears in vehicle list', async ({ page }) => {
    await vehiclesPage.gotoCreate()
    await vehiclesPage.fillCreateForm({
      make: 'Toyota',
      model: 'Corolla',
      year: 2020,
      licensePlate: 'TEST001',
      vin: TEST_VIN,
      mileage: 15000,
      color: 'Silver',
    })
    await vehiclesPage.submitCreate()

    // Should redirect to /vehicles after create
    await page.waitForURL('/vehicles', { timeout: 10_000 })
    await expect(page.locator('body')).toContainText('Toyota')
  })

  // ── Read ──────────────────────────────────────────────────────────────────

  test('Vehicle list shows own vehicles after creation', async ({ page }) => {
    // Seed a vehicle via API
    await api.createVehicle(jwt, {
      make: 'Honda',
      model: 'Civic',
      year: 2019,
      licensePlate: 'HC2019',
      vin: 'JHMEG8G51AS000001',
    })

    await vehiclesPage.goto()
    await expect(page.locator('body')).toContainText('Honda')
    await expect(page.locator('body')).toContainText('Civic')
  })

  test('Vehicle detail page shows vehicle information', async ({ page }) => {
    await api.createVehicle(jwt, {
      make: 'Ford',
      model: 'Focus',
      year: 2021,
      licensePlate: 'FF2021',
      vin: '1FAFP34N75W259452',
    })

    await vehiclesPage.goto()
    await vehiclesPage.clickFirstDetails()
    // Detail page must show the make
    await expect(page.locator('body')).toContainText('Ford')
  })

  // ── Edit ──────────────────────────────────────────────────────────────────

  test('Edit vehicle make via UI → updated make shown in list', async ({ page }) => {
    await api.createVehicle(jwt, {
      make: 'Mazda',
      model: 'CX-5',
      year: 2022,
      licensePlate: 'MZ2022',
      vin: 'JM3KE2CY1F0512480',
    })

    await vehiclesPage.goto()
    await vehiclesPage.clickFirstEdit()
    await vehiclesPage.fillEditMake('MazdaUpdated')
    await vehiclesPage.submitEdit()

    await page.waitForURL('/vehicles', { timeout: 10_000 })
    await expect(page.locator('body')).toContainText('MazdaUpdated')
  })

  // ── Delete ────────────────────────────────────────────────────────────────

  test('Delete vehicle via UI → removed from list', async ({ page }) => {
    await api.createVehicle(jwt, {
      make: 'Kia',
      model: 'Sportage',
      year: 2023,
      licensePlate: 'KIA001',
      vin: 'KNDPCCA29B7010101',
    })

    await vehiclesPage.goto()
    const before = await vehiclesPage.vehicleCount()
    expect(before).toBeGreaterThan(0)

    await vehiclesPage.deleteFirstVehicle()

    // Wait for the card to be removed
    await page.waitForTimeout(1000)
    const after = await vehiclesPage.vehicleCount()
    expect(after).toBe(before - 1)
  })

  // ── Auth guard ────────────────────────────────────────────────────────────

  test('Unauthenticated user is redirected to /login when visiting /vehicles', async ({
    page,
    context,
  }) => {
    // Open a fresh context (no auth cookies)
    await context.clearCookies()
    await page.evaluate(() => localStorage.clear())

    await page.goto('/vehicles')
    await page.waitForURL(/\/login/, { timeout: 10_000 })
  })
})
