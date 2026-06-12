import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { loginSchema, type LoginInput } from './schemas';
import { useLogin, homePathForRole } from './useAuth';
import { useAuthStore } from './auth.store';
import { Input } from '@/shared/components/Input';
import { Button } from '@/shared/components/Button';
import { ErrorState } from '@/shared/components/EmptyState';

export function LoginPage() {
  const user = useAuthStore((s) => s.user);
  const location = useLocation() as { state?: { registered?: boolean } };
  const login = useLogin();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  if (user) {
    return <Navigate to={homePathForRole(user.role)} replace />;
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-md">
        <h1 className="text-center text-2xl font-bold text-slate-900">Imhotep</h1>
        <p className="mt-1 text-center text-sm text-slate-500">
          Connectez-vous à votre espace de gestion locative
        </p>

        <div className="mt-6 rounded-lg bg-white p-6 shadow-sm ring-1 ring-slate-200">
          {location.state?.registered && (
            <p className="mb-4 rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">
              Compte créé avec succès. Vous pouvez vous connecter.
            </p>
          )}

          <form noValidate onSubmit={handleSubmit((values) => login.mutate(values))} className="space-y-4">
            <Input
              label="Adresse email"
              type="email"
              autoComplete="email"
              error={errors.email?.message}
              {...register('email')}
            />
            <Input
              label="Mot de passe"
              type="password"
              autoComplete="current-password"
              error={errors.password?.message}
              {...register('password')}
            />

            {login.isError && <ErrorState message="Identifiants invalides ou serveur indisponible." />}

            <Button type="submit" isLoading={login.isPending} className="w-full">
              Se connecter
            </Button>
          </form>
        </div>

        <p className="mt-4 text-center text-sm text-slate-500">
          Pas encore de compte ?{' '}
          <Link to="/register" className="font-semibold text-brand-600 hover:text-brand-700">
            Créer un compte
          </Link>
        </p>
      </div>
    </main>
  );
}
