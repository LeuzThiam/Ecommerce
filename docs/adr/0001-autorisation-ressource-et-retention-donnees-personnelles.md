# ADR 0001 : Autorisation par ressource et rétention des données personnelles

**Statut :** Acceptée
**Date :** 2026-11-06
**Issue associée :** #22

## Contexte

Trois types de données personnelles sont exposés par des endpoints authentifiés dans ce projet :

- **Profil** (Identity.Api) : email, nom affiché.
- **Adresses** (Identity.Api) : adresses postales liées à un compte.
- **Commandes et factures** (Ordering.Api) : historique d'achat, articles, montants, factures PDF.

Chacune de ces ressources doit être consultable et modifiable uniquement par le client auquel elle appartient — jamais par un autre client authentifié, quel que soit son rôle (sauf un administrateur, dont l'accès élargi à toutes les commandes est une fonctionnalité volontaire et distincte, couverte par la policy `AdminOnly`).

## Décision

### Autorisation par ressource

**Principe retenu : l'appartenance se vérifie en résolvant d'abord l'identité de l'appelant depuis le JWT, jamais en faisant confiance à un identifiant de ressource fourni dans l'URL.**

Concrètement, deux patterns distincts selon la forme de l'agrégat :

1. **Ressource racine identifiable directement (Order)** : l'endpoint charge la ressource par son id, puis compare son `CustomerId` à l'identité de l'appelant (`ClaimTypes.NameIdentifier` du JWT). Toute discordance retourne **404**, jamais 403 — on ne révèle même pas qu'une commande portant cet identifiant existe. Voir `GetOrder` et `DownloadInvoice` dans `Ordering.Api/Program.cs`.

2. **Ressource enfant d'un agrégat (Address)** : l'endpoint résout d'abord le `User` de l'appelant via son identité JWT, puis recherche l'adresse **uniquement dans la collection de cet agrégat** (`User.FindAddress`, interne à `Identity.Domain`). Une adresse appartenant à un autre client n'existe tout simplement pas dans cette collection, donc toute tentative échoue avec une `DomainException` (400) — l'isolation est garantie par construction du modèle de domaine, pas par une vérification ad hoc répétée dans chaque endpoint.

Ce choix (404/400 plutôt que 403) est délibéré : un 403 confirme l'existence de la ressource à un attaquant qui devine des identifiants ; un 404/400 générique ne le fait pas.

Chaque endpoint exposant une ressource client est couvert par un test d'intégration croisé (client A crée la ressource, client B tente d'y accéder) :

- `OrderHistoryTests.GetOrder_BelongingToAnotherCustomer_ReturnsNotFound`
- `InvoiceTests.DownloadInvoice_BelongingToAnotherCustomer_ReturnsNotFound`
- `ProfileAndAddressTests.UpdateAddress_BelongingToAnotherClient_IsRejected`
- `ProfileAndAddressTests.RemoveAddress_BelongingToAnotherClient_IsRejected`
- `ProfileAndAddressTests.SetDefaultAddress_BelongingToAnotherClient_IsRejected`

Le profil (`GetProfile`/`UpdateProfile`) n'a pas de test équivalent : ces endpoints n'acceptent aucun identifiant de ressource en paramètre (ils opèrent toujours sur « mon propre profil » résolu depuis le JWT), donc il n'existe pas de surface d'attaque par substitution d'identifiant à ce niveau.

### Rétention des données personnelles

- **Pendant la durée de vie du compte** : aucune donnée personnelle n'est supprimée automatiquement. Le profil, les adresses et l'historique de commandes/factures restent disponibles aussi longtemps que le compte existe — une facture doit rester consultable indéfiniment pour des raisons comptables, et l'historique de commandes a une valeur pour le client.
- **Suppression de compte** : **non implémentée à ce jour**. Il n'existe aucun endpoint permettant à un client de supprimer son compte ou de demander l'effacement de ses données. C'est une limitation connue de ce portfolio, documentée ici plutôt que laissée implicite — une vraie mise en production nécessiterait un endpoint de suppression (ou d'anonymisation, pour préserver l'intégrité des commandes déjà facturées) conforme à un droit à l'effacement.
- **Logs et environnements de développement** : les emails envoyés en développement (confirmation, réinitialisation de mot de passe) passent par Mailpit ou par les logs applicatifs (`DevelopmentEmailSender`), jamais par un vrai fournisseur SMTP tant que `ASPNETCORE_ENVIRONMENT` n'est pas `Production` — évite une fuite accidentelle d'adresses email réelles pendant le développement.

## Conséquences

- Positif : l'isolation par ressource est garantie structurellement pour les adresses (impossible d'oublier la vérification dans un futur endpoint, puisqu'elle fait partie du modèle de domaine) et testée explicitement pour toutes les ressources concernées.
- Négatif : l'absence de suppression de compte est une dette assumée de ce portfolio, pas un oubli — elle est explicitement hors scope tant qu'aucune Issue ne la demande.
- À surveiller : tout nouvel endpoint exposant une ressource liée à un client doit suivre l'un des deux patterns ci-dessus et être accompagné d'un test d'accès croisé, sous peine de régresser silencieusement sur ce point.
