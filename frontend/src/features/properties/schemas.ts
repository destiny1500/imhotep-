import { z } from 'zod';
import { nonNegativeAmount, positiveAmount, requiredString } from '@/shared/lib/zod';

export const propertyTypeValues = ['Apartment', 'House', 'Commercial', 'Parking', 'Other'] as const;

export const propertyTypeLabels: Record<(typeof propertyTypeValues)[number], string> = {
  Apartment: 'Appartement',
  House: 'Maison',
  Commercial: 'Local commercial',
  Parking: 'Parking',
  Other: 'Autre',
};

export const propertySchema = z.object({
  label: requiredString('Le libellé'),
  addressLine1: requiredString("L'adresse"),
  addressLine2: z.string().trim().optional(),
  city: requiredString('La ville'),
  postalCode: z
    .string({ required_error: 'Le code postal est requis' })
    .trim()
    .regex(/^[0-9A-Za-z -]{3,10}$/, 'Code postal invalide'),
  country: requiredString('Le pays'),
  type: z.enum(propertyTypeValues, { required_error: 'Le type est requis' }),
  surfaceM2: positiveAmount('La surface'),
  rooms: z.coerce
    .number({ invalid_type_error: 'Le nombre de pièces doit être un nombre' })
    .int('Le nombre de pièces doit être entier')
    .positive('Le nombre de pièces doit être positif'),
  rentAmount: positiveAmount('Le loyer'),
  chargesAmount: nonNegativeAmount('Les charges'),
});

export type PropertyFormValues = z.infer<typeof propertySchema>;

export const delegateSchema = z.object({
  agencyId: requiredString("L'identifiant de l'agence"),
  feePercent: z.coerce
    .number({ invalid_type_error: 'Les honoraires doivent être un nombre' })
    .min(0, 'Les honoraires ne peuvent pas être négatifs')
    .max(100, 'Les honoraires ne peuvent pas dépasser 100 %'),
});

export type DelegateFormValues = z.infer<typeof delegateSchema>;
