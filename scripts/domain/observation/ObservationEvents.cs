using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.observation;

public sealed record ObservationCreatedEvent(Observation Observation, double Time) : IDomainEvent;