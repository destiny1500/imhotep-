import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getProperty } from './api';
import { propertyTypeLabels } from './schemas';
import { DelegateDialog } from './DelegateDialog';
import { LeaseList } from '@/features/leases/LeaseList';
import { DocumentList } from '@/features/documents/DocumentList';
import { useAuthStore } from '@/features/auth/auth.store';
import { Card } from '@/shared/components/Card';
import { Badge } from '@/shared/components/Badge';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatCurrency } from '@/shared/lib/formatters';

export function PropertyDetailPage() {
  const { id } = useParams<{ id: string }>();
  const role = useAuthStore((s) => s.user?.role);
  const [delegateOpen, setDelegateOpen] = useState(false);

  const { data: property, isPending, isError } = useQuery({
    queryKey: ['properties', id],
    queryFn: () => getProperty(id!),
    enabled: Boolean(id),
  });

  if (isPending) return <PageSpinner />;
  if (isError || !property) return <ErrorState message="Bien introuvable." />;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link to="/properties" className="text-sm text-brand-600 hover:text-brand-700">
            ← Retour aux biens
          </Link>
          <h1 className="mt-1 text-xl font-bold text-slate-900">{property.label}</h1>
          <p className="text-sm text-slate-500">
            {property.addressLine1}
            {property.addressLine2 ? `, ${property.addressLine2}` : ''} — {property.postalCode}{' '}
            {property.city}, {property.country}
          </p>
        </div>
        <div className="flex gap-2">
          {role === 'Owner' && (
            <Button variant="secondary" onClick={() => setDelegateOpen(true)}>
              Déléguer à une agence
            </Button>
          )}
          <Link to={`/properties/${property.id}/edit`}>
            <Button variant="secondary">Modifier</Button>
          </Link>
          <Link to={`/properties/${property.id}/leases/new`}>
            <Button>Créer un bail</Button>
          </Link>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Card>
          <p className="text-sm text-slate-500">Type</p>
          <p className="mt-1">
            <Badge tone="blue">{propertyTypeLabels[property.type]}</Badge>
          </p>
        </Card>
        <Card>
          <p className="text-sm text-slate-500">Surface</p>
          <p className="mt-1 font-semibold text-slate-900">
            {property.surfaceM2} m² · {property.rooms} pièce{property.rooms > 1 ? 's' : ''}
          </p>
        </Card>
        <Card>
          <p className="text-sm text-slate-500">Loyer</p>
          <p className="mt-1 font-semibold text-slate-900">{formatCurrency(property.rentAmount)}</p>
        </Card>
        <Card>
          <p className="text-sm text-slate-500">Charges</p>
          <p className="mt-1 font-semibold text-slate-900">
            {formatCurrency(property.chargesAmount)}
          </p>
        </Card>
      </div>

      <Card title="Baux">
        <LeaseList propertyId={property.id} />
      </Card>

      <Card title="Documents">
        <DocumentList filter={{ propertyId: property.id }} />
      </Card>

      <DelegateDialog
        propertyId={property.id}
        open={delegateOpen}
        onClose={() => setDelegateOpen(false)}
      />
    </div>
  );
}
