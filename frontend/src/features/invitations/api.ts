import { api } from '@/shared/api/client';
import type { AuthResponse, InvitationInfo } from '@/shared/types/api';

export async function getInvitation(token: string): Promise<InvitationInfo> {
  const { data } = await api.get<InvitationInfo>(`/api/invitations/${encodeURIComponent(token)}`);
  return data;
}

export interface AcceptInvitationPayload {
  firstName: string;
  lastName: string;
  password: string;
}

/** Creates the tenant account and signs them in; the pending lease becomes active. */
export async function acceptInvitation(
  token: string,
  payload: AcceptInvitationPayload,
): Promise<AuthResponse> {
  const { data } = await api.post<AuthResponse>(
    `/api/invitations/${encodeURIComponent(token)}/accept`,
    payload,
  );
  return data;
}
