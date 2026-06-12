import { z } from 'zod';
import { passwordSchema } from '@/features/auth/schemas';
import { requiredString } from '@/shared/lib/zod';

export const acceptInvitationSchema = z
  .object({
    firstName: requiredString('Le prénom'),
    lastName: requiredString('Le nom'),
    password: passwordSchema,
    confirmPassword: z
      .string({ required_error: 'La confirmation est requise' })
      .min(1, 'La confirmation est requise'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Les mots de passe ne correspondent pas',
    path: ['confirmPassword'],
  });

export type AcceptInvitationInput = z.infer<typeof acceptInvitationSchema>;
