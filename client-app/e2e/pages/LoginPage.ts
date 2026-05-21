import { Page, expect } from '@playwright/test'

/**
 * Page Object for /login
 */
export class LoginPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/login')
    await expect(this.page.locator('form')).toBeVisible()
  }

  async fillEmail(email: string): Promise<void> {
    await this.page.locator('input[type="email"]').fill(email)
  }

  async fillPassword(password: string): Promise<void> {
    await this.page.locator('input[type="password"]').fill(password)
  }

  async submit(): Promise<void> {
    await this.page.locator('button[type="submit"]').click()
  }

  async login(email: string, password: string): Promise<void> {
    await this.goto()
    await this.fillEmail(email)
    await this.fillPassword(password)
    await this.submit()
  }

  async expectErrorVisible(): Promise<void> {
    await expect(this.page.locator('.alert-danger')).toBeVisible()
  }
}
