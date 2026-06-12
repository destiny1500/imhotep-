import type { DocumentType } from '@/shared/types/api';

export const documentTypeValues: DocumentType[] = [
  'LeaseContract',
  'RentReceipt',
  'Diagnostic',
  'Insurance',
  'Inventory',
  'Other',
];

export const documentTypeLabels: Record<DocumentType, string> = {
  LeaseContract: 'Contrat de bail',
  RentReceipt: 'Quittance de loyer',
  Diagnostic: 'Diagnostic',
  Insurance: 'Assurance',
  Inventory: 'État des lieux',
  Other: 'Autre',
};
