using Microsoft.EntityFrameworkCore;

using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Shared.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conversion> Conversions => Set<Conversion>();

    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<MetadataResolutionQueueItem> MetadataResolutionQueue => Set<MetadataResolutionQueueItem>();

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

        _ = entity.Property(c => c.Rank)
            .HasMaxLength(64);

        _ = entity.Property(c => c.Status)
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

        _ = entity.HasIndex(c => new { c.Status, c.Rank });

        var profileEntity = modelBuilder.Entity<Profile>();
        _ = profileEntity.HasKey(p => p.Id);
        _ = profileEntity.Property(p => p.OutputDirectory)
            .HasMaxLength(1024);
        _ = profileEntity.Property(p => p.MetadataBasedFilename)
            .HasMaxLength(1024);
        _ = profileEntity.Property(p => p.OutputFilenameTemplate)
            .HasMaxLength(1024);

        // Configure JSON storage for language lists
        var (listToJson, listComparer) = Converters.CreateStringListJson();

        var audioLangProp = profileEntity.Property(p => p.AudioLanguages)
            .HasConversion(listToJson)
            .HasMaxLength(4096)
            .Metadata;
        audioLangProp.SetValueComparer(listComparer);

        var subtitleLangProp = profileEntity.Property(p => p.SubtitleLanguages)
            .HasConversion(listToJson)
            .HasMaxLength(4096)
            .Metadata;
        subtitleLangProp.SetValueComparer(listComparer);

        ConfigureMetadataResolutionQueue(modelBuilder);
    }

    private static void ConfigureMetadataResolutionQueue(ModelBuilder modelBuilder)
    {
        var queue = modelBuilder.Entity<MetadataResolutionQueueItem>();
        _ = queue.HasKey(q => q.Id);
        _ = queue.Property(q => q.SourcePath)
            .IsRequired()
            .HasMaxLength(2048);
        _ = queue.Property(q => q.SourceFileName)
            .HasMaxLength(1024);
        _ = queue.Property(q => q.InputKind)
            .IsRequired()
            .HasMaxLength(32);
        _ = queue.Property(q => q.ParsedTitle)
            .HasMaxLength(512);
        _ = queue.Property(q => q.FailureReason)
            .IsRequired()
            .HasMaxLength(128);
        _ = queue.Property(q => q.FailureDetails)
            .HasMaxLength(2048);
        _ = queue.Property(q => q.Status)
            .IsRequired()
            .HasMaxLength(32);
        _ = queue.Property(q => q.CreatedAtUtc)
            .IsRequired();
        _ = queue.Property(q => q.UpdatedAtUtc)
            .IsRequired();
        _ = queue.HasIndex(q => new { q.SourcePath, q.Status }).IsUnique();
    }
}
