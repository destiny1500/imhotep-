import { z } from 'zod';
import { emailSchema, requiredString } from '@/shared/lib/zod';

export const loginSchema = z.object({
  email: emailSchema,
  password: z
    .string({ required_error: 'Le mot de passe est requis' })
    .min(1, 'Le mot de passe est requis'),
});

export type LoginInput = z.infer<typeof loginSchema>;

export const passwordSchema = z
  .string({ required_error: 'Le mot de passe est requis' })
  .min(12, 'Le mot de passe doit contenir au moins 12 caractères')
  .regex(/[a-z]/, 'Le mot de passe doit contenir une minuscule')
  .regex(/[A-Z]/, 'Le mot de passe doit contenir une majuscule')
  .regex(/[0-9]/, 'Le mot de passe doit contenir un chiffre')
  .regex(/[^A-Za-z0-9]/, 'Le mot de passe doit contenir un caractère spécial');

export const registerSchema = z
  .object({
    email: emailSchema,
    password: passwordSchema,
    confirmPassword: z
      .string({ required_error: 'La confirmation est requise' })
      .min(1, 'La confirmation est requise'),
    firstName: requiredString('Le prénom'),
    lastName: requiredString('Le nom'),
    role: z.enum(['Tenant', 'Owner', 'Agency'], {
      required_error: 'Le rôle est requis',
      invalid_type_error: 'Rôle invalide',
    }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Les mots de passe ne correspondent pas',
    path: ['confirmPassword'],
  });

export type RegisterInput = z.infer<typeof registerSchema>;
