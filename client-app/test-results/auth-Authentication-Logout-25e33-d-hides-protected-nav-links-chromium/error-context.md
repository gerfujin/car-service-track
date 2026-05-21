# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: auth.spec.ts >> Authentication >> Logout redirects to /login and hides protected nav links
- Location: e2e\tests\auth.spec.ts:79:7

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
        - textbox [ref=e28]: e2e-auth-1779388605937@test.com
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
  3   | import { RegisterPage } from '../pages/RegisterPage'
  4   | import { NavPage } from '../pages/NavPage'
  5   | import { ApiHelper } from '../helpers/ApiHelper'
  6   | 
  7   | /**
  8   |  * E2E tests for authentication flows:
  9   |  *   - Register → auto-login → logout
  10  |  *   - Login with valid credentials → redirect to home
  11  |  *   - Login with wrong password → error message
  12  |  *   - Logout → redirected to /login
  13  |  *   - Unauthenticated navigation to protected route → redirect to /login
  14  |  */
  15  | 
  16  | const uniqueEmail = () => `e2e-auth-${Date.now()}@test.com`
  17  | const PASSWORD = 'Test123!'
  18  | 
  19  | test.describe('Authentication', () => {
  20  |   let loginPage: LoginPage
  21  |   let registerPage: RegisterPage
  22  |   let nav: NavPage
  23  | 
  24  |   test.beforeEach(({ page }) => {
  25  |     loginPage = new LoginPage(page)
  26  |     registerPage = new RegisterPage(page)
  27  |     nav = new NavPage(page)
  28  |   })
  29  | 
  30  |   // ── Register ──────────────────────────────────────────────────────────────
  31  | 
  32  |   test('Register with new email redirects to home and shows logout button', async ({ page }) => {
  33  |     const email = uniqueEmail()
  34  |     await registerPage.register(email, PASSWORD, 'Alice', 'E2E')
  35  | 
  36  |     // After registration the app auto-logs in and redirects to /
  37  |     await page.waitForURL('/', { timeout: 10_000 })
  38  |     await nav.expectLoggedIn()
  39  |   })
  40  | 
  41  |   test('Register with already-taken email shows error', async ({ page, request }) => {
  42  |     // Pre-register via API so we have a known-taken email
  43  |     const api = new ApiHelper(request)
  44  |     const email = uniqueEmail()
  45  |     await api.register(email, PASSWORD)
  46  | 
  47  |     await registerPage.register(email, PASSWORD)
  48  |     await registerPage.expectErrorVisible()
  49  |   })
  50  | 
  51  |   // ── Login ─────────────────────────────────────────────────────────────────
  52  | 
  53  |   test('Login with valid credentials redirects to home', async ({ page, request }) => {
  54  |     const api = new ApiHelper(request)
  55  |     const email = uniqueEmail()
  56  |     await api.register(email, PASSWORD)
  57  | 
  58  |     await loginPage.login(email, PASSWORD)
  59  |     await page.waitForURL('/', { timeout: 10_000 })
  60  |     await nav.expectLoggedIn()
  61  |   })
  62  | 
  63  |   test('Login with wrong password shows error message', async ({ page, request }) => {
  64  |     const api = new ApiHelper(request)
  65  |     const email = uniqueEmail()
  66  |     await api.register(email, PASSWORD)
  67  | 
  68  |     await loginPage.login(email, 'WrongPassword!')
  69  |     await loginPage.expectErrorVisible()
  70  |   })
  71  | 
  72  |   test('Login with non-existent email shows error message', async ({ page }) => {
  73  |     await loginPage.login('nobody-exists@test.com', PASSWORD)
  74  |     await loginPage.expectErrorVisible()
  75  |   })
  76  | 
  77  |   // ── Logout ────────────────────────────────────────────────────────────────
  78  | 
  79  |   test('Logout redirects to /login and hides protected nav links', async ({ page, request }) => {
  80  |     const api = new ApiHelper(request)
  81  |     const email = uniqueEmail()
  82  |     await api.register(email, PASSWORD)
  83  | 
  84  |     // Log in via UI
  85  |     await loginPage.login(email, PASSWORD)
> 86  |     await page.waitForURL('/', { timeout: 10_000 })
      |                ^ TimeoutError: page.waitForURL: Timeout 10000ms exceeded.
  87  | 
  88  |     // Logout
  89  |     await nav.logout()
  90  |     await page.waitForURL('/login', { timeout: 10_000 })
  91  |     await nav.expectLoggedOut()
  92  |   })
  93  | 
  94  |   // ── Route guard ───────────────────────────────────────────────────────────
  95  | 
  96  |   test('Navigating to /vehicles without auth redirects to /login', async ({ page }) => {
  97  |     await page.goto('/vehicles')
  98  |     await page.waitForURL(/\/login/, { timeout: 10_000 })
  99  |   })
  100 | 
  101 |   test('Navigating to /orders without auth redirects to /login', async ({ page }) => {
  102 |     await page.goto('/orders')
  103 |     await page.waitForURL(/\/login/, { timeout: 10_000 })
  104 |   })
  105 | })
  106 | 
```