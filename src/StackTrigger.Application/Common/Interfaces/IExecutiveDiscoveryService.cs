namespace StackTrigger.Application.Common.Interfaces;

public sealed record DiscoveredExecutive(
    string FullName,
    string JobTitle,
    string DirectProfileUrl
);

public interface IExecutiveDiscoveryService
{
    Task<DiscoveredExecutive?> FindDecisionMakerAsync(
        string companyName,
        string roleContext,
        CancellationToken cancellationToken = default);
}
