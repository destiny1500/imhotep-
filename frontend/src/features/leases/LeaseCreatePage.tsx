import { useState } from 'react';
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
import { getApiErrorStatus } from '@/shared/api/errors';

function leaseCreateErrorMessage(error: unknown): string {
  switch (getApiErrorStatus(error)) {
    case 404:
      return 'Bien introuvable.';
    case 409:
      return "Création impossible : ce bien a déjà un bail actif ou en attente, ou cet e-mail appartient à un compte qui n'est pas un compte locataire.";
    default:
      return 'La création du bail a échoué.';
  }
}

/** Shown when the tenant has no account yet: the lease is pending and the
 * invitation link must reach the tenant (it is also e-mailed by the backend). */
function InvitationLinkPanel({ url, propertyId }: { url: string; propertyId: string }) {
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // clipboard unavailable (http, permissions): the link stays selectable below
    }
  };

  return (
    <Card>
      <div className="space-y-4">
        <h2 className="text-lg font-semibold text-slate-900">Bail créé — en attente du locataire</h2>
        <p className="text-sm text-slate-600">
          Ce locataire n'a pas encore de compte. Transmettez-lui ce lien d'invitation : il créera
          son compte et le bail lui sera rattaché automatiquement. Le lien expire dans 14 jours.
        </p>
        <div className="flex items-center gap-2">
          <input
            readOnly
            value={url}
            onFocus={(e) => e.currentTarget.select()}
            className="w-full rounded-md border border-slate-300 bg-slate-50 px-3 py-2 text-sm text-slate-700"
            aria-label="Lien d'invitation"
          />
          <Button type="button" onClick={() => void copy()}>
            {copied ? 'Copié !' : 'Copier'}
          </Button>
        </div>
        <div className="flex justify-end">
          <Link to={`/properties/${propertyId}`}>
            <Button variant="secondary">Retour au bien</Button>
          </Link>
        </div>
      </div>
    </Card>
  );
}

export function LeaseCreatePage() {
  const { id: propertyId } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [invitationUrl, setInvitationUrl] = useState<string | null>(null);

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
    onSuccess: (result) => {
      void queryClient.invalidateQueries({ queryKey: ['leases', propertyId] });
      if (result.invitationUrl) {
        setInvitationUrl(result.invitationUrl);
      } else {
        navigate(`/properties/${propertyId}`, { replace: true });
      }
    },
  });

  if (invitationUrl && propertyId) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <h1 className="text-xl font-bold text-slate-900">
          Créer un bail{property ? ` — ${property.label}` : ''}
        </h1>
        <InvitationLinkPanel url={invitationUrl} propertyId={propertyId} />
      </div>
    );
  }

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

          {create.isError && <ErrorState message={leaseCreateErrorMessage(create.error)} />}

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
