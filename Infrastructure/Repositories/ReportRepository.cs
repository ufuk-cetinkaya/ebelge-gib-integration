using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using Domain.Enums;

namespace Infrastructure.Repositories;

public class ReportRepository : RepositoryBase<Report>, IReportRepository
{
    public ReportRepository(RepositoryContext context) : base(context)
    {

    }

    public async Task<int> GetMaxBolumNo(string mukellef, DateTime start, DateTime end)
    {
        return await
            _context
            .Reports
            .Where(a =>
            a.Mukellef == mukellef &&
            a.DonemBaslangic >= start &&
            a.DonemBitis <= end &&
            a.Status == Status.SEND &&
            a.SubStatus == SubStatus.SUCCEED)
            .Select(r => r.BolumNo)
            .DefaultIfEmpty()
            .MaxAsync();
    }

    public async Task<List<Report>> GetReportsByStatus(Status status, SubStatus subStatus)
    {
        return await
            _context
            .Reports
            .Where(a =>
            a.Status == status &&
            a.SubStatus == subStatus)
            .Take(100)
            .ToListAsync();
    }
}