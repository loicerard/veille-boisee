# Conformité RGPD — Veille Boisée

Responsable de traitement : **Loïc Erard** — https://github.com/loicerard

---

## Données collectées

| Donnée | Finalité | Base légale | Durée de conservation |
|--------|----------|-------------|----------------------|
| Identifiant OAuth anonymisé (hash SHA-256) | Authentification, liaison des signalements au compte | Contrat (utilisation de l'app) | Durée du compte + 30 jours |
| Email (chiffré) | Identification en cas de signalement de données fausses par une collectivité | Intérêt légitime (lutte contre les faux signalements) + consentement éclairé à l'inscription | Durée du compte |
| Token push (chiffré AES-256) | Envoi de notifications de statut | Consentement explicite | Durée du compte |
| Coordonnées GPS précises du signalement | Routing cadastral + localisation exacte pour la collectivité | Intérêt légitime (signalement environnemental) | Durée du signalement |
| Photo du signalement (sans métadonnées EXIF) | Preuve visuelle pour la collectivité | Consentement explicite | Durée du signalement |
| Type et description du dépôt | Qualification du signalement | Consentement | Durée du signalement |
| Historique des statuts | Suivi par le citoyen et la collectivité | Intérêt légitime | Durée du signalement + 1 an |

### Ce qui n'est JAMAIS collecté

- Nom, prénom, numéro de téléphone, adresse postale
- Identifiant technique de l'appareil (IMEI, IDFA, Android ID)
- Adresse IP stockée en base de données
- Historique de navigation ou de localisation hors signalement actif

### Accès à l'email — conditions strictes

L'email n'est **jamais transmis automatiquement** aux collectivités. Il est accessible
uniquement sur **signalement formel de données fausses** par une collectivité, avec :
- Traçabilité complète de l'accès (qui, quand, pour quel signalement)
- Notification à l'utilisateur concerné
- Procédure documentée dans la convention de traitement avec la collectivité

---

## Principes appliqués

### Minimisation des données

L'identifiant OAuth est hashé avant persistance. Le nom fourni par le provider OAuth
ne transite pas par nos serveurs. L'email est collecté et stocké chiffré pour le seul
usage décrit ci-dessus (recours en cas de faux signalement).

### Privacy by Design

- **Flouté géographique (affichage public uniquement)** : les coordonnées GPS précises
  sont conservées pour le routing cadastral et transmises à la collectivité. Seul
  l'**affichage sur la carte publique** utilise des coordonnées arrondies à 100 m,
  afin de ne pas exposer la position exacte d'un citoyen
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
| **Rectification** (art. 16) | Mise à jour de l'email depuis les paramètres du compte |
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
| Provider OAuth (Google/Apple/Facebook) | Authentification | Token OAuth (flux côté client) + email récupéré une fois à l'inscription | UE / Clauses contractuelles types | DPA signé |
| Hébergeur applicatif (à définir) | Infrastructure | Données applicatives chiffrées au repos | **UE préféré** | DPA requis |
| IGN / Géoplateforme | Routing cadastral | Coordonnées GPS anonymes (sans userId) | France | Service public |
| geo.api.gouv.fr | Code INSEE | Coordonnées GPS anonymes (sans userId) | France | Service public |
| Collectivités destinataires | Traitement du signalement | Signalement (type, photo, **localisation précise**) — email uniquement sur recours formel | France | Convention de traitement (inclut les conditions d'accès à l'email) |

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
