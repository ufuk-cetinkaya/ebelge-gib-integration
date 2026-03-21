namespace Application.DTOs;

public class GibUserDto(string identifier,
    string title,
    string userType,
    string accountType,
    string firstCreationTime,
    string documentType,
    string unit,
    string alias,
    string aliasCreationTime)
{
    public string Identifier { get; set; } = identifier;
    public string Title { get; set; } = title;
    public string UserType { get; set; } = userType;
    public string AccountType { get; set; } = accountType;
    public string FirstCreationTime { get; set; } = firstCreationTime;
    public string DocumentType { get; set; } = documentType;
    public string Unit { get; set; } = unit;
    public string Alias { get; set; } = alias;
    public string AliasCreationTime { get; set; } = aliasCreationTime;
}