import { useMutation } from '@tanstack/react-query';
import { downloadReceipt } from './api';
import { Button } from '@/shared/components/Button';
import { EmptyState } from '@/shared/components/EmptyState';
import { downloadBlob } from '@/shared/lib/download';
import { formatDate, formatPeriod } from '@/shared/lib/formatters';
import type { Receipt } from '@/shared/types/api';

export function ReceiptList({ receipts }: { receipts: Receipt[] }) {
  const download = useMutation({
    mutationFn: async (receipt: Receipt) => {
      const blob = await downloadReceipt(receipt.id);
      const fallback = `quittance-${receipt.periodYear}-${String(receipt.periodMonth).padStart(2, '0')}.pdf`;
      downloadBlob(blob, receipt.fileName ?? fallback);
    },
  });

  if (receipts.length === 0) {
    return (
      <EmptyState
        title="Aucune quittance"
        description="Les quittances apparaissent ici une fois générées."
      />
    );
  }

  return (
    <table className="min-w-full divide-y divide-slate-200 text-sm">
      <thead className="text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
        <tr>
          <th scope="col" className="py-2 pr-4">Période</th>
          <th scope="col" className="py-2 pr-4">Générée le</th>
          <th scope="col" className="py-2">
            <span className="sr-only">Actions</span>
          </th>
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {receipts.map((receipt) => (
          <tr key={receipt.id}>
            <td className="py-2.5 pr-4 font-medium capitalize text-slate-900">
              {formatPeriod(receipt.periodYear, receipt.periodMonth)}
            </td>
            <td className="py-2.5 pr-4 text-slate-600">{formatDate(receipt.createdAt)}</td>
            <td className="py-2.5 text-right">
              <Button
                variant="secondary"
                onClick={() => download.mutate(receipt)}
                isLoading={download.isPending && download.variables?.id === receipt.id}
              >
                Télécharger
              </Button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
