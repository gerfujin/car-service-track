import { Page, expect } from '@playwright/test'

/**
 * Page Objects for the Payments section (/payments, /payments/:id)
 */
export class PaymentsPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/payments')
    await this.page.waitForSelector('.spinner-border', { state: 'detached', timeout: 10_000 }).catch(() => {})
  }

  async expectHeadingVisible(): Promise<void> {
    await expect(this.page.locator('h2')).toContainText('Payment', { ignoreCase: true })
  }

  async paymentCount(): Promise<number> {
    return this.page.locator('.card').count()
  }

  async clickFirstDetails(): Promise<void> {
    // The payment card links point to the related order, not a "Details" link
    await this.page.locator('a.btn-outline-primary').first().click()
  }

  async expectAmountVisible(amount: string): Promise<void> {
    await expect(this.page.locator('body')).toContainText(amount)
  }
}
