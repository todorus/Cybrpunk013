using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.assignment;

public sealed record AssignmentCreatedEvent(Assignment Assignment, double Time) : IDomainEvent;
public sealed record AssignmentCompletedEvent(Assignment Assignment, double Time) : IDomainEvent;
public sealed record AssignmentCancelledEvent(Assignment Assignment, double Time) : IDomainEvent;
