using FluentAssertions;
using StackTrigger.Domain.Entities;

namespace StackTrigger.Domain.UnitTests.Entities;

public sealed class ClientSubscriptionTests
{
    private const string ValidCompany = "Apex Cloud MSP";
    private const string ValidWebhook = "https://hooks.slack.com/services/T1/B1/K1";
    private readonly List<string> _validKeywords = ["Azure", "Cloud Migration"];

    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateActiveSubscription()
    {
        var client = ClientSubscription.Create(ValidCompany, ValidWebhook, _validKeywords);

        client.Should().NotBeNull();
        client.Id.Should().NotBeEmpty();
        client.CompanyName.Should().Be(ValidCompany);
        client.WebhookUrl.Should().Be(ValidWebhook);
        client.IsActive.Should().BeTrue();
        client.TargetKeywords.Should().BeEquivalentTo(_validKeywords);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCompany_ShouldThrowArgumentException(string? invalidCompany)
    {
        var act = () => ClientSubscription.Create(invalidCompany!, ValidWebhook, _validKeywords);
        act.Should().Throw<ArgumentException>().WithParameterName("companyName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid.com")]
    [InlineData("http://insecure-plain-http.com")]
    [InlineData("https://localhost/internal")]
    [InlineData("https://127.0.0.1/admin")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    public void Create_WithInvalidWebhook_ShouldThrowArgumentException(string? invalidWebhook)
    {
        var act = () => ClientSubscription.Create(ValidCompany, invalidWebhook!, _validKeywords);
        act.Should().Throw<ArgumentException>().WithParameterName("webhookUrl");
    }

    [Fact]
    public void MatchesSignal_WhenSignalSharesKeywords_ShouldReturnTrue()
    {
        var client = ClientSubscription.Create("CyberSec MSP", ValidWebhook, ["CrowdStrike", "SOC2"]);
        var signal = IntentSignal.Create("Target Corp", "CISO", ["SOC2", "AWS"], "https://target.com/job/1");

        var matches = client.MatchesSignal(signal);

        matches.Should().BeTrue();
    }

    [Fact]
    public void MatchesSignal_WhenSignalDoesNotMatchKeywords_ShouldReturnFalse()
    {
        var client = ClientSubscription.Create("CyberSec MSP", ValidWebhook, ["CrowdStrike", "SOC2"]);
        var signal = IntentSignal.Create("Target Corp", "DevOps Engineer", ["GCP", "Kubernetes"], "https://target.com/job/1");

        var matches = client.MatchesSignal(signal);

        matches.Should().BeFalse();
    }

    [Fact]
    public void MatchesSignal_WhenClientIsDeactivated_ShouldReturnFalseEvenIfKeywordsMatch()
    {
        var client = ClientSubscription.Create("CyberSec MSP", ValidWebhook, ["Azure"]);
        client.Deactivate();

        var signal = IntentSignal.Create("Target Corp", "Cloud Architect", ["Azure"], "https://target.com/job/1");

        client.MatchesSignal(signal).Should().BeFalse();
    }
}

