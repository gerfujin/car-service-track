import { Page, expect } from '@playwright/test'

/**
 * Page Object for /register
 */
export class RegisterPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/register')
    await expect(this.page.locator('form')).toBeVisible()
  }

  async fillFirstName(value: string): Promise<void> {
    await this.page.locator('input[autocomplete="given-name"]').fill(value)
  }

  async fillLastName(value: string): Promise<void> {
    await this.page.locator('input[autocomplete="family-name"]').fill(value)
  }

  async fillEmail(value: string): Promise<void> {
    await this.page.locator('input[autocomplete="email"]').fill(value)
  }

  async fillPassword(value: string): Promise<void> {
    await this.page.locator('input[autocomplete="new-password"]').first().fill(value)
  }

  async fillConfirmPassword(value: string): Promise<void> {
    await this.page.locator('input[autocomplete="new-password"]').nth(1).fill(value)
  }

  async submit(): Promise<void> {
    await this.page.locator('button[type="submit"]').click()
  }

  async register(
    email: string,
    password: string,
    firstname = 'E2E',
    lastname = 'Tester',
  ): Promise<void> {
    await this.goto()
    await this.fillFirstName(firstname)
    await this.fillLastName(lastname)
    await this.fillEmail(email)
    await this.fillPassword(password)
    await this.fillConfirmPassword(password)
    await this.submit()
  }

  async expectErrorVisible(): Promise<void> {
    await expect(this.page.locator('.alert-danger')).toBeVisible()
  }
}
