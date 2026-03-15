using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using VideoConverter.Api.Features.Profiles.Api;
using VideoConverter.Api.Shared.Domain.Entities;
using VideoConverter.Common.Models;
using Xunit;

namespace VideoConverter.Tests.Features.Profiles;

public class ProfilePatchingTests
{
    [Fact]
    public void Patch_ShouldUpdate_RequiredVmaf_On_PatchModel()
    {
        // Arrange
        var profile = new Profile
        {
            RequiredVmaf = null,
        };

        var patchModel = new ProfilePatchDto
        {
            RequiredVmaf = profile.RequiredVmaf,
        };

        var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
        patchDoc.Operations.Add(new Operation<ProfilePatchDto>
        {
            op = "replace",
            path = "/RequiredVmaf",
            value = 92.0,
        });

        // Act
        patchDoc.ApplyTo(patchModel);

        // Assert
        patchModel.RequiredVmaf.Should().Be(92.0);
    }

    [Fact]
    public void Patch_ShouldUpdate_RequiredVmaf_With_Int_Value()
    {
        // Arrange
        var profile = new Profile
        {
            RequiredVmaf = null,
        };

        var patchModel = new ProfilePatchDto
        {
            RequiredVmaf = profile.RequiredVmaf,
        };

        // Simulating JSON deserialization where 92 might be parsed as int or other type
        // However, JsonPatchDocument<T> is strongly typed? No, operations are not necessarily.
        // But here we construct it manually.
        // In the real API, the JsonPatchDocument is deserialized from JSON.

        var patchDoc = new JsonPatchDocument<ProfilePatchDto>();
        patchDoc.Operations.Add(new Operation<ProfilePatchDto>
        {
            op = "replace",
            path = "/RequiredVmaf",
            value = 92, // Integer value
        });

        // Act
        patchDoc.ApplyTo(patchModel);

        // Assert
        patchModel.RequiredVmaf.Should().Be(92.0);
    }
}
