import { z } from 'zod';
import { emailSchema, nonNegativeAmount, positiveAmount, requiredString } from '@/shared/lib/zod';

export const leaseSchema = z
  .object({
    tenantEmail: emailSchema,
    startDate: requiredString('La date de début'),
    endDate: z.string().trim().optional(),
    rentAmount: positiveAmount('Le loyer'),
    chargesAmount: nonNegativeAmount('Les charges'),
    depositAmount: nonNegativeAmount('Le dépôt de garantie'),
  })
  .refine(
    (data) => !data.endDate || data.endDate > data.startDate,
    { message: 'La date de fin doit être postérieure à la date de début', path: ['endDate'] },
  );

export type LeaseFormValues = z.infer<typeof leaseSchema>;
