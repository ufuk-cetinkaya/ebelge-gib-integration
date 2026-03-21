using Application.Contracts;
using Quartz;

namespace ReportWorker;

internal class ReportLoaderJob : IJob
{
    private readonly IReportService _report;

    public ReportLoaderJob(IReportService report)
    {
        _report = report;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _report.LoadReport();
    }
}

internal class ReportPackagerJob : IJob
{
    private readonly IReportService _report;

    public ReportPackagerJob(IReportService report)
    {
        _report = report;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _report.PackageReport();
    }
}

internal class ReportSignerJob : IJob
{
    private readonly IReportService _report;

    public ReportSignerJob(IReportService report)
    {
        _report = report;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _report.SignReport();
    }
}

internal class ReportSenderJob : IJob
{
    private readonly IReportService _report;

    public ReportSenderJob(IReportService report)
    {
        _report = report;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _report.SendReport();
    }
}

internal class StatusCheckerJob : IJob
{
    private readonly IReportService _report;

    public StatusCheckerJob(IReportService report)
    {
        _report = report;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _report.CheckStatus();
    }
}