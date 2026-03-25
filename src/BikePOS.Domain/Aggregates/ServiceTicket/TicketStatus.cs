namespace BikePOS.Domain.Aggregates.ServiceTicket;

/// <summary>
/// Represents the lifecycle state of a service ticket.
/// Transitions are enforced by the ServiceTicket aggregate root.
/// </summary>
public enum TicketStatus
{
    Open,
    InProgress,
    WaitingForParts,
    Completed,
    Charged,
    Cancelled
}

public static class TicketStatusTransitions
{
    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> AllowedTransitions = new()
    {
        [TicketStatus.Open] = new() { TicketStatus.InProgress, TicketStatus.WaitingForParts, TicketStatus.Completed, TicketStatus.Cancelled },
        [TicketStatus.InProgress] = new() { TicketStatus.WaitingForParts, TicketStatus.Completed, TicketStatus.Cancelled },
        [TicketStatus.WaitingForParts] = new() { TicketStatus.InProgress, TicketStatus.Completed, TicketStatus.Cancelled },
        [TicketStatus.Completed] = new() { TicketStatus.Charged, TicketStatus.InProgress, TicketStatus.Cancelled },
        [TicketStatus.Charged] = new() { TicketStatus.Open }, // refund reopens
        [TicketStatus.Cancelled] = new() // terminal state
    };

    public static bool CanTransition(TicketStatus from, TicketStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static HashSet<TicketStatus> GetAllowedTransitions(TicketStatus from)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed)
            ? allowed
            : new HashSet<TicketStatus>();
    }
}
