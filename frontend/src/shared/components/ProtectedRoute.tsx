import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/features/auth/auth.store';
import { tokenStorage } from '@/shared/api/token.storage';
import { PageSpinner } from './Spinner';
import type { Role } from '@/shared/types/api';

/**
 * Guards nested routes. Unauthenticated users are redirected to /login.
 * If `allowedRoles` is given, users with another role are sent to their home.
 */
export function ProtectedRoute({ allowedRoles }: { allowedRoles?: Role[] }) {
  const user = useAuthStore((s) => s.user);
  const initialized = useAuthStore((s) => s.initialized);
  const location = useLocation();

  if (!user) {
    // A session restore may still be in flight (refresh token present).
    if (!initialized && tokenStorage.get()) {
      return <PageSpinner />;
    }
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
