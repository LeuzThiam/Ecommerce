# ADR 0002 : Convention de nommage et contrats DTO

**Statut :** Acceptée
**Date :** 2026-11-06
**Issue associée :** #23

## Contexte

L'ancienne version du projet mélangeait le français et l'anglais dans les identifiants de code, et exposait parfois directement des entités de domaine (ou leurs propriétés mutables) comme réponse HTTP. Les deux problèmes se recoupent : un identifiant en franglais ou une entité qui fuit dans un contrat HTTP sont tous les deux des signes qu'une frontière (langue, ou couche architecturale) n'a pas été respectée au moment d'écrire le code.

## Décision

### Langue : code en anglais, contenu en français

- **Identifiants de code** (noms de classes, interfaces, méthodes, propriétés, variables, espaces de noms) : **toujours en anglais**. `Order`, `CreatePaymentIntentAsync`, `customerId`, `IInvoiceService` — jamais `Commande`, `CreerIntentionPaiementAsync`.
- **Contenu destiné à une personne** (commentaires expliquant un *pourquoi* non évident, messages de `DomainException` renvoyés au client, scénarios Gherkin des Issues, messages de commit) : **en français**, puisque c'est la langue de travail de l'équipe et, pour les messages d'erreur, la langue que verra l'utilisateur final du site.

Cette séparation n'est pas nouvelle avec cette ADR — c'est la convention déjà suivie de façon cohérente depuis le début de la réécriture (Identity, Catalog, Ordering, Web, Gateway). Cette issue la documente formellement plutôt que de la laisser implicite, pour qu'elle reste suivie telle quelle par la suite.

### Contrats DTO : jamais d'entité de domaine exposée

**Principe retenu : un endpoint HTTP ne retourne jamais directement une entité du Domain (`Product`, `Order`, `User`, `Payment`, etc.) ni sa forme EF Core — toujours un DTO `sealed record` dédié, défini dans la couche Application.**

Deux raisons à ça :

1. **Encapsulation du domaine** : une entité comme `Product` porte des méthodes mutables (`Deactivate()`, `TryReserveStock()`...) et des invariants internes. La sérialiser directement risquerait, à la moindre évolution du modèle (ajout d'un champ interne, d'une référence vers une autre entité), de divulguer accidentellement un détail d'implémentation jamais destiné au client HTTP.
2. **Découplage contrat/modèle** : le DTO peut évoluer indépendamment de l'entité (ex. `ProductDto` expose `Variants` en lecture seule sans jamais exposer `ProductVariant.Update`, qui reste `internal` au Domain).

Chaque service suit le même schéma en trois couches :

- **Domain** : entités avec constructeurs privés/internes et méthodes métier (`Product`, `Order`, `User`...).
- **Application** : DTOs `sealed record` (ex. `ProductDto`, `OrderDto`, `UserDto`) + interfaces de service qui ne retournent jamais que des DTOs, jamais des entités.
- **Api** : les endpoints Minimal API appellent les services Application et renvoient directement le DTO reçu — ils n'accèdent jamais à `ApplicationDbContext` ni à une entité Domain.

Un audit de chaque endpoint public des trois services (Identity.Api, Catalog.Api, Ordering.Api) confirme qu'aucun ne retourne une entité de domaine : `UserDto`/`AddressDto` (Identity), `ProductDto`/`CategoryDto`/`ProductVariantDto` (Catalog), `OrderDto`/`CartDto`/`PaymentIntentDto`/`PaymentAttemptDto` (Ordering). Les rares endpoints qui ne retournent ni DTO ni liste de DTO renvoient soit un objet anonyme de confirmation (`{ message = "..." }`, pour les accusés de réception sans donnée structurée), soit rien du tout (`Results.Ok()`, `Results.NoContent()`).

## Conséquences

- Positif : un développeur qui lit un fichier Program.cs sait immédiatement que ce qu'il voit passer est un contrat stable, jamais un détail d'implémentation du Domain susceptible de changer sans préavis.
- Positif : la convention linguistique élimine une classe entière de revues de code ("ce nom devrait être en anglais/français ?") en répondant à la question une fois pour toutes.
- À surveiller : tout nouvel endpoint doit introduire son propre DTO plutôt que de réutiliser une entité Domain existante par commodité, même pour un besoin qui semble temporaire — c'est précisément ce genre de raccourci qui a produit la fuite documentée dans l'ancienne version.
