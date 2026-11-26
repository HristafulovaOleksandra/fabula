using Fabula.Api.Auth;
using Fabula.Api.Data;
using Fabula.Api.Dictionary;
using Fabula.Api.Economy;
using Fabula.Api.Email;
using Fabula.Api.Entities;
using Fabula.Api.Storage;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

DotNetEnv.Env.TraversePath().NoClobber().Load();

var builder = WebApplication.CreateBuilder(args);

string RequiredEnvironment(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Змінна середовища {name} не задана. Скопіюй .env.example у .env і заповни значення.");

var connectionString =
    $"Host={Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost"};" +
    $"Port={Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"};" +
    $"Database={RequiredEnvironment("POSTGRES_DB")};" +
    $"Username={RequiredEnvironment("POSTGRES_USER")};" +
    $"Password={RequiredEnvironment("POSTGRES_PASSWORD")}";

var jwtSecret = RequiredEnvironment("JWT_SECRET");
if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT_SECRET має містити щонайменше 32 символи.");
}

var jwt = new JwtSettings(jwtSecret, "fabula-api", "fabula-frontend", TimeSpan.FromDays(7));
var uploadsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "uploads"));

builder.Services.AddDbContext<FabulaDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton(new ImageStorage(uploadsPath));
builder.Services.AddSingleton(builder.Configuration.GetSection("Economy").Get<EconomySettings>()
    ?? throw new InvalidOperationException("У appsettings.json немає розділу Economy."));
builder.Services.AddSingleton(new WordDictionary(Path.Combine(AppContext.BaseDirectory, "Resources", "en-uk.tsv")));
builder.Services.AddSingleton(SmtpSettings.FromEnvironment());
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimits.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = jwt.SigningKey,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.CanAuthor, policy => policy.RequireRole(nameof(UserRole.Author), nameof(UserRole.Admin)))
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(nameof(UserRole.Admin)));

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    return Task.CompletedTask;
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<FabulaDbContext>().Database.Migrate();
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Fabula API").AddPreferredSecuritySchemes("Bearer"));
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = ImageStorage.PublicPrefix
});
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
