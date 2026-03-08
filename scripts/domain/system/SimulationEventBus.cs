using System;
using System.Collections.Generic;

namespace SurveillanceStategodot.scripts.domain.system;

public sealed class SimulationEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.TryGetValue(type, out var handlers))
        {
            handlers = new List<Delegate>();
            _handlers[type] = handlers;
        }

        handlers.Add(handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (_handlers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(type);
            }
        }
    }

    public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        if (!_handlers.TryGetValue(type, out var handlers))
            return;

        // Copy to avoid modification-during-iteration issues.
        var snapshot = handlers.ToArray();
        foreach (var handler in snapshot)
        {
            ((Action<TEvent>)handler)(domainEvent);
        }
    }
}