import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { generateReceipt, getMyReceipts, getReceipts } from './api';
import { ReceiptList } from './ReceiptList';
import { getPayments } from '@/features/payments/api';
import { PaymentList } from '@/features/payments/PaymentList';
import { LeaseSelector } from '@/features/leases/LeaseSelector';
import { useAuthStore } from '@/features/auth/auth.store';
import { Card } from '@/shared/components/Card';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import type { Lease } from '@/shared/types/api';

export function ReceiptsPage() {
  const role = useAuthStore((s) => s.user?.role);
  return (
    <div className="space-y-6">
      <h1 className="text-xl font-bold text-slate-900">Quittances</h1>
      {role === 'Tenant' ? <TenantReceipts /> : <ManagerReceipts />}
    </div>
  );
}

/** Tenant: list and download own receipts. */
function TenantReceipts() {
  const { data: receipts, isPending, isError } = useQuery({
    queryKey: ['receipts', 'my'],
    queryFn: getMyReceipts,
  });

  if (isPending) return <PageSpinner />;
  if (isError) return <ErrorState message="Impossible de charger vos quittances." />;

  return (
    <Card title="Mes quittances">
      <ReceiptList receipts={receipts} />
    </Card>
  );
}

/** Agency (and owner): generate receipts from payments of a lease. */
function ManagerReceipts() {
  const [lease, setLease] = useState<Lease | null>(null);

  return (
    <div className="space-y-6">
      <Card>
        <LeaseSelector onLeaseSelected={setLease} />
      </Card>
      {lease && <LeaseReceipts leaseId={lease.id} />}
    </div>
  );
}

function LeaseReceipts({ leaseId }: { leaseId: string }) {
  const queryClient = useQueryClient();

  const payments = useQuery({
    queryKey: ['payments', leaseId],
    queryFn: () => getPayments(leaseId),
  });
  const receipts = useQuery({
    queryKey: ['receipts', leaseId],
    queryFn: () => getReceipts(leaseId),
  });

  const generate = useMutation({
    mutationFn: generateReceipt,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['receipts', leaseId] });
    },
  });

  return (
    <div className="space-y-6">
      <Card title="Paiements du bail">
        {payments.isPending && <PageSpinner />}
        {payments.isError && <ErrorState message="Impossible de charger les paiements." />}
        {payments.data && (
          <PaymentList
            payments={payments.data}
            renderAction={(payment) => (
              <Button
                variant="secondary"
                onClick={() => generate.mutate(payment.id)}
                isLoading={generate.isPending && generate.variables === payment.id}
              >
                Générer la quittance
              </Button>
            )}
          />
        )}
        {generate.isError && (
          <div className="mt-3">
            <ErrorState message="La génération de la quittance a échoué." />
          </div>
        )}
      </Card>

      <Card title="Quittances générées">
        {receipts.isPending && <PageSpinner />}
        {receipts.isError && <ErrorState message="Impossible de charger les quittances." />}
        {receipts.data && <ReceiptList receipts={receipts.data} />}
      </Card>
    </div>
  );
}
