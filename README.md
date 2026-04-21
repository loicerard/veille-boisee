# Veille Boisée

PWA citoyenne de signalement de décharges sauvages en forêt, avec routing automatique
vers les collectivités et l'ONF via la Géoplateforme IGN et l'API Découpage Administratif.

Voir [CLAUDE.md](./CLAUDE.md) pour le contexte complet, l'architecture et les conventions.

## Stack

- **Front** : Angular (PWA, standalone components + signals)
- **Back** : C# / .NET 10 LTS (ASP.NET Core)
- **Hébergement** : Azure (App Service Linux, Static Web Apps, Key Vault, Application Insights)

## Structure

```
backend/   # Solution .NET (Domain / Application / Infrastructure / Api)
frontend/  # Application Angular
docs/      # Documentation sécurité et RGPD
```

## Démarrage local

### Backend

```bash
cd backend
dotnet restore
dotnet test
dotnet run --project src/VeilleBoisee.Api
```

API exposée sur `https://localhost:5001` (Swagger sur `/swagger`).

### Frontend

```bash
cd frontend
npm install
npm start
```

UI servie sur `http://localhost:4200`.

## Documents clés

- [Conformité RGPD](./docs/RGPD.md)
- [Sécurité](./docs/SECURITY.md)
- [Licence non commerciale](./LICENSE)
