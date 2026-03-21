using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IReportRepository:IRepositoryBase<Report>
{
    Task<int> GetMaxBolumNo(string mukellef, DateTime start, DateTime end);
    Task<List<Report>> GetReportsByStatus(Status status, SubStatus subStatus);
}