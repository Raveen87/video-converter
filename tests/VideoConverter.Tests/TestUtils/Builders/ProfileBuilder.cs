using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.TestUtils.Builders;

public sealed class ProfileBuilder
{
    private string? _outputDirectory;
    private int? _crf;
    private int? _audioBitrateKbps;
    private AudioChannels? _audioChannels;
    private int? _resolution;
    private string? _metadataBasedFilename;
    private IReadOnlyList<string>? _audioLanguages;
    private IReadOnlyList<string>? _subtitleLanguages;

    public ProfileBuilder WithOutputDirectory(string? dir)
    {
        _outputDirectory = dir;
        return this;
    }

    public ProfileBuilder WithCrf(int? crf)
    {
        _crf = crf;
        return this;
    }

    public ProfileBuilder WithAudioBitrateKbps(int? kbps)
    {
        _audioBitrateKbps = kbps;
        return this;
    }

    public ProfileBuilder WithAudioChannels(AudioChannels? channels)
    {
        _audioChannels = channels;
        return this;
    }

    public ProfileBuilder WithResolution(int? height)
    {
        _resolution = height;
        return this;
    }

    public ProfileBuilder WithMetadataBasedFilename(string? template)
    {
        _metadataBasedFilename = template;
        return this;
    }

    public ProfileBuilder WithAudioLanguages(params string[] langs)
    {
        _audioLanguages = langs;
        return this;
    }

    public ProfileBuilder WithSubtitleLanguages(params string[] langs)
    {
        _subtitleLanguages = langs;
        return this;
    }

    public Profile Build()
    {
        return new Profile
        {
            OutputDirectory = _outputDirectory,
            Crf = _crf,
            AudioBitrateKbps = _audioBitrateKbps,
            AudioChannels = _audioChannels,
            Resolution = _resolution,
            MetadataBasedFilename = _metadataBasedFilename,
            AudioLanguages = _audioLanguages is null ? null : _audioLanguages.ToList(),
            SubtitleLanguages = _subtitleLanguages is null ? null : _subtitleLanguages.ToList(),
        };
    }
}
