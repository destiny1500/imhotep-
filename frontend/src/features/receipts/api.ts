import { api } from '@/shared/api/client';
import type { Receipt } from '@/shared/types/api';

/** Tenant's own receipts (quittances). */
export async function getMyReceipts(): Promise<Receipt[]> {
  const { data } = await api.get<Receipt[]>('/api/receipts/my');
  return data;
}

export async function getReceipts(leaseId: string): Promise<Receipt[]> {
  const { data } = await api.get<Receipt[]>('/api/receipts', { params: { leaseId } });
  return data;
}

export async function generateReceipt(paymentId: string): Promise<Receipt> {
  const { data } = await api.post<Receipt>('/api/receipts/generate', { paymentId });
  return data;
}

export async function downloadReceipt(id: string): Promise<Blob> {
  const { data } = await api.get<Blob>(`/api/receipts/${id}/download`, {
    responseType: 'blob',
  });
  return data;
}
