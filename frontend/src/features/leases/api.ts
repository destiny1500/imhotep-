import { api } from '@/shared/api/client';
import type { Lease, MyLease } from '@/shared/types/api';

export async function getLeases(propertyId: string): Promise<Lease[]> {
  const { data } = await api.get<Lease[]>('/api/leases', { params: { propertyId } });
  return data;
}

export interface CreateLeasePayload {
  propertyId: string;
  tenantEmail: string;
  startDate: string;
  endDate?: string;
  rentAmount: number;
  chargesAmount: number;
  depositAmount: number;
}

export async function createLease(payload: CreateLeasePayload): Promise<Lease> {
  const { data } = await api.post<Lease>('/api/leases', payload);
  return data;
}

/** Tenant's active lease including its property. */
export async function getMyLease(): Promise<MyLease> {
  const { data } = await api.get<MyLease>('/api/leases/my');
  return data;
}
