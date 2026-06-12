import { z } from 'zod';
import { positiveAmount, requiredString } from '@/shared/lib/zod';
import type { PaymentMethod } from '@/shared/types/api';

export const paymentMethodValues = ['BankTransfer', 'Card', 'Cash', 'Check'] as const;

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  BankTransfer: 'Virement bancaire',
  Card: 'Carte bancaire',
  Cash: 'Espèces',
  Check: 'Chèque',
};

const currentYear = new Date().getFullYear();

export const paymentSchema = z.object({
  amount: positiveAmount('Le montant'),
  periodYear: z.coerce
    .number({ invalid_type_error: "L'année doit être un nombre" })
    .int('Année invalide')
    .min(2000, 'Année invalide')
    .max(currentYear + 1, 'Année invalide'),
  periodMonth: z.coerce
    .number({ invalid_type_error: 'Le mois doit être un nombre' })
    .int('Mois invalide')
    .min(1, 'Mois invalide')
    .max(12, 'Mois invalide'),
  paidAt: requiredString('La date de paiement'),
  method: z.enum(paymentMethodValues, { required_error: 'Le moyen de paiement est requis' }),
});

export type PaymentFormValues = z.infer<typeof paymentSchema>;
