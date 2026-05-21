# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: service-orders.spec.ts >> Service Orders >> Order detail shows status badge
- Location: e2e\tests\service-orders.spec.ts:98:7

# Error details

```
TimeoutError: page.waitForURL: Timeout 10000ms exceeded.
=========================== logs ===========================
waiting for navigation to "/" until "load"
============================================================
```

# Page snapshot

```yaml
- generic [ref=e3]:
  - navigation [ref=e4]:
    - generic [ref=e5]:
      - link "🔧 CarServiceTrack" [ref=e6] [cursor=pointer]:
        - /url: /
      - generic [ref=e7]:
        - list [ref=e8]:
          - listitem [ref=e9]:
            - link "Home" [ref=e10] [cursor=pointer]:
              - /url: /
        - list [ref=e11]:
          - listitem [ref=e12]:
            - combobox [ref=e13]:
              - option "EN" [selected]
              - option "ET"
          - listitem [ref=e14]:
            - link "Login" [ref=e15] [cursor=pointer]:
              - /url: /login
          - listitem [ref=e16]:
            - link "Register" [ref=e17] [cursor=pointer]:
              - /url: /register
  - generic [ref=e22]:
    - heading "Login" [level=3] [ref=e23]
    - generic [ref=e24]: Invalid email or password
    - generic [ref=e25]:
      - generic [ref=e26]:
        - generic [ref=e27]: Email
        - textbox [ref=e28]: e2e-orders-1779388610357@test.com
      - generic [ref=e29]:
        - generic [ref=e30]: Password
        - textbox [ref=e31]: Test123!
      - button "Sign In" [ref=e32] [cursor=pointer]
    - separator [ref=e33]
    - paragraph [ref=e34]:
      - text: Don't have an account?
      - link "Register" [ref=e35] [cursor=pointer]:
        - /url: /register
```

# Test source

```ts
  1   | import { test, expect } from '@playwright/test'
  2   | import { LoginPage } from '../pages/LoginPage'
  3   | import { OrdersPage } from '../pages/OrdersPage'
  4   | import { ApiHelper } from '../helpers/ApiHelper'
  5   | 
  6   | /**
  7   |  * E2E tests for Service Order flows.
  8   |  *
  9   |  * Covered scenarios:
  10  |  *   - Order list shows own orders
  11  |  *   - Create service order via UI (vehicle + workshop preloaded)
  12  |  *   - Order detail page shows status badge
  13  |  *   - Unauthenticated access → /login
  14  |  */
  15  | 
  16  | const uniqueEmail = () => `e2e-orders-${Date.now()}@test.com`
  17  | const PASSWORD = 'Test123!'
  18  | 
  19  | test.describe('Service Orders', () => {
  20  |   let ordersPage: OrdersPage
  21  |   let loginPage: LoginPage
  22  |   let api: ApiHelper
  23  |   let jwt: string
  24  | 
  25  |   test.beforeEach(async ({ page, request }) => {
  26  |     api = new ApiHelper(request)
  27  |     loginPage = new LoginPage(page)
  28  |     ordersPage = new OrdersPage(page)
  29  | 
  30  |     const email = uniqueEmail()
  31  |     const tokens = await api.register(email, PASSWORD, 'Order', 'Tester')
  32  |     jwt = tokens.jwt
  33  | 
  34  |     await loginPage.login(email, PASSWORD)
> 35  |     await page.waitForURL('/', { timeout: 10_000 })
      |                ^ TimeoutError: page.waitForURL: Timeout 10000ms exceeded.
  36  |   })
  37  | 
  38  |   // ── Read ──────────────────────────────────────────────────────────────────
  39  | 
  40  |   test('Order list loads and shows heading', async () => {
  41  |     await ordersPage.goto()
  42  |     await ordersPage.expectHeadingVisible()
  43  |   })
  44  | 
  45  |   test('Order list shows own order seeded via API', async ({ page }) => {
  46  |     // Seed vehicle + order
  47  |     const vehicle = await api.createVehicle(jwt, {
  48  |       make: 'BMW',
  49  |       model: 'X3',
  50  |       year: 2022,
  51  |       licensePlate: 'BMW001',
  52  |       vin: 'WBA3A5G59DNP26082',
  53  |     })
  54  | 
  55  |     const workshops = await api.getWorkshops(jwt)
  56  |     if (workshops.length === 0) {
  57  |       test.skip()
  58  |       return
  59  |     }
  60  | 
  61  |     await api.createOrder(jwt, vehicle.id, workshops[0].id, 'E2E order seed')
  62  |     await ordersPage.goto()
  63  |     await expect(page.locator('body')).toContainText('BMW')
  64  |   })
  65  | 
  66  |   // ── Create ────────────────────────────────────────────────────────────────
  67  | 
  68  |   test('Create service order via UI → redirected to order detail page', async ({ page }) => {
  69  |     // Need at least one vehicle
  70  |     await api.createVehicle(jwt, {
  71  |       make: 'Audi',
  72  |       model: 'A4',
  73  |       year: 2021,
  74  |       licensePlate: 'AUDI01',
  75  |       vin: 'WAUZZZ8K1BA123456',
  76  |     })
  77  | 
  78  |     const workshops = await api.getWorkshops(jwt)
  79  |     if (workshops.length === 0) {
  80  |       // Can't create an order without a workshop — skip gracefully
  81  |       test.skip()
  82  |       return
  83  |     }
  84  | 
  85  |     await ordersPage.gotoCreate()
  86  |     await ordersPage.selectFirstVehicle()
  87  |     await ordersPage.selectFirstWorkshop()
  88  |     await ordersPage.fillDescription('Oil change please')
  89  |     await ordersPage.submitCreate()
  90  | 
  91  |     // Should redirect to /orders/:id
  92  |     await page.waitForURL(/\/orders\/[0-9a-f-]+$/, { timeout: 10_000 })
  93  |     await ordersPage.expectStatusBadgeVisible()
  94  |   })
  95  | 
  96  |   // ── Detail ────────────────────────────────────────────────────────────────
  97  | 
  98  |   test('Order detail shows status badge', async ({ page }) => {
  99  |     const vehicle = await api.createVehicle(jwt, {
  100 |       make: 'VW',
  101 |       model: 'Golf',
  102 |       year: 2020,
  103 |       licensePlate: 'VWG01',
  104 |       vin: 'WVWZZZ1KZ6W123456',
  105 |     })
  106 | 
  107 |     const workshops = await api.getWorkshops(jwt)
  108 |     if (workshops.length === 0) {
  109 |       test.skip()
  110 |       return
  111 |     }
  112 | 
  113 |     const order = await api.createOrder(jwt, vehicle.id, workshops[0].id, 'Detail test')
  114 | 
  115 |     await page.goto(`/orders/${order.id}`)
  116 |     await ordersPage.expectStatusBadgeVisible()
  117 |   })
  118 | 
  119 |   // ── Auth guard ────────────────────────────────────────────────────────────
  120 | 
  121 |   test('Unauthenticated user is redirected to /login when visiting /orders', async ({
  122 |     page,
  123 |     context,
  124 |   }) => {
  125 |     await context.clearCookies()
  126 |     await page.evaluate(() => localStorage.clear())
  127 | 
  128 |     await page.goto('/orders')
  129 |     await page.waitForURL(/\/login/, { timeout: 10_000 })
  130 |   })
  131 | })
  132 | 
```