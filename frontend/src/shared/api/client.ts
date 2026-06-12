import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/features/auth/auth.store';
import { tokenStorage } from './token.storage';
import type { AuthTokens } from '@/shared/types/api';

export const API_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

/** Main API client: attaches the in-memory access token, handles 401 → refresh → retry. */
export const api = axios.create({
  baseURL: API_URL,
  headers: { Accept: 'application/json' },
});

/**
 * Bare client used only for the token refresh call. It has no interceptors,
 * so a failing refresh can never trigger another refresh (no recursion).
 */
export const refreshClient = axios.create({ baseURL: API_URL });

api.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

/** Single-flight refresh: concurrent 401s share the same in-flight promise. */
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const refreshToken = tokenStorage.get();
  if (!refreshToken) {
    throw new Error('No refresh token available');
  }
  const { data } = await refreshClient.post<AuthTokens>('/api/auth/refresh', { refreshToken });
  tokenStorage.set(data.refreshToken);
  useAuthStore.getState().setAccessToken(data.accessToken);
  return data.accessToken;
}

function clearSessionAndRedirect(): void {
  tokenStorage.clear();
  useAuthStore.getState().clearSession();
  if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
    window.location.assign('/login');
  }
}

type RetriableConfig = InternalAxiosRequestConfig & { _retry?: boolean };

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as RetriableConfig | undefined;
    const status = error.response?.status;
    const isAuthEndpoint = original?.url?.startsWith('/api/auth/') ?? false;

    if (status === 401 && original && !original._retry && !isAuthEndpoint) {
      original._retry = true;
      try {
        if (!refreshPromise) {
          refreshPromise = refreshAccessToken().finally(() => {
            refreshPromise = null;
          });
        }
        const accessToken = await refreshPromise;
        original.headers.Authorization = `Bearer ${accessToken}`;
        return await api(original);
      } catch (refreshError) {
        clearSessionAndRedirect();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);
