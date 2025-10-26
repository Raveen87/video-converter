using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Domain.Entities;

namespace VideoConverter.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conversion> Conversions => Set<Conversion>();
    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Conversion>();
        _ = entity.HasKey(c => c.Id);
        _ = entity.Property(c => c.InputPath)
            .IsRequired()
            .HasMaxLength(2048);
        _ = entity.Property(c => c.OutputDirectory)
            .IsRequired()
            .HasMaxLength(1024);

        _ = entity.Property(c => c.CreatedAtUtc)
            .IsRequired();

        _ = entity.Property(c => c.Crf);
        _ = entity.Property(c => c.AudioBitrateKbps);
        _ = entity.Property(c => c.Resolution);
        _ = entity.Property(c => c.SelectedAudioStreamIndicesCsv)
            .HasMaxLength(512);
        _ = entity.Property(c => c.SelectedSubtitleStreamIndicesCsv)
            .HasMaxLength(512);
        _ = entity.Property(c => c.SelectedExternalSubtitlePathsCsv)
            .HasMaxLength(4096);
        _ = entity.Property(c => c.DetectedCrop)
            .HasMaxLength(64);

        var profileEntity = modelBuilder.Entity<Profile>();
        _ = profileEntity.HasKey(p => p.Id);
        _ = profileEntity.Property(p => p.OutputDirectory)
            .HasMaxLength(1024);
        _ = profileEntity.Property(p => p.MetadataBasedFilename)
            .HasMaxLength(1024);
    }
}
