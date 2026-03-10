# Plan: Schedule & Interrupt Systems for NPC Behavior

Add a baseline schedule loop and a priority-based interrupt override mechanism that both feed into the existing `AssignmentSystem` via the event bus. Schedule provides idle-time routine assignments; interrupts temporarily suspend/replace them. Both systems stay as plain C# domain classes, coordinated through `AssignmentCreatedEvent`/`AssignmentCompletedEvent`.

## Steps

### 1. Add `AssignmentSource` enum and source tracking to Assignment

- New file `AssignmentSource.cs` in `scripts/domain/assignment/` with values `Schedule`, `Interrupt`, `PlayerOrder`.
- Add `AssignmentSource Source` property and nullable `string? InterruptId` to `Assignment`.
- Update `CityscapeClickHandler` to set `Source = PlayerOrder` on operator assignments.

### 2. Flesh out schedule domain model in `scripts/domain/schedule/`

- New `ScheduleEntry` class: holds target `Site`, operation label, duration (the data needed to create an `Option`-like operation+movement assignment). Authored data, kept simple.
- Flesh out existing empty `Schedule` class: holds `List<ScheduleEntry> Entries`, a `CurrentIndex` tracker, and a `ScheduleEntry Advance()` method that loops back to index 0.
- `Schedule` lives on `Character.Schedule` (already wired).

### 3. Implement `ScheduleSystem` in `scripts/domain/schedule/ScheduleSystem.cs`

- Implements `ISimulationSystem`. Subscribes to `AssignmentCompletedEvent`.
- On `Tick`: scans `WorldState.Characters` for any character that has a `Schedule`, has no active assignment tracked in `WorldState`, and has no active interrupt (checked via a new helper on `Character`). Creates the next assignment from `Schedule.Advance()`, builds a `Movement` via `DispatchNav`, sets `CompletionBehavior = AwaitSchedule` and `Source = Schedule`, publishes `AssignmentCreatedEvent`.
- On `AssignmentCompletedEvent` where `Source == Schedule`: marks the character as ready for next schedule entry (the next `Tick` picks it up naturally).
- Needs `DispatchNav` injected (same as `AssignmentSystem`).

### 4. Implement interrupt domain model in new `scripts/domain/interrupt/` folder

- `InterruptPriority` enum: `Low`, `Normal`, `High`, `Critical` — determines if an interrupt can override another.
- `InterruptDisposition` enum: `Suspend` (baseline assignment is saved and resumed later), `Replace` (baseline assignment is cancelled).
- `CharacterInterrupt` class: `Id`, `InterruptType`, `InterruptPriority`, `InterruptDisposition`, target `Character`, the replacement `Assignment` to execute, and `bool IsActive` state.
- Rename/extend the existing `InterruptType` enum (currently in `scripts/domain/InterruptType.cs`) with new values like `Rendezvous`, `BugSweep`, `Flee`, `Relocate`, `ReactToSurveillance` — move it into the new `interrupt` namespace or keep it where it is and reference it.
- Add to `Character`: `CharacterInterrupt? ActiveInterrupt`, `Assignment? SuspendedAssignment` — these are the two pieces of state needed for interrupt flow.

### 5. Implement `InterruptSystem` in `scripts/domain/interrupt/InterruptSystem.cs`

- Implements `ISimulationSystem`. Subscribes to a new `InterruptRequestedEvent` and `AssignmentCompletedEvent`.
- `OnInterruptRequested`: checks priority against `Character.ActiveInterrupt` (if any). If allowed: sets `Character.ActiveInterrupt`, optionally saves current assignment to `Character.SuspendedAssignment` (if disposition is `Suspend`) or cancels it (if `Replace`), then publishes `AssignmentCreatedEvent` for the interrupt's replacement assignment with `Source = Interrupt`.
- `OnAssignmentCompleted` where `Source == Interrupt`: clears `Character.ActiveInterrupt`. If there's a `SuspendedAssignment`, re-publishes it as `AssignmentCreatedEvent` to resume baseline. Otherwise, the character goes idle and `ScheduleSystem` picks up on next tick.
- New events: `InterruptRequestedEvent(CharacterInterrupt Interrupt, double Time)`, `InterruptAppliedEvent`, `InterruptClearedEvent` in `InterruptEvents.cs`.

### 6. Wire new systems into SimulationController and update WorldState

- Add `ScheduleSystem` and `InterruptSystem` to `_systems` list in `SimulationController._Ready()`, ordered before `AssignmentSystem` so schedule/interrupt decisions publish events that `AssignmentSystem` handles in the same frame.
- Add a helper to `WorldState` like `TryGetActiveAssignmentForCharacter(characterId)` so schedule/interrupt systems can check if a character already has an in-progress assignment. This could scan `Assignments` for non-completed entries matching the character.
- Optionally add a `ScheduleEntryResource` authoring resource for Godot editor authoring of schedules via `.tres` files.

## Further Considerations

1. **Cancelling in-flight assignments during interrupt**: When an interrupt suspends a schedule assignment that's mid-movement, should we cancel/stop the movement immediately (remove from `MovementSystem`) or let it arrive first? Recommend: cancel immediately (set phase to `Cancelled`, publish a new `AssignmentCancelledEvent` that `MovementSystem` can react to by removing the movement).
2. **Rendezvous readiness**: The `CharacterInterrupt` design supports a future `RendezvousSystem` that creates multiple `InterruptRequestedEvent`s (one per participant) pointing at the same site — no structural changes needed, just a new system that emits the events.
3. **Schedule authoring**: Should `ScheduleEntry` reference a `Site` by runtime object reference or by site ID string? Recommend site ID string for authored data (resolved at bootstrap), since Resources can't hold runtime references.

