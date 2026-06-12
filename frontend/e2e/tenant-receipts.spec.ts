import { expect, test } from '@playwright/test';
import { API, loginAs } from './helpers';

test.describe('Tenant receipts (quittances)', () => {
  test.beforeEach(async ({ page }) => {
    // The tenant home page (/my-housing) loads the lease.
    await page.route(`${API}/api/leases/my`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'lease-1',
          propertyId: 'prop-1',
          tenantEmail: 'tenant@example.com',
          startDate: '2025-01-01',
          endDate: null,
          rentAmount: 700,
          chargesAmount: 50,
          depositAmount: 700,
          property: {
            id: 'prop-1',
            label: 'T2 rue de la Paix',
            addressLine1: '12 rue de la Paix',
            addressLine2: null,
            city: 'Lyon',
            postalCode: '69001',
            country: 'France',
            type: 'Apartment',
            surfaceM2: 45,
            rooms: 2,
            rentAmount: 700,
            chargesAmount: 50,
          },
        }),
      });
    });
  });

  test('lists the tenant receipts and downloads one', async ({ page }) => {
    await page.route(`${API}/api/receipts/my`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'receipt-1',
            paymentId: 'payment-1',
            periodYear: 2026,
            periodMonth: 5,
            createdAt: '2026-06-01T10:00:00Z',
            fileName: 'quittance-2026-05.pdf',
          },
          {
            id: 'receipt-2',
            paymentId: 'payment-2',
            periodYear: 2026,
            periodMonth: 4,
            createdAt: '2026-05-01T10:00:00Z',
            fileName: 'quittance-2026-04.pdf',
          },
        ]),
      });
    });

    await page.route(`${API}/api/receipts/receipt-1/download`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/pdf',
        body: Buffer.from('%PDF-1.4 fake receipt'),
      });
    });

    await loginAs(page, 'Tenant');
    await page.getByRole('link', { name: 'Quittances' }).click();

    await expect(page).toHaveURL(/\/receipts$/);
    await expect(page.getByRole('heading', { name: 'Quittances' })).toBeVisible();
    await expect(page.getByText('mai 2026')).toBeVisible();
    await expect(page.getByText('avril 2026')).toBeVisible();

    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('row', { name: /mai 2026/ }).getByRole('button', { name: 'Télécharger' }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe('quittance-2026-05.pdf');
  });

  test('shows an empty state when the tenant has no receipts', async ({ page }) => {
    await page.route(`${API}/api/receipts/my`, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await loginAs(page, 'Tenant');
    await page.getByRole('link', { name: 'Quittances' }).click();

    await expect(page.getByText('Aucune quittance')).toBeVisible();
  });
});
