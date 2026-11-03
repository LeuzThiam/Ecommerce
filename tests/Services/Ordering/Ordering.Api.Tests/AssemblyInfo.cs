using Xunit;

// Les tests d'intégration de ce projet utilisent WebApplicationFactory, qui modifie des
// variables d'environnement globales au processus (Environment.SetEnvironmentVariable) dans
// CustomWebApplicationFactory.ConfigureWebHost. Par défaut, xUnit exécute les différentes
// classes de tests en parallèle (sur des threads différents) — plusieurs factories peuvent
// alors démarrer leur hôte en même temps et se marcher dessus via ces variables partagées.
// On désactive la parallélisation pour ce projet, pratique standard recommandée pour les
// tests d'intégration basés sur WebApplicationFactory.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
