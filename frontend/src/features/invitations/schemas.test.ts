import { describe, expect, it } from 'vitest';
import { acceptInvitationSchema } from './schemas';

const valid = {
  firstName: 'Léa',
  lastName: 'Martin',
  password: 'S3cure!Passw0rd',
  confirmPassword: 'S3cure!Passw0rd',
};

describe('acceptInvitationSchema', () => {
  it('accepts a valid submission', () => {
    expect(acceptInvitationSchema.safeParse(valid).success).toBe(true);
  });

  it('enforces the password policy', () => {
    const weak = { ...valid, password: 'court', confirmPassword: 'court' };
    expect(acceptInvitationSchema.safeParse(weak).success).toBe(false);
  });

  it('rejects mismatched password confirmation', () => {
    const mismatch = { ...valid, confirmPassword: 'Autre!Passw0rd99' };
    const result = acceptInvitationSchema.safeParse(mismatch);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.path).toContain('confirmPassword');
    }
  });

  it('requires first and last name', () => {
    expect(acceptInvitationSchema.safeParse({ ...valid, firstName: '' }).success).toBe(false);
    expect(acceptInvitationSchema.safeParse({ ...valid, lastName: '' }).success).toBe(false);
  });
});
