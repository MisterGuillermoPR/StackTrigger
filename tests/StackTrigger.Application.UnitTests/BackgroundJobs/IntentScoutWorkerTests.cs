using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using StackTrigger.Application.Common.Interfaces;
using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;
using StackTrigger.Infrastructure.BackgroundJobs;

namespace StackTrigger.Application.UnitTests.BackgroundJobs;

public sealed class IntentScoutWorkerTests
{
    private readonly IServiceScopeFactory _scopeFactoryMock;
    private readonly IServiceScope _scopeMock;
    private readonly IServiceProvider _serviceProviderMock;
    private readonly IIntentScraper _scraperMock;
    private readonly IMediator _mediatorMock;
    private readonly ILogger<IntentScoutWorker> _loggerMock;

    public IntentScoutWorkerTests()
    {
        _scopeFactoryMock = Substitute.For<IServiceScopeFactory>();
        _scopeMock = Substitute.For<IServiceScope>();
        _serviceProviderMock = Substitute.For<IServiceProvider>();
        _scraperMock = Substitute.For<IIntentScraper>();
        _mediatorMock = Substitute.For<IMediator>();
        _loggerMock = Substitute.For<ILogger<IntentScoutWorker>>();

        _scopeFactoryMock.CreateScope().Returns(_scopeMock);
        _scopeMock.ServiceProvider.Returns(_serviceProviderMock);
        _serviceProviderMock.GetService(typeof(IIntentScraper)).Returns(_scraperMock);
        _serviceProviderMock.GetService(typeof(IMediator)).Returns(_mediatorMock);
    }

    [Fact]
    public async Task Worker_ExecutesScrapeCycleAndDispatchesToMediator()
    {
        // Arrange
        var command1 = new SaveIntentSignalCommand("Alpha Co", "Cloud Engineer", ["AWS"], "https://alpha.com/job/1");
        var command2 = new SaveIntentSignalCommand("Beta Co", "SecOps Lead", ["CrowdStrike"], "https://beta.com/job/2");

        _scraperMock.ScrapeSignalsAsync(Arg.Any<CancellationToken>())
            .Returns([command1, command2]);

        _mediatorMock.Send(command1, Arg.Any<CancellationToken>())
            .Returns(SaveIntentSignalResult.Success(Guid.NewGuid()));
        _mediatorMock.Send(command2, Arg.Any<CancellationToken>())
            .Returns(SaveIntentSignalResult.SkippedDuplicate(command2.SourceUrl));

        var worker = new IntentScoutWorker(_scopeFactoryMock, _loggerMock, TimeSpan.FromHours(1));

        // Act
        await worker.ExecuteScrapeCycleAsync(CancellationToken.None);

        // Assert
        _scopeFactoryMock.Received(1).CreateScope();
        await _scraperMock.Received(1).ScrapeSignalsAsync(Arg.Any<CancellationToken>());
        await _mediatorMock.Received(1).Send(command1, Arg.Any<CancellationToken>());
        await _mediatorMock.Received(1).Send(command2, Arg.Any<CancellationToken>());
    }
}
