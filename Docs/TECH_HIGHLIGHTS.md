# One-pager — technical highlights (print / PPT slide 1)

## Design
**Data → Logic → Editor.** `SkillData` / `EnemyData` / `LevelData` are ScriptableObjects.  
Runtime only interprets; Skill/Level EditorWindows edit the same assets.

## Tools (JD: editors)
Create skill → sliders → save → Play verifies.  
Place enemies in Scene → save `LevelData` → `LevelLoader` spawns WYSIWYG.  
*Subtitle:* Configure skills without code changes; lower GD/engineering iteration cost.

## AI
FSM: **Patrol → Chase → Attack**. Scene Gizmos: yellow detect, red attack.

## Performance & graphics
Object pool (`Queue`) for VFX · PropertyBlock hits · shared materials · URP Bloom ·  
Outline (normal extrude) · Dissolve (noise cutoff).

## Code anchors
`SkillData.cs` · `SkillExecutor.cs` · `SimpleEnemyAI.cs` · `SkillEditorWindow.cs` · `LevelEditorWindow.cs`
