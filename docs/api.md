# API — Référence des endpoints

Documentation interactive complète : **`/swagger`** (OpenAPI généré par FastEndpoints).
Tous les endpoints (hors `auth`) exigent `Authorization: Bearer <accessToken>`.
Les enums circulent en chaînes (`"Owner"`, `"Apartment"`, `"BankTransfer"`).

## Auth (anonyme, rate-limité 10 req/min/IP)

| Méthode | Route | Corps | Réponse |
|---|---|---|---|
| POST | `/api/auth/register` | email, password, firstName, lastName, role (Tenant\|Owner\|Agency) | 201 `{userId}` |
| POST | `/api/auth/login` | email, password | 200 `{accessToken, refreshToken, user}` · 401 · 429 |
| POST | `/api/auth/refresh` | refreshToken | 200 `{accessToken, refreshToken}` (rotation) |
| POST | `/api/auth/logout` 🔒 | refreshToken | 204 (révocation, idempotent) |
| GET | `/api/users/me` 🔒 | — | 200 `UserDto` |

## Biens (Owner / Agency)

| Méthode | Route | Rôles | Notes |
|---|---|---|---|
| GET | `/api/properties` | Owner, Agency, Admin | Owner : ses biens · Agency : biens en mandat |
| POST | `/api/properties` | Owner | 201 `PropertyDto` |
| GET | `/api/properties/{id}` | tous 🔒 | propriétaire, agence mandatée ou locataire du bien — sinon **404** |
| PUT | `/api/properties/{id}` | Owner, Agency | ownership vérifié |
| POST | `/api/properties/{id}/delegate` | Owner | `{agencyId, feePercent}` → crée le mandat ; 409 si déjà délégué |

## Baux

| Méthode | Route | Rôles | Notes |
|---|---|---|---|
| GET | `/api/leases?propertyId=` | Owner, Agency, Admin | |
| POST | `/api/leases` | Owner, Agency | `{propertyId, tenantEmail, startDate, endDate?, rentAmount, chargesAmount, depositAmount}` · 409 si bail actif existant |
| GET | `/api/leases/my` | Tenant | bail actif + infos logement + bailleur/agence |

## Paiements

| Méthode | Route | Rôles | Notes |
|---|---|---|---|
| GET | `/api/payments?leaseId=` | 🔒 | gestionnaire du bien ou locataire du bail |
| GET | `/api/payments/my` | Tenant | historique complet |
| POST | `/api/payments` | Owner, Agency | 409 si la période est déjà payée ; notifie le locataire |

## Quittances

| Méthode | Route | Rôles | Notes |
|---|---|---|---|
| GET | `/api/receipts/my` | Tenant | |
| GET | `/api/receipts?leaseId=` | 🔒 | gestionnaire ou locataire |
| POST | `/api/receipts/generate` | Owner, Agency | `{paymentId}` → 201, numéro unique `Q-AAAA-NNNNNN` ; 409 si déjà émise ; notifie le locataire |
| GET | `/api/receipts/{id}/download` | 🔒 | document HTML ; accès locataire/gestionnaire uniquement |

## Documents (chiffrés au repos)

| Méthode | Route | Notes |
|---|---|---|
| POST | `/api/documents` | multipart : `file`, `type`, `propertyId?` ou `leaseId?` · 20 Mo max, PDF/PNG/JPEG/DOCX |
| GET | `/api/documents?propertyId=&leaseId=` | filtré selon les droits de l'appelant |
| GET | `/api/documents/{id}/download` | déchiffre et renvoie le binaire |

## Messagerie

| Méthode | Route | Notes |
|---|---|---|
| GET | `/api/conversations` | conversations de l'appelant |
| POST | `/api/conversations` | `{participantUserId, propertyId?, subject, body}` → 201 |
| GET | `/api/conversations/{id}/messages` | participants uniquement (sinon 404) |
| POST | `/api/conversations/{id}/messages` | `{body}` → 201, notifie les autres participants |

## Notifications & tableaux de bord

| Méthode | Route | Rôles |
|---|---|---|
| GET | `/api/notifications` | 🔒 (100 dernières) |
| POST | `/api/notifications/{id}/read` | 🔒 propriétaire de la notification |
| GET | `/api/dashboard/owner` | Owner — `{propertiesCount, activeLeases, rentCollectedThisMonth, latePaymentsCount}` |
| GET | `/api/dashboard/agency` | Agency — `{managedProperties, ownersCount, tenantsCount, occupancyRate, latePaymentsCount, missingDocumentsCount, rentCollectedThisMonth}` |

## Erreurs (RFC 7807)

| Code | Signification |
|---|---|
| 400 | Validation (détail par champ dans `errors`) |
| 401 | Non authentifié / identifiants invalides / token expiré |
| 403 | Rôle insuffisant |
| 404 | Ressource introuvable **ou** non possédée (anti-IDOR) |
| 409 | Conflit métier (doublon e-mail, période déjà payée, quittance déjà émise…) |
| 429 | Rate limit dépassé |
