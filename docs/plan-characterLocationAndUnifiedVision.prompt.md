# Refactor Plan: Character Location State & Unified Operator Vision

## Problem

When an operator returns to base after completing an assignment, their vision source remains on the map and is never cleaned up. When dispatched again, the vision source stays stuck at the base position instead of following the operator.

### Root cause analysis

The operator's "where am I?" state is currently implicit and ambiguous:

- `Character.CurrentSite != null` → at a site
- `Character.CurrentSite == null && CurrentMovement != null` → on the nav graph, moving
- `Character.CurrentSite == null && CurrentMovement == null` → **ambiguous**: could be holding position on the nav graph (stakeout, tail hold) OR returned to base and off the map entirely

This ambiguity means no system can reliably determine whether the operator is still "on the map." The VisionSystem was designed around the rule "operator has a vision source while on the nav graph," but there is no authoritative way to know when the operator leaves the nav graph by returning to base.

### Why previous approaches were rejected

**Tie vision source to assignment lifecycle (AssignmentCreatedEvent → AssignmentCompletedEvent):**
Rejected because it assumes one assignment at a time. In the future, operators can be reassigned mid-assignment. It also doesn't account for operators that will later enter sites as part of an assignment — the vision source should drop when entering a site, not when the assignment ends.

**Publish OperatorExitedBaseEvent / OperatorEnteredBaseEvent:**
Rejected because these events would just be proxies for a state change that should be explicit on the character. Adding events that encode derived state is indirect — better to make the state itself authoritative and let systems react to transitions.

**Put LocationChanged C# event on Character directly:**
Rejected because it violates the architectural rule that domain state objects (like `Character`) should be just state. They should not carry observer events. Systems mutate world state, then publish domain events on the shared event bus. Putting events on `Character` would blur the boundary between state and system-level coordination.

## Solution: CharacterLocationType enum + domain event

Add an explicit location kind to `Character`:

```csharp
public enum CharacterLocationType
{
    Base,     // Off-map, at HQ / home
    NavGraph, // On the map, moving or holding position
    Site      // Inside a site building
}
```

Add to `Character`:
- `CharacterLocationType LocationType` property (plain state, no events)

Add a new domain event:

```csharp
public sealed record CharacterLocationChangedEvent(
    Character Character,
    CharacterLocationType PreviousLocation,
    CharacterLocationType NewLocation,
    double Time) : IDomainEvent;
```

Systems that mutate `Character.LocationType` are responsible for:
1. Setting the new value on the character (state change first)
2. Publishing `CharacterLocationChangedEvent` on the event bus (notification after)

This follows the established pattern: state changes happen first, then domain events are published afterward.

### Why this is the right approach

1. **Removes all ambiguity** — every character always knows exactly what kind of location they are at
2. **The VisionSystem rule becomes trivial** — operator has a vision source iff `LocationType == NavGraph`
3. **No assignment lifecycle coupling** — vision source existence is independent of what assignment phase the operator is in
4. **Follows the architectural pattern** — state on the domain object, events on the bus, systems subscribe to events via `SimulationEventBus`
5. **Future-proof for reassignment** — when an operator gets a new assignment while already on the nav graph, their `LocationType` is already `NavGraph`, so nothing changes for vision
6. **Future-proof for operators entering sites** — when that feature is added, setting `LocationType = Site` will automatically remove the vision source via the same event subscription
7. **Base-as-location becomes natural** — spawning at base starts as `Base`, leaving base transitions to `NavGraph`, returning transitions back to `Base`

## Where LocationType gets set and event gets published

| Transition | Set by | New value | Publishes event |
|---|---|---|---|
| Operator spawned | `CityscapeClickHandler` (dispatch methods) | `Base` (initial) | No — initial state, no transition |
| Movement starts from base/site | `MovementSystem.OnMovementStarted` | `NavGraph` | Yes |
| Movement arrives at a site | `MovementSystem` (arrival with `Destination != null`) | `Site` | Yes |
| Return movement arrives at base | `AssignmentSystem.OnMovementArrived` (ReturnMovement phase complete) | `Base` | Yes |
| Assignment failed / cancelled at base | `AssignmentSystem.FailAssignment` | `Base` (if not already) | Yes (if changed) |

Note: NPC characters also get `LocationType`. Their flow is:
- Start at a site (`Site`) — initial state, no event
- Schedule movement starts → `NavGraph` — event published
- Arrive at destination site → `Site` — event published

## VisionSystem changes

- Remove `MovementStartedEvent` subscription (bootstrap hack no longer needed)
- Remove `CharacterExitedSiteEvent` / `CharacterEnteredSiteEvent` subscriptions for vision lifecycle
- Subscribe to `CharacterLocationChangedEvent` on the event bus
- On `CharacterLocationChangedEvent` where `Character.IsOperator`:
  - `NewLocation == NavGraph` → `EnsureVisionSource(character)`
  - `NewLocation == Base` or `NewLocation == Site` → `RemoveVisionSource(character)`

The scan logic (`ScanMovingNpcs`, `ScanSitesForSource`) remains unchanged in `Tick`.

## CharacterPresenter changes

The `CharacterPresenter` currently subscribes to `CharacterEnteredSiteEvent` / `CharacterExitedSiteEvent` to hide/show the map visual. This should also react to `CharacterLocationChangedEvent`:
- `NewLocation == NavGraph` → visible
- `NewLocation == Base` or `NewLocation == Site` → hidden

For operator visuals, `NewLocation == Base` should also trigger cleanup (QueueFree) since the operator is off the map. This replaces the current `AssignmentCompletedEvent` cleanup and the site event visibility toggling — both become unified under one event.

## Cleanup: `CharacterEnteredSiteEvent` and `CharacterExitedSiteEvent` are obsolete

These events are **residual and should be deleted** as part of this refactoring. Every consumer can be migrated to `CharacterLocationChangedEvent`:

| Consumer | Current use | Replacement |
|---|---|---|
| `VisionSystem` | Vision source lifecycle (already planned to migrate) | `CharacterLocationChangedEvent` |
| `CharacterPresenter` | Map visual hide/show (already planned to migrate) | `CharacterLocationChangedEvent` |
| `AssignmentSystem` | Tail reactions — target entered/exited a site | `CharacterLocationChangedEvent` with `Character == target && NewLocation == Site/NavGraph` |

The tail reactions in `AssignmentSystem` currently read `evt.Character` (the NPC target) and `evt.Character.Position.WorldPosition`. After migration, `CharacterLocationChangedEvent` carries the same `Character` reference, and `Character.Position.WorldPosition` / `Character.CurrentSite.EntryPosition` remain available as plain state. The logic is identical — only the event type changes.

`CharacterSiteEvents.cs` (the file declaring both records) should be **deleted** once all consumers are migrated.

Note: `MovementSystem` also publishes these events. Removing the publish calls is part of the cleanup.

## Observation system — not impacted

The observation system is **not affected** by this refactoring. Here's why:

All observation creation is tick-based scanning inside `VisionSystem.Tick`, not event-driven:

- **`SpottedMoving`** — `ScanMovingNpcs()` checks every NPC with `CurrentMovement != null` against all vision source ranges. It depends on `Character.CurrentMovement`, which is set/cleared by `MovementSystem` the same way before and after this refactoring. `CurrentMovement` is not replaced by `LocationType`.
- **`SpottedAtSite`** — `ScanSitesForSource()` checks `site.Occupants` and `site.ActiveOperations` against each vision source's range. Occupant membership is driven by `MovementSystem` via `site.AddOccupant()`/`site.RemoveOccupant()` — direct calls that are **not** contingent on the site events. Removing the events does not affect occupant tracking.

The vision source lifecycle changes (now driven by `CharacterLocationChangedEvent` instead of movement/site events) do not alter scan behavior. `RebuildInRange()` — called on vision source removal — is a full re-scan of all sources and characters, so it has no timing sensitivity to the event change.

### Dead code to remove: `ObservationType.EnteredSite` and `ExitedSite`

`ObservationType.EnteredSite` and `ObservationType.ExitedSite` exist in the enum and have display labels in `LogEntryNode.cs`, but **are never published as observations anywhere in the codebase**. Remove them:

- Delete `EnteredSite` and `ExitedSite` from `ObservationType.cs`
- Remove the corresponding `case` branches from `DescribeObservation()` in `LogEntryNode.cs`

If event-driven "seen entering/exiting a site" observations are ever wanted in the future, they can be added then — with `CharacterLocationChangedEvent` as the trigger.

## Files to modify

1. **New: `CharacterLocationType.cs`** — the enum (in `scripts/domain/`)
2. **New: `CharacterLocationChangedEvent.cs`** — the domain event (in `scripts/domain/`)
3. **`Character.cs`** — add `CharacterLocationType LocationType` property
4. **`MovementSystem.cs`** — set `LocationType = NavGraph` on movement start, `LocationType = Site` on site arrival, publish `CharacterLocationChangedEvent` after each; remove `CharacterEnteredSiteEvent` and `CharacterExitedSiteEvent` publish calls
5. **`AssignmentSystem.cs`** — set `LocationType = Base` when return movement completes, publish `CharacterLocationChangedEvent`; replace `CharacterEnteredSiteEvent`/`CharacterExitedSiteEvent` subscriptions with `CharacterLocationChangedEvent` for tail reactions
6. **`CityscapeClickHandler.cs`** — set initial `LocationType = Base` on operator spawn (no event — initial state)
7. **`VisionSystem.cs`** — replace movement/site event subscriptions with `CharacterLocationChangedEvent` subscription for vision source lifecycle
8. **`CharacterPresenter.cs`** — subscribe to `CharacterLocationChangedEvent` for visibility and cleanup (replaces site event and assignment completion subscriptions)
9. **Delete: `CharacterSiteEvents.cs`** — `CharacterEnteredSiteEvent` and `CharacterExitedSiteEvent` are now fully replaced
10. **`ObservationType.cs`** — remove `EnteredSite` and `ExitedSite` variants
11. **`LogEntryNode.cs`** — remove `EnteredSite`/`ExitedSite` cases from `DescribeObservation()`
