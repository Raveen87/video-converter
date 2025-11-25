using VideoConverter.Api.Shared.Domain.Entities;

namespace VideoConverter.Api.Shared.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conversion> Conversions => Set<Conversion>();

    public DbSet<Profile> Profiles => Set<Profile>();

    public DbSet<MetadataResolutionQueueItem> MetadataResolutionQueue => Set<MetadataResolutionQueueItem>();

    public DbSet<DirectoryQueueBatch> DirectoryQueueBatches => Set<DirectoryQueueBatch>();

    public DbSet<DirectoryQueueBatchItem> DirectoryQueueBatchItems => Set<DirectoryQueueBatchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureConversion(modelBuilder);
        ConfigureProfile(modelBuilder);
        ConfigureMetadataResolutionQueue(modelBuilder);
        ConfigureDirectoryQueueBatch(modelBuilder);
    }

    private static void ConfigureConversion(ModelBuilder modelBuilder)
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
            .IsRequired()
            .HasConversion(Converters.CreateConversionStatusString())
            .HasMaxLength(32);

        _ = entity.Property(c => c.Crf);
        _ = entity.Property(c => c.AudioBitrateKbps);
        _ = entity.Property(c => c.AudioChannels)
            .HasConversion(Converters.CreateAudioChannelsString())
            .HasMaxLength(32);
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
    }

    private static void ConfigureProfile(ModelBuilder modelBuilder)
    {
        var profileEntity = modelBuilder.Entity<Profile>();
        _ = profileEntity.HasKey(p => p.Id);
        _ = profileEntity.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(256);
        _ = profileEntity.Property(p => p.OutputDirectory)
            .HasMaxLength(1024);
        _ = profileEntity.Property(p => p.AudioChannels)
            .HasConversion(Converters.CreateAudioChannelsString())
            .HasMaxLength(32);
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

    private static void ConfigureDirectoryQueueBatch(ModelBuilder modelBuilder)
    {
        var batch = modelBuilder.Entity<DirectoryQueueBatch>();
        _ = batch.HasKey(b => b.Id);
        _ = batch.Property(b => b.CreatedAtUtc)
            .IsRequired();
        _ = batch.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(32);
        _ = batch.Property(b => b.TotalItems)
            .IsRequired();
        _ = batch.Property(b => b.ProcessedItems)
            .IsRequired();
        _ = batch.Property(b => b.CurrentItemPath)
            .HasMaxLength(2048);
        _ = batch.Property(b => b.ErrorMessage)
            .HasMaxLength(2048);
        _ = batch.Property(b => b.ClaimedAtUtc);
        _ = batch.Property(b => b.RequestJson)
            .HasMaxLength(8192);
        _ = batch.HasIndex(b => new { b.Status, b.ClaimedAtUtc });

        var item = modelBuilder.Entity<DirectoryQueueBatchItem>();
        _ = item.HasKey(i => i.Id);
        _ = item.Property(i => i.InputPath)
            .IsRequired()
            .HasMaxLength(2048);
        _ = item.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(32);
        _ = item.Property(i => i.Reason)
            .HasMaxLength(512);
        _ = item.HasOne(i => i.Batch)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = item.HasIndex(i => new { i.BatchId, i.Status, i.Id });
    }
}
