using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.movement;

public sealed record MovementStartedEvent(Movement Movement, double Time) : IDomainEvent;
public sealed record MovementArrivedEvent(Movement Movement, double Time) : IDomainEvent;