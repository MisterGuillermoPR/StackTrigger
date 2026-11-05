using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using StackTrigger.Infrastructure.Scraping;

namespace StackTrigger.Application.UnitTests.Scraping;

public sealed class AtsHtmlScraperTests
{
    private readonly ILogger<AtsHtmlScraper> _loggerMock;
    private readonly AtsScraperOptions _options;

    public AtsHtmlScraperTests()
    {
        _loggerMock = Substitute.For<ILogger<AtsHtmlScraper>>();
        _options = new AtsScraperOptions
        {
            TargetUrls = ["https://careers.techcorp.com/jobs"],
            MonitoredKeywords = ["Azure", "Cloud Migration", ".NET", "Clean Architecture", "SOC2"]
        };
    }

    [Fact]
    public async Task ScrapeSignalsAsync_WhenMatchingJobFoundInHtml_ShouldExtractAndReturnCommand()
    {
        // Arrange
        var recentDate = DateTimeOffset.UtcNow.ToString("O");
        var fakeHtml = $$"""
            <!DOCTYPE html>
            <html>
            <head><title>Careers at CyberScale</title></head>
            <body>
                <div class="job-board">
                    <article class="job-posting" data-job-id="101">
                        <h2 class="job-title">Senior Enterprise Cloud Architect</h2>
                        <time datetime="{{recentDate}}">Recently posted</time>
                        <span class="company-name">CyberScale MSP</span>
                        <a href="https://careers.techcorp.com/jobs/101">View Job</a>
                        <div class="job-description">
                            We are seeking a seasoned architect to lead our massive Cloud Migration from on-prem to Azure.
                            Experience with modern .NET, Clean Architecture, and obtaining SOC2 compliance is required.
                        </div>
                    </article>
                    <article class="job-posting" data-job-id="102">
                        <h2 class="job-title">Office Operations Assistant</h2>
                        <time datetime="{{recentDate}}">Recently posted</time>
                        <span class="company-name">CyberScale MSP</span>
                        <a href="https://careers.techcorp.com/jobs/102">View Job</a>
                        <div class="job-description">
                            Handle front desk reception, schedule meetings, and order office snacks.
                        </div>
                    </article>
                </div>
            </body>
            </html>
            """;

        var fakeHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(fakeHtml)
        });

        var httpClient = new HttpClient(fakeHandler);
        var optionsMock = Options.Create(_options);
        var scraper = new AtsHtmlScraper(httpClient, optionsMock, _loggerMock);

        // Act
        var results = (await scraper.ScrapeSignalsAsync(CancellationToken.None)).ToList();

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(1); // The second job has no relevant keywords

        var signal = results[0];
        signal.CompanyName.Should().Be("CyberScale MSP");
        signal.JobTitle.Should().Be("Senior Enterprise Cloud Architect");
        signal.SourceUrl.Should().Be("https://careers.techcorp.com/jobs/101");
        signal.TargetTechnologies.Should().Contain([
            "Azure",
            "Cloud Migration",
            ".NET",
            "Clean Architecture",
            "SOC2"
        ]);
    }

    [Fact]
    public async Task ParseHtmlContentAsync_SingleJobDocument_ShouldExtractCorrectly()
    {
        // Arrange
        var recentDate = DateTimeOffset.UtcNow.ToString("O");
        var fakeSingleJobHtml = $$"""
            <!DOCTYPE html>
            <html>
            <body>
                <h1 class="job-title">Lead Platform Security Engineer</h1>
                <time datetime="{{recentDate}}">Recently posted</time>
                <div class="company-name">DefendLogix</div>
                <div class="job-description">
                    Leading our Azure zero-trust rollout and achieving SOC2 certification across all cloud assets.
                </div>
            </body>
            </html>
            """;

        var fakeHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(fakeSingleJobHtml)
        });

        var httpClient = new HttpClient(fakeHandler);
        var optionsMock = Options.Create(_options);
        var scraper = new AtsHtmlScraper(httpClient, optionsMock, _loggerMock);

        // Act
        var results = await scraper.ParseHtmlContentAsync(fakeSingleJobHtml, "https://defendlogix.com/careers/lead-sec");

        // Assert
        results.Should().HaveCount(1);
        results[0].CompanyName.Should().Be("DefendLogix");
        results[0].JobTitle.Should().Be("Lead Platform Security Engineer");
        results[0].SourceUrl.Should().Be("https://defendlogix.com/careers/lead-sec");
        results[0].TargetTechnologies.Should().Contain(["Azure", "SOC2"]);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}

