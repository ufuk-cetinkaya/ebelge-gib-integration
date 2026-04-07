using Domain.Enums;

namespace Application.DTOs;

public record DocumentFilter
{
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
    public DateTime EndDate { get; set; } = DateTime.Now;
    public DocumentTypes DocumentType { get; set; } = DocumentTypes.INVOICE;
    public Direction Direction { get; set; } = Direction.IN;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}