using Application.Contracts;
using Quartz;

namespace EnvelopeWorker;

internal class EnvelopeExtractorJob : IJob
{
    private readonly IEnvelopeService _envelope;

    public EnvelopeExtractorJob(IEnvelopeService envelope)
    {
        _envelope = envelope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _envelope.ExtractEnvelope();
    }
}

internal class DocumentSignerJob : IJob
{
    private readonly IEnvelopeService _envelope;

    public DocumentSignerJob(IEnvelopeService envelope)
    {
        _envelope = envelope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _envelope.SignDocuments();
    }
}

internal class EnvelopeCreatorJob : IJob
{
    private readonly IEnvelopeService _envelope;

    public EnvelopeCreatorJob(IEnvelopeService envelope)
    {
        _envelope = envelope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _envelope.CreateEnvelope();
    }
}

internal class EnvelopeSenderJob : IJob
{
    private readonly IEnvelopeService _envelope;

    public EnvelopeSenderJob(IEnvelopeService envelope)
    {
        _envelope = envelope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _envelope.SendEnvelope();
    }
}

internal class StatusCheckerJob : IJob
{
    private readonly IEnvelopeService _envelope;

    public StatusCheckerJob(IEnvelopeService envelope)
    {
        _envelope = envelope;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _envelope.CheckStatus();
    }
}
