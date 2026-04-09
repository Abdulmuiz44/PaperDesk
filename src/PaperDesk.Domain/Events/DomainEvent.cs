namespace PaperDesk.Domain.Events;

public abstract record DomainEvent(DateTimeOffset OccurredUtc);
