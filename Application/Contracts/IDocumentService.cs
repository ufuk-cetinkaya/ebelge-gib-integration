using Application.DTOs;

namespace Application.Contracts;

public interface IDocumentService
{
    Task LoadDocument(byte[] content);
    Task CancelDocument(Guid uuid);
    Task<List<DocumentDto>> GetDocuments(DocumentFilter filter);
    Task<byte[]> GetXmlContent(Guid uuid);
    Task<byte[]> GetHtmlContent(Guid uuid);
    Task<byte[]> GetPdfContent(Guid uuid);
}