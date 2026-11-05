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

        // Check if signal with this source URL already exists to avoid redundant processing/constraint violations
        var exists = await _repository.ExistsWithSourceUrlAsync(request.SourceUrl, cancellationToken);
        if (exists)
        {
            _logger.LogInformation(
                "Skipping duplicate intent signal for source URL: {SourceUrl}",
                request.SourceUrl);

            return SaveIntentSignalResult.SkippedDuplicate(request.SourceUrl);
        }

        // Instantiate using the Domain entity static factory method (encapsulates validation)
        var signal = IntentSignal.Create(
            companyName: request.CompanyName,
            jobTitle: request.JobTitle,
            targetTechnologies: request.TargetTechnologies,
            sourceUrl: request.SourceUrl,
            dateDetected: request.DateDetected
        );

        await _repository.AddAsync(signal, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Persisted intent signal {SignalId} for company '{CompanyName}' - detected tech: [{Technologies}]",
            signal.Id,
            signal.CompanyName,
            string.Join(", ", signal.TargetTechnologies));

        return SaveIntentSignalResult.Success(signal.Id);
    }
}

