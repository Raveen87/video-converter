using System.Net;
using FluentAssertions;
using VideoConverter.Web.Common.Exceptions;

namespace VideoConverter.Tests.Exceptions;

public class ApiErrorMapperTests
{
    [Fact]
    public void Map_ArgumentException_ReturnsValidationApiError()
    {
        var mapper = new ApiErrorMapper();
        var ex = new ArgumentException("bad");

        var apiError = mapper.Map(ex);
        var status = mapper.GetStatusCode(apiError);
        var body = mapper.ToResponse(apiError);

        _ = status.Should().Be((int)HttpStatusCode.BadRequest);
        _ = body["statusCode"].Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public void GetStatusCode_DomainValidationException_Returns400()
    {
        var mapper = new ApiErrorMapper();
        var validation = new DomainValidationException("err", "user");

        var status = mapper.GetStatusCode(validation);
        _ = status.Should().Be((int)HttpStatusCode.BadRequest);
    }
}


