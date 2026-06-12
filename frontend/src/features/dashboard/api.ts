import { api } from '@/shared/api/client';
import type { AgencyDashboard, OwnerDashboard } from '@/shared/types/api';

export async function getOwnerDashboard(): Promise<OwnerDashboard> {
  const { data } = await api.get<OwnerDashboard>('/api/dashboard/owner');
  return data;
}

export async function getAgencyDashboard(): Promise<AgencyDashboard> {
  const { data } = await api.get<AgencyDashboard>('/api/dashboard/agency');
  return data;
}
