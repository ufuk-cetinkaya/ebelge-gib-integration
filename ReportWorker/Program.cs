using Application.Contracts;
using Application.DTOs;
using Application.Services;
using Domain.Repositories;
using EArsivWsService;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using ReportWorker;
using SignerWs;
using System.ServiceModel;
using System.Xml;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<RepositoryContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<ServiceConfig>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IReportRepository, ReportRepository>();

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddScoped<EArsivWs, EArsivWsClient>(client =>
{
    BasicHttpBinding binding = new();
    EndpointAddress endpoint = new(builder.Configuration["AppSettings:EArsivUrl"]);
    return new(binding, endpoint);
});
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
    var job1 = new JobKey("report-loader");
    q.AddJob<ReportLoaderJob>(opts => opts.WithIdentity(job1));
    q.AddTrigger(opts => opts
    .ForJob(job1)
    .WithIdentity("report-loader-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job2 = new JobKey("report-packager");
    q.AddJob<ReportPackagerJob>(opts => opts.WithIdentity(job2));
    q.AddTrigger(opts => opts
    .ForJob(job2)
    .WithIdentity("report-packager-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job3 = new JobKey("report-signer");
    q.AddJob<ReportSignerJob>(opts => opts.WithIdentity(job3));
    q.AddTrigger(opts => opts
    .ForJob(job3)
    .WithIdentity("report-signer-trigger")
    .WithCronSchedule("0 * * * * ?"));

    var job4 = new JobKey("report-sender");
    q.AddJob<ReportSenderJob>(opts => opts.WithIdentity(job4));
    q.AddTrigger(opts => opts
    .ForJob(job4)
    .WithIdentity("report-sender-trigger")
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