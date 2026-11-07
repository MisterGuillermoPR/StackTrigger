using MediatR;

namespace StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;

public sealed record SaveIntentSignalCommand(
    string CompanyName,
    string JobTitle,
    IReadOnlyList<string> TargetTechnologies,
    string SourceUrl,
    DateTimeOffset? DateDetected = null,
    string? SalaryRange = null,
    string? DecisionMakerTitle = null,
    string? LinkedInSearchUrl = null,
    string? OutreachIcebreaker = null
) : IRequest<SaveIntentSignalResult>;

public sealed record SaveIntentSignalResult(
    bool Saved,
    Guid? SignalId,
    string Reason
)
{
    public static SaveIntentSignalResult Success(Guid signalId) =>
        new(true, signalId, "Signal successfully created and persisted.");

    public static SaveIntentSignalResult SkippedDuplicate(string sourceUrl) =>
        new(false, null, $"Signal from '{sourceUrl}' already exists. Skipped duplicate.");
}
