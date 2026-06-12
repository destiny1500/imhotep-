import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getLeases } from './api';
import { getProperties } from '@/features/properties/api';
import { Badge } from '@/shared/components/Badge';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatDate } from '@/shared/lib/formatters';
import type { Lease, Property } from '@/shared/types/api';

interface TenantRow {
  lease: Lease;
  property: Property;
}

async function getAllTenants(): Promise<TenantRow[]> {
  const properties = await getProperties();
  const leasesPerProperty = await Promise.all(
    properties.map(async (property) => {
      const leases = await getLeases(property.id);
      return leases.map((lease) => ({ lease, property }));
    }),
  );
  return leasesPerProperty.flat();
}

/** Agency view: every tenant across the managed portfolio. */
export function TenantsPage() {
  const { data: rows, isPending, isError } = useQuery({
    queryKey: ['tenants'],
    queryFn: getAllTenants,
  });

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-bold text-slate-900">Locataires</h1>

      {isPending && <PageSpinner />}
      {isError && <ErrorState message="Impossible de charger les locataires." />}

      {rows && rows.length === 0 && (
        <EmptyState
          title="Aucun locataire"
          description="Les locataires apparaissent ici dès qu'un bail est créé sur un bien du portefeuille."
        />
      )}

      {rows && rows.length > 0 && (
        <div className="overflow-hidden rounded-lg bg-white shadow-sm ring-1 ring-slate-200">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
              <tr>
                <th scope="col" className="px-4 py-3">Locataire</th>
                <th scope="col" className="px-4 py-3">Bien</th>
                <th scope="col" className="px-4 py-3">Début du bail</th>
                <th scope="col" className="px-4 py-3">Statut</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {rows.map(({ lease, property }) => {
                const active = !lease.endDate || new Date(lease.endDate) > new Date();
                return (
                  <tr key={lease.id} className="hover:bg-slate-50">
                    <td className="px-4 py-3 font-medium text-slate-900">
                      {lease.tenantName ?? lease.tenantEmail}
                      <span className="block text-xs font-normal text-slate-500">
                        {lease.tenantEmail}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <Link
                        to={`/properties/${property.id}`}
                        className="text-brand-600 hover:text-brand-700"
                      >
                        {property.label}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-slate-600">{formatDate(lease.startDate)}</td>
                    <td className="px-4 py-3">
                      <Badge tone={active ? 'green' : 'gray'}>{active ? 'Actif' : 'Terminé'}</Badge>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
