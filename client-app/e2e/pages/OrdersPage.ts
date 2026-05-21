import { Page, expect } from '@playwright/test'

/**
 * Page Objects for the Service Orders section (/orders, /orders/create, /orders/:id)
 */
export class OrdersPage {
  constructor(private readonly page: Page) {}

  // ── List ──────────────────────────────────────────────────────────────────

  async goto(): Promise<void> {
    await this.page.goto('/orders')
    await this.page.waitForSelector('.spinner-border', { state: 'detached', timeout: 10_000 }).catch(() => {})
  }

  async expectHeadingVisible(): Promise<void> {
    await expect(this.page.locator('h2')).toContainText('Order', { ignoreCase: true })
  }

  async orderCount(): Promise<number> {
    return this.page.locator('.card').count()
  }

  async clickCreateButton(): Promise<void> {
    await this.page.locator('a[href="/orders/create"]').click()
  }

  /** Click "Details" on the first order card */
  async clickFirstDetails(): Promise<void> {
    await this.page.locator('a:has-text("Details")').first().click()
  }

  // ── Create form ───────────────────────────────────────────────────────────

  async gotoCreate(): Promise<void> {
    await this.page.goto('/orders/create')
    // Wait for vehicle/workshop selects to load
    await this.page.waitForSelector('.spinner-border', { state: 'detached', timeout: 10_000 }).catch(() => {})
  }

  async selectFirstVehicle(): Promise<void> {
    const select = this.page.locator('select').first()
    await select.selectOption({ index: 1 })
  }

  async selectFirstWorkshop(): Promise<void> {
    const select = this.page.locator('select').nth(1)
    await select.selectOption({ index: 1 })
  }

  async fillDescription(text: string): Promise<void> {
    await this.page.locator('textarea').fill(text)
  }

  async submitCreate(): Promise<void> {
    await this.page.locator('button[type="submit"]').click()
  }

  // ── Detail page ───────────────────────────────────────────────────────────

  async expectStatusBadgeVisible(): Promise<void> {
    await expect(this.page.locator('.badge')).toBeVisible()
  }
}
