import { useQuery } from '@tanstack/react-query';
import { getMyLease } from './api';
import { propertyTypeLabels } from '@/features/properties/schemas';
import { Card } from '@/shared/components/Card';
import { Badge } from '@/shared/components/Badge';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatCurrency, formatDate } from '@/shared/lib/formatters';
import { isAxiosError } from 'axios';

export function MyHousingPage() {
  const { data: lease, isPending, isError, error } = useQuery({
    queryKey: ['leases', 'my'],
    queryFn: getMyLease,
    retry: false,
  });

  if (isPending) return <PageSpinner />;

  if (isError) {
    if (isAxiosError(error) && error.response?.status === 404) {
      return (
        <EmptyState
          title="Aucun bail actif"
          description="Aucun logement n'est associé à votre compte pour le moment."
        />
      );
    }
    return <ErrorState message="Impossible de charger votre logement." />;
  }

  const { property } = lease;

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-bold text-slate-900">Mon logement</h1>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card title="Logement">
          <dl className="space-y-3 text-sm">
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Libellé</dt>
              <dd className="font-medium text-slate-900">{property.label}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Adresse</dt>
              <dd className="text-right font-medium text-slate-900">
                {property.addressLine1}
                {property.addressLine2 ? `, ${property.addressLine2}` : ''}
                <br />
                {property.postalCode} {property.city}, {property.country}
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Type</dt>
              <dd>
                <Badge tone="blue">{propertyTypeLabels[property.type]}</Badge>
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Surface</dt>
              <dd className="font-medium text-slate-900">{property.surfaceM2} m²</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Pièces</dt>
              <dd className="font-medium text-slate-900">{property.rooms}</dd>
            </div>
          </dl>
        </Card>

        <Card title="Bail">
          <dl className="space-y-3 text-sm">
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Début du bail</dt>
              <dd className="font-medium text-slate-900">{formatDate(lease.startDate)}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Fin du bail</dt>
              <dd className="font-medium text-slate-900">
                {lease.endDate ? formatDate(lease.endDate) : 'En cours'}
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Loyer hors charges</dt>
              <dd className="font-medium text-slate-900">{formatCurrency(lease.rentAmount)}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Charges</dt>
              <dd className="font-medium text-slate-900">{formatCurrency(lease.chargesAmount)}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Total mensuel</dt>
              <dd className="font-semibold text-slate-900">
                {formatCurrency(lease.rentAmount + lease.chargesAmount)}
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-slate-500">Dépôt de garantie</dt>
              <dd className="font-medium text-slate-900">{formatCurrency(lease.depositAmount)}</dd>
            </div>
          </dl>
        </Card>
      </div>
    </div>
  );
}
