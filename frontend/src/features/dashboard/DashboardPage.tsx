import { useQuery } from '@tanstack/react-query';
import { getAgencyDashboard, getOwnerDashboard } from './api';
import { useAuthStore } from '@/features/auth/auth.store';
import { StatCard } from '@/shared/components/Card';
import { ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatCurrency, formatPercent } from '@/shared/lib/formatters';

export function DashboardPage() {
  const role = useAuthStore((s) => s.user?.role);
  return (
    <div className="space-y-6">
      <h1 className="text-xl font-bold text-slate-900">Tableau de bord</h1>
      {role === 'Agency' ? <AgencyDashboardView /> : <OwnerDashboardView />}
    </div>
  );
}

function OwnerDashboardView() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['dashboard', 'owner'],
    queryFn: getOwnerDashboard,
  });

  if (isPending) return <PageSpinner />;
  if (isError) return <ErrorState message="Impossible de charger le tableau de bord." />;

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard label="Biens" value={String(data.propertiesCount)} />
      <StatCard label="Baux actifs" value={String(data.activeLeases)} />
      <StatCard
        label="Loyers encaissés ce mois"
        value={formatCurrency(data.rentCollectedThisMonth)}
      />
      <StatCard
        label="Paiements en retard"
        value={String(data.latePaymentsCount)}
        hint={data.latePaymentsCount > 0 ? 'À relancer' : 'Tout est à jour'}
      />
    </div>
  );
}

function AgencyDashboardView() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['dashboard', 'agency'],
    queryFn: getAgencyDashboard,
  });

  if (isPending) return <PageSpinner />;
  if (isError) return <ErrorState message="Impossible de charger le tableau de bord." />;

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard label="Biens gérés" value={String(data.managedProperties)} />
      <StatCard label="Propriétaires" value={String(data.ownersCount)} />
      <StatCard label="Locataires" value={String(data.tenantsCount)} />
      <StatCard label="Taux d'occupation" value={formatPercent(data.occupancyRate)} />
      <StatCard
        label="Loyers encaissés ce mois"
        value={formatCurrency(data.rentCollectedThisMonth)}
      />
      <StatCard
        label="Paiements en retard"
        value={String(data.latePaymentsCount)}
        hint={data.latePaymentsCount > 0 ? 'À relancer' : 'Tout est à jour'}
      />
      <StatCard
        label="Documents manquants"
        value={String(data.missingDocumentsCount)}
        hint={data.missingDocumentsCount > 0 ? 'Dossiers incomplets' : 'Dossiers complets'}
      />
    </div>
  );
}
