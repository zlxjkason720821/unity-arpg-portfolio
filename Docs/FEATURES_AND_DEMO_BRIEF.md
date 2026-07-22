# FEATURES & DEMO BRIEF — for writing a presentation / demo script

> Hand this file to another AI (e.g. Claude) to generate a 1–2 min demo video script, PPT outline, or voiceover.
> Repo: Unity ARPG Portfolio (client / tools engineer portfolio piece).

---

## One-sentence pitch

A Unity URP ARPG combat prototype that proves three JD pillars in one repo: **data-driven gameplay**, **custom Skill/Level editors**, and **visual + performance work** (pool, shaders, post-FX).

---

## Target role mapping

| JD line | What to show | Proof in repo |
|---------|--------------|---------------|
| Implement game features & logic with sound **design** | Combat + code shot of SO vs executor; AI FSM with Gizmos | `SkillData`, `SkillExecutor`, `SimpleEnemyAI` |
| **Game editors / tools** (core highlight) | Unbroken Skill Editor + Level Editor loops | `SkillEditorWindow`, `LevelEditorWindow`, `LevelData`, `LevelLoader` |
| Optimize visuals & performance | Bloom, outline, dissolve; pool / PropertyBlock story | `SimpleObjectPool`, `HitFeedback`, Shaders, URP Volume |
| OOP design | Architecture diagram Data→Logic→Editor | README mermaid |
| DS & algorithms | Queue pool, NonAlloc overlap, AnimationCurve, SO serialize | Docs + code |
| English R/W | English README / identifiers / comments | README, scripts |
| Curiosity / learning | “Learned while building” section | README |
| Graphics bonus | Outline = normal extrude; Dissolve = noise cutoff | `SimpleOutline`, `SimpleDissolve` |

---

## Full feature inventory

### Runtime combat (Demo scene / DemoBootstrap)

- Third-person-ish follow camera (`CameraFollow`)
- Player: WASD move, J/LMB basic attack, Q/1 Cleave, E/2 Shockwave (IME-safe keys)
- Data-driven skills via `SkillData` ScriptableObject
- `SkillExecutor`: cooldown, overlap hits, VFX from pool, no hardcoded damage/CD/range
- Enemies: ring spawn, Grunt/Brute variants from `EnemyData`
- AI FSM: **Patrol → Chase → Attack** (+ lose target → Patrol)
- Scene Gizmos: yellow detect, red attack, cyan patrol, state label
- Hit feedback: flash + knockback via **MaterialPropertyBlock** (no material instance break)
- Death: **dissolve** shader animation then destroy
- UI: world health bars, skill CD slots, on-screen control hint
- URP Global Volume: Bloom + ACES Tonemapping + mild color adjust
- Shared outline materials for player/enemies (batching-friendly)

### Editors (menu: ARPG Tools)

- **Create Sample Data**: seed Skill / Enemy / Level assets
- **Skill Editor**: list, search, Create Skill, sliders (damage/CD/range…), VFX/SFX fields, fixed-axis damage-by-level bar chart, CD/Range meters
- **Level Editor**: pick EnemyData, Place Enemy / Place Goal in Scene, save `LevelData` SO, spawn list edit
- **LevelLoader**: runtime spawn from `LevelData` (WYSIWYG)
- **Ensure URP Setup**: assigns URP pipeline asset

### Performance / graphics talking points

- Object pool: `Queue<GameObject>` Get/Release for skill pulses
- Hit recolor: PropertyBlock vs `renderer.material`
- Outline: Cull Front + normal extrusion
- Dissolve: procedural noise + `_Cutoff` animate
- Profiler before/after: document in `Docs/profiler/` (user captures real numbers)

### Docs already in repo (use as sources)

| Path | Role |
|------|------|
| `README.md` | English overview + architecture diagram |
| `Docs/DEMO_VIDEO_SCRIPT.md` | Shot list with captions |
| `Docs/INTERVIEW_TALKING_POINTS.md` | Q&A oral answers |
| `Docs/TECH_HIGHLIGHTS.md` | One-pager |
| `Docs/SCENE_SETUP.md` | How to open / verify |
| `Docs/profiler/README.md` | How to capture Profiler stills |

---

## Controls (for script stage directions)

- Move: WASD  
- Attack: J or Left Mouse  
- Skill 1: Q or 1  
- Skill 2: E or 2  
- Open tools: top menu **ARPG Tools** (not double-clicking `.cs` files)

---

## Suggested demo narrative arcs (for Claude to expand)

### Arc A — “Design, not just features” (~35s)
Gameplay hits → cut/split to `SkillData.cs` + `SkillExecutor.cs` highlighting data fields vs logic → Scene Gizmos AI Patrol→Chase→Attack.

### Arc B — “Tools that ship” (~75s, longest)
Skill Editor one-take: Create → sliders → save → verify in Play.  
Level Editor one-take: Place → Save LevelData → Play with LevelLoader.  
Caption: *Configure skills without code changes — lower designer–programmer iteration cost.*

### Arc C — “Look & cost” (~20s)
Slow outline/dissolve; optional PPT slide for Profiler GC / Stats batches (honest numbers).

### Close
Architecture card: Data → Logic → Editor.

---

## Key file paths (for “show code” moments)

```
Assets/Scripts/Skills/SkillData.cs
Assets/Scripts/Skills/SkillExecutor.cs
Assets/Scripts/AI/SimpleEnemyAI.cs
Assets/Scripts/Pooling/SimpleObjectPool.cs
Assets/Scripts/Combat/HitFeedback.cs
Assets/Scripts/Combat/DeathDissolve.cs
Assets/Scripts/Demo/DemoBootstrap.cs
Assets/Scripts/Level/LevelLoader.cs
Assets/Scripts/Data/LevelData.cs
Assets/Editor/SkillEditor/SkillEditorWindow.cs
Assets/Editor/LevelEditor/LevelEditorWindow.cs
Assets/Shaders/SimpleOutline.shader
Assets/Shaders/SimpleDissolve.shader
Assets/Scenes/Demo.unity
```

---

## Must-say lines (CN / EN)

- EN: “Skills are data; the executor is an interpreter.”  
- CN: “技能参数是数据，不是硬编码。”  
- EN: “Configure new skills without changing code — lower GD/programmer iteration cost.”  
- CN: “无需修改代码即可配置新技能，降低策划与程序协作成本。”  
- EN Outline: “Second pass, Cull Front, extrude vertices along normals.”  
- CN Outline: “第二个 Pass 正面剔除，沿法线外扩顶点形成描边。”

---

## Constraints / honesty

- Art is capsule placeholders (intentional — focus is systems/tools).  
- Profiler numbers must be captured on the author’s machine; don’t invent fake %.  
- Input Manager deprecation warning in Unity 6 is expected; gameplay uses legacy Input for simplicity.

---

## Ask to Claude (copy-paste)

Please write a **1.5–2.0 minute bilingual (EN captions + CN voiceover optional) demo video script** for this Unity portfolio project, using `FEATURES_AND_DEMO_BRIEF.md` and `Docs/DEMO_VIDEO_SCRIPT.md` as sources. Include: shot timing, on-screen text, what to click, and a one-page PPT outline (architecture + Profiler before/after placeholders). Emphasize Responsibility 2 (editors) with the longest unbroken takes.
