using System.Net;
using AwesomeAssertions;

namespace Thrivts.Api.IntegrationTests;

public class ApiSmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiSmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_endpoint_rejects_an_anonymous_caller()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/admin/deals/{Guid.NewGuid()}/advance-status", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
