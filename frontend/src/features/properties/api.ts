import { api } from '@/shared/api/client';
import type { Property, PropertyInput } from '@/shared/types/api';

export async function getProperties(): Promise<Property[]> {
  const { data } = await api.get<Property[]>('/api/properties');
  return data;
}

export async function getProperty(id: string): Promise<Property> {
  const { data } = await api.get<Property>(`/api/properties/${id}`);
  return data;
}

export async function createProperty(payload: PropertyInput): Promise<Property> {
  const { data } = await api.post<Property>('/api/properties', payload);
  return data;
}

export async function updateProperty(id: string, payload: PropertyInput): Promise<Property> {
  const { data } = await api.put<Property>(`/api/properties/${id}`, payload);
  return data;
}

export async function delegateProperty(
  id: string,
  payload: { agencyId: string; feePercent: number },
): Promise<void> {
  await api.post(`/api/properties/${id}/delegate`, payload);
}
