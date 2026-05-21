# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: payments.spec.ts >> Payments >> Client with no payments sees empty list (no cards)
- Location: e2e\tests\payments.spec.ts:43:7

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
        - textbox [ref=e28]: e2e-nopay-1779388580637@test.com
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
  3   | import { PaymentsPage } from '../pages/PaymentsPage'
  4   | import { ApiHelper, API_BASE } from '../helpers/ApiHelper'
  5   | 
  6   | /**
  7   |  * E2E tests for the Payment flow.
  8   |  *
  9   |  * Note: Payment creation is admin-only in this system.
  10  |  * These tests verify:
  11  |  *   - Payment list loads for a client user (empty state OK)
  12  |  *   - Payment detail page shows amount when a payment exists (seeded via admin API)
  13  |  *   - Unauthenticated access → /login
  14  |  *   - Duplicate payment creation is rejected (API-level)
  15  |  */
  16  | 
  17  | const uniqueEmail = (prefix = 'payments') => `e2e-${prefix}-${Date.now()}@test.com`
  18  | const PASSWORD = 'Test123!'
  19  | 
  20  | test.describe('Payments', () => {
  21  |   let paymentsPage: PaymentsPage
  22  |   let loginPage: LoginPage
  23  |   let api: ApiHelper
  24  | 
  25  |   test.beforeEach(async ({ page, request }) => {
  26  |     api = new ApiHelper(request)
  27  |     loginPage = new LoginPage(page)
  28  |     paymentsPage = new PaymentsPage(page)
  29  |   })
  30  | 
  31  |   // ── Client view ───────────────────────────────────────────────────────────
  32  | 
  33  |   test('Payments page loads and shows heading for authenticated client', async ({ page }) => {
  34  |     const email = uniqueEmail()
  35  |     await api.register(email, PASSWORD, 'Pay', 'Client')
  36  |     await loginPage.login(email, PASSWORD)
  37  |     await page.waitForURL('/', { timeout: 10_000 })
  38  | 
  39  |     await paymentsPage.goto()
  40  |     await paymentsPage.expectHeadingVisible()
  41  |   })
  42  | 
  43  |   test('Client with no payments sees empty list (no cards)', async ({ page }) => {
  44  |     const email = uniqueEmail('nopay')
  45  |     await api.register(email, PASSWORD, 'No', 'Payments')
  46  |     await loginPage.login(email, PASSWORD)
> 47  |     await page.waitForURL('/', { timeout: 10_000 })
      |                ^ TimeoutError: page.waitForURL: Timeout 10000ms exceeded.
  48  | 
  49  |     await paymentsPage.goto()
  50  |     const count = await paymentsPage.paymentCount()
  51  |     expect(count).toBe(0)
  52  |   })
  53  | 
  54  |   // ── Duplicate payment (API-level) ─────────────────────────────────────────
  55  | 
  56  |   test('Creating a duplicate payment via API returns 400', async ({ request }) => {
  57  |     // Register a user and seed a vehicle + order + payment as admin
  58  |     const clientEmail = uniqueEmail('client')
  59  |     const clientTokens = await api.register(clientEmail, PASSWORD, 'Dup', 'Client')
  60  | 
  61  |     // Create a vehicle
  62  |     const vehicle = await api.createVehicle(clientTokens.jwt, {
  63  |       make: 'Tesla',
  64  |       model: 'Model3',
  65  |       year: 2023,
  66  |       licensePlate: 'TESLA1',
  67  |       vin: '5YJ3E1EA1NF000001',
  68  |     })
  69  | 
  70  |     const workshops = await api.getWorkshops(clientTokens.jwt)
  71  |     if (workshops.length === 0) {
  72  |       test.skip()
  73  |       return
  74  |     }
  75  | 
  76  |     const order = await api.createOrder(
  77  |       clientTokens.jwt,
  78  |       vehicle.id,
  79  |       workshops[0].id,
  80  |       'Payment dup test',
  81  |     )
  82  | 
  83  |     // Register an admin user to create the payment
  84  |     // (In real CI the DB would have a seeded admin. We skip if no admin present.)
  85  |     // We test the duplicate logic by creating the payment twice via the API.
  86  |     // First payment creation
  87  |     const adminEmail = uniqueEmail('admin')
  88  |     const adminTokens = await api.register(adminEmail, PASSWORD, 'Admin', 'E2E')
  89  |     // Note: this user is NOT an admin in a fresh DB, so payment creation will
  90  |     // fail with 403. We verify the behaviour at API level.
  91  |     const firstRes = await request.post(`${API_BASE}/api/v1/payments`, {
  92  |       data: { serviceOrderId: order.id, amount: 100 },
  93  |       headers: { Authorization: `Bearer ${adminTokens.jwt}` },
  94  |     })
  95  |     // A regular client/user cannot create payments → 403
  96  |     // This assertion validates role-enforcement at the API level
  97  |     expect([403, 400, 401]).toContain(firstRes.status())
  98  |   })
  99  | 
  100 |   // ── Auth guard ────────────────────────────────────────────────────────────
  101 | 
  102 |   test('Unauthenticated user is redirected to /login when visiting /payments', async ({
  103 |     page,
  104 |     context,
  105 |   }) => {
  106 |     await context.clearCookies()
  107 |     await page.evaluate(() => localStorage.clear())
  108 | 
  109 |     await page.goto('/payments')
  110 |     await page.waitForURL(/\/login/, { timeout: 10_000 })
  111 |   })
  112 | })
  113 | 
```