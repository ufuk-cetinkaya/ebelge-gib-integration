using Application.Contracts;
using Application.DTOs;
using Application.Services;
using DocumentApi;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

var documentGroup = app.MapGroup("/document");

documentGroup.MapGet("/", async (
    [AsParameters] DocumentFilter filter,
    IDocumentService documentService) =>
{
    var documents = await documentService.GetDocuments(filter);
    return TypedResults.Ok(documents);
});

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
});

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
});

documentGroup.MapDelete("/cancel/{uuid:guid}", async (
    Guid uuid,
    IDocumentService documentService) =>
{
    await documentService.CancelDocument(uuid);
    return TypedResults.Ok();
});

app.Run();