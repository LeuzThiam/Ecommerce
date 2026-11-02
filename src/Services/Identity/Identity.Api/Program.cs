using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Identity.Domain.Common;
using Identity.Domain.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La chaîne de connexion DefaultConnection est requise.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
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
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGroup("/api/identity/account").MapIdentityApi<ApplicationUser>();

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

    await userRepository.AddAsync(domainUser, cancellationToken);

    return Results.Created($"/api/identity/register/{domainUser.Id}",
        new UserDto(domainUser.Id, domainUser.Email.Value, domainUser.DisplayName, domainUser.CreatedAtUtc));
})
.WithName("Register");

app.MapGet("/health/live", () => Results.Ok());

app.Run();

record RegisterRequest(string Email, string Password, string DisplayName);