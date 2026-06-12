import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { createProperty, getProperty, updateProperty } from './api';
import { propertySchema, propertyTypeLabels, propertyTypeValues, type PropertyFormValues } from './schemas';
import { Input, Select } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { Card } from '@/shared/components/Card';
import { ErrorState } from '@/shared/components/EmptyState';
import { PageSpinner } from '@/shared/components/Spinner';

const DEFAULT_VALUES: PropertyFormValues = {
  label: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  postalCode: '',
  country: 'France',
  type: 'Apartment',
  surfaceM2: 0,
  rooms: 1,
  rentAmount: 0,
  chargesAmount: 0,
};

export function PropertyFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdit = Boolean(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const existing = useQuery({
    queryKey: ['properties', id],
    queryFn: () => getProperty(id!),
    enabled: isEdit,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PropertyFormValues>({
    resolver: zodResolver(propertySchema),
    defaultValues: DEFAULT_VALUES,
  });

  useEffect(() => {
    if (existing.data) {
      reset({ ...existing.data, addressLine2: existing.data.addressLine2 ?? '' });
    }
  }, [existing.data, reset]);

  const save = useMutation({
    mutationFn: (values: PropertyFormValues) =>
      isEdit ? updateProperty(id!, values) : createProperty(values),
    onSuccess: (property) => {
      void queryClient.invalidateQueries({ queryKey: ['properties'] });
      navigate(`/properties/${property.id}`, { replace: true });
    },
  });

  if (isEdit && existing.isPending) return <PageSpinner />;
  if (isEdit && existing.isError) return <ErrorState message="Bien introuvable." />;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <Link to="/properties" className="text-sm text-brand-600 hover:text-brand-700">
          ← Retour aux biens
        </Link>
        <h1 className="mt-2 text-xl font-bold text-slate-900">
          {isEdit ? 'Modifier le bien' : 'Ajouter un bien'}
        </h1>
      </div>

      <Card>
        <form noValidate onSubmit={handleSubmit((values) => save.mutate(values))} className="space-y-4">
          <Input label="Libellé" placeholder="ex. T2 rue de la Paix" error={errors.label?.message} {...register('label')} />
          <Input label="Adresse" error={errors.addressLine1?.message} {...register('addressLine1')} />
          <Input label="Complément d'adresse (optionnel)" error={errors.addressLine2?.message} {...register('addressLine2')} />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Input label="Code postal" error={errors.postalCode?.message} {...register('postalCode')} />
            <Input label="Ville" error={errors.city?.message} {...register('city')} />
            <Input label="Pays" error={errors.country?.message} {...register('country')} />
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Select label="Type de bien" error={errors.type?.message} {...register('type')}>
              {propertyTypeValues.map((value) => (
                <option key={value} value={value}>
                  {propertyTypeLabels[value]}
                </option>
              ))}
            </Select>
            <Input label="Surface (m²)" type="number" step="0.1" min="0" error={errors.surfaceM2?.message} {...register('surfaceM2')} />
            <Input label="Nombre de pièces" type="number" min="1" error={errors.rooms?.message} {...register('rooms')} />
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input label="Loyer mensuel (€)" type="number" step="0.01" min="0" error={errors.rentAmount?.message} {...register('rentAmount')} />
            <Input label="Charges mensuelles (€)" type="number" step="0.01" min="0" error={errors.chargesAmount?.message} {...register('chargesAmount')} />
          </div>

          {save.isError && <ErrorState message="L'enregistrement a échoué." />}

          <div className="flex justify-end gap-2">
            <Link to="/properties">
              <Button variant="secondary">Annuler</Button>
            </Link>
            <Button type="submit" isLoading={save.isPending}>
              {isEdit ? 'Enregistrer' : 'Créer le bien'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
