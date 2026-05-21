# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: vehicles.spec.ts >> Vehicle CRUD >> Edit vehicle make via UI → updated make shown in list
- Location: e2e\tests\vehicles.spec.ts:100:7

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
        - textbox [ref=e28]: e2e-vehicles-1779388610356@test.com
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
  3   | import { VehiclesPage } from '../pages/VehiclesPage'
  4   | import { ApiHelper } from '../helpers/ApiHelper'
  5   | 
  6   | /**
  7   |  * E2E tests for the Vehicle CRUD flows.
  8   |  * Each test registers a fresh user via the API to guarantee isolation.
  9   |  *
  10  |  * Covered scenarios:
  11  |  *   - Create vehicle via UI → appears in list
  12  |  *   - View vehicle detail
  13  |  *   - Edit vehicle make via UI
  14  |  *   - Delete vehicle via UI → removed from list
  15  |  *   - Unauthenticated access → redirected to /login
  16  |  */
  17  | 
  18  | const uniqueEmail = () => `e2e-vehicles-${Date.now()}@test.com`
  19  | const PASSWORD = 'Test123!'
  20  | 
  21  | /** A valid 17-char VIN that passes the front-end pattern */
  22  | const TEST_VIN = 'JH4KA7650MC000001'
  23  | 
  24  | test.describe('Vehicle CRUD', () => {
  25  |   let vehiclesPage: VehiclesPage
  26  |   let loginPage: LoginPage
  27  |   let api: ApiHelper
  28  |   let userEmail: string
  29  |   let jwt: string
  30  | 
  31  |   test.beforeEach(async ({ page, request }) => {
  32  |     api = new ApiHelper(request)
  33  |     loginPage = new LoginPage(page)
  34  |     vehiclesPage = new VehiclesPage(page)
  35  | 
  36  |     // Register + login a fresh client user for each test
  37  |     userEmail = uniqueEmail()
  38  |     const tokens = await api.register(userEmail, PASSWORD, 'Vehicle', 'Tester')
  39  |     jwt = tokens.jwt
  40  | 
  41  |     // Log in through the UI so cookies / localStorage are set
  42  |     await loginPage.login(userEmail, PASSWORD)
> 43  |     await page.waitForURL('/', { timeout: 10_000 })
      |                ^ TimeoutError: page.waitForURL: Timeout 10000ms exceeded.
  44  |   })
  45  | 
  46  |   // ── Create ────────────────────────────────────────────────────────────────
  47  | 
  48  |   test('Create vehicle via UI → appears in vehicle list', async ({ page }) => {
  49  |     await vehiclesPage.gotoCreate()
  50  |     await vehiclesPage.fillCreateForm({
  51  |       make: 'Toyota',
  52  |       model: 'Corolla',
  53  |       year: 2020,
  54  |       licensePlate: 'TEST001',
  55  |       vin: TEST_VIN,
  56  |       mileage: 15000,
  57  |       color: 'Silver',
  58  |     })
  59  |     await vehiclesPage.submitCreate()
  60  | 
  61  |     // Should redirect to /vehicles after create
  62  |     await page.waitForURL('/vehicles', { timeout: 10_000 })
  63  |     await expect(page.locator('body')).toContainText('Toyota')
  64  |   })
  65  | 
  66  |   // ── Read ──────────────────────────────────────────────────────────────────
  67  | 
  68  |   test('Vehicle list shows own vehicles after creation', async ({ page }) => {
  69  |     // Seed a vehicle via API
  70  |     await api.createVehicle(jwt, {
  71  |       make: 'Honda',
  72  |       model: 'Civic',
  73  |       year: 2019,
  74  |       licensePlate: 'HC2019',
  75  |       vin: 'JHMEG8G51AS000001',
  76  |     })
  77  | 
  78  |     await vehiclesPage.goto()
  79  |     await expect(page.locator('body')).toContainText('Honda')
  80  |     await expect(page.locator('body')).toContainText('Civic')
  81  |   })
  82  | 
  83  |   test('Vehicle detail page shows vehicle information', async ({ page }) => {
  84  |     await api.createVehicle(jwt, {
  85  |       make: 'Ford',
  86  |       model: 'Focus',
  87  |       year: 2021,
  88  |       licensePlate: 'FF2021',
  89  |       vin: '1FAFP34N75W259452',
  90  |     })
  91  | 
  92  |     await vehiclesPage.goto()
  93  |     await vehiclesPage.clickFirstDetails()
  94  |     // Detail page must show the make
  95  |     await expect(page.locator('body')).toContainText('Ford')
  96  |   })
  97  | 
  98  |   // ── Edit ──────────────────────────────────────────────────────────────────
  99  | 
  100 |   test('Edit vehicle make via UI → updated make shown in list', async ({ page }) => {
  101 |     await api.createVehicle(jwt, {
  102 |       make: 'Mazda',
  103 |       model: 'CX-5',
  104 |       year: 2022,
  105 |       licensePlate: 'MZ2022',
  106 |       vin: 'JM3KE2CY1F0512480',
  107 |     })
  108 | 
  109 |     await vehiclesPage.goto()
  110 |     await vehiclesPage.clickFirstEdit()
  111 |     await vehiclesPage.fillEditMake('MazdaUpdated')
  112 |     await vehiclesPage.submitEdit()
  113 | 
  114 |     await page.waitForURL('/vehicles', { timeout: 10_000 })
  115 |     await expect(page.locator('body')).toContainText('MazdaUpdated')
  116 |   })
  117 | 
  118 |   // ── Delete ────────────────────────────────────────────────────────────────
  119 | 
  120 |   test('Delete vehicle via UI → removed from list', async ({ page }) => {
  121 |     await api.createVehicle(jwt, {
  122 |       make: 'Kia',
  123 |       model: 'Sportage',
  124 |       year: 2023,
  125 |       licensePlate: 'KIA001',
  126 |       vin: 'KNDPCCA29B7010101',
  127 |     })
  128 | 
  129 |     await vehiclesPage.goto()
  130 |     const before = await vehiclesPage.vehicleCount()
  131 |     expect(before).toBeGreaterThan(0)
  132 | 
  133 |     await vehiclesPage.deleteFirstVehicle()
  134 | 
  135 |     // Wait for the card to be removed
  136 |     await page.waitForTimeout(1000)
  137 |     const after = await vehiclesPage.vehicleCount()
  138 |     expect(after).toBe(before - 1)
  139 |   })
  140 | 
  141 |   // ── Auth guard ────────────────────────────────────────────────────────────
  142 | 
  143 |   test('Unauthenticated user is redirected to /login when visiting /vehicles', async ({
```