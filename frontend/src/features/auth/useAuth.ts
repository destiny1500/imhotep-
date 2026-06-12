import { useCallback, useEffect } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from './auth.store';
import * as authApi from './api';
import { tokenStorage } from '@/shared/api/token.storage';
import { refreshClient } from '@/shared/api/client';
import type { AuthTokens, Role, User } from '@/shared/types/api';

export function homePathForRole(role: Role): string {
  return role === 'Tenant' ? '/my-housing' : '/dashboard';
}

export function useLogin() {
  const setSession = useAuthStore((s) => s.setSession);
  const navigate = useNavigate();

  return useMutation({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      authApi.login(email, password),
    onSuccess: (data) => {
      tokenStorage.set(data.refreshToken);
      setSession(data.user, data.accessToken);
      navigate(homePathForRole(data.user.role), { replace: true });
    },
  });
}

export function useRegister() {
  const navigate = useNavigate();
  return useMutation({
    mutationFn: authApi.register,
    onSuccess: () => {
      navigate('/login', { replace: true, state: { registered: true } });
    },
  });
}

export function useLogout() {
  const clearSession = useAuthStore((s) => s.clearSession);
  const navigate = useNavigate();

  return useCallback(async () => {
    const refreshToken = tokenStorage.get();
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        // best effort — the server-side token may already be revoked
      }
    }
    tokenStorage.clear();
    clearSession();
    navigate('/login', { replace: true });
  }, [clearSession, navigate]);
}

/**
 * On app start, if a refresh token survived a reload, exchange it for a fresh
 * access token and rehydrate the user. Otherwise mark the store initialized.
 */
export function useSessionRestore() {
  const initialized = useAuthStore((s) => s.initialized);

  useEffect(() => {
    if (useAuthStore.getState().initialized) return;

    const refreshToken = tokenStorage.get();
    if (!refreshToken) {
      useAuthStore.getState().setInitialized();
      return;
    }

    let cancelled = false;
    (async () => {
      try {
        const { data } = await refreshClient.post<AuthTokens>('/api/auth/refresh', {
          refreshToken,
        });
        tokenStorage.set(data.refreshToken);
        useAuthStore.getState().setAccessToken(data.accessToken);
        const me: User = await authApi.getMe();
        if (!cancelled) {
          useAuthStore.getState().setSession(me, data.accessToken);
        }
      } catch {
        if (!cancelled) {
          tokenStorage.clear();
          useAuthStore.getState().clearSession();
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [initialized]);
}
