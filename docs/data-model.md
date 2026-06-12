# Modèle de données

## Schéma relationnel

```
User (Tenant | Owner | Agency | Admin)
 ├──1:N── RefreshToken            (hash SHA-256, rotation, révocation)
 ├──1:N── Property (OwnerId)      ──N:1?── User (ManagingAgencyId, rôle Agency)
 │           ├──1:N── Lease ──N:1── User (TenantId, rôle Tenant)
 │           │          ├──1:N── Payment ──1:1?── RentReceipt
 │           │          └──1:N── Document
 │           └──1:N── Document
 ├──1:N── ManagementContract      (Owner ↔ Agency ↔ Property, % honoraires)
 ├──1:N── Notification
 └──N:M── Conversation (via ConversationParticipant) ──1:N── Message

AuditLog (autonome : UserId?, Action, EntityType/Id, IP, TimestampUtc, MetadataJson)
```

## Entités

### User
| Champ | Type | Contraintes |
|---|---|---|
| Id | uuid | PK |
| Email | varchar(254) | **unique**, normalisé en minuscules |
| PasswordHash | varchar(500) | PBKDF2-SHA256, 600 000 itérations, format `iter.salt.hash` |
| Role | enum | Tenant / Owner / Agency / Admin (Admin non auto-inscriptible) |
| FailedLoginAttempts, LockoutEndUtc | int, timestamptz? | verrouillage 15 min après 5 échecs |

### Property
Bien immobilier. `OwnerId` (FK Restrict), `ManagingAgencyId` nullable (FK SetNull) —
renseigné quand un mandat de gestion est actif. Adresse complète, `Type`, `Status`
(Available/Rented/UnderMaintenance/Archived), `SurfaceM2`, `Rooms`,
`RentAmount`/`ChargesAmount` en `numeric(12,2)`.

### ManagementContract
Mandat de gestion : Property + Owner + Agency, `FeePercent numeric(5,2)`,
dates de début/fin, statut. L'historique des mandats est conservé même après résiliation.

### Lease
Bail : Property + Tenant, dates (`date`), loyer/charges/dépôt `numeric(12,2)`, statut.
Règle métier : **un seul bail actif par bien** (vérifié au handler).

### Payment
`LeaseId`, montant, **période = (PeriodYear, PeriodMonth)** avec index composite —
un seul paiement complété par période (anti-doublon). `RecordedById` trace qui a saisi.

### RentReceipt (quittance)
1:1 avec Payment. `Number` **unique** au format `Q-AAAA-NNNNNN`, période couverte,
montants loyer/charges copiés du bail au moment de l'émission (immutabilité comptable),
`IssuedById`.

### Document
Métadonnées uniquement — le binaire est chiffré AES-256-GCM sur disque,
`StoragePath` = nom aléatoire opaque (GUID). Rattaché à un bien **ou** un bail.
Types : LeaseContract, RentReceipt, Diagnostic, Insurance, Inventory, Other.
Taille max 20 Mo, content-types whitelistés (PDF, PNG, JPEG, DOCX).

### Conversation / ConversationParticipant / Message
Messagerie privée : seuls les participants lisent/écrivent (vérifié à chaque requête).
`ConversationParticipant` a une PK composite (ConversationId, UserId).
Corps de message ≤ 10 000 caractères.

### Notification
Par utilisateur : type (PaymentRecorded, DocumentAdded, RentReminder, ReceiptIssued,
MessageReceived, LeaseCreated), titre, corps, lu/non-lu. Index `(UserId, IsRead)`.

### AuditLog
Trace immuable de toutes les commandes sensibles. PK `bigint` auto, indexée par
horodatage et utilisateur. `MetadataJson` (JSONB sous PostgreSQL) pour contexte
additionnel non sensible. **Jamais de payload brut** (pas de mots de passe ni PII inutile).

## Règles d'intégrité clés

- `User.Email` unique · `RefreshToken.TokenHash` unique · `RentReceipt.Number` unique
- Suppressions : cascade Property→Lease→Payment→Receipt ; Restrict sur les FK
  utilisateur (on n'efface pas un utilisateur référencé par l'historique comptable)
- Tous les montants en `numeric` (jamais de flottants)
- Toutes les dates serveur en UTC (`*Utc`), dates métier en `date` (`DateOnly`)
