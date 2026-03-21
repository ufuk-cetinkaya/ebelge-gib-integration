using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Infrastructure.Repositories;

public class EnvelopeRepository : RepositoryBase<Envelope>, IEnvelopeRepository
{
    public EnvelopeRepository(RepositoryContext context) : base(context)
    {

    }

    public async Task<List<Envelope>> GetReceivedEnv()
    {
        return await
            _context
            .Envelopes
            .Where(a =>
            a.Status == Status.RECEIVE &&
            a.SubStatus == SubStatus.NEW &&
            a.Direction == Direction.IN)
            .Take(100)
            .ToListAsync();
    }

    public async Task<Envelope?> GetEnvelope(string? instanceIdentifier, Direction direction)
    {
        return await
            _context
            .Envelopes
            .Where(a =>
            a.InstanceIdentifier == instanceIdentifier &&
            a.Direction == direction)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetEnvelopeCount(string instanceIdentifier)
    {
        return await
            _context
            .Envelopes
            .Where(a =>
            a.InstanceIdentifier == instanceIdentifier && 
            a.Status == Status.RECEIVE &&
            a.SubStatus != SubStatus.FAILED)
            .CountAsync();
    }

    public async Task<byte[]> GetContent(int? id)
    {
        return await
            _context
            .Envelopes
            .Where(a => a.Id == id)
            .Select(a => a.Content)
            .SingleAsync();
    }

    public async Task<List<Envelope>> GetPackagedEnv()
    {
        return await
            _context
            .Envelopes
            .Where(a =>
            a.Status == Status.PACKAGE &&
            a.SubStatus == SubStatus.SUCCEED &&
            a.Direction == Direction.OUT)
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<Envelope>> GetEnvForStatusCheck()
    {
        return await
            _context
            .Envelopes
            .Where(a =>
            a.CreateDate > DateTime.Now.AddDays(-7) && (
            a.StatusCheck == StatusCheck.N || (
            a.StatusCheck == StatusCheck.P &&
            a.ModifyDate < DateTime.Now.AddHours(-2))))
            .Take(100)
            .ToListAsync();
    }
}