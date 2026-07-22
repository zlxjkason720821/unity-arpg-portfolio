# Profiler 对比怎么截（投递前自己做）

本仓库已在代码里标好「优化前 / 优化后」位置，你只需打开 Profiler 截两张图放进本目录。

## 建议对比

### A. GC Alloc（对象池）

1. 临时把 `SkillExecutor` 的池关掉（不调用 `SetVfxPool`）→ 狂按技能 → Profiler 看 `GC.Alloc`
2. 恢复对象池 → 同样操作再截一张
3. 文件命名：`gc_before.png` / `gc_after.png`

锚点：`Assets/Scripts/Skills/SkillExecutor.cs` → `SpawnFeedback`

### B. Batches / SetPass（材质）

1. 受击若改用 `renderer.material.color`（拆实例）会抬高 Batches
2. 当前实现用 `MaterialPropertyBlock` + 敌人共用 `sharedMaterial`
3. Frame Debugger / Stats 面板对比 Batches

锚点：`HitFeedback.cs`、`DemoBootstrap.EnsureMaterials`

## 截图放置

把 png 直接放在本文件夹即可，README 会指引到这里。
