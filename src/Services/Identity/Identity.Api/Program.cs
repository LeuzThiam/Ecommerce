using System.Text;
using Identity.Application;
using Identity.Domain.Common;
using Identity.Domain.Identity;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

var app = builder.Build();

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
    CancellationToken cancellationToken) =>
{
    if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
    {
        return Results.Conflict(new { message = "Cet email est déjà utilisé." });
    }

    User domainUser;
    try
    {
        domainUser = User.Register(request.Email, request.DisplayName);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }

    var applicationUser = new ApplicationUser
    {
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

    var roles = await userManager.GetRolesAsync(user);
    var token = tokenGenerator.GenerateToken(user.Id, user.Email!, [.. roles]);

    return Results.Ok(new { accessToken = token });
})
.WithName("Login");

app.MapGet("/health/live", () => Results.Ok());

app.Run();

record RegisterRequest(string Email, string Password, string DisplayName);
record LoginRequest(string Email, string Password);

public partial class Program;
