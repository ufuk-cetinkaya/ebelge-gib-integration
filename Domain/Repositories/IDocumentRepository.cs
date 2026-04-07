using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IDocumentRepository:IRepositoryBase<Document>
{
    Task<byte[]?> GetContent(Guid uuid);
    Task<DateTime> GetMinDoc();

    Task<int> GetDocumentCount(DateTime startDate, DateTime endDate, DocumentTypes documentType, Direction direction);
    Task<List<Document>> GetDocuments(DateTime startDate, DateTime endDate, DocumentTypes documentType, Direction direction, int skip, int take);
    Task<List<Document>> GetLoadedDocs();
    Task<List<Document>> GetSignedDocs();
    Task<List<Document>> GetSignedDocs(string supplier, DateTime start, DateTime end);
    Task<Document?> GetDocToCancel(Guid uuid);
    Task<List<Document>> GetDocsByReportId(int reportId);
    Task<List<string>> GetSuppliers();
    Task<int> Count(string supplier, string id, string uuid);
    Task<int?> GetRefEnvId(string instanceIdentifier);
}