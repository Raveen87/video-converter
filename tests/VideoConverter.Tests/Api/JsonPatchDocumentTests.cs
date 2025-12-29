using System.Text.Json;
using FluentAssertions;
using VideoConverter.Api.Shared.Infrastructure.Serialization;

namespace VideoConverter.Tests.Api;

public class JsonPatchDocumentTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void JsonPatchDocument_Serializes_ToJsonArray()
    {
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("John"),
        });

        var json = JsonSerializer.Serialize(patchDoc, _options);

        json.Should().Be("""[{"op":"replace","path":"/name","value":"John"}]""");
    }

    [Fact]
    public void JsonPatchDocument_Deserializes_FromJsonArray()
    {
        var json = string.Empty + """[{"op":"replace","path":"/name","value":"John"}]""";

        var patchDoc = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        patchDoc.Should().NotBeNull();
        patchDoc!.Operations.Should().HaveCount(1);
        patchDoc.Operations[0].Op.Should().Be("replace");
        patchDoc.Operations[0].Path.Should().Be("/name");
        patchDoc.Operations[0].Value.Should().NotBeNull();
        patchDoc.Operations[0].Value!.GetValue<string>().Should().Be("John");
    }

    [Fact]
    public void JsonPatchDocument_RoundTrips_WithSingleOperation()
    {
        var original = new JsonPatchDocument<TestModel>();
        original.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode(30),
        });

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Operations.Should().HaveCount(1);
        deserialized.Operations[0].Op.Should().Be("replace");
        deserialized.Operations[0].Path.Should().Be("/age");
        deserialized.Operations[0].Value!.GetValue<int>().Should().Be(30);
    }

    [Fact]
    public void JsonPatchDocument_RoundTrips_WithMultipleOperations()
    {
        var original = new JsonPatchDocument<TestModel>();
        original.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Alice"),
        });
        original.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode(25),
        });
        original.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/isActive",
            Value = JsonSerializer.SerializeToNode(true),
        });

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Operations.Should().HaveCount(3);
        deserialized.Operations[0].Value!.GetValue<string>().Should().Be("Alice");
        deserialized.Operations[1].Value!.GetValue<int>().Should().Be(25);
        deserialized.Operations[2].Value!.GetValue<bool>().Should().Be(true);
    }

    [Fact]
    public void JsonPatchDocument_Serializes_EmptyDocument()
    {
        var patchDoc = new JsonPatchDocument<TestModel>();

        var json = JsonSerializer.Serialize(patchDoc, _options);

        json.Should().Be("[]");
    }

    [Fact]
    public void JsonPatchDocument_Deserializes_EmptyArray()
    {
        var json = string.Empty + "[]";

        var patchDoc = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        patchDoc.Should().NotBeNull();
        patchDoc!.Operations.Should().BeEmpty();
    }

    [Fact]
    public void JsonPatchOperation_Applies_WithSimplePath()
    {
        var model = new TestModel { Name = "Original" };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Updated"),
        };

        operation.Apply(model);

        model.Name.Should().Be("Updated");
    }

    [Fact]
    public void JsonPatchOperation_Applies_WithPathWithoutLeadingSlash()
    {
        var model = new TestModel { Name = "Original" };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "name",
            Value = JsonSerializer.SerializeToNode("Updated"),
        };

        operation.Apply(model);

        model.Name.Should().Be("Updated");
    }

    [Fact]
    public void JsonPatchOperation_Applies_WithCaseInsensitivePath()
    {
        var model = new TestModel { Name = "Original" };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/NAME",
            Value = JsonSerializer.SerializeToNode("Updated"),
        };

        operation.Apply(model);

        model.Name.Should().Be("Updated");
    }

    [Fact]
    public void JsonPatchOperation_IgnoresOperation_WithEmptyPath()
    {
        var model = new TestModel { Name = "Original" };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = string.Empty,
            Value = JsonSerializer.SerializeToNode("Updated"),
        };

        operation.Apply(model);

        model.Name.Should().Be("Original");
    }

    [Fact]
    public void JsonPatchOperation_IgnoresOperation_WithSlashOnlyPath()
    {
        var model = new TestModel { Name = "Original" };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/",
            Value = JsonSerializer.SerializeToNode("Updated"),
        };

        operation.Apply(model);

        model.Name.Should().Be("Original");
    }

    [Fact]
    public void JsonPatchOperation_ThrowsException_WhenOpIsEmpty()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = string.Empty,
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Value"),
        };

        var act = () => operation.Apply(model);

        act.Should().Throw<JsonPatchException>()
            .WithMessage("JSON Patch operation 'op' must be specified. Supported operations: 'replace'.");
    }

    [Fact]
    public void JsonPatchOperation_ThrowsException_WhenOpIsWhitespace()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "   ",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Value"),
        };

        var act = () => operation.Apply(model);

        act.Should().Throw<JsonPatchException>()
            .WithMessage("JSON Patch operation 'op' must be specified. Supported operations: 'replace'.");
    }

    [Fact]
    public void JsonPatchOperation_ThrowsException_WhenOpIsUnsupported()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "add",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Value"),
        };

        var act = () => operation.Apply(model);

        act.Should().Throw<JsonPatchException>()
            .WithMessage("Unsupported JSON Patch operation 'add'. Supported operations: 'replace'.");
    }

    [Fact]
    public void JsonPatchOperation_ThrowsException_WhenPropertyNotFound()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/nonExistentProperty",
            Value = JsonSerializer.SerializeToNode("Value"),
        };

        var act = () => operation.Apply(model);

        act.Should().Throw<JsonPatchException>()
            .WithMessage("Property 'nonExistentProperty' not found on type 'TestModel'.");
    }

    [Fact]
    public void JsonPatchOperation_ThrowsException_WhenPropertyIsReadOnly()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/readOnlyProperty",
            Value = JsonSerializer.SerializeToNode("Value"),
        };

        var act = () => operation.Apply(model);

        act.Should().Throw<JsonPatchException>()
            .WithMessage("Property 'readOnlyProperty' is not writable.");
    }

    [Fact]
    public void JsonPatchOperation_ThrowsException_WhenValueTypeIsIncompatible()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode("not a number"),
        };

        var act = () => operation.Apply(model);

        act.Should().Throw<JsonPatchException>()
            .WithMessage("Failed to apply value to property 'age'.*");
    }

    [Fact]
    public void JsonPatchDocument_ThrowsException_WhenDeserializingNonArray()
    {
        var json = """{"op":"replace","path":"/name","value":"John"}""";

        var act = () => JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        act.Should().Throw<JsonException>()
            .WithMessage("Expected JSON Array for Patch Document.");
    }

    [Fact]
    public void JsonPatchOperation_ConvertsValue_ToStringProperty()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("TestName"),
        };

        operation.Apply(model);

        model.Name.Should().Be("TestName");
    }

    [Fact]
    public void JsonPatchOperation_ConvertsValue_ToIntProperty()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode(42),
        };

        operation.Apply(model);

        model.Age.Should().Be(42);
    }

    [Fact]
    public void JsonPatchOperation_ConvertsValue_ToBoolProperty()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/isActive",
            Value = JsonSerializer.SerializeToNode(true),
        };

        operation.Apply(model);

        model.IsActive.Should().BeTrue();
    }

    [Fact]
    public void JsonPatchOperation_ConvertsValue_ToNullableIntProperty()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/score",
            Value = JsonSerializer.SerializeToNode(100),
        };

        operation.Apply(model);

        model.Score.Should().Be(100);
    }

    [Fact]
    public void JsonPatchOperation_SetsNull_ForNullableProperty()
    {
        var model = new TestModel { Score = 100 };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/score",
            Value = null,
        };

        operation.Apply(model);

        model.Score.Should().BeNull();
    }

    [Fact]
    public void JsonPatchOperation_ConvertsValue_ToDateTimeProperty()
    {
        var model = new TestModel();
        var testDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/createdAt",
            Value = JsonSerializer.SerializeToNode(testDate),
        };

        operation.Apply(model);

        model.CreatedAt.Should().Be(testDate);
    }

    [Fact]
    public void JsonPatchOperation_ConvertsValue_ToDoubleProperty()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/rating",
            Value = JsonSerializer.SerializeToNode(4.5),
        };

        operation.Apply(model);

        model.Rating.Should().Be(4.5);
    }

    [Fact]
    public void JsonPatchDocument_ApplyTo_AppliesSingleOperation()
    {
        var model = new TestModel { Name = "Original" };
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Updated"),
        });

        patchDoc.ApplyTo(model);

        model.Name.Should().Be("Updated");
    }

    [Fact]
    public void JsonPatchDocument_ApplyTo_AppliesMultipleOperations()
    {
        var model = new TestModel { Name = "Original", Age = 20, IsActive = false };
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Updated"),
        });
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode(30),
        });
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/isActive",
            Value = JsonSerializer.SerializeToNode(true),
        });

        patchDoc.ApplyTo(model);

        model.Name.Should().Be("Updated");
        model.Age.Should().Be(30);
        model.IsActive.Should().BeTrue();
    }

    [Fact]
    public void JsonPatchDocument_ApplyTo_HandlesEmptyOperations()
    {
        var model = new TestModel { Name = "Original" };
        var patchDoc = new JsonPatchDocument<TestModel>();

        patchDoc.ApplyTo(model);

        model.Name.Should().Be("Original");
    }

    [Fact]
    public void JsonPatchDocument_ProducesValidJsonPatchFormat_WithStringValue()
    {
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("John Doe"),
        });

        var json = JsonSerializer.Serialize(patchDoc, _options);

        // Validate it's proper JSON Patch format (RFC 6902)
        json.Should().Contain("\"op\"");
        json.Should().Contain("\"path\"");
        json.Should().Contain("\"value\"");
        json.Should().Contain("\"replace\"");
        json.Should().Contain("\"/name\"");
        json.Should().Contain("\"John Doe\"");
        json.Should().StartWith("[");
        json.Should().EndWith("]");
    }

    [Fact]
    public void JsonPatchDocument_ProducesValidJsonPatchFormat_WithNumericValue()
    {
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode(42),
        });

        var json = JsonSerializer.Serialize(patchDoc, _options);

        json.Should().Be("""[{"op":"replace","path":"/age","value":42}]""");
    }

    [Fact]
    public void JsonPatchDocument_ProducesValidJsonPatchFormat_WithBooleanValue()
    {
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/isActive",
            Value = JsonSerializer.SerializeToNode(true),
        });

        var json = JsonSerializer.Serialize(patchDoc, _options);

        json.Should().Be("""[{"op":"replace","path":"/isActive","value":true}]""");
    }

    [Fact]
    public void JsonPatchDocument_ProducesValidJsonPatchFormat_WithNullValue()
    {
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/score",
            Value = null,
        });

        var json = JsonSerializer.Serialize(patchDoc, _options);

        json.Should().Be("""[{"op":"replace","path":"/score","value":null}]""");
    }

    [Fact]
    public void JsonPatchDocument_AcceptsValidJsonPatchFormat_FromClient()
    {
        // Simulate JSON sent from a client
        var clientJson = """
        [
            {"op": "replace", "path": "/name", "value": "Jane Smith"},
            {"op": "replace", "path": "/age", "value": 28},
            {"op": "replace", "path": "/isActive", "value": true}
        ]
        """;

        var patchDoc = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(clientJson, _options);

        patchDoc.Should().NotBeNull();
        patchDoc!.Operations.Should().HaveCount(3);
        patchDoc.Operations[0].Op.Should().Be("replace");
        patchDoc.Operations[0].Path.Should().Be("/name");
        patchDoc.Operations[0].Value!.GetValue<string>().Should().Be("Jane Smith");
        patchDoc.Operations[1].Value!.GetValue<int>().Should().Be(28);
        patchDoc.Operations[2].Value!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void JsonPatchDocument_AcceptsMinimalJsonPatchFormat()
    {
        var json = """[{"op":"replace","path":"/name","value":"Test"}]""";

        var patchDoc = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        patchDoc.Should().NotBeNull();
        patchDoc!.Operations.Should().HaveCount(1);
    }

    [Fact]
    public void JsonPatchOperation_HandlesComplexJsonValue()
    {
        var model = new TestModel();
        var complexJson = string.Empty + """{"FirstName":"John","LastName":"Doe","Age":30}""";
        var patchDoc = new JsonPatchDocument<TestModel>();
        patchDoc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = "/metadata",
            Value = JsonSerializer.SerializeToNode(complexJson),
        });

        var json = JsonSerializer.Serialize(patchDoc, _options);
        var deserialized = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Operations[0].Value.Should().NotBeNull();

        // Apply the operation to the model
        deserialized.ApplyTo(model);
        model.Metadata.Should().Be(complexJson);
    }

    [Fact]
    public void JsonPatchOperation_HandlesOperation_WithCaseVariations()
    {
        var model = new TestModel();
        var operation = new JsonPatchOperation
        {
            Op = "REPLACE",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode("Test"),
        };

        operation.Apply(model);

        model.Name.Should().Be("Test");
    }

    [Fact]
    public void JsonPatchDocument_DeserializesOperations_WithExtraWhitespace()
    {
        var json = """
        [
            {
                "op"    :   "replace"  ,
                "path"  :   "/name"    ,
                "value" :   "Test"
            }
        ]
        """;

        var patchDoc = JsonSerializer.Deserialize<JsonPatchDocument<TestModel>>(json, _options);

        patchDoc.Should().NotBeNull();
        patchDoc!.Operations.Should().HaveCount(1);
        patchDoc.Operations[0].Op.Should().Be("replace");
    }

    [Fact]
    public void JsonPatchOperation_AppliesZeroValue_Correctly()
    {
        var model = new TestModel { Age = 100 };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/age",
            Value = JsonSerializer.SerializeToNode(0),
        };

        operation.Apply(model);

        model.Age.Should().Be(0);
    }

    [Fact]
    public void JsonPatchOperation_AppliesEmptyString_Correctly()
    {
        var model = new TestModel { Name = "Original" };
        var operation = new JsonPatchOperation
        {
            Op = "replace",
            Path = "/name",
            Value = JsonSerializer.SerializeToNode(string.Empty),
        };

        operation.Apply(model);

        model.Name.Should().Be(string.Empty);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        public bool IsActive { get; set; }

        public int? Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.MinValue;

        public double Rating { get; set; } = 0.0;

        public string? Metadata { get; set; } = string.Empty;

        public string ReadOnlyProperty { get; } = "ReadOnly";
    }
}
