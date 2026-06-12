import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from '@/shared/components/Layout';
import { ProtectedRoute } from '@/shared/components/ProtectedRoute';
import { useAuthStore } from '@/features/auth/auth.store';
import { homePathForRole } from '@/features/auth/useAuth';
import { LoginPage } from '@/features/auth/LoginPage';
import { RegisterPage } from '@/features/auth/RegisterPage';
import { InvitationPage } from '@/features/invitations/InvitationPage';
import { DashboardPage } from '@/features/dashboard/DashboardPage';
import { PropertiesPage } from '@/features/properties/PropertiesPage';
import { PropertyDetailPage } from '@/features/properties/PropertyDetailPage';
import { PropertyFormPage } from '@/features/properties/PropertyFormPage';
import { LeaseCreatePage } from '@/features/leases/LeaseCreatePage';
import { MyHousingPage } from '@/features/leases/MyHousingPage';
import { TenantsPage } from '@/features/leases/TenantsPage';
import { PaymentsPage } from '@/features/payments/PaymentsPage';
import { ReceiptsPage } from '@/features/receipts/ReceiptsPage';
import { DocumentsPage } from '@/features/documents/DocumentsPage';
import { MessagesPage } from '@/features/messages/MessagesPage';
import { ConversationPage } from '@/features/messages/ConversationPage';

/** Redirects "/" to the role-specific home page. */
function RoleHomeRedirect() {
  const user = useAuthStore((s) => s.user);
  if (!user) return <Navigate to="/login" replace />;
  return <Navigate to={homePathForRole(user.role)} replace />;
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/invitation/:token" element={<InvitationPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<RoleHomeRedirect />} />

          <Route element={<ProtectedRoute allowedRoles={['Owner', 'Agency', 'Admin']} />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/properties" element={<PropertiesPage />} />
            <Route path="/properties/new" element={<PropertyFormPage />} />
            <Route path="/properties/:id" element={<PropertyDetailPage />} />
            <Route path="/properties/:id/edit" element={<PropertyFormPage />} />
            <Route path="/properties/:id/leases/new" element={<LeaseCreatePage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Agency', 'Admin']} />}>
            <Route path="/tenants" element={<TenantsPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Tenant']} />}>
            <Route path="/my-housing" element={<MyHousingPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Tenant', 'Agency', 'Admin']} />}>
            <Route path="/receipts" element={<ReceiptsPage />} />
          </Route>

          <Route path="/payments" element={<PaymentsPage />} />
          <Route path="/documents" element={<DocumentsPage />} />
          <Route path="/messages" element={<MessagesPage />} />
          <Route path="/messages/:id" element={<ConversationPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
