import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { acceptInvitation, getInvitation } from './api';
import { acceptInvitationSchema, type AcceptInvitationInput } from './schemas';
import { useAuthStore } from '@/features/auth/auth.store';
import { tokenStorage } from '@/shared/api/token.storage';
import { getApiErrorStatus } from '@/shared/api/errors';
import { Input } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { Card } from '@/shared/components/Card';
import { Spinner } from '@/shared/components/Spinner';
import { ErrorState } from '@/shared/components/EmptyState';
import { formatCurrency, formatDate } from '@/shared/lib/formatters';

/**
 * Public page reached from the invitation link a landlord sends to a tenant
 * who has no account yet. Creating the account signs the tenant in and
 * activates the pending lease automatically.
 */
export function InvitationPage() {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();
  const setSession = useAuthStore((s) => s.setSession);

  const invitation = useQuery({
    queryKey: ['invitations', token],
    queryFn: () => getInvitation(token!),
    enabled: Boolean(token),
    retry: false,
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<AcceptInvitationInput>({
    resolver: zodResolver(acceptInvitationSchema),
    defaultValues: { firstName: '', lastName: '', password: '', confirmPassword: '' },
  });

  const accept = useMutation({
    mutationFn: (values: AcceptInvitationInput) =>
      acceptInvitation(token!, {
        firstName: values.firstName,
        lastName: values.lastName,
        password: values.password,
      }),
    onSuccess: (auth) => {
      tokenStorage.set(auth.refreshToken);
      setSession(auth.user, auth.accessToken);
      navigate('/my-housing', { replace: true });
    },
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 px-4 py-8">
      <div className="w-full max-w-lg space-y-6">
        <h1 className="text-center text-2xl font-bold text-slate-900">Imhotep</h1>

        {invitation.isLoading && <Spinner />}

        {invitation.isError && (
          <Card>
            <div className="space-y-4 text-center">
              <ErrorState message="Cette invitation est invalide, expirée ou déjà utilisée." />
              <Link to="/login" className="text-sm text-brand-600 hover:text-brand-700">
                Aller à la connexion
              </Link>
            </div>
          </Card>
        )}

        {invitation.data && (
          <Card>
            <div className="space-y-6">
              <div className="space-y-1">
                <h2 className="text-lg font-semibold text-slate-900">
                  Invitation de {invitation.data.ownerName}
                </h2>
                <p className="text-sm text-slate-600">
                  Un bail vous attend pour « {invitation.data.propertyLabel} » à{' '}
                  {invitation.data.city} — loyer {formatCurrency(invitation.data.rentAmount)} +{' '}
                  {formatCurrency(invitation.data.chargesAmount)} de charges, à partir du{' '}
                  {formatDate(invitation.data.startDate)}.
                </p>
                <p className="text-sm text-slate-600">
                  Créez votre compte locataire pour <strong>{invitation.data.email}</strong> :
                  le bail vous sera rattaché automatiquement.
                </p>
              </div>

              <form
                noValidate
                onSubmit={handleSubmit((values) => accept.mutate(values))}
                className="space-y-4"
              >
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <Input label="Prénom" error={errors.firstName?.message} {...register('firstName')} />
                  <Input label="Nom" error={errors.lastName?.message} {...register('lastName')} />
                </div>
                <Input
                  label="Mot de passe"
                  type="password"
                  autoComplete="new-password"
                  error={errors.password?.message}
                  {...register('password')}
                />
                <Input
                  label="Confirmer le mot de passe"
                  type="password"
                  autoComplete="new-password"
                  error={errors.confirmPassword?.message}
                  {...register('confirmPassword')}
                />

                {accept.isError && (
                  <ErrorState
                    message={
                      getApiErrorStatus(accept.error) === 404
                        ? 'Cette invitation est invalide, expirée ou déjà utilisée.'
                        : "La création du compte a échoué. Vérifiez les champs et réessayez."
                    }
                  />
                )}

                <Button type="submit" className="w-full" isLoading={accept.isPending}>
                  Créer mon compte et accéder à mon logement
                </Button>
              </form>
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}
