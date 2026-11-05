using MediatR;

namespace StackTrigger.Application.IntentSignals.Commands.ProcessPendingSignals;

public sealed record ProcessPendingSignalsCommand(
    string? CustomWebhookUrl = null,
    int? BatchLimit = null
) : IRequest<ProcessPendingSignalsResult>;

public sealed record ProcessPendingSignalsResult(
    int TotalProcessed,
    int SuccessfullyDispatched,
    int FailedCount
);

