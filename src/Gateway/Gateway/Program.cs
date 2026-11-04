using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ocelot.{Environment}.json surcharge les hôtes downstream (ex: noms de service Docker Compose
// au lieu de "localhost") sans dupliquer toute la configuration des routes.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Ocelot intercepte toute requête qui atteint son middleware et répond lui-même 404 pour
// tout chemin non configuré dans ocelot.json — un endpoint classique (MapGet) enregistré
// avant lui ne suffit pas. On utilise donc un middleware brut, qui court-circuite la requête
// avant même qu'Ocelot ne la voie.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health/live")
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        return;
    }

    await next();
});

// Ocelot gère tout le reste du routage (identity/catalog/ordering) lui-même, à partir
// d'ocelot.json.
await app.UseOcelot();

app.Run();

public partial class Program;
