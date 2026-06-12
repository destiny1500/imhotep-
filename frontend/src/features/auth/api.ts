import { api } from '@/shared/api/client';
import type { AuthResponse, Role, User } from '@/shared/types/api';

export interface RegisterPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: Role;
}

export async function register(payload: RegisterPayload): Promise<{ userId: string }> {
  const { data } = await api.post<{ userId: string }>('/api/auth/register', payload);
  return data;
}

export async function login(email: string, password: string): Promise<AuthResponse> {
  const { data } = await api.post<AuthResponse>('/api/auth/login', { email, password });
  return data;
}

export async function logout(refreshToken: string): Promise<void> {
  await api.post('/api/auth/logout', { refreshToken });
}

export async function getMe(): Promise<User> {
  const { data } = await api.get<User>('/api/users/me');
  return data;
}
