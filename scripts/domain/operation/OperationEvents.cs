using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.operation;

public sealed record OperationStartedEvent(Operation Operation, double Time) : IDomainEvent;
public sealed record OperationCompletedEvent(Operation Operation, double Time) : IDomainEvent;