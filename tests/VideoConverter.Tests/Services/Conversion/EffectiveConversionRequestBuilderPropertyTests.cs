using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using VideoConverter.Api.Features.Conversions.Services;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Tests.TestUtils.Builders;

namespace VideoConverter.Tests.Services.Conversion;

/// <summary>
/// Property-based tests for EffectiveConversionRequestBuilder.
/// Feature: output-path-template.
/// </summary>
public class EffectiveConversionRequestBuilderPropertyTests
{
    private static readonly EffectiveConversionRequestBuilder Builder
        = new(new Mock<ILogger<EffectiveConversionRequestBuilder>>().Object);

    /// <summary>
    /// Feature: output-path-template, Property 2: Profile template precedence.
    /// Validates: Requirements 1.3, 1.4.
    /// For any conversion request with an output path template and a profile with an output path template,
    /// the effective output path template SHALL be the profile's template.
    /// </summary>
    /// <returns>A property that validates profile template precedence.</returns>
    [Property(MaxTest = 100)]
    public Property ProfileOutputPathTemplate_TakesPrecedence_OverRequestTemplate()
    {
        // Generate non-empty, non-whitespace template strings
        var templateGen = from s in ArbMap.Default.GeneratorFor<NonEmptyString>()
                          where !string.IsNullOrWhiteSpace(s.Get)
                          select s.Get;

        // Generate pairs of profile and request templates
        var inputGen = from profileTemplate in templateGen
                       from requestTemplate in templateGen
                       select (ProfileTemplate: profileTemplate, RequestTemplate: requestTemplate);

        return Prop.ForAll(inputGen.ToArbitrary(), input =>
        {
            var request = new ConversionCreateDtoBuilder()
                .WithInputPath("input.mp4")
                .WithOutputDirectory("/output")
                .WithOutputPathTemplate(input.RequestTemplate)
                .Build();

            var profile = new Profile
            {
                OutputPathTemplate = input.ProfileTemplate,
            };

            var effective = Builder.Build(request, profile);

            // Profile template should take precedence
            return string.Equals(effective.OutputPathTemplate, input.ProfileTemplate, StringComparison.Ordinal)
                .Label($"Expected profile template '{input.ProfileTemplate}' but got '{effective.OutputPathTemplate}'");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 2: Profile template precedence (fallback case).
    /// Validates: Requirements 1.3.
    /// For any conversion request with an output path template and a profile without an output path template,
    /// the effective output path template SHALL be the request's template.
    /// </summary>
    /// <returns>A property that validates request template is used when profile has none.</returns>
    [Property(MaxTest = 100)]
    public Property RequestOutputPathTemplate_UsedWhenProfileHasNone()
    {
        // Generate non-empty, non-whitespace template strings
        var templateGen = from s in ArbMap.Default.GeneratorFor<NonEmptyString>()
                          where !string.IsNullOrWhiteSpace(s.Get)
                          select s.Get;

        // Generate null or whitespace-only profile templates
        var emptyProfileTemplateGen = Gen.Elements<string?>(null, string.Empty, "   ", "\t");

        var inputGen = from requestTemplate in templateGen
                       from profileTemplate in emptyProfileTemplateGen
                       select (RequestTemplate: requestTemplate, ProfileTemplate: profileTemplate);

        return Prop.ForAll(inputGen.ToArbitrary(), input =>
        {
            var request = new ConversionCreateDtoBuilder()
                .WithInputPath("input.mp4")
                .WithOutputDirectory("/output")
                .WithOutputPathTemplate(input.RequestTemplate)
                .Build();

            var profile = new Profile
            {
                OutputPathTemplate = input.ProfileTemplate,
            };

            var effective = Builder.Build(request, profile);

            // Request template should be used when profile has none
            return string.Equals(effective.OutputPathTemplate, input.RequestTemplate, StringComparison.Ordinal)
                .Label($"Expected request template '{input.RequestTemplate}' but got '{effective.OutputPathTemplate}'");
        });
    }

    /// <summary>
    /// Feature: output-path-template, Property 2: Profile template precedence (no profile case).
    /// Validates: Requirements 1.3.
    /// For any conversion request with an output path template and no profile,
    /// the effective output path template SHALL be the request's template.
    /// </summary>
    /// <returns>A property that validates request template is used when no profile.</returns>
    [Property(MaxTest = 100)]
    public Property RequestOutputPathTemplate_UsedWhenNoProfile()
    {
        // Generate non-empty, non-whitespace template strings
        var templateGen = from s in ArbMap.Default.GeneratorFor<NonEmptyString>()
                          where !string.IsNullOrWhiteSpace(s.Get)
                          select s.Get;

        return Prop.ForAll(templateGen.ToArbitrary(), requestTemplate =>
        {
            var request = new ConversionCreateDtoBuilder()
                .WithInputPath("input.mp4")
                .WithOutputDirectory("/output")
                .WithOutputPathTemplate(requestTemplate)
                .Build();

            var effective = Builder.Build(request, null);

            // Request template should be used when no profile
            return string.Equals(effective.OutputPathTemplate, requestTemplate, StringComparison.Ordinal)
                .Label($"Expected request template '{requestTemplate}' but got '{effective.OutputPathTemplate}'");
        });
    }
}
