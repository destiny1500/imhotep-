import { describe, expect, it } from 'vitest';
import { paymentSchema } from './schemas';

const validPayment = {
  amount: 850.5,
  periodYear: new Date().getFullYear(),
  periodMonth: 6,
  paidAt: '2026-06-05',
  method: 'BankTransfer' as const,
};

describe('paymentSchema', () => {
  it('accepts a valid payment', () => {
    expect(paymentSchema.safeParse(validPayment).success).toBe(true);
  });

  it('rejects a negative amount', () => {
    const result = paymentSchema.safeParse({ ...validPayment, amount: -100 });
    expect(result.success).toBe(false);
  });

  it('rejects a zero amount', () => {
    expect(paymentSchema.safeParse({ ...validPayment, amount: 0 }).success).toBe(false);
  });

  it('rejects an out-of-range month', () => {
    expect(paymentSchema.safeParse({ ...validPayment, periodMonth: 13 }).success).toBe(false);
    expect(paymentSchema.safeParse({ ...validPayment, periodMonth: 0 }).success).toBe(false);
  });

  it('rejects an unknown payment method', () => {
    expect(paymentSchema.safeParse({ ...validPayment, method: 'Crypto' }).success).toBe(false);
  });

  it('coerces string amounts coming from form inputs', () => {
    const result = paymentSchema.safeParse({ ...validPayment, amount: '850.50' });
    expect(result.success).toBe(true);
    if (result.success) expect(result.data.amount).toBe(850.5);
  });
});
