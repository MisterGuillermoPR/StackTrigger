using MediatR;
using Microsoft.Extensions.Logging;
using StackTrigger.Domain.Entities;
using StackTrigger.Domain.Repositories;

namespace StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;

public sealed class SaveIntentSignalCommandHandler
    : IRequestHandler<SaveIntentSignalCommand, SaveIntentSignalResult>
{
    private readonly IIntentSignalRepository _repository;
    private readonly ILogger<SaveIntentSignalCommandHandler> _logger;

    public SaveIntentSignalCommandHandler(
        IIntentSignalRepository repository,
        ILogger<SaveIntentSignalCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SaveIntentSignalResult> Handle(
        SaveIntentSignalCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var exists = await _repository.ExistsWithSourceUrlAsync(request.SourceUrl, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Skipping duplicate intent signal for source URL: {SourceUrl}", request.SourceUrl);
            return SaveIntentSignalResult.SkippedDuplicate(request.SourceUrl);
        }

        var signal = IntentSignal.Create(
            companyName: request.CompanyName,
            jobTitle: request.JobTitle,
            targetTechnologies: request.TargetTechnologies,
            sourceUrl: request.SourceUrl,
            dateDetected: request.DateDetected,
            salaryRange: request.SalaryRange,
            decisionMakerTitle: request.DecisionMakerTitle,
            linkedInSearchUrl: request.LinkedInSearchUrl,
            outreachIcebreaker: request.OutreachIcebreaker
        );

        await _repository.AddAsync(signal, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Persisted enriched intent signal {SignalId} for '{Company}' - Target DM: {DM}",
            signal.Id,
            signal.CompanyName,
            signal.DecisionMakerTitle);

        return SaveIntentSignalResult.Success(signal.Id);
    }
}
