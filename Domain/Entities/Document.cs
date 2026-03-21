using Domain.Enums;

namespace Domain.Entities;

public class Document
{
    public int Id { get; set; }
    public int? EnvelopeId { get; set; }
    public int? ReportId { get; set; }
    public string ProfileId { get; set; } = null!;
    public string DocumentId { get; set; } = null!;
    public string Uuid { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public DocumentTypes Type { get; set; }
    public string? TypeCode { get; set; }
    public decimal? PayableAmount { get; set; }
    public string? Currency { get; set; }
    public string SupplierIdentifier { get; set; } = null!;
    public string SupplierTitle { get; set; } = null!;
    public string CustomerIdentifier { get; set; } = null!;
    public string CustomerTitle { get; set; } = null!;
    public string? RefId { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResponseDesc { get; set; }
    public Direction Direction { get; set; }
    public Status Status { get; set; }
    public SubStatus SubStatus { get; set; }
    public string? ErrorDesc { get; set; }
    public bool CancelFlag { get; set; }
    public DateTime? CancelDate { get; set; }
    public int? CancelReportId { get; set; }
    public DateTime? SigningTime { get; set; }
    public DateTime CreateDate { get; set; }
    public byte[] Content { get; set; } = null!;
    public Envelope? Envelope { get; set; }
    public Report? Report { get; set; }
}