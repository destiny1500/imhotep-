import { create } from 'zustand';
import type { User } from '@/shared/types/api';

/**
 * In-memory session state. The access token is intentionally NOT persisted
 * (see shared/api/token.storage.ts for the refresh token strategy).
 */
export interface AuthState {
  user: User | null;
  accessToken: string | null;
  /** True once the initial session restore attempt has finished. */
  initialized: boolean;
  setSession: (user: User, accessToken: string) => void;
  setAccessToken: (accessToken: string) => void;
  setInitialized: () => void;
  clearSession: () => void;
}

export const useAuthStore = create<AuthState>()((set) => ({
  user: null,
  accessToken: null,
  initialized: false,
  setSession: (user, accessToken) => set({ user, accessToken, initialized: true }),
  setAccessToken: (accessToken) => set({ accessToken }),
  setInitialized: () => set({ initialized: true }),
  clearSession: () => set({ user: null, accessToken: null, initialized: true }),
}));
