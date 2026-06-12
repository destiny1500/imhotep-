export type Role = 'Tenant' | 'Owner' | 'Agency' | 'Admin';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: Role;
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface AuthResponse extends AuthTokens {
  user: User;
}

export type PropertyType = 'Apartment' | 'House' | 'Commercial' | 'Parking' | 'Other';

export interface Property {
  id: string;
  label: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  postalCode: string;
  country: string;
  type: PropertyType;
  surfaceM2: number;
  rooms: number;
  rentAmount: number;
  chargesAmount: number;
}

export interface PropertyInput {
  label: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  postalCode: string;
  country: string;
  type: PropertyType;
  surfaceM2: number;
  rooms: number;
  rentAmount: number;
  chargesAmount: number;
}

export type LeaseStatus = 'Active' | 'Terminated' | 'Pending';

export interface Lease {
  id: string;
  propertyId: string;
  tenantEmail: string;
  tenantName?: string | null;
  startDate: string;
  endDate?: string | null;
  rentAmount: number;
  chargesAmount: number;
  depositAmount: number;
  status?: LeaseStatus;
}

/** Response of POST /api/leases. `invitationUrl` is set when the tenant had no
 * account yet: the lease is pending until the invitation is accepted. */
export interface CreateLeaseResult {
  lease: Lease;
  invitationUrl: string | null;
}

/** What an invited tenant sees before creating their account. */
export interface InvitationInfo {
  email: string;
  propertyLabel: string;
  city: string;
  rentAmount: number;
  chargesAmount: number;
  startDate: string;
  ownerName: string;
}

/** Tenant's active lease including the property (housing info page). */
export interface MyLease extends Lease {
  property: Property;
}

export type PaymentMethod = 'BankTransfer' | 'Card' | 'Cash' | 'Check';

export interface Payment {
  id: string;
  leaseId: string;
  amount: number;
  periodYear: number;
  periodMonth: number;
  paidAt: string;
  method: PaymentMethod;
}

export interface Receipt {
  id: string;
  paymentId: string;
  leaseId?: string;
  periodYear: number;
  periodMonth: number;
  createdAt: string;
  fileName?: string | null;
}

export type DocumentType =
  | 'LeaseContract'
  | 'RentReceipt'
  | 'Diagnostic'
  | 'Insurance'
  | 'Inventory'
  | 'Other';

export interface DocumentItem {
  id: string;
  fileName: string;
  type: DocumentType;
  sizeBytes: number;
  createdAt: string;
}

export interface ConversationParticipant {
  id: string;
  name: string;
  role: Role;
}

export interface Conversation {
  id: string;
  subject: string;
  participants: ConversationParticipant[];
  lastMessageAt: string;
}

export interface Message {
  id: string;
  senderId: string;
  senderName: string;
  body: string;
  sentAt: string;
}

export interface NotificationItem {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}

export interface OwnerDashboard {
  propertiesCount: number;
  activeLeases: number;
  rentCollectedThisMonth: number;
  latePaymentsCount: number;
}

export interface AgencyDashboard {
  managedProperties: number;
  ownersCount: number;
  tenantsCount: number;
  occupancyRate: number;
  latePaymentsCount: number;
  missingDocumentsCount: number;
  rentCollectedThisMonth: number;
}
