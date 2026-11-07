# Ecommerce

[![CI](https://github.com/LeuzThiam/Ecommerce/actions/workflows/ci.yml/badge.svg)](https://github.com/LeuzThiam/Ecommerce/actions/workflows/ci.yml)

Un site e-commerce construit comme une véritable architecture microservices, de zéro, avec .NET 10 / C# 14. Projet portfolio : chaque fonctionnalité est née d'une Issue GitHub avec ses scénarios Gherkin, implémentée avec sa propre couverture de tests, et vérifiée manuellement de bout en bout avant d'être mergée.

## Aperçu

- **Catalogue** : produits, catégories, variantes (taille/couleur avec prix et stock indépendants), recherche avec filtres.
- **Panier et paiement** : panier anonyme par cookie, checkout, paiement Stripe, confirmation par webhook, gestion de stock avec verrou de concurrence optimiste.
- **Compte client** : inscription avec confirmation par email, réinitialisation de mot de passe, profil, plusieurs adresses, historique de commandes, factures PDF.
- **Administration** : gestion du catalogue (création/modification/désactivation logique), vue sur toutes les commandes, remboursement, espace admin complet côté Web.
- **Transverse** : autorisation par ressource (un client ne peut jamais atteindre les données d'un autre), observabilité (health checks, gestion d'erreurs globale en Problem Details RFC 7807), CI automatisée.

## Architecture

Trois services métier indépendants, une passerelle API, un front web - chacun avec sa propre base de données, déployable et testable séparément.

```
                        ┌─────────────┐
                        │  Ecommerce  │   MVC, consomme les API via la Gateway
                        │     Web     │
                        └──────┬──────┘
                               │
                        ┌──────▼──────┐
                        │   Gateway   │   Ocelot - point d'entrée HTTP unique
                        └──────┬──────┘
             ┌─────────────────┼─────────────────┐
      ┌──────▼──────┐   ┌──────▼──────┐   ┌──────▼──────┐
      │ Identity.Api│   │ Catalog.Api │   │ Ordering.Api│
      │  IdentityDb │   │  CatalogDb  │   │  OrderingDb │
      └─────────────┘   └─────────────┘   └─────────────┘
```

- **Identity.Api** — comptes, authentification JWT, rôles (Administrator/Client), profil, adresses.
- **Catalog.Api** — produits, variantes, catégories, réservation de stock.
- **Ordering.Api** — panier, commandes, paiements Stripe, factures PDF.
- **Gateway** — point d'entrée unique (Ocelot), route chaque appel vers le bon service.
- **Ecommerce.Web** — front MVC, espace client et espace admin, consomme uniquement les API via la Gateway (jamais d'accès direct à une base de données).

**Vraie séparation de services**, pas juste une séparation de dossiers : chaque service a son propre projet Domain/Application/Infrastructure/Api, sa propre base de données, son propre `DomainException`. Aucune bibliothèque partagée entre services — un pattern identique (ex. le gestionnaire d'erreurs global) est délibérément dupliqué dans chaque service plutôt que factorisé, pour que chaque service reste déployable et modifiable indépendamment des autres.

Chaque service suit la même architecture en couches :

- **Domain** — entités avec constructeurs privés/internes et méthodes métier, aucune dépendance externe.
- **Application** — DTOs (`sealed record`) et interfaces de service ; ne retourne jamais une entité de domaine.
- **Infrastructure** — implémentations EF Core, clients HTTP vers les autres services.
- **Api** — endpoints Minimal API, authentification JWT, autorisation par rôle.

Voir [docs/adr](docs/adr) pour les décisions d'architecture documentées (autorisation par ressource, rétention des données personnelles, convention de nommage).

## Stack technique

- **.NET 10** / **C# 14**, Minimal API, EF Core 10
- **SQL Server** (une base par service) ; SQLite en mémoire pour les tests
- **Ocelot** pour la passerelle API
- **Stripe** pour les paiements, **QuestPDF** pour les factures, **MailKit** pour l'envoi d'emails
- **JWT** pour l'authentification inter-services
- **xUnit** + `WebApplicationFactory` pour les tests d'intégration
- **Docker Compose** pour l'environnement complet, **GitHub Actions** pour la CI

## Démarrage rapide (Docker Compose)

```bash
cp .env.example .env
# renseigner MSSQL_SA_PASSWORD, JWT_SECRET_KEY, STRIPE_SECRET_KEY, STRIPE_WEBHOOK_SECRET dans .env
docker compose up --build
```

| Service | URL |
|---|---|
| Web (front) | http://localhost:5001 |
| Gateway | http://localhost:5000 |
| Identity.Api | http://localhost:5138 |
| Catalog.Api | http://localhost:5223 |
| Ordering.Api | http://localhost:5087 |
| Mailpit (emails interceptés en dev) | http://localhost:8025 |

Aucune variable n'a de valeur par défaut dans le code : chaque service refuse de démarrer si une configuration requise est absente, plutôt que de se rabattre silencieusement sur une valeur devinée.

## Démarrage en local (sans Docker)

Prérequis : SDK .NET 10 (`global.json` fixe la version exacte), SQL Server LocalDB.

```bash
export Jwt__SecretKey="une-cle-partagee-par-tous-les-services"
export Stripe__SecretKey="sk_test_..."       # Ordering.Api seulement
export Stripe__WebhookSecret="whsec_..."     # Ordering.Api seulement

dotnet run --project src/Services/Identity/Identity.Api
dotnet run --project src/Services/Catalog/Catalog.Api
dotnet run --project src/Services/Ordering/Ordering.Api
dotnet run --project src/Gateway/Gateway
dotnet run --project src/Web/Ecommerce.Web
```

Chaque service applique ses migrations EF Core automatiquement au démarrage (hors environnement `Testing`).

## Structure du dépôt

```
src/
  Services/
    Identity/    Identity.Domain · Identity.Application · Identity.Infrastructure · Identity.Api
    Catalog/     Catalog.Domain · Catalog.Application · Catalog.Infrastructure · Catalog.Api
    Ordering/    Ordering.Domain · Ordering.Application · Ordering.Infrastructure · Ordering.Api
  Gateway/       Gateway (Ocelot)
  Web/           Ecommerce.Web (MVC : catalogue public, espace client, espace admin)
tests/
  Services/      un projet *.Domain.Tests et un projet *.Api.Tests par service
docs/
  adr/           décisions d'architecture documentées
```

## Tests

```bash
dotnet test Ecommerce.slnx
```

Deux niveaux de tests automatisés par service :

- **Domain.Tests** — règles métier pures, aucune dépendance (ex. une commande ne peut être remboursée que si elle est payée).
- **Api.Tests** — tests d'intégration via `WebApplicationFactory`, base SQLite en mémoire, vrais tokens JWT signés, doubles de test pour Stripe et les appels inter-services (`FakePaymentGateway`, `FakeProductCatalogClient`...).

Chaque fonctionnalité sensible (paiement, remboursement, autorisation par ressource) est couverte par des tests d'accès croisé explicites : un client A crée une ressource, un client B tente d'y accéder, le test échoue si l'isolation n'est pas respectée.

Au-delà des tests automatisés, chaque fonctionnalité a été vérifiée manuellement de bout en bout contre de vrais services (SQL Server LocalDB, vrais tokens JWT, navigateur réel pour le Web) avant d'être mergée - plusieurs bugs d'intégration (incompatibilités EF Core/SQLite, schéma d'authentification écrasé, binding culturel des formulaires HTML) n'ont été repérés que par cette vérification, pas par les tests automatisés seuls.

## CI

GitHub Actions restaure, compile et teste l'ensemble de la solution à chaque pull request et à chaque push sur `main`/`develop` - voir [.github/workflows/ci.yml](.github/workflows/ci.yml).
