import { api } from '@/shared/api/client';
import type { Conversation, Message } from '@/shared/types/api';

export async function getConversations(): Promise<Conversation[]> {
  const { data } = await api.get<Conversation[]>('/api/conversations');
  return data;
}

export interface CreateConversationPayload {
  participantUserId: string;
  propertyId?: string;
  subject: string;
  body: string;
}

export async function createConversation(payload: CreateConversationPayload): Promise<Conversation> {
  const { data } = await api.post<Conversation>('/api/conversations', payload);
  return data;
}

export async function getMessages(conversationId: string): Promise<Message[]> {
  const { data } = await api.get<Message[]>(`/api/conversations/${conversationId}/messages`);
  return data;
}

export async function sendMessage(conversationId: string, body: string): Promise<Message> {
  const { data } = await api.post<Message>(`/api/conversations/${conversationId}/messages`, { body });
  return data;
}
