import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getMyPayments, getPayments } from './api';
import { PaymentList } from './PaymentList';
import { RecordPaymentForm } from './RecordPaymentForm';
import { LeaseSelector } from '@/features/leases/LeaseSelector';
import { useAuthStore } from '@/features/auth/auth.store';
import { Card } from '@/shared/components/Card';
import { ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import type { Lease } from '@/shared/types/api';

export function PaymentsPage() {
  const role = useAuthStore((s) => s.user?.role);
  return (
    <div className="space-y-6">
      <h1 className="text-xl font-bold text-slate-900">Paiements</h1>
      {role === 'Tenant' ? <TenantPayments /> : <ManagerPayments />}
    </div>
  );
}

/** Tenant: read-only payment history. */
function TenantPayments() {
  const { data: payments, isPending, isError } = useQuery({
    queryKey: ['payments', 'my'],
    queryFn: getMyPayments,
  });

  if (isPending) return <PageSpinner />;
  if (isError) return <ErrorState message="Impossible de charger vos paiements." />;

  return (
    <Card title="Historique de mes paiements">
      <PaymentList payments={payments} />
    </Card>
  );
}

/** Owner/Agency: pick a lease, record payments and review the history. */
function ManagerPayments() {
  const [lease, setLease] = useState<Lease | null>(null);

  return (
    <div className="space-y-6">
      <Card>
        <LeaseSelector onLeaseSelected={setLease} />
      </Card>

      {lease && (
        <>
          <Card title="Enregistrer un paiement">
            <RecordPaymentForm leaseId={lease.id} />
          </Card>
          <LeasePayments leaseId={lease.id} />
        </>
      )}
    </div>
  );
}

function LeasePayments({ leaseId }: { leaseId: string }) {
  const { data: payments, isPending, isError } = useQuery({
    queryKey: ['payments', leaseId],
    queryFn: () => getPayments(leaseId),
  });

  return (
    <Card title="Paiements du bail">
      {isPending && <PageSpinner />}
      {isError && <ErrorState message="Impossible de charger les paiements." />}
      {payments && <PaymentList payments={payments} />}
    </Card>
  );
}
