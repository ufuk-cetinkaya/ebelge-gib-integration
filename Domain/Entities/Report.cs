using Domain.Enums;

namespace Domain.Entities;

public class Report
{
    public int Id { get; set; }
    public string Hazirlayan { get; set; } = null!;
    public string Mukellef { get; set; } = null!;
    public string RaporNo { get; set; } = null!;
    public DateTime DonemBaslangic { get; set; }
    public DateTime DonemBitis { get; set; }
    public DateTime BolumBaslangic { get; set; }
    public DateTime BolumBitis { get; set; }
    public int BolumNo { get; set; }
    public int BelgeSayisi { get; set; }
    public Status Status { get; set; }
    public SubStatus SubStatus { get; set; }
    public string? ErrorDesc { get; set; }
    public int? ResponseCode { get; set; }
    public string? ResponseDesc { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] Content { get; set; } = null!;
    public ICollection<Document>? Documents { get; set; }
}