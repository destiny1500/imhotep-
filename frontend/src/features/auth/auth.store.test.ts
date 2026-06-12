import { describe, expect, it } from 'vitest';
import { useAuthStore } from './auth.store';
import type { User } from '@/shared/types/api';

const user: User = {
  id: 'u1',
  email: 'marie@example.com',
  firstName: 'Marie',
  lastName: 'Durand',
  role: 'Owner',
};

describe('auth.store', () => {
  it('starts with an empty, uninitialized session', () => {
    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
    expect(state.initialized).toBe(false);
  });

  it('setSession stores the user and access token and marks the store initialized', () => {
    useAuthStore.getState().setSession(user, 'access-123');

    const state = useAuthStore.getState();
    expect(state.user).toEqual(user);
    expect(state.accessToken).toBe('access-123');
    expect(state.initialized).toBe(true);
  });

  it('setAccessToken replaces only the token', () => {
    useAuthStore.getState().setSession(user, 'old-token');
    useAuthStore.getState().setAccessToken('new-token');

    const state = useAuthStore.getState();
    expect(state.accessToken).toBe('new-token');
    expect(state.user).toEqual(user);
  });

  it('clearSession removes the user and token but stays initialized', () => {
    useAuthStore.getState().setSession(user, 'access-123');
    useAuthStore.getState().clearSession();

    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.accessToken).toBeNull();
    expect(state.initialized).toBe(true);
  });
});
