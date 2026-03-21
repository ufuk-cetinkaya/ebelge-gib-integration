using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class RepositoryContext : DbContext
{
    public RepositoryContext(DbContextOptions<RepositoryContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Envelope)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.EnvelopeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Report)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Document");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PayableAmount)
                  .HasPrecision(18, 2);

            entity.Property(x => x.Type)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasConversion<string>();

            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasMaxLength(10)
                  .HasConversion<string>();

            entity.Property(x => x.SubStatus)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasConversion<string>();

            entity.Property(x => x.Direction)
                  .IsRequired()
                  .HasMaxLength(3)
                  .HasConversion<string>();

            entity.Property(x => x.Uuid)
                 .IsRequired()
                 .HasMaxLength(36);

            entity.Property(x => x.DocumentId)
                  .IsRequired()
                  .HasMaxLength(36);

            entity.Property(x => x.ProfileId)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(x => x.SupplierIdentifier)
                  .IsRequired()
                  .HasMaxLength(11);

            entity.Property(x => x.SupplierTitle)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(x => x.CustomerIdentifier)
                  .IsRequired()
                  .HasMaxLength(11);

            entity.Property(x => x.CustomerTitle)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(x => x.RefId)
                  .HasMaxLength(36);

            entity.Property(x => x.TypeCode)
                  .HasMaxLength(20);

            entity.Property(x => x.Currency)
                  .HasMaxLength(3);

            entity.Property(x => x.ResponseCode)
                  .HasMaxLength(10);

            entity.Property(x => x.ResponseDesc)
                  .HasMaxLength(255);

            entity.Property(x => x.ErrorDesc)
                  .HasMaxLength(255);
        });

        modelBuilder.Entity<Envelope>(entity =>
        {
            entity.ToTable("Envelope");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                  .IsRequired()
                  .HasMaxLength(15)
                  .HasConversion<string>();

            entity.Property(x => x.PackageType)
                  .HasMaxLength(20)
                  .HasConversion<string>();

            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasMaxLength(10)
                  .HasConversion<string>();

            entity.Property(x => x.SubStatus)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasConversion<string>();

            entity.Property(x => x.Direction)
                  .IsRequired()
                  .HasMaxLength(3)
                  .HasConversion<string>();

            entity.Property(x => x.StatusCheck)
                  .HasMaxLength(1)
                  .HasConversion<string>();

            entity.Property(x => x.InstanceIdentifier)
                 .IsRequired()
                 .HasMaxLength(36);

            entity.Property(x => x.SenderIdentifier)
                  .IsRequired()
                  .HasMaxLength(11);

            entity.Property(x => x.SenderTitle)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(x => x.SenderAlias)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(x => x.ReceiverIdentifier)
                  .IsRequired()
                  .HasMaxLength(11);

            entity.Property(x => x.ReceiverTitle)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(x => x.ReceiverAlias)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(x => x.ResponseDesc)
                  .HasMaxLength(255);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("Report");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RaporNo)
                 .IsRequired()
                 .HasMaxLength(36);

            entity.Property(x => x.Hazirlayan)
                  .IsRequired()
                  .HasMaxLength(11);

            entity.Property(x => x.Mukellef)
                  .IsRequired()
                  .HasMaxLength(11);

            entity.Property(x => x.Status)
                  .IsRequired()
                  .HasMaxLength(10)
                  .HasConversion<string>();

            entity.Property(x => x.SubStatus)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasConversion<string>();

            entity.Property(x => x.ResponseDesc)
                  .HasMaxLength(255);

            entity.Property(x => x.ErrorDesc)
                  .HasMaxLength(255);
        });
    }
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Envelope> Envelopes => Set<Envelope>();
    public DbSet<Report> Reports => Set<Report>();
}