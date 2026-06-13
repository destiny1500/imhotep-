import { z } from 'zod';
import { requiredString } from '@/shared/lib/zod';

export const newConversationSchema = z.object({
  participantUserId: requiredString('Le destinataire'),
  subject: requiredString('Le sujet'),
  body: requiredString('Le message'),
});

export type NewConversationValues = z.infer<typeof newConversationSchema>;

export const newMessageSchema = z.object({
  body: requiredString('Le message'),
});

export type NewMessageValues = z.infer<typeof newMessageSchema>;
