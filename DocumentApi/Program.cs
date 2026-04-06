using Application.Contracts;
using Application.DTOs;
using Application.Services;
using DocumentApi;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
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
var secretKey = jwtSettings["Key"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new Exception("JWT Key is missing in configuration!");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication()
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
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

var documentGroup = app.MapGroup("/document");

documentGroup.MapGet("/", async (
    [AsParameters] DocumentFilter filter,
    IDocumentService documentService) =>
{
    var documents = await documentService.GetDocuments(filter);
    return TypedResults.Ok(documents);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); ;

documentGroup.MapGet("/preview/{uuid:guid}/{type}", async (
    Guid uuid,
    string type,
    IDocumentService documentService) =>
{
    var (content, contentType) = type.ToLower() switch
    {
        "xml" => (await documentService.GetXmlContent(uuid), "application/xml"),
        "html" => (await documentService.GetHtmlContent(uuid), "text/html"),
        "pdf" => (await documentService.GetPdfContent(uuid), "application/pdf"),
        _ => throw new BadHttpRequestException("Döküman tipi xml, html veya pdf olmalıdır.")
    };
    return TypedResults.File(content, contentType);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

documentGroup.MapPost("/upload", async (
    HttpRequest request,
    IDocumentService documentService) =>
{
    if (request.ContentLength is null or 0)
        throw new BadHttpRequestException("Dosya içeriği bulunamadı.");
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    await documentService.LoadDocument(ms.ToArray());
    return TypedResults.Ok();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

documentGroup.MapDelete("/cancel/{uuid:guid}", async (
    Guid uuid,
    IDocumentService documentService) =>
{
    await documentService.CancelDocument(uuid);
    return TypedResults.Ok();
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

app.Run();