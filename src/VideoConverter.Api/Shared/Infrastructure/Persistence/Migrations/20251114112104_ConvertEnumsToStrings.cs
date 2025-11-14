using Microsoft.EntityFrameworkCore.Migrations;

namespace VideoConverter.Api.Shared.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumsToStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert existing integer enum values to strings before changing column types
            // ConversionStatus: 0→"Pending", 1→"Running", 2→"Completed", 3→"Failed"
            _ = migrationBuilder.Sql(@"
                UPDATE Conversions
                SET Status = CASE Status
                    WHEN 0 THEN 'Pending'
                    WHEN 1 THEN 'Running'
                    WHEN 2 THEN 'Completed'
                    WHEN 3 THEN 'Failed'
                    ELSE 'Pending'
                END
            ");

            // AudioChannels: 20→"Stereo20", 51→"Surround51"
            migrationBuilder.Sql(@"
                UPDATE Conversions
                SET AudioChannels = CASE AudioChannels
                    WHEN 20 THEN 'Stereo20'
                    WHEN 51 THEN 'Surround51'
                    ELSE NULL
                END
            ");

            migrationBuilder.Sql(@"
                UPDATE Profiles
                SET AudioChannels = CASE AudioChannels
                    WHEN 20 THEN 'Stereo20'
                    WHEN 51 THEN 'Surround51'
                    ELSE NULL
                END
            ");

            // SQLite doesn't support ALTER COLUMN, so we need to recreate the tables
            // For Conversions table
            migrationBuilder.Sql(@"
                CREATE TABLE Conversions_new (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InputPath TEXT NOT NULL,
                    OutputDirectory TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    Rank TEXT,
                    Status TEXT NOT NULL,
                    Crf INTEGER,
                    AudioBitrateKbps INTEGER,
                    AudioChannels TEXT,
                    Resolution INTEGER,
                    SelectedAudioStreamIndicesCsv TEXT,
                    SelectedSubtitleStreamIndicesCsv TEXT,
                    SelectedExternalSubtitlePathsCsv TEXT,
                    DetectedCrop TEXT,
                    MetadataBasedFilename TEXT
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO Conversions_new (
                    Id, InputPath, OutputDirectory, CreatedAtUtc, Rank, Status,
                    Crf, AudioBitrateKbps, AudioChannels, Resolution,
                    SelectedAudioStreamIndicesCsv, SelectedSubtitleStreamIndicesCsv,
                    SelectedExternalSubtitlePathsCsv, DetectedCrop, MetadataBasedFilename
                )
                SELECT
                    Id, InputPath, OutputDirectory, CreatedAtUtc, Rank, Status,
                    Crf, AudioBitrateKbps, AudioChannels, Resolution,
                    SelectedAudioStreamIndicesCsv, SelectedSubtitleStreamIndicesCsv,
                    SelectedExternalSubtitlePathsCsv, DetectedCrop, MetadataBasedFilename
                FROM Conversions
            ");

            migrationBuilder.Sql("DROP TABLE Conversions");
            migrationBuilder.Sql("ALTER TABLE Conversions_new RENAME TO Conversions");

            // For Profiles table
            migrationBuilder.Sql(@"
                CREATE TABLE Profiles_new (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OutputDirectory TEXT,
                    Crf INTEGER,
                    AudioBitrateKbps INTEGER,
                    AudioChannels TEXT,
                    Resolution INTEGER,
                    MetadataBasedFilename TEXT,
                    OutputFilenameTemplate TEXT,
                    AudioLanguages TEXT,
                    SubtitleLanguages TEXT
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO Profiles_new (
                    Id, OutputDirectory, Crf, AudioBitrateKbps, AudioChannels,
                    Resolution, MetadataBasedFilename, OutputFilenameTemplate,
                    AudioLanguages, SubtitleLanguages
                )
                SELECT
                    Id, OutputDirectory, Crf, AudioBitrateKbps, AudioChannels,
                    Resolution, MetadataBasedFilename, OutputFilenameTemplate,
                    AudioLanguages, SubtitleLanguages
                FROM Profiles
            ");

            migrationBuilder.Sql("DROP TABLE Profiles");
            migrationBuilder.Sql("ALTER TABLE Profiles_new RENAME TO Profiles");

            // Recreate indexes
            migrationBuilder.Sql("CREATE INDEX IX_Conversions_Status_Rank ON Conversions(Status, Rank)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert string enum values back to integers
            migrationBuilder.Sql(@"
                UPDATE Conversions
                SET Status = CASE Status
                    WHEN 'Pending' THEN 0
                    WHEN 'Running' THEN 1
                    WHEN 'Completed' THEN 2
                    WHEN 'Failed' THEN 3
                    ELSE 0
                END
            ");

            migrationBuilder.Sql(@"
                UPDATE Conversions
                SET AudioChannels = CASE AudioChannels
                    WHEN 'Stereo20' THEN 20
                    WHEN 'Surround51' THEN 51
                    ELSE NULL
                END
            ");

            migrationBuilder.Sql(@"
                UPDATE Profiles
                SET AudioChannels = CASE AudioChannels
                    WHEN 'Stereo20' THEN 20
                    WHEN 'Surround51' THEN 51
                    ELSE NULL
                END
            ");

            // Recreate tables with integer columns
            migrationBuilder.Sql(@"
                CREATE TABLE Conversions_new (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InputPath TEXT NOT NULL,
                    OutputDirectory TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    Rank TEXT,
                    Status INTEGER NOT NULL,
                    Crf INTEGER,
                    AudioBitrateKbps INTEGER,
                    AudioChannels INTEGER,
                    Resolution INTEGER,
                    SelectedAudioStreamIndicesCsv TEXT,
                    SelectedSubtitleStreamIndicesCsv TEXT,
                    SelectedExternalSubtitlePathsCsv TEXT,
                    DetectedCrop TEXT,
                    MetadataBasedFilename TEXT
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO Conversions_new (
                    Id, InputPath, OutputDirectory, CreatedAtUtc, Rank, Status,
                    Crf, AudioBitrateKbps, AudioChannels, Resolution,
                    SelectedAudioStreamIndicesCsv, SelectedSubtitleStreamIndicesCsv,
                    SelectedExternalSubtitlePathsCsv, DetectedCrop, MetadataBasedFilename
                )
                SELECT
                    Id, InputPath, OutputDirectory, CreatedAtUtc, Rank, Status,
                    Crf, AudioBitrateKbps, AudioChannels, Resolution,
                    SelectedAudioStreamIndicesCsv, SelectedSubtitleStreamIndicesCsv,
                    SelectedExternalSubtitlePathsCsv, DetectedCrop, MetadataBasedFilename
                FROM Conversions
            ");

            migrationBuilder.Sql("DROP TABLE Conversions");
            migrationBuilder.Sql("ALTER TABLE Conversions_new RENAME TO Conversions");

            migrationBuilder.Sql(@"
                CREATE TABLE Profiles_new (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OutputDirectory TEXT,
                    Crf INTEGER,
                    AudioBitrateKbps INTEGER,
                    AudioChannels INTEGER,
                    Resolution INTEGER,
                    MetadataBasedFilename TEXT,
                    OutputFilenameTemplate TEXT,
                    AudioLanguages TEXT,
                    SubtitleLanguages TEXT
                )
            ");

            migrationBuilder.Sql(@"
                INSERT INTO Profiles_new (
                    Id, OutputDirectory, Crf, AudioBitrateKbps, AudioChannels,
                    Resolution, MetadataBasedFilename, OutputFilenameTemplate,
                    AudioLanguages, SubtitleLanguages
                )
                SELECT
                    Id, OutputDirectory, Crf, AudioBitrateKbps, AudioChannels,
                    Resolution, MetadataBasedFilename, OutputFilenameTemplate,
                    AudioLanguages, SubtitleLanguages
                FROM Profiles
            ");

            migrationBuilder.Sql("DROP TABLE Profiles");
            migrationBuilder.Sql("ALTER TABLE Profiles_new RENAME TO Profiles");

            // Recreate indexes
            migrationBuilder.Sql("CREATE INDEX IX_Conversions_Status_Rank ON Conversions(Status, Rank)");
        }
    }
}
