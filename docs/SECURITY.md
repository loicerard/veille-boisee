# Sécurité — Veille Boisée

## Rate Limiting / Throttling

| Endpoint | Limite | Fenêtre | Réponse |
|----------|--------|---------|---------|
| Tous endpoints publics (non auth) | 30 req | par minute par IP | `429` + `Retry-After` |
| Endpoints authentifiés | 100 req | par minute par userId | `429` + `Retry-After` |
| `POST /reports` (soumission signalement) | 5 req | par heure par compte | `429` + `Retry-After` |
| `POST /auth/*` (connexion / refresh) | 10 req | par 15 min par IP | `429` + `Retry-After` |

**Implémentation** :
- Compteurs stockés en Redis avec TTL automatique
- Header `X-RateLimit-Remaining` inclus dans chaque réponse
- Header `X-RateLimit-Reset` indiquant le timestamp de reset
- Réponse `429 Too Many Requests` avec corps JSON : `{ "retryAfter": <seconds> }`

---

## Authentification

- **OAuth 2.0 uniquement** — aucun mot de passe stocké en base de données
- Providers : Google, Apple, Facebook
- **JWT access token** : durée de vie **15 minutes**, signé en RS256 (clé asymétrique)
- **Refresh token** : durée de vie **30 jours**, rotation à chaque renouvellement
  (l'ancien token est invalidé immédiatement à l'émission du nouveau)
- Refresh tokens révoqués conservés en blacklist Redis jusqu'à leur expiration naturelle
- Suppression de compte → invalidation immédiate de tous les tokens actifs

---

## Validation des entrées

**Règle absolue : validation côté serveur systématique. La validation côté client est indicative uniquement.**

| Champ | Règle de validation |
|-------|---------------------|
| Coordonnées GPS (lon) | `lon ∈ [-5.5, 9.6]` (France métropolitaine + DOM-TOM) |
| Coordonnées GPS (lat) | `lat ∈ [41.2, 51.2]` |
| Photo upload | Type MIME vérifié serveur (pas uniquement l'extension), max **10 MB**, max 8000×8000 px |
| Type de dépôt | Enum strict issu d'une liste fixe — aucune valeur libre acceptée |
| Commentaire optionnel | Max 500 caractères, sanitisation HTML (strip tags) |

**Les coordonnées, le type de zone et l'entité destinataire sont toujours recalculés
côté serveur** — jamais acceptés tels quels depuis le client.

---

## OWASP Top 10

| Risque | Contre-mesure |
|--------|---------------|
| **A01 Contrôle d'accès** | Vérification d'autorisation dans chaque use case handler, pas uniquement dans le middleware |
| **A02 Défaillances cryptographiques** | HTTPS obligatoire, HSTS, secrets chiffrés au repos, pas de données sensibles en clair |
| **A03 Injection** | ORM avec requêtes paramétrées exclusivement — jamais de concaténation de chaîne SQL |
| **A04 Conception non sécurisée** | Threat modeling avant chaque nouvelle fonctionnalité exposée |
| **A05 Mauvaise configuration** | Pas de debug en prod, headers de sécurité systématiques, pas de stack trace publique |
| **A06 Composants vulnérables** | `npm audit` / `pip audit` bloquant en CI, Dependabot activé |
| **A07 Auth défaillante** | OAuth uniquement, JWT court, refresh rotation, blacklist |
| **A08 Intégrité logicielle** | Vérification des checksums des dépendances (lockfile committé) |
| **A09 Logs insuffisants** | Logs structurés, alertes sur patterns suspects, sans PII |
| **A10 SSRF** | Liste blanche stricte des URLs appelées côté serveur (uniquement IGN, INSEE) |

---

## Headers de sécurité HTTP

```http
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
Content-Security-Policy: default-src 'self'; img-src 'self' data: blob:; connect-src 'self' https://geo.api.gouv.fr https://data.geopf.fr https://apicarto.ign.fr
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(self), camera=(self), microphone=()
```

---

## Uploads de fichiers

- Stockage des photos **hors du répertoire web** (jamais servi directement par le serveur HTTP)
- **Re-encodage systématique** des images (strip intégral des métadonnées EXIF — géolocalisation,
  appareil, auteur) avant stockage
- Nom de fichier **généré aléatoirement** (UUID) — jamais le nom d'origine fourni par l'utilisateur
- Validation du type MIME par inspection du contenu binaire (magic bytes), pas par l'extension

---

## Gestion des secrets

- Zéro secret dans le code source ou dans git
- `.env` listé dans `.gitignore` ; un `.env.example` documenté est committé
- En production : gestionnaire de secrets dédié (HashiCorp Vault, AWS Secrets Manager,
  ou équivalent) — pas de variables d'environnement en clair sur le serveur
- **Rotation des clés JWT** : au minimum une fois par an, immédiatement en cas de compromission

---

## Protection CSRF

- API REST stateless avec JWT dans le header `Authorization` :
  les cookies de session ne sont pas utilisés → risque CSRF très réduit
- Si des cookies sont introduits (ex: refresh token en httpOnly cookie) :
  ajouter le pattern double-submit cookie ou le header `SameSite=Strict`

---

## Logs et surveillance

- Logs structurés JSON : `timestamp`, `level`, `endpoint`, `statusCode`, `userId_hash`
  (hash irréversible de l'userId — jamais l'identifiant brut)
- **Zéro coordonnée GPS brute dans les logs**
- **Zéro email, nom ou identifiant OAuth lisible dans les logs**
- Alertes automatiques sur :
  - Taux d'erreurs 5xx > 1 % sur 5 minutes
  - Pic de réponses 429 (attaque potentielle)
  - Tentatives d'authentification répétées depuis une même IP (> 20/min)
- Conservation des logs : **90 jours maximum**

---

## Divulgation responsable des vulnérabilités

Signaler toute vulnérabilité **en privé** avant toute divulgation publique :
- Issue GitHub privée avec le label `security`
- Ou directement par email à l'auteur (voir profil GitHub)

Un délai de correction raisonnable (90 jours) sera accordé avant divulgation publique coordonnée.
