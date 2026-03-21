using Domain.Enums;

namespace Application.DTOs;

public record DocumentFilter
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DocumentTypes DocumentType { get; set; }
    public Direction Direction { get; set; }
}