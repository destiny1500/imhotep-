import { api } from '@/shared/api/client';
import type { DocumentItem, DocumentType } from '@/shared/types/api';

export interface DocumentFilter {
  propertyId?: string;
  leaseId?: string;
}

export async function getDocuments(filter: DocumentFilter): Promise<DocumentItem[]> {
  const { data } = await api.get<DocumentItem[]>('/api/documents', { params: filter });
  return data;
}

export async function uploadDocument(payload: {
  file: File;
  type: DocumentType;
  propertyId?: string;
  leaseId?: string;
}): Promise<DocumentItem> {
  const formData = new FormData();
  formData.append('file', payload.file);
  formData.append('type', payload.type);
  if (payload.propertyId) formData.append('propertyId', payload.propertyId);
  if (payload.leaseId) formData.append('leaseId', payload.leaseId);
  const { data } = await api.post<DocumentItem>('/api/documents', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
}

export async function downloadDocument(id: string): Promise<Blob> {
  const { data } = await api.get<Blob>(`/api/documents/${id}/download`, {
    responseType: 'blob',
  });
  return data;
}
