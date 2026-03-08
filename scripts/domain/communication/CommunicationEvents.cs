using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.communication;

public sealed record CommunicationEmittedEvent(Communication Communication, double Time) : IDomainEvent;
public sealed record InterceptCreatedEvent(Intercept Intercept, double Time) : IDomainEvent;