using StackTrigger.Application.IntentSignals.Commands.SaveIntentSignal;

namespace StackTrigger.Application.Common.Interfaces;

public interface IIntentScraper
{
    Task<IEnumerable<SaveIntentSignalCommand>> ScrapeSignalsAsync(CancellationToken cancellationToken);
}

