import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { createLease } from './api';
import { leaseSchema, type LeaseFormValues } from './schemas';
import { getProperty } from '@/features/properties/api';
import { Input } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { Card } from '@/shared/components/Card';
import { ErrorState } from '@/shared/components/EmptyState';

export function LeaseCreatePage() {
  const { id: propertyId } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: property } = useQuery({
    queryKey: ['properties', propertyId],
    queryFn: () => getProperty(propertyId!),
    enabled: Boolean(propertyId),
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LeaseFormValues>({
    resolver: zodResolver(leaseSchema),
    defaultValues: {
      tenantEmail: '',
      startDate: '',
      endDate: '',
      rentAmount: property?.rentAmount ?? 0,
      chargesAmount: property?.chargesAmount ?? 0,
      depositAmount: 0,
    },
  });

  const create = useMutation({
    mutationFn: (values: LeaseFormValues) =>
      createLease({
        propertyId: propertyId!,
        tenantEmail: values.tenantEmail,
        startDate: values.startDate,
        endDate: values.endDate || undefined,
        rentAmount: values.rentAmount,
        chargesAmount: values.chargesAmount,
        depositAmount: values.depositAmount,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leases', propertyId] });
      navigate(`/properties/${propertyId}`, { replace: true });
    },
  });

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <Link to={`/properties/${propertyId}`} className="text-sm text-brand-600 hover:text-brand-700">
          ← Retour au bien
        </Link>
        <h1 className="mt-2 text-xl font-bold text-slate-900">
          Créer un bail{property ? ` — ${property.label}` : ''}
        </h1>
      </div>

      <Card>
        <form noValidate onSubmit={handleSubmit((values) => create.mutate(values))} className="space-y-4">
          <Input
            label="Email du locataire"
            type="email"
            error={errors.tenantEmail?.message}
            {...register('tenantEmail')}
          />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input label="Date de début" type="date" error={errors.startDate?.message} {...register('startDate')} />
            <Input label="Date de fin (optionnelle)" type="date" error={errors.endDate?.message} {...register('endDate')} />
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Input label="Loyer (€)" type="number" step="0.01" min="0" error={errors.rentAmount?.message} {...register('rentAmount')} />
            <Input label="Charges (€)" type="number" step="0.01" min="0" error={errors.chargesAmount?.message} {...register('chargesAmount')} />
            <Input label="Dépôt de garantie (€)" type="number" step="0.01" min="0" error={errors.depositAmount?.message} {...register('depositAmount')} />
          </div>

          {create.isError && <ErrorState message="La création du bail a échoué." />}

          <div className="flex justify-end gap-2">
            <Link to={`/properties/${propertyId}`}>
              <Button variant="secondary">Annuler</Button>
            </Link>
            <Button type="submit" isLoading={create.isPending}>
              Créer le bail
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
