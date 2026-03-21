namespace Application.Contracts;

public interface IReportService
{
    Task LoadReport();
    Task PackageReport();
    Task SignReport();
    Task SendReport();
    Task CheckStatus();
}