import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';
import { useAuthStore } from '@/features/auth/auth.store';

afterEach(() => {
  cleanup();
  localStorage.clear();
  useAuthStore.setState({ user: null, accessToken: null, initialized: false });
});
