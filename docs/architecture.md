# Architecture technique

## Vue d'ensemble

```
┌────────────────────────┐        HTTPS / JSON         ┌─────────────────────────────┐
│  SPA React 18 (Vite)   │ ──────────────────────────▶ │  API .NET 10 (FastEndpoints)│
│  React Query / Zustand │ ◀── JWT access (15 min) ─── │  + JWT Bearer + RateLimiter │
└────────────────────────┘     refresh rotatif (7 j)   └──────────────┬──────────────┘
                                                                      │ MediatR (CQRS)
                                                       ┌──────────────▼──────────────┐
                                                       │   Application (use cases)   │
                                                       │ Validation • Audit • Mapping│
                                                       └──────────────┬──────────────┘
                                                       ┌──────────────▼──────────────┐
                                                       │ Infrastructure              │
                                                       │ EF Core → PostgreSQL        │
                                                       │ AES-256-GCM → documents     │
                                                       └─────────────────────────────┘
```

## Clean Architecture — dépendances

```
Imhotep.Api ──▶ Imhotep.Application ──▶ Imhotep.Domain
     │                   ▲
     └──▶ Imhotep.Infrastructure ──────────┘
```

| Couche | Contenu | Règle |
|---|---|---|
| **Domain** | Entités, enums, logique métier pure (`Property.IsManagedBy`, lockout de `User`) | Zéro dépendance externe |
| **Application** | Use cases CQRS (commands/queries MediatR), validateurs FluentValidation, DTOs, mapping explicite (`EntityMapping` / `Projections`), interfaces (`IAppDbContext`, `IJwtTokenService`, …) | Ne connaît ni EF concret, ni HTTP |
| **Infrastructure** | `AppDbContext` (Npgsql), PBKDF2, JWT, stockage chiffré AES-GCM | Implémente les interfaces d'Application |
| **Api** | Endpoints FastEndpoints (mapping HTTP pur), Program.cs, middleware d'exceptions, `CurrentUserService` | Aucune logique métier |

## CQRS + pipeline MediatR

Chaque use case est un fichier autonome : `record` (command/query) + validateur + handler.

Pipeline d'exécution d'une requête :

```
Endpoint ──▶ ValidationBehavior ──▶ Handler ──▶ AuditBehavior ──▶ Réponse
              (FluentValidation,      (règles      (trace en BDD si
               échec ⇒ 400)           métier +      IAuditableCommand)
                                      anti-IDOR)
```

- **Commands** (écritures) implémentent `IAuditableCommand` → journalisées dans `AuditLogs`
  (action, utilisateur, IP, horodatage — jamais le payload, pour ne pas persister de secrets).
- **Queries** (lectures) utilisent `AsNoTracking()` + projections explicites traduites en SQL
  (`Select(Projections.ToXxxDto)`) — AutoMapper a été écarté volontairement
  (vulnérabilité GHSA-rvv3-g6hj-g44x corrigée uniquement dans les versions sous licence commerciale).

## Découpage en modules (features)

```
Application/Features/
  Auth/           Register, Login (lockout), RefreshToken (rotation + détection de vol), Logout, GetMe
  Properties/     Create, Update, Get, List, Delegate (mandat de gestion → agence)
  Leases/         Create (notifie le locataire), List, GetMyLease (espace locataire)
  Payments/       Record (anti-doublon par période), List, GetMyPayments
  Receipts/       Generate (numérotation Q-AAAA-NNNNNN), List, GetMy, Download (HTML)
  Documents/      Upload (chiffré AES-GCM), List, Download — contrôle d'accès par bien/bail
  Messaging/      StartConversation, SendMessage, ListConversations, GetMessages
  Notifications/  List, MarkRead — émises par les autres modules
  Dashboard/      KPI propriétaire et agence (occupation, retards, encaissements)
```

Chaque module est indépendant : il ne référence que `Common/` et `Domain`.

## Flux principal : quittance de loyer

```
Agence/Propriétaire                    API                              Locataire
       │ POST /api/payments ──▶ RecordPaymentCommand
       │                        ├─ vérifie IsManagedBy (anti-IDOR)
       │                        ├─ refuse les doublons de période (409)
       │                        └─ notification "Paiement enregistré" ──▶ 🔔
       │ POST /api/receipts/generate ──▶ GenerateReceiptCommand
       │                        ├─ numérote Q-2026-000042 (unique)
       │                        └─ notification "Quittance disponible" ──▶ 🔔
       │                                              GET /api/receipts/my ◀── consulte
       │                                              GET /api/receipts/{id}/download ◀── télécharge
```

## Frontend — architecture feature-based

```
frontend/src/
  app/          providers (QueryClient, Router), routes protégées par rôle
  shared/       client axios (auth + refresh single-flight), composants UI, types API
  features/
    auth/         store Zustand (token en mémoire), pages login/register, schémas Zod
    properties/   liste, détail, formulaire (RHF + Zod), délégation à une agence
    leases/       baux par bien, page « Mon logement » (locataire)
    payments/     saisie paiement, historiques
    receipts/     quittances : liste, génération, téléchargement
    documents/    upload, liste, téléchargement
    messages/     conversations et fil de messages
    notifications/ cloche + liste
    dashboard/    KPI propriétaire / agence
```

Navigation par rôle : Locataire (Mon logement, Paiements, Quittances, Documents, Messages) ·
Propriétaire (Biens, Paiements, Documents, Messages, Tableau de bord) ·
Agence (Portefeuille, Locataires, Quittances, Tableau de bord, Messages).

## Choix de base de données

**PostgreSQL** : transactions ACID pour les écritures critiques (paiement + quittance +
notification atomiques), `JSONB` pour les métadonnées d'audit, index uniques
(email, hash de refresh token, numéro de quittance), fiabilité et coût.
Les tests d'intégration substituent SQLite in-memory au travers du même `AppDbContext`.

## Décisions et compromis

- **404 plutôt que 403** quand l'appelant n'est pas autorisé sur une ressource précise :
  l'existence de la ressource ne fuit pas (anti-IDOR).
- **Quittance générée en HTML** téléchargeable ; le passage en PDF (QuestPDF) est une
  évolution isolée dans `DownloadReceiptQueryHandler.Render`.
- **Refresh token en localStorage côté front** (isolé dans `token.storage.ts`) ; en production,
  préférer un cookie `httpOnly` + `SameSite=Strict` — le backend n'a pas besoin de changer.
- **`EnsureCreated()` en développement uniquement** ; les migrations EF sont la voie de
  production (commandes dans le README).
