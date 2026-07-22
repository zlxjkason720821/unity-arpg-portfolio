# Interview Talking Points

Short answers you can rehearse. Match them to JD lines.

---

## Responsibility 1 — Features & logic (design, not “it runs”)

**Q: Why ScriptableObject for skills?**  
A: Parameters (damage, CD, range, VFX refs) are **data**. `SkillExecutor` is an **interpreter**. Designers change assets without recompiling; basic attack and skills share one execution path.

**Q: Show me the AI.**  
A: Explicit FSM: `Patrol` → (`dist <= detect`) `Chase` → (`dist <= attack`) `Attack`; leave `loseTargetRange` returns to Patrol. Detect/attack radii drawn with Gizmos for debugging and demos.

---

## Responsibility 2 — Editor tools (core highlight)

**Value sentence (use as subtitle):**  
“New skills can be authored without code changes, cutting designer–programmer iteration cost.”

**Closed loop to demonstrate:** Create Skill → sliders → save asset → Play verifies.  
Level: Place in Scene → save `LevelData` → `LevelLoader` spawns same poses.

---

## Responsibility 3 — Visual & performance

| Claim | Where | Honest note |
|-------|--------|-------------|
| Less GC from VFX | `SimpleObjectPool` + `SkillExecutor.SpawnFeedback` | Measure with Profiler; even small wins count if labeled truthfully |
| Fewer material breaks | Shared mats + `MaterialPropertyBlock` in `HitFeedback` | Avoid `renderer.material` which clones instances |
| Post & stylization | URP Bloom/ACES; Outline; Dissolve | Show slow / side-by-side in video |

---

## OOP design

```
Data (ScriptableObjects)  →  Logic (MonoBehaviours)  →  Editor (EditorWindows)
SkillData / EnemyData / LevelData
        ↓ depends on
SkillExecutor / SimpleEnemyAI / LevelLoader / PlayerController
        ↑ edited by
SkillEditorWindow / LevelEditorWindow
```

Dependency rule: **Editor and Logic depend on Data; Data never depends on Editor.**

---

## Data structures & algorithms (light but explainable)

| Choice | Structure / idea | Why |
|--------|------------------|-----|
| Object pool | `Queue<GameObject>` | O(1) acquire/release; FIFO reuse of idle instances |
| Hit query | `OverlapSphereNonAlloc` + fixed buffer | Avoids per-frame GC vs `OverlapSphere` allocating arrays |
| Level save | `ScriptableObject` asset (Unity serialization) | Same asset Editors write and runtime reads — no custom binary needed for this scope |
| Damage falloff / preview | `AnimationCurve` | Piecewise curve evaluation (Unity hermite-style interpolation between keys) |

---

## Graphics (must be able to say this aloud)

**Outline:** Extra pass, **Cull Front**, vertex positions pushed along **object-space normals** by `_OutlineWidth`, solid `_OutlineColor`. Silhouette appears around the mesh.

**Dissolve:** Per-pixel noise (procedural hash or texture). `clip(noise - _Cutoff)`; animate `_Cutoff` 0→1 on death; edge color near the cutoff band.

Reference: [Unity Manual — Writing shaders](https://docs.unity3d.com/Manual/shader-writing.html), [URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest).

---

## Learning narrative (curiosity)

First-time practices in this project (say so honestly if true for you):

- Custom `EditorWindow` workflows  
- URP Volume post-processing from code  
- Stylized outline / dissolve HLSL in URP  
- Object pooling for combat VFX  
