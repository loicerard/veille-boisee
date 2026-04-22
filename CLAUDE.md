# Veille Boisée — Contexte & Guide de développement

PWA de signalement de décharges sauvages en forêt avec routing automatique vers les collectivités
et l'ONF, via la Géoplateforme IGN et l'API Découpage Administratif.

- **Licence** : Non commerciale — voir [LICENSE](./LICENSE)
- **Sécurité** : voir [docs/SECURITY.md](./docs/SECURITY.md)
- **RGPD** : voir [docs/RGPD.md](./docs/RGPD.md)

---

## Stack technique

| Couche | Techno |
|--------|--------|
| **Frontend** | Angular (PWA, standalone components, signals), hébergé sur **Azure Static Web Apps** |
| **Backend** | C# / **.NET 10 LTS** (ASP.NET Core Controllers + MediatR), hébergé sur **Azure App Service Linux** |
| **Résilience HTTP** | `HttpClientFactory` + `Microsoft.Extensions.Http.Resilience` (retry, timeout, circuit breaker) |
| **Tests** | xUnit + FluentAssertions + NSubstitute ; `WebApplicationFactory` pour les tests d'intégration |
| **Secrets** | Azure Key Vault (production), `.env` / `dotnet user-secrets` (dev local) |
| **Observabilité** | Serilog (JSON) → Application Insights |

---

## Architecture

### Clean Architecture (Hexagonale)

Organisation de la solution .NET (`backend/`) :

```
backend/src/
├── VeilleBoisee.Domain/           # Entités, value objects, domain events — zéro dépendance externe
├── VeilleBoisee.Application/      # Use cases CQRS : commands + queries + interfaces des ports
├── VeilleBoisee.Infrastructure/   # Repositories, adapters APIs externes (IGN, geo.api.gouv.fr)
└── VeilleBoisee.Api/              # Controllers ASP.NET Core, middleware de sécurité, DI
```

Le front Angular (`frontend/`) consomme l'API via un service HTTP dédié par domaine métier.

**Règle absolue** : les dépendances pointent toujours vers l'intérieur.
`domain` ne dépend de rien. `application` ne dépend que de `domain`.
`infrastructure` implémente les interfaces définies dans `application`.

### CQRS

Séparation stricte commandes / requêtes :

| Type | Exemples | Effet de bord |
|------|----------|---------------|
| **Command** | `SubmitReportCommand`, `UpdateReportStatusCommand`, `DeleteAccountCommand` | Oui — modifie l'état |
| **Query** | `GetReportsByUserQuery`, `GetReportsByAreaQuery`, `GetReportStatusQuery` | Non — lecture pure |

- Un handler par commande ou requête, dans `application/`
- Aucune logique métier dans les controllers
- Les commands retournent uniquement un identifiant ou un statut — jamais les données modifiées (utiliser une Query pour ça)

---

## Qualité du code

### Langue

- **Documentation, commentaires, issues, PR, messages de commit** : rédigés en français
- **Code** : anglais exclusivement — identifiants, noms de classes, méthodes, propriétés,
  paramètres, variables locales, enums, fichiers, branches Git, messages de log techniques
- Les termes métier typiquement français (`Commune`, `CodeInsee`, `Parcelle`, `ONF`, `Signalement`)
  restent tels quels : ils font partie du ubiquitous language du domaine administratif français
  et leur traduction serait une perte de sens
- Les chaînes destinées à l'utilisateur final (UI, e-mails, notifications) suivent la langue
  du produit — par défaut français

### Clean Code

- Nommage explicite : pas d'abréviations, pas de `data`, `tmp`, `obj`, `res`, `info`
- Fonctions ≤ 20 lignes, un seul niveau d'abstraction par fonction
- Pas de commentaires qui décrivent le QUOI — uniquement le POURQUOI quand la raison
  n'est pas évidente depuis le code seul
- Pas de nombres magiques : utiliser des constantes nommées

### Zéro Code Mort

- Zéro import inutilisé (échec du lint = échec du CI)
- Zéro variable non utilisée
- Zéro branche de code inaccessible
- Zéro feature flag persistant non tracé en issue GitHub ouverte

### Tests

- **TDD préféré** : écrire le test avant l'implémentation
- **Couverture minimale** : 80 % sur la couche `application`
- **Convention AAA** : Arrange / Act / Assert, séparés par une ligne vide
- Chaque use case a au moins un test unitaire heureux et un test d'erreur
- Les adapters externes (IGN, INSEE, OAuth) sont **toujours mockés** en tests unitaires
- Pas d'accès base de données en test unitaire : utiliser des repositories in-memory
- Tests d'intégration dans un dossier séparé (`tests/integration/`), déclenchés
  explicitement en CI (pas dans le watch mode)

---

## APIs utilisées

### Flux de routing (dans l'ordre)

```
Coordonnées GPS (lon, lat)
        │
        ▼
geo.api.gouv.fr/communes?lat=&lon=          → code INSEE commune
        │
        ▼
Géoplateforme IGN — WFS BDPARCELLAIRE       → numéro parcelle, section, commune
        │
        ├──► WFS BDFORET_V2 (Géoplateforme) → forêt domaniale ONF ? (tag ONF)
        │
        └──► API Carto module Nature         → zone Natura 2000 ? (tag zone_protégée)
```

### Référence des endpoints

| Service | URL de base | Données retournées |
|---------|------------|-------------------|
| Découpage administratif | `https://geo.api.gouv.fr/communes?lat={lat}&lon={lon}` | Code INSEE, nom commune |
| Parcelle cadastrale | `https://data.geopf.fr/wfs` (layer `BDPARCELLAIRE-VECTEUR`) | Parcelle, section |
| Forêt ONF | `https://data.geopf.fr/wfs` (layer `BDFORET_V2:formation_vegetale`) | Qualification forêt domaniale |
| Natura 2000 | `https://apicarto.ign.fr/api/nature/natura-habitat` | Sites Natura 2000 |

> **Important** : L'ancien `apicarto.ign.fr/api/cadastre` est en fin de vie.
> Utiliser exclusivement la **Géoplateforme IGN** (`data.geopf.fr`) pour tout
> nouveau développement cadastral.

### Routing ONF

Aucune API publique ne permet de router vers une agence ONF spécifique.
Fichier de configuration à maintenir manuellement :

```json
// config/routing/onf-agencies.json
{
  "01": { "agence": "Agence ONF Ain", "email": "ain@onf.fr" },
  "75": { "agence": "Agence ONF Île-de-France", "email": "idf@onf.fr" }
}
```

Ce fichier est versionné et modifiable sans redéploiement.

---

## RGPD by Design

Voir [docs/RGPD.md](./docs/RGPD.md) pour les détails. À respecter systématiquement :

- **Email collecté** (chiffré) pour recours en cas de faux signalement — jamais transmis automatiquement aux collectivités
- **Pas de donnée personnelle dans les logs** (ni coordonnées GPS brutes)
- **Droit à l'effacement** : soft delete → purge définitive ≤ 30 jours
- **Métadonnées EXIF supprimées** des photos avant stockage
- **Coordonnées GPS précises** stockées et transmises à la collectivité — **floutées à 100 m uniquement pour l'affichage public**

---

## Sécurité

Voir [docs/SECURITY.md](./docs/SECURITY.md) pour les détails. Points critiques :

- **Rate limiting** actif sur tous les endpoints (seuils dans SECURITY.md)
- **Validation côté serveur** systématique — ne jamais faire confiance au client
- **Secrets via variables d'environnement** — jamais dans le code ni dans git
- **HTTPS obligatoire**, HSTS activé
