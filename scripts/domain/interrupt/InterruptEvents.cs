using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.interrupt;

/// <summary>Published by any system that wants to apply an interrupt to a character.</summary>
public sealed record InterruptRequestedEvent(CharacterInterrupt Interrupt, double Time) : IDomainEvent;

/// <summary>Published after InterruptSystem has successfully applied the interrupt.</summary>
public sealed record InterruptAppliedEvent(CharacterInterrupt Interrupt, double Time) : IDomainEvent;

/// <summary>Published after an interrupt's assignment completes and the interrupt is cleared.</summary>
public sealed record InterruptClearedEvent(CharacterInterrupt Interrupt, double Time) : IDomainEvent;

