import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getLeases } from './api';
import { getProperties } from '@/features/properties/api';
import { Select } from '@/shared/components/Input';
import { ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatDate } from '@/shared/lib/formatters';
import type { Lease } from '@/shared/types/api';

/** Property → lease cascading selector used by owner/agency payment and receipt pages. */
export function LeaseSelector({
  onLeaseSelected,
}: {
  onLeaseSelected: (lease: Lease | null) => void;
}) {
  const [propertyId, setPropertyId] = useState('');
  const [leaseId, setLeaseId] = useState('');

  const properties = useQuery({ queryKey: ['properties'], queryFn: getProperties });
  const leases = useQuery({
    queryKey: ['leases', propertyId],
    queryFn: () => getLeases(propertyId),
    enabled: Boolean(propertyId),
  });

  if (properties.isPending) return <PageSpinner />;
  if (properties.isError) return <ErrorState message="Impossible de charger les biens." />;

  const selectProperty = (id: string) => {
    setPropertyId(id);
    setLeaseId('');
    onLeaseSelected(null);
  };

  const selectLease = (id: string) => {
    setLeaseId(id);
    onLeaseSelected(leases.data?.find((l) => l.id === id) ?? null);
  };

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <Select label="Bien" value={propertyId} onChange={(e) => selectProperty(e.target.value)}>
        <option value="">— Sélectionner un bien —</option>
        {properties.data.map((p) => (
          <option key={p.id} value={p.id}>
            {p.label}
          </option>
        ))}
      </Select>

      <Select
        label="Bail"
        value={leaseId}
        onChange={(e) => selectLease(e.target.value)}
        disabled={!propertyId || leases.isPending}
      >
        <option value="">
          {!propertyId
            ? '— Sélectionnez d’abord un bien —'
            : leases.isPending
              ? 'Chargement…'
              : leases.data?.length === 0
                ? 'Aucun bail pour ce bien'
                : '— Sélectionner un bail —'}
        </option>
        {(leases.data ?? []).map((lease) => (
          <option key={lease.id} value={lease.id}>
            {lease.tenantName ?? lease.tenantEmail} (depuis le {formatDate(lease.startDate)})
          </option>
        ))}
      </Select>
    </div>
  );
}
