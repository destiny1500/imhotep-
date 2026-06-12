import { expect, test } from '@playwright/test';
import { API, loginAs, mockAuth } from './helpers';

test.describe('Login flow', () => {
  test('shows validation errors on empty submit', async ({ page }) => {
    await page.goto('/login');
    await page.getByRole('button', { name: 'Se connecter' }).click();

    await expect(page.getByText("L'email est requis")).toBeVisible();
    await expect(page.getByText('Le mot de passe est requis')).toBeVisible();
  });

  test('rejects invalid credentials with an error message', async ({ page }) => {
    await page.route(`${API}/api/auth/login`, async (route) => {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid credentials' }),
      });
    });

    await page.goto('/login');
    await page.getByLabel('Adresse email').fill('owner@example.com');
    await page.getByLabel('Mot de passe').fill('mauvais-mot-de-passe');
    await page.getByRole('button', { name: 'Se connecter' }).click();

    await expect(page.getByText(/Identifiants invalides/)).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test('logs an owner in and lands on the dashboard', async ({ page }) => {
    await mockAuth(page, 'Owner');
    await page.route(`${API}/api/dashboard/owner`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          propertiesCount: 3,
          activeLeases: 2,
          rentCollectedThisMonth: 2450,
          latePaymentsCount: 1,
        }),
      });
    });

    await page.goto('/login');
    await page.getByLabel('Adresse email').fill('owner@example.com');
    await page.getByLabel('Mot de passe').fill('Mot2Passe!Tres-Solide');
    await page.getByRole('button', { name: 'Se connecter' }).click();

    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByRole('heading', { name: 'Tableau de bord' })).toBeVisible();
    await expect(page.getByText('Baux actifs')).toBeVisible();
    await expect(page.getByText('2 450,00 €')).toBeVisible();
  });

  test('logs a tenant in and lands on the housing page', async ({ page }) => {
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

    await loginAs(page, 'Tenant');

    await expect(page).toHaveURL(/\/my-housing$/);
    await expect(page.getByRole('heading', { name: 'Mon logement' })).toBeVisible();
    await expect(page.getByText('T2 rue de la Paix')).toBeVisible();
  });
});
