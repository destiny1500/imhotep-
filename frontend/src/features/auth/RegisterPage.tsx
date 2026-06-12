import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link } from 'react-router-dom';
import { registerSchema, type RegisterInput } from './schemas';
import { useRegister } from './useAuth';
import { Input, Select } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';

export function RegisterPage() {
  const registerMutation = useRegister();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
      role: 'Tenant',
    },
  });

  const onSubmit = ({ email, password, firstName, lastName, role }: RegisterInput) =>
    registerMutation.mutate({ email, password, firstName, lastName, role });

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-md">
        <h1 className="text-center text-2xl font-bold text-slate-900">Créer un compte</h1>
        <p className="mt-1 text-center text-sm text-slate-500">
          Rejoignez Imhotep en tant que locataire, propriétaire ou agence
        </p>

        <div className="mt-6 rounded-lg bg-white p-6 shadow-sm ring-1 ring-slate-200">
          <form noValidate onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Input
                label="Prénom"
                autoComplete="given-name"
                error={errors.firstName?.message}
                {...register('firstName')}
              />
              <Input
                label="Nom"
                autoComplete="family-name"
                error={errors.lastName?.message}
                {...register('lastName')}
              />
            </div>
            <Input
              label="Adresse email"
              type="email"
              autoComplete="email"
              error={errors.email?.message}
              {...register('email')}
            />
            <Select label="Rôle" error={errors.role?.message} {...register('role')}>
              <option value="Tenant">Locataire</option>
              <option value="Owner">Propriétaire</option>
              <option value="Agency">Agence</option>
            </Select>
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

            {registerMutation.isError && (
              <ErrorState message="Impossible de créer le compte. Vérifiez les informations saisies." />
            )}

            <Button type="submit" isLoading={registerMutation.isPending} className="w-full">
              Créer mon compte
            </Button>
          </form>
        </div>

        <p className="mt-4 text-center text-sm text-slate-500">
          Déjà un compte ?{' '}
          <Link to="/login" className="font-semibold text-brand-600 hover:text-brand-700">
            Se connecter
          </Link>
        </p>
      </div>
    </main>
  );
}
