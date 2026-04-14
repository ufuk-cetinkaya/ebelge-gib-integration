using Application.Contracts;
using Application.Services;
using DocumentApi;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<RepositoryContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDocumentService, DocumentService>();

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks().AddDbContextCheck<RepositoryContext>("SQL Server");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddRequestDecompression();

builder.Services.AddResponseCompression();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Logging.ClearProviders();

builder.Logging.AddConsole();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

if (string.IsNullOrEmpty(jwtSettings["SecretKey"]))
{
    throw new Exception("JWT Key is missing in configuration!");
}

builder.Services.AddAuthentication()
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])),
        ValidateIssuer = true,
        ValidIssuer = "AuthService",
        ValidateAudience = true,
        ValidAudience = "AllServices",
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "role",
        NameClaimType = "sub"
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (args.Contains("migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
    Console.WriteLine("Running database migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("Migration completed.");
    return;
}

app.UseRouting();

app.UseExceptionHandler();

app.UseRequestDecompression();

app.UseResponseCompression();

app.UseCors();

app.UseStatusCodePages();

app.MapHealthChecks("/health/live");

app.MapHealthChecks("/health/ready");

app.UseAuthentication();

app.UseAuthorization();

app.MapDocumentEndpoints();

app.Run();