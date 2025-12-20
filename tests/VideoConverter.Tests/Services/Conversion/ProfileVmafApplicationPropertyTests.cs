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
/// Property-based tests for VMAF profile application in EffectiveConversionRequestBuilder.
/// Feature: vmaf-quality-scoring, Property 8: Profile VMAF setting application.
/// </summary>
public class ProfileVmafApplicationPropertyTests
{
    private static readonly EffectiveConversionRequestBuilder Builder
        = new(new Mock<ILogger<EffectiveConversionRequestBuilder>>().Object);

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 8: Profile VMAF setting application.
    /// Validates: Requirements 6.2, 6.3.
    /// For any profile with a VmafEnabled setting and any conversion create request with an explicit VmafEnabled,
    /// the resulting conversion's VmafEnabled should equal the request's value (override).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RequestVmafEnabled_OverridesProfileValue()
    {
        var inputGen = from profileVmaf in ArbMap.Default.GeneratorFor<bool>()
                       from requestVmaf in ArbMap.Default.GeneratorFor<bool>()
                       select (ProfileVmaf: profileVmaf, RequestVmaf: requestVmaf);

        return Prop.ForAll(inputGen.ToArbitrary(), input =>
        {
            var request = new ConversionCreateDtoBuilder()
                .WithInputPath("input.mp4")
                .WithOutputDirectory("/output")
                .WithVmafEnabled(input.RequestVmaf)
                .Build();

            var profile = new Profile
            {
                VmafEnabled = input.ProfileVmaf,
            };

            var effective = Builder.Build(request, profile);

            // Request value should override profile value
            return (effective.VmafEnabled == input.RequestVmaf)
                .Label($"Expected request VmafEnabled '{input.RequestVmaf}' but got '{effective.VmafEnabled}'");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 8: Profile VMAF setting application.
    /// Validates: Requirements 6.2.
    /// For any profile with a VmafEnabled setting and a conversion create request without explicit VmafEnabled,
    /// the resulting conversion's VmafEnabled should equal the profile's value (inheritance).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProfileVmafEnabled_InheritedWhenRequestNotSpecified()
    {
        return Prop.ForAll(ArbMap.Default.GeneratorFor<bool>().ToArbitrary(), profileVmaf =>
        {
            var request = new ConversionCreateDtoBuilder()
                .WithInputPath("input.mp4")
                .WithOutputDirectory("/output")
                .WithVmafEnabled(null) // Not specified
                .Build();

            var profile = new Profile
            {
                VmafEnabled = profileVmaf,
            };

            var effective = Builder.Build(request, profile);

            // Profile value should be inherited when request doesn't specify
            return (effective.VmafEnabled == profileVmaf)
                .Label($"Expected profile VmafEnabled '{profileVmaf}' but got '{effective.VmafEnabled}'");
        });
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 8: Profile VMAF setting application.
    /// Validates: Requirements 6.2, 6.3.
    /// For any conversion create request without a profile and without explicit VmafEnabled,
    /// the resulting conversion's VmafEnabled should be null (default).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property VmafEnabled_NullWhenNoProfileAndNoRequest()
    {
        var request = new ConversionCreateDtoBuilder()
            .WithInputPath("input.mp4")
            .WithOutputDirectory("/output")
            .WithVmafEnabled(null)
            .Build();

        var effective = Builder.Build(request, null);

        // Should be null when neither profile nor request specifies
        effective.VmafEnabled.Should().BeNull();
        return true.ToProperty();
    }

    /// <summary>
    /// Feature: vmaf-quality-scoring, Property 8: Profile VMAF setting application.
    /// Validates: Requirements 6.3.
    /// For any conversion create request with explicit VmafEnabled and no profile,
    /// the resulting conversion's VmafEnabled should equal the request's value.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RequestVmafEnabled_UsedWhenNoProfile()
    {
        return Prop.ForAll(ArbMap.Default.GeneratorFor<bool>().ToArbitrary(), requestVmaf =>
        {
            var request = new ConversionCreateDtoBuilder()
                .WithInputPath("input.mp4")
                .WithOutputDirectory("/output")
                .WithVmafEnabled(requestVmaf)
                .Build();

            var effective = Builder.Build(request, null);

            // Request value should be used when no profile
            return (effective.VmafEnabled == requestVmaf)
                .Label($"Expected request VmafEnabled '{requestVmaf}' but got '{effective.VmafEnabled}'");
        });
    }
}
