using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IEnvelopeRepository : IRepositoryBase<Envelope>
{
    Task<List<Envelope>> GetReceivedEnv();
    Task<Envelope?> GetEnvelope(string? instanceIdentifier, Direction direction);
    Task<int> GetEnvelopeCount(string instanceIdentifier);
    Task<byte[]> GetContent(int? id);
    Task<List<Envelope>> GetPackagedEnv();
    Task<List<Envelope>> GetEnvForStatusCheck();
}