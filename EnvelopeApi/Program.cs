using Application.Contracts;
using Application.Services;
using Domain.Repositories;
using EFaturaWsService;
using EnvelopeApi;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SoapCore;
using System.ServiceModel.Channels;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<RepositoryContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<EFaturaPortType, EFaturaService>();

builder.Services.AddScoped<IEnvelopeHandler, EnvelopeHandler>();

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddScoped<IEnvelopeRepository, EnvelopeRepository>();

builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks().AddDbContextCheck<RepositoryContext>("SQL Server");

builder.Services.AddSoapCore();

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

app.MapHealthChecks("/health/live");

app.MapHealthChecks("/health/ready");

app.UseEndpoints(endpoints =>
{
    _ = endpoints.UseSoapEndpoint<EFaturaPortType>(
        "/EFaturaMerkez/services/EFatura",
        new SoapEncoderOptions() { MessageVersion = MessageVersion.Soap12WSAddressingAugust2004 },
        SoapSerializer.XmlSerializer);
});
app.Run();