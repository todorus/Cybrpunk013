# Bug: Pursuit path not drawn on first target exit

## Symptom

When an operator is dispatched to tail a target that is currently at a site, the operator travels to the target's site and holds position. When the target exits the site for the first time, the operator correctly follows, but the path line is never drawn. From the second exit onwards the path draws normally.

## Root cause

The bug is a stale-movement clobber in `MovementSystem.Tick`.

When the target exits a site, the following happens synchronously within a single event cascade:

1. `ScheduleSystem` (or `PlotSystem`) publishes `AssignmentCreatedEvent` for the target's next schedule entry.
2. `AssignmentSystem.OnAssignmentCreated` creates the target's movement → publishes `MovementStartedEvent`.
3. `MovementSystem.OnMovementStarted` (target) sets `target.LocationType = NavGraph` → publishes `CharacterLocationChangedEvent`.
4. `AssignmentSystem.OnCharacterLocationChanged` finds the operator's tail assignment in `PursuingTarget` phase:
   - Calls `StopCurrentMovement` → `ForceArrive()` on the **old** operator pursuit movement (marks `HasArrived = true`).
   - Calls `StartPursuitPhase` → creates a **new** operator pursuit movement → publishes `MovementStartedEvent`.
5. `MovementSystem.OnMovementStarted` (operator) adds the new movement to `_activeMovements` and sets `character.CurrentMovement = newMovement`.

All of the above resolves synchronously. Then `MovementSystem.Tick` continues iterating `_activeMovements`. It encounters the **old** operator movement (still in the list, now with `HasArrived = true`) and executes the arrival block:

```csharp
// MovementSystem.cs – arrival block inside Tick
movement.Character.CurrentMovement = null;          // ← clobbers the new movement!
movement.Character.CurrentSite = movement.Destination;
```

This unconditionally sets `character.CurrentMovement = null`, **overwriting the new pursuit movement** that was just assigned in step 5.

With `CurrentMovement` null, `CharacterMapVisual.DrawRemainingPath` returns early:

```csharp
var movement = _character.CurrentMovement;
if (movement == null) return;   // ← path cleared, nothing drawn
```

The operator still physically moves (the new movement is in `_activeMovements` and `AdvanceMovement` updates `Character.Position`), but every call to `DrawRemainingPath` finds a null `CurrentMovement` and draws nothing.

On subsequent target exits, the old movement has already been removed from `_activeMovements` during a prior tick, so the clobber does not repeat.

## Fix

In `MovementSystem.Tick`, guard the `CurrentMovement = null` assignment so it only clears if the character's current movement still references the arrived movement:

```csharp
// Before
movement.Character.CurrentMovement = null;

// After
if (movement.Character.CurrentMovement == movement)
    movement.Character.CurrentMovement = null;
```

This is safe because:
- In the normal (non-clobbered) case the check is always true — `CurrentMovement` still points to the arrived movement.
- In the clobbered case, `CurrentMovement` already points to a new movement, and the guard prevents it from being cleared.

### Affected file

`scripts/domain/movement/MovementSystem.cs` — arrival block inside `Tick`, around line 61.

