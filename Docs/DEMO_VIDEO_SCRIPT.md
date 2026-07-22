# Demo Video Script (1.5–2.0 min)

Use this shot list when recording. Prefer **one continuous take per editor segment** (no jump cuts) so reviewers trust the tool is real.

On-screen captions (Chinese or bilingual) are recommended; code/README stay English.

---

## Shot A — Combat + Design (Responsibility 1) · ~35s

| Time | Visual | Caption / VO |
|------|--------|----------------|
| 0:00–0:12 | Game view: WASD, J attack, Q/E skills, hit flash | “Data-driven combat loop” |
| 0:12–0:20 | **Alt+Tab or split**: open `SkillData.cs` + `SkillExecutor.cs`, highlight fields / `TryCast` | “Skill numbers live in ScriptableObject data — executor only reads & runs” |
| 0:20–0:35 | Scene view, **Gizmos ON**, select an enemy: yellow detect / red attack circles; watch label `AI: Patrol → Chase → Attack` | “Explicit FSM: Patrol → Chase → Attack” |

**How to show AI states:** stay outside yellow circle (Patrol), walk in (Chase), enter red circle (Attack).

---

## Shot B — Skill Editor (Responsibility 2) · ~40s · ONE TAKE

Caption: **“Configure a new skill without changing code — lower GD/programmer iteration cost.”**

1. Menu `ARPG Tools > Skill Editor`
2. Click **Create Skill**
3. Drag **Base Damage / Cooldown / Range** (bar chart updates)
4. Assign VFX/SFX fields if available (or leave empty — pool pulse still works)
5. Play / cast to show the new numbers matter in-game

Do **not** cut between create → edit → verify.

---

## Shot C — Level Editor (Responsibility 2) · ~35s · ONE TAKE

Caption: **“What you place in Scene is what runtime spawns from LevelData.”**

1. `ARPG Tools > Level Editor`
2. Select / create LevelData + EnemyData
3. **Place Enemy** → click ground in Scene (handles visible)
4. **Save Level Asset**
5. Scene with `LevelLoader` referencing that asset → Play → enemies at placed positions

---

## Shot D — Visual / Graphics (Responsibility 3 + graphics bonus) · ~20s

| Visual | Caption |
|--------|---------|
| Slow hit: outline thickens / flash | “Outline via normal extrusion (second pass, Cull Front)” |
| Death dissolve | “Dissolve: procedural noise threshold + edge color” |
| Optional: Game Stats (Batches) or Profiler stills in PPT | “Object pool cuts Instantiate GC; shared materials + PropertyBlock limit draw-call churn” |

Profiler before/after can live on **one PPT slide** instead of the video.

---

## Closing slide / README card · 5s

Architecture diagram (Data → Logic → Editor) + one line:

> Same ScriptableObject assets feed runtime systems and custom EditorWindows.

---

## Checklist before upload

- [ ] Gizmos enabled in Scene view for AI shot  
- [ ] Skill Editor take unbroken  
- [ ] Level Editor place → save → play unbroken  
- [ ] Code shot shows `SkillData` fields vs `SkillExecutor` logic  
- [ ] Caption maps to JD wording (“editor tools”, “data-driven”)  
