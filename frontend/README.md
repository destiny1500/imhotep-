# Imhotep — Frontend

Application web de gestion locative (propriétaires, agences, locataires) construite avec
Vite, React 18, TypeScript (strict), TailwindCSS, TanStack Query, Zustand, react-hook-form + zod
et react-router-dom v6.

## Prérequis

- Node.js 22+
- L'API .NET Imhotep accessible (par défaut `http://localhost:5000`)

## Installation

```bash
npm install
cp .env.example .env   # puis ajuster VITE_API_URL si besoin
```

## Scripts

| Script              | Description                                              |
| ------------------- | -------------------------------------------------------- |
| `npm run dev`       | Serveur de développement Vite (http://localhost:5173)     |
| `npm run build`     | Typecheck + build de production dans `dist/`             |
| `npm run preview`   | Sert le build de production localement                   |
| `npm run typecheck` | `tsc --noEmit`                                           |
| `npm run test`      | Tests unitaires et de composants (Vitest + Testing Library) |
| `npm run test:watch`| Vitest en mode watch                                     |
| `npm run e2e`       | Tests end-to-end Playwright (nécessite `npx playwright install` au préalable) |

Les tests e2e (`e2e/`) ne font pas partie de `npm run test` : ils utilisent Playwright,
mockent l'API via `page.route()` et démarrent le serveur Vite automatiquement.

## Architecture

```
src/
  app/            # main.tsx, App.tsx, router.tsx, providers.tsx
  shared/
    api/          # client axios (token en mémoire, refresh single-flight sur 401), token.storage.ts
    components/   # Button, Input, Card, Badge, Spinner, EmptyState, Layout, ProtectedRoute
    lib/          # helpers zod, formatters (EUR, dates fr-FR), download blob
    types/        # types de l'API partagés
  features/
    auth/         # login, register, store zustand, restauration de session
    properties/   # liste, détail, formulaire, délégation à une agence
    leases/       # baux par bien, création, page « Mon logement », locataires (agence)
    payments/     # enregistrement et historique des paiements
    receipts/     # quittances : liste/téléchargement (locataire), génération (agence)
    documents/    # dépôt, liste et téléchargement de documents
    messages/     # conversations et messages
    notifications/# cloche de notifications, marquage lu
    dashboard/    # tableaux de bord propriétaire et agence
```

## Sécurité

- Le jeton d'accès reste **en mémoire** (store zustand, jamais persisté).
- Le refresh token est isolé dans `src/shared/api/token.storage.ts` (localStorage) ;
  en production, un cookie httpOnly est préférable.
- Sur une réponse 401, l'intercepteur axios tente **un seul** refresh partagé
  (single-flight) puis rejoue la requête ; en cas d'échec, la session est purgée
  et l'utilisateur est redirigé vers `/login`.
- Routes protégées par rôle (`ProtectedRoute` + navigation latérale par rôle).
- Tous les formulaires sont validés par des schémas zod.
