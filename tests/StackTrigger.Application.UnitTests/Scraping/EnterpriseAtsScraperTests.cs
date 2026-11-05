using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackTrigger.Infrastructure.Scraping;

namespace StackTrigger.Application.UnitTests.Scraping;

public sealed class EnterpriseAtsScraperTests
{
    private readonly ILogger<EnterpriseAtsScraper> _loggerMock = Substitute.For<ILogger<EnterpriseAtsScraper>>();

    [Fact]
    public async Task ScrapeSignalsAsync_ShouldParseGreenhouseJsonApiPristinely()
    {
        // Arrange
        const string fakeGreenhouseResponse = """
        {
          "jobs": [
            {
              "id": 50123,
              "title": "Principal Cloud Migration Architect",
              "absolute_url": "https://boards.greenhouse.io/cloudflare/jobs/50123",
              "content": "&lt;p&gt;Leading our massive migration to Azure and implementing SOC2 compliance across core services.&lt;/p&gt;"
            },
            {
              "id": 50124,
              "title": "Executive Assistant",
              "absolute_url": "https://boards.greenhouse.io/cloudflare/jobs/50124",
              "content": "&lt;p&gt;Calendar management and travel bookings.&lt;/p&gt;"
            }
          ]
        }
        """;

        var fakeHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(fakeGreenhouseResponse)
        });

        var httpClient = new HttpClient(fakeHandler);
        var options = Options.Create(new AtsScraperOptions
        {
            GreenhouseCompanies = ["cloudflare"],
            LeverCompanies = [],
            TargetUrls = [], // No HTML targets for this isolated test
            MonitoredKeywords = ["Azure", "Cloud Migration", "SOC2"]
        });

        var htmlLogger = Substitute.For<ILogger<AtsHtmlScraper>>();
        var isolatedHtmlScraper = new AtsHtmlScraper(new HttpClient(fakeHandler), options, htmlLogger);

        var scraper = new EnterpriseAtsScraper(httpClient, options, _loggerMock, isolatedHtmlScraper);

        // Act
        var results = (await scraper.ScrapeSignalsAsync(CancellationToken.None)).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].CompanyName.Should().Be("Cloudflare");
        results[0].JobTitle.Should().Be("Principal Cloud Migration Architect");
        results[0].SourceUrl.Should().Be("https://boards.greenhouse.io/cloudflare/jobs/50123");
        results[0].TargetTechnologies.Should().Contain(["Azure", "Cloud Migration", "SOC2"]);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public MockHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) =>
            Task.FromResult(_response);
    }
}
