# Conformité RGPD — Veille Boisée

Responsable de traitement : **Loïc Erard** — https://github.com/loicerard

---

## Données collectées

| Donnée | Finalité | Base légale | Durée de conservation |
|--------|----------|-------------|----------------------|
| Identifiant OAuth anonymisé (hash SHA-256) | Authentification, liaison des signalements au compte | Contrat (utilisation de l'app) | Durée du compte + 30 jours |
| Token push (chiffré AES-256) | Envoi de notifications de statut | Consentement explicite | Durée du compte |
| Coordonnées GPS du signalement (floutées à 100 m) | Routing cadastral + localisation pour la collectivité | Intérêt légitime (signalement environnemental) | Durée du signalement |
| Photo du signalement (sans métadonnées EXIF) | Preuve visuelle pour la collectivité | Consentement explicite | Durée du signalement |
| Type et description du dépôt | Qualification du signalement | Consentement | Durée du signalement |
| Historique des statuts | Suivi par le citoyen et la collectivité | Intérêt légitime | Durée du signalement + 1 an |

### Ce qui n'est JAMAIS collecté

- Email, nom, prénom, numéro de téléphone, adresse postale
- Identifiant technique de l'appareil (IMEI, IDFA, Android ID)
- Adresse IP stockée en base de données
- Historique de navigation ou de localisation hors signalement actif
- Coordonnées GPS précises après le traitement cadastral

---

## Principes appliqués

### Minimisation des données

Seul le hash de l'identifiant OAuth est persisté. L'email et le nom fournis par le
provider OAuth lors de la connexion ne transitent pas par nos serveurs : la vérification
du token se fait côté client via les bibliothèques officielles du provider.

### Privacy by Design

- **Flouté géographique** : les coordonnées GPS sont utilisées pour le routing cadastral
  puis **arrondies à 100 m** avant tout stockage ou transmission à un tiers
- **Strip EXIF** : toutes les photos sont re-encodées avec suppression complète des
  métadonnées avant stockage (géolocalisation, modèle d'appareil, auteur, date)
- **UUID décorrélés** : chaque signalement reçoit un UUID aléatoire indépendant du
  compte utilisateur — les deux tables ne sont joinables que côté serveur authentifié
- **Pas de tracking transversal** : aucun cookie de tracking, aucun pixel publicitaire,
  aucun SDK analytics tiers

### Transparence

La politique de confidentialité est accessible depuis l'écran d'accueil de la PWA,
avant toute création de compte. Elle liste exhaustivement les données collectées,
leur finalité, leur durée de conservation et les droits de l'utilisateur.

---

## Droits des utilisateurs

| Droit (RGPD art.) | Implémentation technique |
|-------------------|--------------------------|
| **Accès** (art. 15) | Export JSON de toutes les données liées au compte (endpoint `GET /account/export`) |
| **Effacement** (art. 17) | Suppression du compte → processus en 2 phases (voir ci-dessous) |
| **Portabilité** (art. 20) | Export JSON / CSV depuis le profil |
| **Opposition** (art. 21) | Désactivation des notifications push depuis les paramètres, à tout moment |
| **Rectification** (art. 16) | N/A — aucune donnée nominative stockée, donc rien à rectifier |
| **Limitation** (art. 18) | Sur demande : compte gelé (lecture seule) sans suppression immédiate |

---

## Processus de suppression de compte

```
1. Utilisateur demande la suppression
   └─► Confirmation requise (écran dédié, pas de suppression accidentelle)

2. Suppression immédiate
   ├─► Compte marqué PENDING_DELETION
   ├─► Accès révoqué (tous les JWT et refresh tokens invalidés)
   └─► Token push détruit

3. Dans les 30 jours (tâche cron quotidienne)
   ├─► Signalements : userId remplacé par NULL (anonymisation, pas suppression)
   └─► Purge définitive des entrées liées au compte (hashes OAuth, tokens)

4. Confirmation envoyée à l'utilisateur (via notification push si encore actif,
   ou confirmation à l'écran immédiatement)
```

### Signalements après suppression de compte

Les signalements anonymisés (sans lien avec l'utilisateur) sont **conservés**.
Justification RGPD : leur valeur environnementale (signalement à une collectivité)
est indépendante de l'identité du déclarant. La minimisation est maintenue
(aucune donnée personnelle ne subsiste dans le signalement).

---

## Sous-traitants et flux de données

| Prestataire | Rôle | Données transmises | Localisation | Garanties |
|------------|------|-------------------|-------------|-----------|
| Provider OAuth (Google/Apple/Facebook) | Authentification | Token OAuth (flux côté client uniquement — nos serveurs ne voient pas l'email) | UE / Clauses contractuelles types | DPA signé |
| Hébergeur applicatif (à définir) | Infrastructure | Données applicatives chiffrées au repos | **UE préféré** | DPA requis |
| IGN / Géoplateforme | Routing cadastral | Coordonnées GPS anonymes (sans userId) | France | Service public |
| geo.api.gouv.fr | Code INSEE | Coordonnées GPS anonymes (sans userId) | France | Service public |
| Collectivités destinataires | Traitement du signalement | Signalement anonymisé (type, photo, localisation floutée) | France | Convention de traitement |

**Aucun transfert de données hors UE** sans mécanisme de protection adéquat
(clauses contractuelles types ou décision d'adéquation au minimum).

---

## Logs et surveillance

- Les logs ne contiennent **aucune donnée personnelle directement identifiante**
- `userId` remplacé par `userId_hash` (SHA-256 salé, irréversible) dans les logs
- Coordonnées GPS brutes **absentes des logs**
- Conservation des logs : **90 jours maximum**
- Accès aux logs restreint aux administrateurs système (contrôle d'accès basé sur les rôles)

---

## Registre des activités de traitement (art. 30 RGPD)

Ce document constitue la base technique du registre. Le responsable de traitement
(Loïc Erard) tient ce registre à jour et le met à disposition sur demande de la CNIL.

**Dernière mise à jour** : 2025
