using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Domain.Enums;

namespace Infrastructure.Repositories;

public class DocumentRepository : RepositoryBase<Document>, IDocumentRepository
{
    public DocumentRepository(RepositoryContext context) : base(context)
    {

    }

    public async Task<byte[]?> GetContent(Guid uuid)
    {
        return await
            _context
            .Documents
            .Where(a => a.Uuid == uuid.ToString())
            .Select(a => a.Content)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetRefEnvId(string instanceIdentifier)
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.RefId == instanceIdentifier &&
            a.Direction == Direction.OUT)
            .Select(a => a.EnvelopeId)
            .FirstOrDefaultAsync();
    }

    public async Task<DateTime> GetMinDoc()
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.Status == Status.SIGN &&
            a.SubStatus == SubStatus.SUCCEED &&
            a.Direction == Direction.OUT &&
            a.ReportId == null && (
            a.Type == DocumentTypes.INVOICE ||
            a.Type == DocumentTypes.CREDITNOTE) && (
            a.ProfileId == "EARSIVFATURA" ||
            a.ProfileId == "EARSIVBELGE"))
            .Select(a => a.IssueDate)
            .DefaultIfEmpty()
            .MinAsync();
    }

    public async Task<List<Document>> GetDocuments(DateTime startDate, DateTime endDate, DocumentTypes documentType, Direction direction)
    {
        return await
            _context
            .Documents
            .AsNoTracking()
            .Where(a =>
            a.IssueDate >= startDate &&
            a.IssueDate <= endDate &&
            a.Type == documentType &&
            a.Direction == direction)
            .OrderBy(a => a.IssueDate)
            .ToListAsync();
    }

    public async Task<List<Document>> GetLoadedDocs()
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.Status == Status.LOAD &&
            a.SubStatus == SubStatus.SUCCEED &&
            a.Direction == Direction.OUT)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Document>> GetSignedDocs()
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.Status == Status.SIGN &&
            a.SubStatus == SubStatus.SUCCEED &&
            a.Direction == Direction.OUT &&
            a.EnvelopeId == null && (
            a.Type == DocumentTypes.INVOICE ||
            a.Type == DocumentTypes.APPLICATIONRESPONSE ||
            a.Type == DocumentTypes.DESPATCHADVICE ||
            a.Type == DocumentTypes.RECEIPTADVICE) && !(
            a.ProfileId == "EARSIVFATURA" ||
            a.ProfileId == "EARSIVBELGE"))
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Document>> GetSignedDocs(string supplier, DateTime start, DateTime end)
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.SupplierIdentifier == supplier &&
            a.IssueDate >= start &&
            a.IssueDate <= end &&
            a.Status == Status.SIGN &&
            a.SubStatus == SubStatus.SUCCEED &&
            a.Direction == Direction.OUT &&
            a.ReportId == null && (
            a.Type == DocumentTypes.INVOICE ||
            a.Type == DocumentTypes.CREDITNOTE) && (
            a.ProfileId == "EARSIVFATURA" ||
            a.ProfileId == "EARSIVBELGE"))
            .Take(100)
            .ToListAsync();
    }

    public async Task<Document?> GetDocToCancel(Guid uuid)
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.Uuid == uuid.ToString() && (
            a.Status == Status.SIGN ||
            a.Status == Status.PACKAGE) &&
            a.SubStatus == SubStatus.SUCCEED &&
            a.Direction == Direction.OUT &&
            ! a.CancelFlag && (
            a.Type == DocumentTypes.INVOICE ||
            a.Type == DocumentTypes.CREDITNOTE) && (
            a.ProfileId == "EARSIVFATURA" ||
            a.ProfileId == "EARSIVBELGE"))
            .FirstOrDefaultAsync();
    }

    public async Task<List<Document>> GetDocsByReportId(int reportId)
    {
        return await
            _context
            .Documents
            .Where(a => a.ReportId == reportId)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<string>> GetSuppliers()
    {
        return await
            _context
            .Documents
            .Select(a => a.SupplierIdentifier)
            .Distinct()
            .Take(100)
            .ToListAsync();
    }

    public async Task<int> Count(string supplier, string id, string uuid)
    {
        return await
            _context
            .Documents
            .Where(a =>
            a.SupplierIdentifier == supplier && (
            a.DocumentId == id ||
            a.Uuid == uuid))
            .CountAsync();
    }
}