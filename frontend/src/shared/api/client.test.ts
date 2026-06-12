import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { api, refreshClient } from './client';
import { tokenStorage } from './token.storage';
import { useAuthStore } from '@/features/auth/auth.store';
import type { User } from '@/shared/types/api';

const user: User = {
  id: 'u1',
  email: 'paul@example.com',
  firstName: 'Paul',
  lastName: 'Bernard',
  role: 'Tenant',
};

function ok<T>(config: InternalAxiosRequestConfig, data: T): AxiosResponse<T> {
  return { data, status: 200, statusText: 'OK', headers: {}, config };
}

function unauthorized(config: InternalAxiosRequestConfig): AxiosError {
  return new AxiosError('Unauthorized', AxiosError.ERR_BAD_REQUEST, config, null, {
    data: { message: 'Token expired' },
    status: 401,
    statusText: 'Unauthorized',
    headers: {},
    config,
  });
}

const originalApiAdapter = api.defaults.adapter;
const originalRefreshAdapter = refreshClient.defaults.adapter;

describe('api client 401 refresh flow', () => {
  beforeEach(() => {
    tokenStorage.set('refresh-token-1');
    useAuthStore.getState().setSession(user, 'expired-access-token');
  });

  afterEach(() => {
    api.defaults.adapter = originalApiAdapter;
    refreshClient.defaults.adapter = originalRefreshAdapter;
    vi.restoreAllMocks();
  });

  it('refreshes the token on 401 and retries the original request', async () => {
    const seenAuthHeaders: (string | undefined)[] = [];
    let apiCalls = 0;
    let refreshCalls = 0;

    api.defaults.adapter = async (config) => {
      apiCalls += 1;
      seenAuthHeaders.push(config.headers.Authorization as string | undefined);
      if (apiCalls === 1) throw unauthorized(config);
      return ok(config, [{ id: 'r1' }]);
    };

    refreshClient.defaults.adapter = async (config) => {
      refreshCalls += 1;
      expect(JSON.parse(config.data as string)).toEqual({ refreshToken: 'refresh-token-1' });
      return ok(config, { accessToken: 'fresh-access-token', refreshToken: 'refresh-token-2' });
    };

    const response = await api.get('/api/receipts/my');

    expect(response.status).toBe(200);
    expect(response.data).toEqual([{ id: 'r1' }]);
    expect(apiCalls).toBe(2);
    expect(refreshCalls).toBe(1);
    expect(seenAuthHeaders[0]).toBe('Bearer expired-access-token');
    expect(seenAuthHeaders[1]).toBe('Bearer fresh-access-token');
    expect(useAuthStore.getState().accessToken).toBe('fresh-access-token');
    expect(tokenStorage.get()).toBe('refresh-token-2');
  });

  it('shares a single refresh between concurrent 401 responses', async () => {
    const callsPerUrl = new Map<string, number>();
    let refreshCalls = 0;

    api.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      const count = (callsPerUrl.get(url) ?? 0) + 1;
      callsPerUrl.set(url, count);
      if (count === 1) throw unauthorized(config);
      return ok(config, { url });
    };

    refreshClient.defaults.adapter = async (config) => {
      refreshCalls += 1;
      // Simulate network latency so both 401 handlers run while refreshing.
      await new Promise((resolve) => setTimeout(resolve, 10));
      return ok(config, { accessToken: 'fresh-access-token', refreshToken: 'refresh-token-2' });
    };

    const [a, b] = await Promise.all([api.get('/api/payments/my'), api.get('/api/receipts/my')]);

    expect(a.status).toBe(200);
    expect(b.status).toBe(200);
    expect(refreshCalls).toBe(1);
  });

  it('clears the session when the refresh fails', async () => {
    // jsdom logs a "not implemented: navigation" error on redirect — silence it.
    vi.spyOn(console, 'error').mockImplementation(() => {});

    api.defaults.adapter = async (config) => {
      throw unauthorized(config);
    };
    refreshClient.defaults.adapter = async (config) => {
      throw new AxiosError('Invalid refresh token', AxiosError.ERR_BAD_REQUEST, config, null, {
        data: {},
        status: 401,
        statusText: 'Unauthorized',
        headers: {},
        config,
      });
    };

    await expect(api.get('/api/payments/my')).rejects.toBeInstanceOf(AxiosError);
    expect(useAuthStore.getState().user).toBeNull();
    expect(useAuthStore.getState().accessToken).toBeNull();
    expect(tokenStorage.get()).toBeNull();
  });

  it('does not try to refresh on auth endpoints', async () => {
    let refreshCalls = 0;
    api.defaults.adapter = async (config) => {
      throw unauthorized(config);
    };
    refreshClient.defaults.adapter = async (config) => {
      refreshCalls += 1;
      return ok(config, { accessToken: 'x', refreshToken: 'y' });
    };

    await expect(api.post('/api/auth/login', { email: 'a@b.fr', password: 'x' })).rejects.toThrow();
    expect(refreshCalls).toBe(0);
  });
});
