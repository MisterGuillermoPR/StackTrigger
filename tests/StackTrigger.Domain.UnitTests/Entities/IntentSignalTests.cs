using FluentAssertions;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Enums;

namespace StackTrigger.Domain.UnitTests.Entities;

public sealed class IntentSignalTests
{
    private const string ValidCompany = "Acme Cloud Services";
    private const string ValidJobTitle = "Principal Cloud Architect";
    private readonly List<string> _validTech = ["Azure", ".NET 9", "Cloud Migration"];
    private const string ValidUrl = "https://careers.acme.com/jobs/12345";

    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateIntentSignalWithPendingStatus()
    {
        // Arrange
        var detectedAt = DateTimeOffset.UtcNow;

        // Act
        var signal = IntentSignal.Create(ValidCompany, ValidJobTitle, _validTech, ValidUrl, detectedAt);

        // Assert
        signal.Should().NotBeNull();
        signal.Id.Should().NotBeEmpty();
        signal.CompanyName.Should().Be(ValidCompany);
        signal.JobTitle.Should().Be(ValidJobTitle);
        signal.SourceUrl.Should().Be(ValidUrl);
        signal.DateDetected.Should().Be(detectedAt);
        signal.DispatchStatus.Should().Be(DispatchStatus.Pending);
        signal.TargetTechnologies.Should().BeEquivalentTo(_validTech);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCompanyName_ShouldThrowArgumentException(string? invalidCompany)
    {
        // Act
        var act = () => IntentSignal.Create(invalidCompany!, ValidJobTitle, _validTech, ValidUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("companyName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidJobTitle_ShouldThrowArgumentException(string? invalidJobTitle)
    {
        // Act
        var act = () => IntentSignal.Create(ValidCompany, invalidJobTitle!, _validTech, ValidUrl);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("jobTitle");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://invalid-protocol.com/job/1")]
    public void Create_WithInvalidSourceUrl_ShouldThrowArgumentException(string? invalidUrl)
    {
        // Act
        var act = () => IntentSignal.Create(ValidCompany, ValidJobTitle, _validTech, invalidUrl!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sourceUrl");
    }

    [Fact]
    public void Create_WithNullOrEmptyTechnologies_ShouldThrowArgumentException()
    {
        // Act & Assert for null
        var actNull = () => IntentSignal.Create(ValidCompany, ValidJobTitle, null!, ValidUrl);
        actNull.Should().Throw<ArgumentNullException>()
            .WithParameterName("targetTechnologies");

        // Act & Assert for empty list
        var actEmpty = () => IntentSignal.Create(ValidCompany, ValidJobTitle, new List<string>(), ValidUrl);
        actEmpty.Should().Throw<ArgumentException>()
            .WithParameterName("targetTechnologies");

        // Act & Assert for whitespace list
        var actWhitespace = () => IntentSignal.Create(ValidCompany, ValidJobTitle, new List<string> { "   ", "" }, ValidUrl);
        actWhitespace.Should().Throw<ArgumentException>()
            .WithParameterName("targetTechnologies");
    }

    [Fact]
    public void Create_WithDuplicateAndWhitespaceTech_ShouldSanitizeAndDeduplicate()
    {
        // Arrange
        var messyTech = new List<string> { " Azure ", "azure", ".NET 9", ".net 9 ", "Kubernetes" };

        // Act
        var signal = IntentSignal.Create(ValidCompany, ValidJobTitle, messyTech, ValidUrl);

        // Assert
        signal.TargetTechnologies.Should().HaveCount(3);
        signal.TargetTechnologies.Should().Contain(["Azure", ".NET 9", "Kubernetes"]);
    }

    [Fact]
    public void MarkAsDispatched_ShouldUpdateDispatchStatusToDispatched()
    {
        // Arrange
        var signal = IntentSignal.Create(ValidCompany, ValidJobTitle, _validTech, ValidUrl);

        // Act
        signal.MarkAsDispatched();

        // Assert
        signal.DispatchStatus.Should().Be(DispatchStatus.Dispatched);
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateDispatchStatusToFailed()
    {
        // Arrange
        var signal = IntentSignal.Create(ValidCompany, ValidJobTitle, _validTech, ValidUrl);

        // Act
        signal.MarkAsFailed();

        // Assert
        signal.DispatchStatus.Should().Be(DispatchStatus.Failed);
    }

    [Fact]
    public void ResetToPending_ShouldUpdateDispatchStatusBackToPending()
    {
        // Arrange
        var signal = IntentSignal.Create(ValidCompany, ValidJobTitle, _validTech, ValidUrl);
        signal.MarkAsFailed();

        // Act
        signal.ResetToPending();

        // Assert
        signal.DispatchStatus.Should().Be(DispatchStatus.Pending);
    }
}

