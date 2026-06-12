import { describe, expect, it } from 'vitest';
import { loginSchema, registerSchema } from './schemas';

const validRegistration = {
  email: 'jean@example.com',
  password: 'Tr3s-Solide!MotDePasse',
  confirmPassword: 'Tr3s-Solide!MotDePasse',
  firstName: 'Jean',
  lastName: 'Martin',
  role: 'Owner' as const,
};

describe('registerSchema', () => {
  it('accepts a valid registration', () => {
    expect(registerSchema.safeParse(validRegistration).success).toBe(true);
  });

  it('rejects a password shorter than 12 characters', () => {
    const result = registerSchema.safeParse({
      ...validRegistration,
      password: 'Sh0rt!pass',
      confirmPassword: 'Sh0rt!pass',
    });
    expect(result.success).toBe(false);
  });

  it.each([
    ['no uppercase', 'tres-solide!motdepasse1'],
    ['no lowercase', 'TRES-SOLIDE!MOTDEPASSE1'],
    ['no digit', 'Tres-Solide!MotDePasse'],
    ['no special character', 'Tres3SolideMotDePasse'],
  ])('rejects a weak password (%s)', (_label, password) => {
    const result = registerSchema.safeParse({
      ...validRegistration,
      password,
      confirmPassword: password,
    });
    expect(result.success).toBe(false);
  });

  it('rejects mismatched password confirmation', () => {
    const result = registerSchema.safeParse({
      ...validRegistration,
      confirmPassword: 'Autre-Mot2Passe!Valide',
    });
    expect(result.success).toBe(false);
  });

  it('rejects an invalid email', () => {
    const result = registerSchema.safeParse({ ...validRegistration, email: 'pas-un-email' });
    expect(result.success).toBe(false);
  });
});

describe('loginSchema', () => {
  it('requires email and password', () => {
    const result = loginSchema.safeParse({ email: '', password: '' });
    expect(result.success).toBe(false);
  });

  it('accepts valid credentials', () => {
    expect(loginSchema.safeParse({ email: 'a@b.fr', password: 'x' }).success).toBe(true);
  });
});
