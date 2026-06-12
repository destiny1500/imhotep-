import { api } from '@/shared/api/client';
import type { NotificationItem } from '@/shared/types/api';

export async function getNotifications(): Promise<NotificationItem[]> {
  const { data } = await api.get<NotificationItem[]>('/api/notifications');
  return data;
}

export async function markNotificationRead(id: string): Promise<void> {
  await api.post(`/api/notifications/${id}/read`);
}
