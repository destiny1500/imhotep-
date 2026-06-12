import { api } from '@/shared/api/client';
import type { Payment, PaymentMethod } from '@/shared/types/api';

export async function getPayments(leaseId: string): Promise<Payment[]> {
  const { data } = await api.get<Payment[]>('/api/payments', { params: { leaseId } });
  return data;
}

/** Tenant's own payment history. */
export async function getMyPayments(): Promise<Payment[]> {
  const { data } = await api.get<Payment[]>('/api/payments/my');
  return data;
}

export interface CreatePaymentPayload {
  leaseId: string;
  amount: number;
  periodYear: number;
  periodMonth: number;
  paidAt: string;
  method: PaymentMethod;
}

export async function createPayment(payload: CreatePaymentPayload): Promise<Payment> {
  const { data } = await api.post<Payment>('/api/payments', payload);
  return data;
}
