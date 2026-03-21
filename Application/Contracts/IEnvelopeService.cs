namespace Application.Contracts;

public interface IEnvelopeService
{
    Task ExtractEnvelope();
    Task SignDocuments();
    Task CreateEnvelope();
    Task SendEnvelope();
    Task CheckStatus();
}
