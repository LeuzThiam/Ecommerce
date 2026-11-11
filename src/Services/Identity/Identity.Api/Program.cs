using System.Security.Claims;
using System.Text;
using Identity.Application;
using Identity.Domain.Common;
using Identity.Domain.Identity;
using Identity.Infrastructure;
using Identity.Infrastructure.Emailing;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey est requis — doit être identique sur Identity.Api, Catalog.Api et Ordering.Api.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Ecommerce";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Ecommerce";

// AddIdentity() (ci-dessus) configure déjà un schéma d'authentification par défaut (cookie,
// "Identity.Application") pour DefaultAuthenticateScheme/DefaultChallengeScheme. Sans ces trois
// lignes explicites, ce schéma cookie reste prioritaire malgré l'appel suivant — un
// .RequireAuthorization() sur Identity.Api lui-même échouerait silencieusement (redirection vers
// une page de login inexistante) même avec un Bearer token valide.
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// En Production, les emails partent réellement par SMTP (config requise au moment de l'envoi,
// voir SmtpEmailSender). Partout ailleurs (Development, Testing), on se contente de les logguer
// pour pouvoir tester les flux de confirmation/reset sans serveur SMTP.
if (builder.Environment.IsProduction())
{
    builder.Services.AddOptions<SmtpOptions>().Bind(builder.Configuration.GetSection(SmtpOptions.SectionName));
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, DevelopmentEmailSender>();
}

var publicBaseUrl = builder.Configuration["PublicBaseUrl"] ?? "http://localhost:5000";

var app = builder.Build();

// Gestionnaire global : traduit toute DomainException (violation d'une règle métier) en Problem
// Details 400 avec le message du domaine, au lieu de laisser chaque endpoint répéter son propre
// try/catch. Toute autre exception est logguée (jamais avalée silencieusement) puis renvoyée
// comme Problem Details 500 générique, sans fuite de détails internes au client.
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    if (exception is DomainException domainException)
    {
        await Results.Problem(title: "Règle métier violée", detail: domainException.Message, statusCode: StatusCodes.Status400BadRequest)
            .ExecuteAsync(context);
        return;
    }

    context.RequestServices.GetRequiredService<ILogger<Program>>()
        .LogError(exception, "Erreur non gérée lors du traitement de {Method} {Path}", context.Request.Method, context.Request.Path);

    await Results.Problem(title: "Une erreur inattendue est survenue.", statusCode: StatusCodes.Status500InternalServerError)
        .ExecuteAsync(context);
}));

// En environnement "Testing", la factory de test crée le schéma avec EnsureCreated() sur
// SQLite — appliquer les migrations (générées pour SQL Server) échouerait inutilement. Le seed
// des rôles, lui, ne dépend d'aucun fournisseur particulier et doit tourner dans tous les cas —
// sans ça, AddToRoleAsync échoue en test faute de rôle "Client" existant.
using (var scope = app.Services.CreateScope())
{
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var roleName in new[] { "Administrator", "Client" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/identity/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager,
    IUserRepository userRepository,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
    {
        return Results.Conflict(new { message = "Cet email est déjà utilisé." });
    }

    var domainUser = User.Register(request.Email, request.DisplayName);

    var applicationUser = new ApplicationUser
    {
        // Le JWT émis au login porte l'Id d'ApplicationUser (claim "sub") - il doit être
        // identique à celui du User du domaine, sinon aucun endpoint authentifié (profil,
        // adresses) ne peut retrouver le bon agrégat à partir du token.
        Id = domainUser.Id,
        UserName = domainUser.Email.Value,
        Email = domainUser.Email.Value
    };

    var identityResult = await userManager.CreateAsync(applicationUser, request.Password);
    if (!identityResult.Succeeded)
    {
        return Results.ValidationProblem(identityResult.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
    }

    // Tout nouveau compte reçoit le rôle Client par défaut — le rôle Administrateur se
    // promeut manuellement (pas d'auto-inscription en administrateur).
    await userManager.AddToRoleAsync(applicationUser, "Client");

    await userRepository.AddAsync(domainUser, cancellationToken);

    var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));
    var confirmationLink = $"{publicBaseUrl}/api/identity/confirm-email?userId={applicationUser.Id}&token={encodedToken}";
    await emailSender.SendAsync(
        applicationUser.Email!,
        "Confirmez votre adresse email",
        $"Bonjour {domainUser.DisplayName},\n\nConfirmez votre compte en suivant ce lien :\n{confirmationLink}",
        cancellationToken);

    return Results.Created($"/api/identity/register/{domainUser.Id}",
        new UserDto(domainUser.Id, domainUser.Email.Value, domainUser.DisplayName, domainUser.CreatedAtUtc));
})
.WithName("Register");

app.MapPost("/api/identity/login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenGenerator tokenGenerator,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    if (!await userManager.IsEmailConfirmedAsync(user))
    {
        return Results.BadRequest(new { message = "Veuillez confirmer votre adresse email avant de vous connecter." });
    }

    var roles = await userManager.GetRolesAsync(user);
    var token = tokenGenerator.GenerateToken(user.Id, user.Email!, [.. roles]);

    return Results.Ok(new { accessToken = token });
})
.WithName("Login");

app.MapGet("/api/identity/confirm-email", async (
    Guid userId,
    string token,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
    {
        return Results.NotFound();
    }

    string decodedToken;
    try
    {
        decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
    }
    catch (FormatException)
    {
        return Results.BadRequest(new { message = "Lien de confirmation invalide." });
    }

    var result = await userManager.ConfirmEmailAsync(user, decodedToken);
    if (!result.Succeeded)
    {
        return Results.BadRequest(new { message = "Lien de confirmation invalide ou expiré." });
    }

    return Results.Ok(new { message = "Email confirmé. Vous pouvez maintenant vous connecter." });
})
.WithName("ConfirmEmail");

app.MapPost("/api/identity/forgot-password", async (
    ForgotPasswordRequest request,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);

    // Ne révèle jamais si l'email correspond à un compte existant - toujours la même réponse,
    // que l'email envoie réellement un message ou non.
    if (user is not null)
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(resetToken));
        await emailSender.SendAsync(
            user.Email!,
            "Réinitialisation de votre mot de passe",
            $"Pour réinitialiser votre mot de passe, appelez POST /api/identity/reset-password avec cet email et ce token :\n{encodedToken}",
            cancellationToken);
    }

    return Results.Ok(new { message = "Si un compte existe avec cet email, un email de réinitialisation a été envoyé." });
})
.WithName("ForgotPassword");

app.MapPost("/api/identity/reset-password", async (
    ResetPasswordRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null)
    {
        return Results.BadRequest(new { message = "Email ou token invalide." });
    }

    string decodedToken;
    try
    {
        decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
    }
    catch (FormatException)
    {
        return Results.BadRequest(new { message = "Token invalide." });
    }

    var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
    if (!result.Succeeded)
    {
        return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));
    }

    return Results.Ok(new { message = "Mot de passe réinitialisé." });
})
.WithName("ResetPassword");

app.MapGet("/api/identity/profile", async (
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new UserDto(domainUser.Id, domainUser.Email.Value, domainUser.DisplayName, domainUser.CreatedAtUtc));
})
.RequireAuthorization()
.WithName("GetProfile");

app.MapPut("/api/identity/profile", async (
    UpdateProfileRequest request,
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    domainUser.UpdateDisplayName(request.DisplayName);

    await userRepository.UpdateAsync(domainUser, cancellationToken);

    return Results.Ok(new UserDto(domainUser.Id, domainUser.Email.Value, domainUser.DisplayName, domainUser.CreatedAtUtc));
})
.RequireAuthorization()
.WithName("UpdateProfile");

app.MapGet("/api/identity/addresses", async (
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(domainUser.Addresses.Select(ToAddressDto));
})
.RequireAuthorization()
.WithName("GetAddresses");

app.MapPost("/api/identity/addresses", async (
    AddressRequest request,
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    var address = domainUser.AddAddress(request.Line1, request.Line2, request.City, request.PostalCode, request.Country);

    await userRepository.UpdateAsync(domainUser, cancellationToken);

    return Results.Created($"/api/identity/addresses/{address.Id}", ToAddressDto(address));
})
.RequireAuthorization()
.WithName("AddAddress");

app.MapPut("/api/identity/addresses/{addressId:guid}", async (
    Guid addressId,
    AddressRequest request,
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    domainUser.UpdateAddress(addressId, request.Line1, request.Line2, request.City, request.PostalCode, request.Country);

    await userRepository.UpdateAsync(domainUser, cancellationToken);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("UpdateAddress");

app.MapDelete("/api/identity/addresses/{addressId:guid}", async (
    Guid addressId,
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    domainUser.RemoveAddress(addressId);

    await userRepository.UpdateAsync(domainUser, cancellationToken);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("RemoveAddress");

app.MapPost("/api/identity/addresses/{addressId:guid}/default", async (
    Guid addressId,
    ClaimsPrincipal principal,
    IUserRepository userRepository,
    CancellationToken cancellationToken) =>
{
    var domainUser = await userRepository.GetByIdAsync(GetUserId(principal), cancellationToken);
    if (domainUser is null)
    {
        return Results.NotFound();
    }

    domainUser.SetDefaultAddress(addressId);

    await userRepository.UpdateAsync(domainUser, cancellationToken);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("SetDefaultAddress");

// Aucune UI ni endpoint n'existait pour voir qui est administrateur ou le devenir - la seule
// façon de promouvoir un compte était une requête SQL manuelle directement en base. Ces trois
// endpoints couvrent uniquement le rôle Administrator (le seul concerné) : Client reste attribué
// une fois pour toutes à l'inscription et n'est jamais retiré.
app.MapGet("/api/identity/admin/users", async (
    IUserRepository userRepository,
    UserManager<ApplicationUser> userManager,
    CancellationToken cancellationToken) =>
{
    var domainUsers = await userRepository.GetAllAsync(cancellationToken);

    var result = new List<UserAdminDto>();
    foreach (var domainUser in domainUsers)
    {
        var appUser = await userManager.FindByIdAsync(domainUser.Id.ToString());
        var roles = appUser is null ? [] : await userManager.GetRolesAsync(appUser);
        result.Add(new UserAdminDto(
            domainUser.Id,
            domainUser.Email.Value,
            domainUser.DisplayName,
            domainUser.CreatedAtUtc,
            appUser?.EmailConfirmed ?? false,
            [.. roles]));
    }

    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("GetAllUsersAdmin");

app.MapPost("/api/identity/admin/users/{userId:guid}/promote", async (
    Guid userId,
    UserManager<ApplicationUser> userManager,
    CancellationToken cancellationToken) =>
{
    var appUser = await userManager.FindByIdAsync(userId.ToString());
    if (appUser is null)
    {
        return Results.NotFound();
    }

    if (!await userManager.IsInRoleAsync(appUser, "Administrator"))
    {
        await userManager.AddToRoleAsync(appUser, "Administrator");
    }

    return Results.NoContent();
})
.RequireAuthorization("AdminOnly")
.WithName("PromoteUser");

app.MapPost("/api/identity/admin/users/{userId:guid}/demote", async (
    Guid userId,
    ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    CancellationToken cancellationToken) =>
{
    // Un admin qui se retire lui-même ses propres droits pourrait verrouiller tout le monde hors
    // de l'administration s'il est le seul admin restant - interdit sans exception.
    if (userId == GetUserId(principal))
    {
        throw new DomainException("Vous ne pouvez pas retirer vos propres droits administrateur.");
    }

    var appUser = await userManager.FindByIdAsync(userId.ToString());
    if (appUser is null)
    {
        return Results.NotFound();
    }

    if (await userManager.IsInRoleAsync(appUser, "Administrator"))
    {
        await userManager.RemoveFromRoleAsync(appUser, "Administrator");
    }

    return Results.NoContent();
})
.RequireAuthorization("AdminOnly")
.WithName("DemoteUser");

app.MapHealthChecks("/health/live");

app.Run();

static Guid GetUserId(ClaimsPrincipal principal) =>
    Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

static AddressDto ToAddressDto(Address address) =>
    new(address.Id, address.Line1, address.Line2, address.City, address.PostalCode, address.Country, address.IsDefault);

record RegisterRequest(string Email, string Password, string DisplayName);
record LoginRequest(string Email, string Password);
record ForgotPasswordRequest(string Email);
record ResetPasswordRequest(string Email, string Token, string NewPassword);
record UpdateProfileRequest(string DisplayName);
record AddressRequest(string Line1, string? Line2, string City, string PostalCode, string Country);

public partial class Program;
