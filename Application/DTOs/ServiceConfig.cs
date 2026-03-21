namespace Application.DTOs;

public record ServiceConfig
{
    public required string DocumentApiUrl { get; set; }
    public required string EntegratorVkn { get; set; }
}