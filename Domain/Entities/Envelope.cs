using Domain.Enums;

namespace Domain.Entities;

public class Envelope
{
    public int Id { get; set; }
    public string InstanceIdentifier { get; set; } = null!;
    public EnvelopeType Type { get; set; }
    public DocumentTypes? PackageType { get; set; }
    public string SenderIdentifier { get; set; } = null!;
    public string SenderTitle { get; set; } = null!;
    public string SenderAlias { get; set; } = null!;
    public string ReceiverIdentifier { get; set; } = null!;
    public string ReceiverTitle { get; set; } = null!;
    public string ReceiverAlias { get; set; } = null!;
    public int? ResponseCode { get; set; }
    public string? ResponseDesc { get; set; }
    public Direction Direction { get; set; }
    public Status Status { get; set; }
    public SubStatus SubStatus { get; set; }
    public StatusCheck? StatusCheck { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ModifyDate { get; set; }
    public byte[] Content { get; set; } = null!;    
    public ICollection<Document>? Documents { get; set; }
}