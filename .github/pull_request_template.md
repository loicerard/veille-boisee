## Description
<!-- Que fait cette PR ? Pourquoi ce changement ? -->

## Issue liée
Closes #

## Checklist

### Code
- [ ] Tests unitaires écrits et passants
- [ ] Couverture ≥ 80 % sur la couche `application`
- [ ] Zéro import / variable inutilisé (lint ok)
- [ ] Pas de code mort introduit
- [ ] Pas de commentaire décrivant le QUOI (seulement le POURQUOI si non-évident)

### Sécurité
- [ ] Pas de secret dans le code (tokens, clés API, mots de passe)
- [ ] Toute nouvelle entrée utilisateur est validée côté serveur
- [ ] Rate limiting vérifié si un nouvel endpoint est exposé

### RGPD
- [ ] Aucune nouvelle donnée personnelle collectée sans justification
- [ ] Si nouvelles données : inventaire `docs/RGPD.md` mis à jour
- [ ] Pas de donnée personnelle dans les logs
