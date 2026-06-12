import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { useAuthStore } from '@/features/auth/auth.store';
import type { Role } from '@/shared/types/api';

function renderWithGuard(allowedRoles?: Role[]) {
  return render(
    <MemoryRouter initialEntries={['/secret']}>
      <Routes>
        <Route path="/login" element={<div>Page de connexion</div>} />
        <Route path="/" element={<div>Accueil</div>} />
        <Route element={<ProtectedRoute allowedRoles={allowedRoles} />}>
          <Route path="/secret" element={<div>Contenu protégé</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

function authenticateAs(role: Role) {
  useAuthStore.getState().setSession(
    { id: 'u1', email: 'test@example.com', firstName: 'Test', lastName: 'User', role },
    'access-token',
  );
}

describe('ProtectedRoute', () => {
  it('redirects an unauthenticated user to /login', () => {
    renderWithGuard();
    expect(screen.getByText('Page de connexion')).toBeInTheDocument();
    expect(screen.queryByText('Contenu protégé')).not.toBeInTheDocument();
  });

  it('renders the protected content for an authenticated user', () => {
    authenticateAs('Owner');
    renderWithGuard();
    expect(screen.getByText('Contenu protégé')).toBeInTheDocument();
  });

  it('renders the content when the user role is allowed', () => {
    authenticateAs('Agency');
    renderWithGuard(['Agency', 'Owner']);
    expect(screen.getByText('Contenu protégé')).toBeInTheDocument();
  });

  it('redirects to the home page when the user role is not allowed', () => {
    authenticateAs('Tenant');
    renderWithGuard(['Owner', 'Agency']);
    expect(screen.getByText('Accueil')).toBeInTheDocument();
    expect(screen.queryByText('Contenu protégé')).not.toBeInTheDocument();
  });
});
