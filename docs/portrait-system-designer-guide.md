# Portrait System — Designer Scene Guide

The portrait system renders 2D avatar images from 3D character scenes using a shared offscreen "photo studio". The code is fully implemented; designers need to author three scenes and wire exported references.

---

## How it works

1. A single **PortraitCache** node lives in the main scene. It owns a single **PortraitStudio** (the photo studio).
2. When a portrait is requested, the studio instantiates the character's avatar scene under `SubjectAnchor`, fires a one-shot render through a `SubViewport`, grabs the image, caches it, and frees the avatar instance.
3. UI widgets (**PortraitTextureRect**) request portraits from the cache and display the resulting texture. They never own a viewport themselves.

---

## Scene 1 — `AutomatedPortrait.tscn` (already exists, needs script + wiring)

Attach **`PortraitStudio.cs`** to the root node and wire the two exported references.

```
Node                            ← root, attach PortraitStudio.cs
│   @export _viewport           → SubViewport
│   @export _subjectAnchor      → SubViewport/PortraitStudio/SubjectAnchor
│
└─ SubViewport
   │   render_target_update_mode = Disabled (0)
   │   transparent_bg = true
   │
   └─ PortraitStudio  (instance of portrait_studio.tscn)
      ├─ Camera3D
      ├─ Lights …
      └─ SubjectAnchor   ← Node3D, avatars are placed here at runtime
```

**Important:** Set `render_target_update_mode` on the SubViewport to **Disabled (0)** in the scene. The script enforces this in `_Ready`, but setting it in the scene avoids any flash on first load.

No signals to wire.

---

## Scene 2 — `portrait_cache.tscn` (new scene)

Attach **`PortraitCache.cs`** to the root node and wire the one exported reference.

```
Node                            ← root, attach PortraitCache.cs
│   @export _studio             → AutomatedPortrait
│
└─ AutomatedPortrait  (instance of AutomatedPortrait.tscn)
```

Place this scene **once** in the main scene tree, somewhere persistent — e.g. as a sibling of `SimulationController`. Do **not** instantiate it per-character or per-panel.

No signals to wire.

---

## Scene 3 — `portrait_texture_rect.tscn` (new scene)

Attach **`PortraitTextureRect.cs`** to a `TextureRect` node and wire the one exported reference.

```
TextureRect                     ← attach PortraitTextureRect.cs
    @export _cache              → <the shared PortraitCache node>
    expand_mode  = 1            (Keep Aspect)
    stretch_mode = 5            (Keep Aspect Centered)
```

Wire `_cache` to the shared `PortraitCache` node in the tree. This widget can be instanced anywhere in the UI.

No signals to wire. At runtime, call `SetCharacter(characterResource)` from code to request and display a portrait.

---

## Character resources — add avatar scene per character

Each `CharacterResource.tres` now has an **`AvatarScene`** export. Assign the character's 3D `PackedScene` there.

- Characters without an `AvatarScene` produce a null portrait — no crash, the `TextureRect` simply stays blank.
- The avatar scene root is expected to sit correctly at the local origin of `SubjectAnchor` with identity transform. Position, lighting, and camera framing are the studio's responsibility.

---

## Avatar scene authoring convention

- The avatar scene root is the **portrait subject pivot**.
- When instanced under `SubjectAnchor` with `Transform = Identity`, it must appear correctly in the camera frame.
- The studio owns framing, lighting, and background — do not bake those into the avatar scene.
- Future extension: if a `Marker3D` named `PortraitFocus` or `HeadAnchor` is present on the avatar root, the studio can optionally use it to reframe. This is not implemented yet but the architecture supports it.

