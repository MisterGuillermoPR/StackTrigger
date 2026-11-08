using MediatR;

namespace StackTrigger.Application.Digests.Commands.SendWeeklyDigests;

public sealed record SendWeeklyDigestsCommand(
    Guid? TargetSubscriptionId = null,
    int LookbackDays = 7
) : IRequest<SendWeeklyDigestsResult>;

public sealed record SendWeeklyDigestsResult(
    int TotalProcessed,
    int TotalDelivered,
    int TotalFailed,
    IReadOnlyList<string> DeliveredToFirms
);
