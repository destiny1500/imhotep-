import type { Page } from '@playwright/test';
import type { Role } from '../src/shared/types/api';

export const API = 'http://localhost:5000';

export function makeUser(role: Role) {
  return {
    id: `user-${role.toLowerCase()}`,
    email: `${role.toLowerCase()}@example.com`,
    firstName: 'Camille',
    lastName: 'Dupont',
    role,
  };
}

/** Mocks the auth endpoints and the notifications poll for a given role. */
export async function mockAuth(page: Page, role: Role) {
  const user = makeUser(role);

  await page.route(`${API}/api/auth/login`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        accessToken: 'e2e-access-token',
        refreshToken: 'e2e-refresh-token',
        user,
      }),
    });
  });

  await page.route(`${API}/api/notifications`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  return user;
}

/** Performs the login through the real UI form. */
export async function loginAs(page: Page, role: Role) {
  const user = await mockAuth(page, role);
  await page.goto('/login');
  await page.getByLabel('Adresse email').fill(user.email);
  await page.getByLabel('Mot de passe').fill('Mot2Passe!Tres-Solide');
  await page.getByRole('button', { name: 'Se connecter' }).click();
  return user;
}
