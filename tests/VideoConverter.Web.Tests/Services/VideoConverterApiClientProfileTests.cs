using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using VideoConverter.Common.Models;
using VideoConverter.Web.Models;
using VideoConverter.Web.Services;
using Xunit;

namespace VideoConverter.Web.Tests.Services;

public sealed class VideoConverterApiClientProfileTests
{
    [Fact]
    public async Task UpdateProfileAsync_WithRequiredVmafChange_IncludesItInPatch()
    {
        // Arrange
        var profileId = 1;
        var patch = new ProfilePatchDto
        {
            RequiredVmaf = 95.5,
        };
        var changedFields = new HashSet<string>(StringComparer.Ordinal) { nameof(ProfilePatchDto.RequiredVmaf) };
        var request = new ProfilePatchRequest(patch, changedFields);

        var expectedResponse = new ProfileDto(
            profileId, "Test Profile", null, null, null, null, null, null, null, null, null, true, 95.5);

        var handler = new Mock<HttpMessageHandler>();
        string? capturedRequestBody = null;

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.ToString().EndsWith($"api/profiles/{profileId}", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Content != null)
                {
                    using var reader = new StreamReader(req.Content.ReadAsStream());
                    capturedRequestBody = reader.ReadToEnd();
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        // Act
        var result = await client.UpdateProfileAsync(profileId, request);

        // Assert
        result.Should().NotBeNull();
        result.RequiredVmaf.Should().Be(95.5);

        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("\"op\":\"replace\"");
        capturedRequestBody.Should().Contain("\"path\":\"/RequiredVmaf\"");
        capturedRequestBody.Should().Contain("\"value\":95.5");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithVmafEnabledChange_IncludesItInPatch()
    {
        // Arrange
        var profileId = 1;
        var patch = new ProfilePatchDto
        {
            VmafEnabled = true,
        };
        var changedFields = new HashSet<string>(StringComparer.Ordinal) { nameof(ProfilePatchDto.VmafEnabled) };
        var request = new ProfilePatchRequest(patch, changedFields);

        var expectedResponse = new ProfileDto(
            profileId, "Test Profile", null, null, null, null, null, null, null, null, null, true, null);

        var handler = new Mock<HttpMessageHandler>();
        string? capturedRequestBody = null;

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.ToString().EndsWith($"api/profiles/{profileId}", StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Content != null)
                {
                    using var reader = new StreamReader(req.Content.ReadAsStream());
                    capturedRequestBody = reader.ReadToEnd();
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse),
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://localhost"),
        };
        var client = new VideoConverterApiClient(httpClient, NullLogger<VideoConverterApiClient>.Instance);

        // Act
        await client.UpdateProfileAsync(profileId, request);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain("\"path\":\"/VmafEnabled\"");
        capturedRequestBody.Should().Contain("\"value\":true");
    }
}
