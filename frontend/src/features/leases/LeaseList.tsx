import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getLeases } from './api';
import { Badge } from '@/shared/components/Badge';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { Spinner } from '@/shared/components/Spinner';
import { formatCurrency, formatDate } from '@/shared/lib/formatters';

export function LeaseList({ propertyId }: { propertyId: string }) {
  const { data: leases, isPending, isError } = useQuery({
    queryKey: ['leases', propertyId],
    queryFn: () => getLeases(propertyId),
  });

  if (isPending) return <Spinner label="Chargement des baux…" />;
  if (isError) return <ErrorState message="Impossible de charger les baux." />;
  if (leases.length === 0) {
    return (
      <EmptyState
        title="Aucun bail"
        description="Créez un bail pour mettre ce bien en location."
      />
    );
  }

  return (
    <table className="min-w-full divide-y divide-slate-200 text-sm">
      <thead className="text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
        <tr>
          <th scope="col" className="py-2 pr-4">Locataire</th>
          <th scope="col" className="py-2 pr-4">Période</th>
          <th scope="col" className="py-2 pr-4 text-right">Loyer</th>
          <th scope="col" className="py-2 pr-4">Statut</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {leases.map((lease) => {
          const active = !lease.endDate || new Date(lease.endDate) > new Date();
          return (
            <tr key={lease.id}>
              <td className="py-2.5 pr-4 font-medium text-slate-900">
                <Link to={`/messages`} className="hover:text-brand-600">
                  {lease.tenantName ?? lease.tenantEmail}
                </Link>
              </td>
              <td className="py-2.5 pr-4 text-slate-600">
                {formatDate(lease.startDate)}
                {lease.endDate ? ` → ${formatDate(lease.endDate)}` : ' → en cours'}
              </td>
              <td className="py-2.5 pr-4 text-right">
                {formatCurrency(lease.rentAmount + lease.chargesAmount)}
              </td>
              <td className="py-2.5 pr-4">
                <Badge tone={active ? 'green' : 'gray'}>{active ? 'Actif' : 'Terminé'}</Badge>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
