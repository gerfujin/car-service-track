import { Page, expect } from '@playwright/test'

/**
 * Helper that wraps the NavBar component interactions —
 * logout, navigation links, auth state checks.
 */
export class NavPage {
  constructor(private readonly page: Page) {}

  /** Click the logout button in the navbar */
  async logout(): Promise<void> {
    await this.page.locator('nav .btn-outline-danger').click()
  }

  /** Assert the user is authenticated (logout button visible) */
  async expectLoggedIn(): Promise<void> {
    await expect(this.page.locator('nav .btn-outline-danger')).toBeVisible()
  }

  /** Assert the user is a guest (login link visible) */
  async expectLoggedOut(): Promise<void> {
    await expect(this.page.locator('nav a[href="/login"]')).toBeVisible()
  }

  /** Navigate to /vehicles via the nav link */
  async goToVehicles(): Promise<void> {
    await this.page.locator('nav a[href="/vehicles"]').click()
  }

  /** Navigate to /orders via the nav link */
  async goToOrders(): Promise<void> {
    await this.page.locator('nav a[href="/orders"]').click()
  }

  /** Navigate to /payments via the nav link */
  async goToPayments(): Promise<void> {
    await this.page.locator('nav a[href="/payments"]').click()
  }
}
