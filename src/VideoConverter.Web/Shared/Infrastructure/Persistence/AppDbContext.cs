using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VideoConverter.Web.Shared.Domain.Entities;

namespace VideoConverter.Web.Shared.Infrastructure.Persistence;

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

        // Configure JSON storage for language lists
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var listToJson = new ValueConverter<List<string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v) ? null : JsonSerializer.Deserialize<List<string>>(v!, jsonOptions)!);
        var listComparer = new ValueComparer<List<string>?>(
            (l, r) => (l ?? new List<string>()).SequenceEqual(r ?? new List<string>()),
            v => v == null ? 0 : v.Aggregate(0, (a, s) => HashCode.Combine(a, s == null ? 0 : s.GetHashCode())),
            v => v == null ? null : v.ToList());

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
}
