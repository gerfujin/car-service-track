import { test, expect } from '@playwright/test'
import { LoginPage } from '../pages/LoginPage'
import { OrdersPage } from '../pages/OrdersPage'
import { ApiHelper } from '../helpers/ApiHelper'

/**
 * E2E tests for Service Order flows.
 *
 * Covered scenarios:
 *   - Order list shows own orders
 *   - Create service order via UI (vehicle + workshop preloaded)
 *   - Order detail page shows status badge
 *   - Unauthenticated access → /login
 */

const uniqueEmail = () => `e2e-orders-${Date.now()}@test.com`
const PASSWORD = 'Test123!'

test.describe('Service Orders', () => {
  let ordersPage: OrdersPage
  let loginPage: LoginPage
  let api: ApiHelper
  let jwt: string

  test.beforeEach(async ({ page, request }) => {
    api = new ApiHelper(request)
    loginPage = new LoginPage(page)
    ordersPage = new OrdersPage(page)

    const email = uniqueEmail()
    const tokens = await api.register(email, PASSWORD, 'Order', 'Tester')
    jwt = tokens.jwt

    await loginPage.login(email, PASSWORD)
    await page.waitForURL('/', { timeout: 10_000 })
  })

  // ── Read ──────────────────────────────────────────────────────────────────

  test('Order list loads and shows heading', async () => {
    await ordersPage.goto()
    await ordersPage.expectHeadingVisible()
  })

  test('Order list shows own order seeded via API', async ({ page }) => {
    // Seed vehicle + order
    const vehicle = await api.createVehicle(jwt, {
      make: 'BMW',
      model: 'X3',
      year: 2022,
      licensePlate: 'BMW001',
      vin: 'WBA3A5G59DNP26082',
    })

    const workshops = await api.getWorkshops(jwt)
    if (workshops.length === 0) {
      test.skip()
      return
    }

    await api.createOrder(jwt, vehicle.id, workshops[0].id, 'E2E order seed')
    await ordersPage.goto()
    await expect(page.locator('body')).toContainText('BMW')
  })

  // ── Create ────────────────────────────────────────────────────────────────

  test('Create service order via UI → redirected to order detail page', async ({ page }) => {
    // Need at least one vehicle
    await api.createVehicle(jwt, {
      make: 'Audi',
      model: 'A4',
      year: 2021,
      licensePlate: 'AUDI01',
      vin: 'WAUZZZ8K1BA123456',
    })

    const workshops = await api.getWorkshops(jwt)
    if (workshops.length === 0) {
      // Can't create an order without a workshop — skip gracefully
      test.skip()
      return
    }

    await ordersPage.gotoCreate()
    await ordersPage.selectFirstVehicle()
    await ordersPage.selectFirstWorkshop()
    await ordersPage.fillDescription('Oil change please')
    await ordersPage.submitCreate()

    // Should redirect to /orders/:id
    await page.waitForURL(/\/orders\/[0-9a-f-]+$/, { timeout: 10_000 })
    await ordersPage.expectStatusBadgeVisible()
  })

  // ── Detail ────────────────────────────────────────────────────────────────

  test('Order detail shows status badge', async ({ page }) => {
    const vehicle = await api.createVehicle(jwt, {
      make: 'VW',
      model: 'Golf',
      year: 2020,
      licensePlate: 'VWG01',
      vin: 'WVWZZZ1KZ6W123456',
    })

    const workshops = await api.getWorkshops(jwt)
    if (workshops.length === 0) {
      test.skip()
      return
    }

    const order = await api.createOrder(jwt, vehicle.id, workshops[0].id, 'Detail test')

    await page.goto(`/orders/${order.id}`)
    await ordersPage.expectStatusBadgeVisible()
  })

  // ── Auth guard ────────────────────────────────────────────────────────────

  test('Unauthenticated user is redirected to /login when visiting /orders', async ({
    page,
    context,
  }) => {
    await context.clearCookies()
    await page.evaluate(() => localStorage.clear())

    await page.goto('/orders')
    await page.waitForURL(/\/login/, { timeout: 10_000 })
  })
})
