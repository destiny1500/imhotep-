import { z } from 'zod';

/** Required non-empty trimmed string with a French error message. */
export const requiredString = (label: string) =>
  z.string({ required_error: `${label} est requis(e)` }).trim().min(1, `${label} est requis(e)`);

/** Strictly positive amount in euros (coerced from form inputs). */
export const positiveAmount = (label = 'Le montant') =>
  z.coerce
    .number({ invalid_type_error: `${label} doit être un nombre` })
    .positive(`${label} doit être strictement positif`);

/** Amount >= 0 in euros (coerced from form inputs). */
export const nonNegativeAmount = (label = 'Le montant') =>
  z.coerce
    .number({ invalid_type_error: `${label} doit être un nombre` })
    .min(0, `${label} ne peut pas être négatif`);

export const emailSchema = z
  .string({ required_error: "L'email est requis" })
  .trim()
  .min(1, "L'email est requis")
  .email("Format d'email invalide");
