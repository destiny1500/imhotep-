import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { DocumentList } from './DocumentList';
import { UploadDocumentForm } from './UploadDocumentForm';
import { getProperties } from '@/features/properties/api';
import { getMyLease } from '@/features/leases/api';
import { useAuthStore } from '@/features/auth/auth.store';
import { Card } from '@/shared/components/Card';
import { Select } from '@/shared/components/Input';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';

/** Tenant: documents of their lease. Owner/Agency: documents per property. */
export function DocumentsPage() {
  const role = useAuthStore((s) => s.user?.role);
  return (
    <div className="space-y-6">
      <h1 className="text-xl font-bold text-slate-900">Documents</h1>
      {role === 'Tenant' ? <TenantDocuments /> : <ManagerDocuments />}
    </div>
  );
}

function TenantDocuments() {
  const { data: lease, isPending, isError } = useQuery({
    queryKey: ['leases', 'my'],
    queryFn: getMyLease,
    retry: false,
  });

  if (isPending) return <PageSpinner />;
  if (isError) {
    return (
      <EmptyState
        title="Aucun bail actif"
        description="Vos documents apparaîtront ici lorsqu'un bail sera associé à votre compte."
      />
    );
  }

  return (
    <div className="space-y-6">
      <Card title="Déposer un document">
        <UploadDocumentForm leaseId={lease.id} />
      </Card>
      <Card title="Documents de mon bail">
        <DocumentList filter={{ leaseId: lease.id }} />
      </Card>
    </div>
  );
}

function ManagerDocuments() {
  const [propertyId, setPropertyId] = useState('');
  const { data: properties, isPending, isError } = useQuery({
    queryKey: ['properties'],
    queryFn: getProperties,
  });

  if (isPending) return <PageSpinner />;
  if (isError) return <ErrorState message="Impossible de charger les biens." />;
  if (properties.length === 0) {
    return (
      <EmptyState
        title="Aucun bien"
        description="Ajoutez un bien pour gérer ses documents."
      />
    );
  }

  return (
    <div className="space-y-6">
      <Card>
        <div className="max-w-md">
          <Select
            label="Bien concerné"
            value={propertyId}
            onChange={(e) => setPropertyId(e.target.value)}
          >
            <option value="">— Sélectionner un bien —</option>
            {properties.map((p) => (
              <option key={p.id} value={p.id}>
                {p.label}
              </option>
            ))}
          </Select>
        </div>
      </Card>

      {propertyId && (
        <>
          <Card title="Déposer un document">
            <UploadDocumentForm propertyId={propertyId} />
          </Card>
          <Card title="Documents du bien">
            <DocumentList filter={{ propertyId }} />
          </Card>
        </>
      )}
    </div>
  );
}
