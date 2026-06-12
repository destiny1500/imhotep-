import { Badge } from '@/shared/components/Badge';
import { EmptyState } from '@/shared/components/EmptyState';
import { formatCurrency, formatDate, formatPeriod } from '@/shared/lib/formatters';
import { paymentMethodLabels } from './schemas';
import type { Payment } from '@/shared/types/api';
import type { ReactNode } from 'react';

export function PaymentList({
  payments,
  renderAction,
}: {
  payments: Payment[];
  renderAction?: (payment: Payment) => ReactNode;
}) {
  if (payments.length === 0) {
    return <EmptyState title="Aucun paiement" description="Aucun paiement enregistré pour le moment." />;
  }

  return (
    <table className="min-w-full divide-y divide-slate-200 text-sm">
      <thead className="text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
        <tr>
          <th scope="col" className="py-2 pr-4">Période</th>
          <th scope="col" className="py-2 pr-4 text-right">Montant</th>
          <th scope="col" className="py-2 pr-4">Payé le</th>
          <th scope="col" className="py-2 pr-4">Moyen</th>
          {renderAction && (
            <th scope="col" className="py-2">
              <span className="sr-only">Actions</span>
            </th>
          )}
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {payments.map((payment) => (
          <tr key={payment.id}>
            <td className="py-2.5 pr-4 font-medium capitalize text-slate-900">
              {formatPeriod(payment.periodYear, payment.periodMonth)}
            </td>
            <td className="py-2.5 pr-4 text-right">{formatCurrency(payment.amount)}</td>
            <td className="py-2.5 pr-4 text-slate-600">{formatDate(payment.paidAt)}</td>
            <td className="py-2.5 pr-4">
              <Badge tone="green">{paymentMethodLabels[payment.method]}</Badge>
            </td>
            {renderAction && <td className="py-2.5 text-right">{renderAction(payment)}</td>}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
