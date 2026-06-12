import { AppRouter } from './router';
import { useSessionRestore } from '@/features/auth/useAuth';

export function App() {
  // Restore the session from the refresh token on first load.
  useSessionRestore();
  return <AppRouter />;
}
