import { Page, expect } from '@playwright/test'

/**
 * Page Objects for the Vehicles section (/vehicles, /vehicles/create, /vehicles/:id/edit)
 */
export class VehiclesPage {
  constructor(private readonly page: Page) {}

  // ── List ──────────────────────────────────────────────────────────────────

  async goto(): Promise<void> {
    await this.page.goto('/vehicles')
    // Wait for spinner to disappear
    await this.page.waitForSelector('.spinner-border', { state: 'detached', timeout: 10_000 }).catch(() => {})
  }

  async expectHeadingVisible(): Promise<void> {
    await expect(this.page.locator('h2')).toContainText('Vehicle', { ignoreCase: true })
  }

  /** Returns the count of vehicle cards currently rendered */
  async vehicleCount(): Promise<number> {
    return this.page.locator('.card').count()
  }

  /** Click the "Add Vehicle" (or equivalent) create button */
  async clickCreateButton(): Promise<void> {
    await this.page.locator('a[href="/vehicles/create"]').click()
  }

  /** Click the delete button for the first vehicle card that has it */
  async deleteFirstVehicle(): Promise<void> {
    this.page.once('dialog', dialog => dialog.accept())
    await this.page.locator('.btn-outline-danger').first().click()
  }

  /** Click the Details link of the first vehicle card */
  async clickFirstDetails(): Promise<void> {
    await this.page.locator('a:has-text("Details")').first().click()
  }

  /** Click the Edit link of the first vehicle card */
  async clickFirstEdit(): Promise<void> {
    await this.page.locator('a:has-text("Edit")').first().click()
  }

  // ── Create form ───────────────────────────────────────────────────────────

  async gotoCreate(): Promise<void> {
    await this.page.goto('/vehicles/create')
    await expect(this.page.locator('form')).toBeVisible()
  }

  async fillCreateForm(data: {
    make: string
    model: string
    year: number
    licensePlate: string
    vin: string
    mileage?: number
    color?: string
  }): Promise<void> {
    await this.page.locator('input[placeholder="e.g. Toyota"]').fill(data.make)
    await this.page.locator('input[placeholder="e.g. Corolla"]').fill(data.model)
    await this.page.locator('input[type="number"][min="1900"]').fill(String(data.year))
    await this.page.locator('input[placeholder="e.g. 123ABC"]').fill(data.licensePlate)
    // VIN field — type character by character to trigger uppercase transform
    const vinInput = this.page.locator('input[maxlength="17"]')
    await vinInput.fill(data.vin)
    if (data.mileage !== undefined) {
      await this.page.locator('input[type="number"][min="0"]').fill(String(data.mileage))
    }
    if (data.color) {
      await this.page.locator('input[placeholder="e.g. Silver"]').fill(data.color)
    }
  }

  async submitCreate(): Promise<void> {
    await this.page.locator('button[type="submit"]').click()
  }

  // ── Edit form ─────────────────────────────────────────────────────────────

  async fillEditMake(value: string): Promise<void> {
    const input = this.page.locator('input[placeholder="e.g. Toyota"]')
    await input.clear()
    await input.fill(value)
  }

  async submitEdit(): Promise<void> {
    await this.page.locator('button[type="submit"]').click()
  }
}
