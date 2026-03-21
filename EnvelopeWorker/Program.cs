using Application.Contracts;
using Application.Services;
using Domain.Repositories;
using EFaturaWsService;
using EnvelopeWorker;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using SignerWs;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<RepositoryContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEnvelopeService, EnvelopeService>();

builder.Services.AddScoped<IEnvelopeRepository, EnvelopeRepository>();

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddScoped<EFaturaPortType, EFaturaPortTypeClient>(client =>
{
    MtomMessageEncodingBindingElement encoding = new(MessageVersion.Soap12, Encoding.UTF8);
    HttpsTransportBindingElement transport = new();
    transport.MaxReceivedMessageSize = int.MaxValue;
    CustomBinding binding = new(encoding, transport);
    EndpointAddress endpoint = new(builder.Configuration["AppSettings:EFaturaUrl"]);
    return new(binding, endpoint);
});

builder.Services.AddHttpClient<IGibUserClient, GibUserClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AppSettings:GibUserUrl"]);
}).AddStandardResilienceHandler();

builder.Services.AddScoped<ISignerWs, SignerWsClient>(client =>
{
    BasicHttpBinding binding = new()
    {
        MaxReceivedMessageSize = 52428800,
        MaxBufferSize = 52428800,
        ReaderQuotas = new XmlDictionaryReaderQuotas
        {
            MaxDepth = 64,
            MaxStringContentLength = 52428800,
            MaxArrayLength = 52428800,
            MaxBytesPerRead = 4096,
            MaxNameTableCharCount = 16384
        }
    };
    EndpointAddress endpoint = new(builder.Configuration["AppSettings:SignerUrl"]);
    return new(binding, endpoint);
});

builder.Services.AddQuartz(q =>
{
    var job1 = new JobKey("envelope-extractor");
    q.AddJob<EnvelopeExtractorJob>(opts => opts.WithIdentity(job1));
    q.AddTrigger(opts => opts
    .ForJob(job1)
    .WithIdentity("envelope-extractor-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job2 = new JobKey("document-signer");
    q.AddJob<DocumentSignerJob>(opts => opts.WithIdentity(job2));
    q.AddTrigger(opts => opts
    .ForJob(job2)
    .WithIdentity("document-signer-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job3 = new JobKey("envelope-creator");
    q.AddJob<EnvelopeCreatorJob>(opts => opts.WithIdentity(job3));
    q.AddTrigger(opts => opts
    .ForJob(job3)
    .WithIdentity("envelope-creator-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job4 = new JobKey("envelope-sender");
    q.AddJob<EnvelopeSenderJob>(opts => opts.WithIdentity(job4));
    q.AddTrigger(opts => opts
    .ForJob(job4)
    .WithIdentity("envelope-sender-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job5 = new JobKey("status-checker");
    q.AddJob<StatusCheckerJob>(opts => opts.WithIdentity(job5));
    q.AddTrigger(opts => opts
    .ForJob(job5)
    .WithIdentity("status-checker-trigger")
    .WithCronSchedule("0 * * * * ?"));
});

builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = true;
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

app.Run();
