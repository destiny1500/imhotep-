# Imhotep — Gestion immobilière

Application web de gestion locative reliant **propriétaires**, **agences** et **locataires** :
quittances de loyer, documents légaux chiffrés, suivi des paiements, messagerie sécurisée,
notifications et tableaux de bord.

## Monorepo

```
backend/    API .NET 8 — Clean Architecture, CQRS (MediatR), FastEndpoints, EF Core + PostgreSQL
frontend/   SPA React 18 — TypeScript, Vite, TailwindCSS, React Query, Zustand
docs/       Plans techniques, schémas d'architecture, modèle de données, sécurité, API
```

## Démarrage rapide

### Prérequis
- .NET SDK 8.0, Node.js ≥ 20, Docker (pour PostgreSQL)

### Base de données + API
```bash
docker compose up -d postgres
cd backend
dotnet run --project src/Imhotep.Api          # http://localhost:5000, Swagger sur /swagger
```
En développement le schéma est créé automatiquement. En production :
```bash
dotnet ef migrations add Initial -p src/Imhotep.Infrastructure -s src/Imhotep.Api
dotnet ef database update -p src/Imhotep.Infrastructure -s src/Imhotep.Api
```

### Frontend
```bash
cd frontend
cp .env.example .env
npm install && npm run dev                     # http://localhost:5173
```

### Tests
```bash
cd backend && dotnet test                      # unitaires + intégration (SQLite in-memory)
cd frontend && npm run test                    # Vitest (+ axe-core a11y)
cd frontend && npm run e2e                     # Playwright (navigateurs requis)
```

## Configuration sensible

| Clé | Description |
|---|---|
| `Jwt:SigningKey` | Clé HMAC-SHA256 ≥ 256 bits — **jamais en clair dans le repo** (user-secrets / variables d'env / vault) |
| `DocumentStorage:EncryptionKeyBase64` | Clé AES-256 (base64) pour le chiffrement des documents au repos |
| `ConnectionStrings:Default` | Chaîne PostgreSQL |

Les valeurs présentes dans `appsettings.Development.json` et `docker-compose.yml`
sont des valeurs de **développement uniquement**.

## Documentation

- [Architecture technique](docs/architecture.md) — couches, CQRS, flux, schémas
- [Modèle de données](docs/data-model.md) — entités, relations, contraintes
- [Sécurité](docs/security.md) — OWASP Top 10, RBAC, tokens, chiffrement, audit
- [API](docs/api.md) — endpoints, rôles requis, contrats (OpenAPI complet via `/swagger`)
