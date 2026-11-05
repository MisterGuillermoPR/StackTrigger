using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.UnitTests.IntentSignals.Commands;

public sealed class SaveIntentSignalCommandHandlerTests
{
    private readonly IIntentSignalRepository _repositoryMock;
    private readonly ILogger<SaveIntentSignalCommandHandler> _loggerMock;
    private readonly SaveIntentSignalCommandHandler _handler;

    public SaveIntentSignalCommandHandlerTests()
    {
        _repositoryMock = Substitute.For<IIntentSignalRepository>();
        _loggerMock = Substitute.For<ILogger<SaveIntentSignalCommandHandler>>();
        _handler = new SaveIntentSignalCommandHandler(_repositoryMock, _loggerMock);
    }

    [Fact]
    public async Task Handle_WhenSourceUrlDoesNotExist_ShouldCreateAndPersistIntentSignal()
    {
        // Arrange
        var command = new SaveIntentSignalCommand(
            CompanyName: "Contoso Cloud",
            JobTitle: "Lead DevOps Engineer",
            TargetTechnologies: ["AWS", "Terraform", "Kubernetes"],
            SourceUrl: "https://careers.contoso.com/jobs/991"
        );

        _repositoryMock.ExistsWithSourceUrlAsync(command.SourceUrl, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Saved.Should().BeTrue();
        result.SignalId.Should().NotBeNull();
        result.SignalId.Should().NotBeEmpty();

        await _repositoryMock.Received(1).AddAsync(
            Arg.Is<IntentSignal>(s =>
                s.CompanyName == "Contoso Cloud" &&
                s.JobTitle == "Lead DevOps Engineer" &&
                s.SourceUrl == "https://careers.contoso.com/jobs/991" &&
                s.TargetTechnologies.Count == 3),
            Arg.Any<CancellationToken>());

        await _repositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSourceUrlAlreadyExists_ShouldQuietlySkipAndNotPersist()
    {
        // Arrange
        var command = new SaveIntentSignalCommand(
            CompanyName: "Contoso Cloud",
            JobTitle: "Lead DevOps Engineer",
            TargetTechnologies: ["AWS", "Terraform"],
            SourceUrl: "https://careers.contoso.com/jobs/991"
        );

        _repositoryMock.ExistsWithSourceUrlAsync(command.SourceUrl, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Saved.Should().BeFalse();
        result.SignalId.Should().BeNull();
        result.Reason.Should().Contain("already exists. Skipped duplicate.");

        await _repositoryMock.DidNotReceive().AddAsync(
            Arg.Any<IntentSignal>(),
            Arg.Any<CancellationToken>());

        await _repositoryMock.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }
}

