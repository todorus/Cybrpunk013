using Godot;
using SurveillanceStategodot.scripts.domain.assignment;
using SurveillanceStategodot.scripts.domain.schedule;
using SurveillanceStategodot.scripts.domain.system;

namespace SurveillanceStategodot.scripts.domain.interrupt;

public sealed class InterruptSystem : ISimulationSystem
{
    private readonly ScheduleSystem _scheduleSystem;

    private WorldState _world = null!;
    private SimulationEventBus _eventBus = null!;

    public InterruptSystem(ScheduleSystem scheduleSystem)
    {
        _scheduleSystem = scheduleSystem;
    }

    public void Initialize(WorldState world, SimulationEventBus eventBus)
    {
        _world = world;
        _eventBus = eventBus;

        _eventBus.Subscribe<InterruptRequestedEvent>(OnInterruptRequested);
        _eventBus.Subscribe<AssignmentCompletedEvent>(OnAssignmentCompleted);
    }

    public void Tick(double delta) { }

    private void OnInterruptRequested(InterruptRequestedEvent evt)
    {
        var interrupt = evt.Interrupt;
        var character = interrupt.Character;

        // Reject if an existing interrupt has equal or higher priority.
        if (character.ActiveInterrupt != null &&
            character.ActiveInterrupt.Priority >= interrupt.Priority)
        {
            GD.Print($"[InterruptSystem] Interrupt '{interrupt.Type}' rejected for '{character.Id}': " +
                     $"existing interrupt '{character.ActiveInterrupt.Type}' has equal or higher priority.");
            return;
        }

        // If there is an existing lower-priority interrupt active, clear it first.
        if (character.ActiveInterrupt != null)
        {
            ClearInterrupt(character.ActiveInterrupt, evt.Time, resumeBaseline: false);
        }

        // Determine what to do with the character's current active assignment.
        var currentAssignment = FindActiveAssignment(character);

        if (currentAssignment != null)
        {
            switch (interrupt.Disposition)
            {
                case InterruptDisposition.Suspend:
                    // Save baseline assignment so it can be resumed later.
                    character.SuspendedAssignment = currentAssignment;
                    _scheduleSystem.SuppressCharacter(character.Id);
                    CancelAssignment(currentAssignment, evt.Time);
                    break;

                case InterruptDisposition.Replace:
                    // Cancel without saving; schedule will issue next entry when interrupt clears.
                    character.SuspendedAssignment = null;
                    _scheduleSystem.SuppressCharacter(character.Id);
                    CancelAssignment(currentAssignment, evt.Time);
                    break;
            }
        }
        else
        {
            // Character was idle; suppress schedule from issuing new work.
            _scheduleSystem.SuppressCharacter(character.Id);
        }

        // Apply the interrupt.
        interrupt.IsActive = true;
        character.ActiveInterrupt = interrupt;

        // Mark the replacement assignment clearly.
        interrupt.ReplacementAssignment.Source = AssignmentSource.Interrupt;
        interrupt.ReplacementAssignment.InterruptId = interrupt.Id;

        // State change done — now publish events.
        _eventBus.Publish(new InterruptAppliedEvent(interrupt, evt.Time));
        _eventBus.Publish(new AssignmentCreatedEvent(interrupt.ReplacementAssignment, evt.Time));
    }

    private void OnAssignmentCompleted(AssignmentCompletedEvent evt)
    {
        var assignment = evt.Assignment;
        if (assignment.Source != AssignmentSource.Interrupt || assignment.InterruptId == null)
            return;

        var character = assignment.Character;
        if (character == null)
            return;

        var interrupt = character.ActiveInterrupt;
        if (interrupt == null || interrupt.Id != assignment.InterruptId)
            return;

        ClearInterrupt(interrupt, evt.Time, resumeBaseline: true);
    }

    private void ClearInterrupt(CharacterInterrupt interrupt, double time, bool resumeBaseline)
    {
        var character = interrupt.Character;

        interrupt.IsActive = false;
        character.ActiveInterrupt = null;

        if (resumeBaseline)
        {
            var suspended = character.SuspendedAssignment;
            character.SuspendedAssignment = null;

            _scheduleSystem.UnsuppressCharacter(character.Id);

            if (suspended != null)
            {
                // Resume the suspended baseline assignment from scratch (re-issue it).
                // We re-publish AssignmentCreatedEvent so AssignmentSystem handles it cleanly.
                // Reset phase so AssignmentSystem starts it fresh from outbound movement.
                suspended.Phase = AssignmentPhase.OutboundMovement;
                _eventBus.Publish(new AssignmentCreatedEvent(suspended, time));
            }
            // If no suspended assignment, ScheduleSystem will issue the next entry on next Tick.
        }

        _eventBus.Publish(new InterruptClearedEvent(interrupt, time));
    }

    private void CancelAssignment(Assignment assignment, double time)
    {
        assignment.Phase = AssignmentPhase.Cancelled;
        // State change first, then event — MovementSystem will remove the in-flight movement.
        _eventBus.Publish(new AssignmentCancelledEvent(assignment, time));
    }

    private Assignment FindActiveAssignment(Character character)
    {
        foreach (var assignment in _world.Assignments)
        {
            if (assignment.Character?.Id != character.Id)
                continue;
            if (assignment.Phase == AssignmentPhase.Completed || assignment.Phase == AssignmentPhase.Cancelled)
                continue;
            return assignment;
        }
        return null;
    }
}



