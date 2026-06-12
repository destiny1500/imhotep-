/**
 * Refresh-token persistence, isolated in a single module.
 *
 * NOTE: storing the refresh token in localStorage is acceptable for this
 * project, but in production an httpOnly, Secure, SameSite cookie set by the
 * backend is preferred (it is not readable by JavaScript and therefore not
 * exfiltratable through XSS). The access token itself is never persisted:
 * it lives in memory only (zustand store).
 */
const STORAGE_KEY = 'imhotep.refreshToken';

export const tokenStorage = {
  get(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  },
  set(refreshToken: string): void {
    try {
      localStorage.setItem(STORAGE_KEY, refreshToken);
    } catch {
      // storage unavailable (private mode, SSR…) — session will not survive reloads
    }
  },
  clear(): void {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // ignore
    }
  },
};
