namespace Application.Contracts;

public interface IEnvelopeHandler
{
    Task<bool> Exists(string instanceIdentifier);
    Task<string> Enqueue(string instanceIdentifier, byte[] content);
    Task<string?> CreateAppResponse(string instanceIdentifier);
}