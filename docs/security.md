# Sécurité

## Authentification

| Mesure | Implémentation |
|---|---|
| Hashage des mots de passe | PBKDF2-HMAC-SHA256, **600 000 itérations** (OWASP 2023), sel 128 bits aléatoire, comparaison à temps constant (`FixedTimeEquals`). Format versionné `iter.salt.hash` → itérations augmentables sans migration. |
| Politique de mot de passe | ≥ 12 caractères, majuscule + minuscule + chiffre + symbole (validée serveur **et** client) |
| Access token | JWT HMAC-SHA256, **15 min**, claims minimaux (sub, email, rôle, jti) |
| Refresh token | 256 bits d'entropie, stocké **hashé SHA-256**, durée 7 j, **rotation à chaque usage** |
| Détection de vol | la réutilisation d'un refresh token déjà roté **révoque toute la chaîne** de tokens de l'utilisateur |
| Anti-bruteforce | verrouillage de compte 15 min après 5 échecs **+** rate limiting 10 req/min/IP sur `/api/auth/*` |
| Énumération d'utilisateurs | message d'erreur identique que le compte existe ou non |
| Élévation de privilèges | le rôle `Admin` est rejeté à l'inscription (validateur) |
| Invitations locataire | token 256 bits stocké **hashé SHA-256**, usage unique, expiration 14 j ; endpoints anonymes rate-limités ; token invalide/expiré/utilisé ⇒ 404 générique |

## Autorisation (RBAC + ownership)

Deux niveaux systématiques :
1. **Rôle** — déclaré sur l'endpoint FastEndpoints (`Roles("Owner", "Agency")`) → 403.
2. **Possession** — chaque handler revérifie que la ressource appartient à l'appelant
   (`Property.IsManagedBy`, `Lease.TenantId == userId`, participant de conversation…).
   Échec ⇒ **404** et non 403, pour ne pas révéler l'existence de la ressource (**anti-IDOR**).

Les tests d'intégration (`SecurityTests`, `RentalFlowTests`) couvrent : accès anonyme (401),
mauvais rôle (403), ressource d'autrui (404), token forgé (401), bruteforce (429).

## OWASP Top 10 — couverture

| Risque | Contre-mesure |
|---|---|
| A01 Broken Access Control | RBAC sur endpoint + vérification d'ownership dans chaque handler ; 404 anti-IDOR |
| A02 Cryptographic Failures | PBKDF2 600k ; AES-256-GCM (authentifié) pour les documents ; refresh tokens hashés ; clés hors repo |
| A03 Injection | EF Core paramétré partout (aucun SQL brut) ; FluentValidation sur toute entrée |
| A04 Insecure Design | Clean Architecture, règles métier centralisées, doublons paiement/quittance refusés (409) |
| A05 Security Misconfiguration | En-têtes durcis (nosniff, DENY, CSP, Referrer-Policy, Permissions-Policy) ; erreurs internes jamais exposées (ProblemDetails générique) ; clé JWT < 32 octets ⇒ démarrage refusé |
| A06 Vulnerable Components | versions épinglées, CI ; surface de dépendances réduite |
| A07 Identification & AuthN | cf. tableau authentification ci-dessus |
| A08 Software & Data Integrity | AES-**GCM** = chiffrement authentifié (toute altération du fichier est détectée) ; numéros de quittance uniques |
| A09 Logging & Monitoring | `AuditLog` sur chaque commande sensible (action, user, IP, horodatage) ; exceptions loguées côté serveur |
| A10 SSRF | aucune URL fournie par l'utilisateur n'est récupérée côté serveur |

## XSS / CSRF / uploads

- **XSS** : React échappe par défaut ; `dangerouslySetInnerHTML` proscrit ; le HTML
  des quittances encode toutes les valeurs dynamiques (`WebUtility.HtmlEncode`) ; CSP `default-src 'none'` sur l'API.
- **CSRF** : authentification par header `Authorization: Bearer` (jamais de cookie de session)
  ⇒ pas de soumission cross-site implicite ; CORS restreint aux origines configurées.
- **Uploads** : taille ≤ 20 Mo, content-types whitelistés, nom de fichier nettoyé
  (`Path.GetFileName`), stockage sous nom GUID aléatoire (pas de path traversal),
  chiffrement au repos AES-256-GCM (nonce aléatoire par fichier, tag d'intégrité).

## Données sensibles

- Documents légaux chiffrés au repos ; clé AES 256 bits via configuration/KMS, jamais en BDD.
- Audit log sans payload : pas de mots de passe ni de données personnelles superflues.
- Erreurs 500 : message générique au client, détail uniquement dans les logs serveur.

## Points durcis côté frontend

- Access token **en mémoire uniquement** (store Zustand non persisté).
- Refresh isolé dans un module unique avec refresh « single-flight » (une seule
  requête de refresh simultanée) ; échec ⇒ purge de session + redirection login.
- Tous les formulaires validés par Zod avant envoi (montants positifs, e-mails, etc.).

## Reste à faire avant production

- Cookies `httpOnly` pour le refresh token (à la place du localStorage).
- Clés gérées par un vault (Azure Key Vault / AWS KMS) + rotation périodique.
- HTTPS/HSTS au niveau du reverse proxy ; `X-Forwarded-For` de confiance pour le rate limiting.
- Verrouillage distribué du rate limiting (Redis) en multi-instance.
- Scan de dépendances automatisé (Dependabot/Renovate) et tests de pénétration.
