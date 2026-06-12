import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getProperties } from './api';
import { propertyTypeLabels } from './schemas';
import { useAuthStore } from '@/features/auth/auth.store';
import { Button } from '@/shared/components/Button';
import { Badge } from '@/shared/components/Badge';
import { EmptyState, ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';
import { formatCurrency } from '@/shared/lib/formatters';

export function PropertiesPage() {
  const role = useAuthStore((s) => s.user?.role);
  const { data: properties, isPending, isError } = useQuery({
    queryKey: ['properties'],
    queryFn: getProperties,
  });

  const title = role === 'Agency' ? 'Portefeuille' : 'Mes biens';

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold text-slate-900">{title}</h1>
        <Link to="/properties/new">
          <Button>Ajouter un bien</Button>
        </Link>
      </div>

      {isPending && <PageSpinner />}
      {isError && <ErrorState message="Impossible de charger les biens." />}

      {properties && properties.length === 0 && (
        <EmptyState
          title="Aucun bien pour le moment"
          description="Ajoutez votre premier bien pour commencer la gestion locative."
          action={
            <Link to="/properties/new">
              <Button>Ajouter un bien</Button>
            </Link>
          }
        />
      )}

      {properties && properties.length > 0 && (
        <div className="overflow-hidden rounded-lg bg-white shadow-sm ring-1 ring-slate-200">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
              <tr>
                <th scope="col" className="px-4 py-3">Bien</th>
                <th scope="col" className="px-4 py-3">Adresse</th>
                <th scope="col" className="px-4 py-3">Type</th>
                <th scope="col" className="px-4 py-3 text-right">Loyer</th>
                <th scope="col" className="px-4 py-3 text-right">Charges</th>
                <th scope="col" className="px-4 py-3">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {properties.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50">
                  <td className="px-4 py-3 font-medium text-slate-900">{p.label}</td>
                  <td className="px-4 py-3 text-slate-600">
                    {p.addressLine1}, {p.postalCode} {p.city}
                  </td>
                  <td className="px-4 py-3">
                    <Badge tone="blue">{propertyTypeLabels[p.type]}</Badge>
                  </td>
                  <td className="px-4 py-3 text-right">{formatCurrency(p.rentAmount)}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(p.chargesAmount)}</td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      to={`/properties/${p.id}`}
                      className="font-medium text-brand-600 hover:text-brand-700"
                    >
                      Détails
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
