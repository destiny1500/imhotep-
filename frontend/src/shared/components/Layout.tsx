import { NavLink, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/features/auth/auth.store';
import { useLogout } from '@/features/auth/useAuth';
import { NotificationsBell } from '@/features/notifications/NotificationsBell';
import type { Role } from '@/shared/types/api';

interface NavItem {
  to: string;
  label: string;
}

const NAV_BY_ROLE: Record<Role, NavItem[]> = {
  Tenant: [
    { to: '/my-housing', label: 'Mon logement' },
    { to: '/payments', label: 'Paiements' },
    { to: '/receipts', label: 'Quittances' },
    { to: '/documents', label: 'Documents' },
    { to: '/messages', label: 'Messages' },
  ],
  Owner: [
    { to: '/properties', label: 'Mes biens' },
    { to: '/payments', label: 'Paiements' },
    { to: '/documents', label: 'Documents' },
    { to: '/messages', label: 'Messages' },
    { to: '/dashboard', label: 'Tableau de bord' },
  ],
  Agency: [
    { to: '/properties', label: 'Portefeuille' },
    { to: '/tenants', label: 'Locataires' },
    { to: '/receipts', label: 'Quittances' },
    { to: '/dashboard', label: 'Tableau de bord' },
    { to: '/messages', label: 'Messages' },
  ],
  Admin: [
    { to: '/properties', label: 'Biens' },
    { to: '/messages', label: 'Messages' },
  ],
};

const ROLE_LABELS: Record<Role, string> = {
  Tenant: 'Locataire',
  Owner: 'Propriétaire',
  Agency: 'Agence',
  Admin: 'Administrateur',
};

export function Layout() {
  const user = useAuthStore((s) => s.user);
  const logout = useLogout();

  if (!user) return null; // guarded by ProtectedRoute

  const nav = NAV_BY_ROLE[user.role];

  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-slate-200 bg-white sm:flex">
        <div className="flex h-16 items-center border-b border-slate-200 px-5">
          <span className="text-lg font-bold text-brand-700">Imhotep</span>
        </div>
        <nav aria-label="Navigation principale" className="flex-1 space-y-1 px-3 py-4">
          {nav.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `block rounded-md px-3 py-2 text-sm font-medium ${
                  isActive
                    ? 'bg-brand-50 text-brand-700'
                    : 'text-slate-600 hover:bg-slate-50 hover:text-slate-900'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="border-t border-slate-200 px-5 py-4">
          <p className="truncate text-sm font-medium text-slate-900">
            {user.firstName} {user.lastName}
          </p>
          <p className="text-xs text-slate-500">{ROLE_LABELS[user.role]}</p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-16 items-center justify-end gap-3 border-b border-slate-200 bg-white px-4 sm:px-6">
          <NotificationsBell />
          <button
            type="button"
            onClick={() => void logout()}
            className="rounded-md px-3 py-1.5 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          >
            Se déconnecter
          </button>
        </header>
        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
