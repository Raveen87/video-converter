using VideoConverter.Api.Features.Conversions.Dtos;
using VideoConverter.Api.Shared.Domain.ValueObjects;

namespace VideoConverter.Tests.TestUtils.Builders;

public sealed class ConversionCreateDtoBuilder
{
    private string _inputPath = "input.mp4";
    private string _outputDirectory = Path.GetTempPath();
    private int? _crf;
    private int? _audioBitrateKbps;
    private AudioChannels? _audioChannels;
    private int? _resolution;
    private IReadOnlyList<string>? _audioLanguages;
    private IReadOnlyList<string>? _subtitleLanguages;

    public ConversionCreateDtoBuilder WithInputPath(string path)
    {
        _inputPath = path;
        return this;
    }

    public ConversionCreateDtoBuilder WithOutputDirectory(string dir)
    {
        _outputDirectory = dir;
        return this;
    }

    public ConversionCreateDtoBuilder WithCrf(int? crf)
    {
        _crf = crf;
        return this;
    }

    public ConversionCreateDtoBuilder WithAudioBitrateKbps(int? kbps)
    {
        _audioBitrateKbps = kbps;
        return this;
    }

    public ConversionCreateDtoBuilder WithAudioChannels(AudioChannels? channels)
    {
        _audioChannels = channels;
        return this;
    }

    public ConversionCreateDtoBuilder WithResolution(int? height)
    {
        _resolution = height;
        return this;
    }

    public ConversionCreateDtoBuilder WithAudioLanguages(params string[] langs)
    {
        _audioLanguages = langs;
        return this;
    }

    public ConversionCreateDtoBuilder WithSubtitleLanguages(params string[] langs)
    {
        _subtitleLanguages = langs;
        return this;
    }

    public ConversionCreateDto Build()
    {
        return new ConversionCreateDto(
            _inputPath,
            _outputDirectory,
            _crf,
            _audioBitrateKbps,
            _audioChannels,
            _resolution,
            _audioLanguages,
            _subtitleLanguages);
    }
}
