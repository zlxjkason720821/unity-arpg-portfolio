using ARPG.AI;
using ARPG.Character;
using ARPG.Combat;
using ARPG.Data;
using ARPG.Pooling;
using ARPG.Skills;
using ARPG.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ARPG.Demo
{
    /// <summary>
    /// 成品 Demo 一键装配：环境、URP 后处理、对象池、玩家、多敌人、镜头跟随、HUD。
    /// </summary>
    public class DemoBootstrap : MonoBehaviour
    {
        [SerializeField] bool buildIfSceneEmpty = true;
        [SerializeField] Vector3 playerSpawn = new Vector3(0f, 1f, 0f);
        [SerializeField] int enemyCount = 4;
        [SerializeField] float enemyRingRadius = 7f;

        static Material _sharedPlayerMat;
        static Material _sharedEnemyMat;
        static Material _sharedDissolveMat;
        static Material _sharedVfxMat;

        void Awake()
        {
            if (!buildIfSceneEmpty)
                return;
            if (FindAnyObjectByType<PlayerController>() != null)
                return;

            EnsureMaterials();
            EnsureEnvironment();
            EnsurePostProcess();
            var pool = EnsureVfxPool();

            var player = CreatePlayer(playerSpawn, pool);
            SpawnEnemyRing(player.transform, pool);
            CreateHud(player.GetComponent<SkillExecutor>());

            Debug.Log("[DemoBootstrap] Demo ready. WASD move | J/LMB attack | Q/E skills | defeat red enemies.");
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerSpawn, 0.5f);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(playerSpawn, enemyRingRadius);
        }
#endif

        static void EnsureMaterials()
        {
            // 同类敌人共用材质 → 利于 SRP Batcher / 减少变体（Draw Call 优化点）
            if (_sharedPlayerMat == null)
            {
                var outline = Shader.Find("ARPG/SimpleOutline");
                if (outline != null)
                {
                    _sharedPlayerMat = new Material(outline)
                    {
                        name = "M_Player_Outline",
                        hideFlags = HideFlags.DontSave
                    };
                    _sharedPlayerMat.SetColor("_BaseColor", new Color(0.25f, 0.55f, 1f, 1f));
                    _sharedPlayerMat.SetColor("_OutlineColor", new Color(0.05f, 0.15f, 0.4f, 1f));
                    _sharedPlayerMat.SetFloat("_OutlineWidth", 0.008f);
                }
                else
                {
                    _sharedPlayerMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    {
                        color = new Color(0.25f, 0.55f, 1f, 1f),
                        hideFlags = HideFlags.DontSave
                    };
                }
            }

            if (_sharedEnemyMat == null)
            {
                var outline = Shader.Find("ARPG/SimpleOutline");
                if (outline != null)
                {
                    _sharedEnemyMat = new Material(outline)
                    {
                        name = "M_Enemy_Outline",
                        hideFlags = HideFlags.DontSave
                    };
                    _sharedEnemyMat.SetColor("_BaseColor", new Color(0.85f, 0.22f, 0.2f, 1f));
                    _sharedEnemyMat.SetColor("_OutlineColor", new Color(0.15f, 0f, 0f, 1f));
                    _sharedEnemyMat.SetFloat("_OutlineWidth", 0.01f);
                }
                else
                {
                    _sharedEnemyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    {
                        color = new Color(0.85f, 0.22f, 0.2f, 1f),
                        hideFlags = HideFlags.DontSave
                    };
                }
            }

            if (_sharedDissolveMat == null)
            {
                var dissolve = Shader.Find("ARPG/SimpleDissolve");
                if (dissolve != null)
                {
                    _sharedDissolveMat = new Material(dissolve)
                    {
                        name = "M_Enemy_Dissolve",
                        hideFlags = HideFlags.DontSave
                    };
                    _sharedDissolveMat.SetColor("_BaseColor", new Color(0.85f, 0.22f, 0.2f, 1f));
                    _sharedDissolveMat.SetColor("_EdgeColor", new Color(1f, 0.55f, 0.1f, 1f));
                    _sharedDissolveMat.SetFloat("_UseProceduralNoise", 1f);
                }
            }

            if (_sharedVfxMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
                _sharedVfxMat = new Material(sh)
                {
                    name = "M_SkillVfx",
                    hideFlags = HideFlags.DontSave,
                    color = new Color(1f, 0.85f, 0.2f, 0.8f)
                };
            }
        }

        static void EnsureEnvironment()
        {
            if (FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.96f, 0.9f);
                lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            cam.transform.position = new Vector3(0f, 12f, -10f);
            cam.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
            if (cam.GetComponent<CameraFollow>() == null)
                cam.gameObject.AddComponent<CameraFollow>();

            if (GameObject.Find("Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(4f, 1f, 4f);
                var rend = ground.GetComponent<Renderer>();
                if (rend != null)
                {
                    var gmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                    {
                        hideFlags = HideFlags.DontSave,
                        color = new Color(0.18f, 0.2f, 0.22f, 1f)
                    };
                    rend.sharedMaterial = gmat;
                }
            }
        }

        static void EnsurePostProcess()
        {
            if (FindAnyObjectByType<Volume>() != null)
                return;

            // URP Bloom + ACES Tonemapping：阶段三视觉项
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.DontSave;
            volume.profile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.contrast.Override(12f);
            colorAdj.saturation.Override(8f);

            // 确保相机开后处理
            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetComponent<UniversalAdditionalCameraData>();
                if (data == null)
                    data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
            }
        }

        static SimpleObjectPool EnsureVfxPool()
        {
            var existing = FindAnyObjectByType<SimpleObjectPool>();
            if (existing != null)
                return existing;

            var root = new GameObject("VfxPool");
            var pool = root.AddComponent<SimpleObjectPool>();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = "SkillVfxPulse";
            Object.Destroy(prefab.GetComponent<Collider>());
            prefab.GetComponent<Renderer>().sharedMaterial = _sharedVfxMat;
            prefab.AddComponent<PooledLifetime>();
            prefab.SetActive(false);
            prefab.transform.SetParent(root.transform, false);

            pool.Configure(prefab, 12);
            return pool;
        }

        static GameObject CreatePlayer(Vector3 pos, SimpleObjectPool pool)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag = "Player";
            go.transform.position = pos;
            go.GetComponent<Renderer>().sharedMaterial = _sharedPlayerMat;

            Object.Destroy(go.GetComponent<CapsuleCollider>());
            var cc = go.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = Vector3.up;

            var combatant = go.AddComponent<Combatant>();
            combatant.Configure(120f, 14f, destroyOnDeath: false);
            go.AddComponent<HitFeedback>();

            var socket = new GameObject("Weapon");
            socket.transform.SetParent(go.transform, false);
            socket.transform.localPosition = new Vector3(0.45f, 1f, 0.55f);

            var skills = go.AddComponent<SkillExecutor>();
            skills.SetDefaultVfxSocket(socket.transform);
            skills.SetEquippedSkills(CreateStarterSkills());
            skills.SetVfxPool(pool);

            var player = go.AddComponent<PlayerController>();
            player.BindCamera(Camera.main);

            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null)
                follow.SetTarget(go.transform);

            CreateWorldHealthBar(go.transform, combatant, new Color(0.25f, 0.9f, 0.45f));
            return go;
        }

        void SpawnEnemyRing(Transform player, SimpleObjectPool pool)
        {
            for (int i = 0; i < enemyCount; i++)
            {
                float ang = i * Mathf.PI * 2f / enemyCount;
                Vector3 pos = playerSpawn + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * enemyRingRadius;
                pos.y = 1f;
                bool brute = i % 3 == 0;
                CreateEnemy(pos, player, pool, brute);
            }
        }

        static void CreateEnemy(Vector3 pos, Transform player, SimpleObjectPool pool, bool brute)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = brute ? "Enemy_Brute" : "Enemy_Grunt";
            go.transform.position = pos;
            go.transform.localScale = brute ? new Vector3(1.35f, 1.35f, 1.35f) : Vector3.one;
            go.GetComponent<Renderer>().sharedMaterial = _sharedEnemyMat;

            var combatant = go.AddComponent<Combatant>();
            go.AddComponent<HitFeedback>();
            var dissolve = go.AddComponent<DeathDissolve>();
            dissolve.SetDissolveTemplate(_sharedDissolveMat);

            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            enemyData.enemyId = brute ? "brute" : "grunt";
            enemyData.displayName = brute ? "Brute" : "Grunt";
            enemyData.maxHealth = brute ? 110f : 55f;
            enemyData.attackPower = brute ? 12f : 7f;
            enemyData.detectRange = 16f;
            enemyData.attackRange = brute ? 2.1f : 2.4f;
            enemyData.moveSpeed = brute ? 2.4f : 3.4f;
            combatant.ApplyEnemyData(enemyData, destroyOnDeath: true);

            var skills = go.AddComponent<SkillExecutor>();
            var bite = ScriptableObject.CreateInstance<SkillData>();
            bite.skillId = "enemy_bite";
            bite.displayName = "Bite";
            bite.baseDamage = enemyData.attackPower;
            bite.cooldown = brute ? 1.8f : 1.35f;
            bite.range = enemyData.attackRange;
            bite.radius = 1.15f;
            bite.hitMask = ~0;
            skills.SetEquippedSkills(new[] { bite });
            skills.SetVfxPool(pool);

            var ai = go.AddComponent<SimpleEnemyAI>();
            ai.Configure(enemyData, player);

            CreateWorldHealthBar(go.transform, combatant, new Color(0.95f, 0.25f, 0.25f));
        }

        static SkillData[] CreateStarterSkills()
        {
            var basic = ScriptableObject.CreateInstance<SkillData>();
            basic.skillId = "basic_slash";
            basic.displayName = "Slash";
            basic.isBasicAttack = true;
            basic.baseDamage = 14f;
            basic.cooldown = 0.32f;
            basic.range = 2.1f;
            basic.radius = 1.15f;
            basic.canMoveWhileCasting = true;
            basic.hitMask = ~0;

            var skill1 = ScriptableObject.CreateInstance<SkillData>();
            skill1.skillId = "arc_cleave";
            skill1.displayName = "Cleave";
            skill1.baseDamage = 26f;
            skill1.cooldown = 2.2f;
            skill1.range = 2.8f;
            skill1.radius = 2.0f;
            skill1.attackPowerScale = 1.25f;
            skill1.hitMask = ~0;

            var skill2 = ScriptableObject.CreateInstance<SkillData>();
            skill2.skillId = "shockwave";
            skill2.displayName = "Shockwave";
            skill2.baseDamage = 20f;
            skill2.cooldown = 3.6f;
            skill2.range = 0.5f;
            skill2.radius = 3.6f;
            skill2.attackPowerScale = 0.9f;
            skill2.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.35f);
            skill2.hitMask = ~0;

            return new[] { basic, skill1, skill2 };
        }

        static void CreateWorldHealthBar(Transform owner, Combatant combatant, Color fillColor)
        {
            var canvasGo = new GameObject("HealthBar");
            canvasGo.transform.SetParent(owner, false);
            canvasGo.transform.localPosition = new Vector3(0f, 2.35f, 0f);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 18f);
            rt.localScale = Vector3.one * 0.01f;

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(bg.GetComponent<RectTransform>());

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(canvasGo.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);

            var bar = canvasGo.AddComponent<HealthBarUI>();
            bar.Bind(combatant, fill);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void CreateHud(SkillExecutor executor)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            CreateHint(canvasGo.transform);
            CreateSkillSlot(canvasGo.transform, executor, 0, "J/LMB", new Vector2(-140f, 48f));
            CreateSkillSlot(canvasGo.transform, executor, 1, "Q/1", new Vector2(0f, 48f));
            CreateSkillSlot(canvasGo.transform, executor, 2, "E/2", new Vector2(140f, 48f));
        }

        static void CreateHint(Transform parent)
        {
            var go = new GameObject("Hint");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22;
            text.color = new Color(1f, 1f, 1f, 0.85f);
            text.alignment = TextAnchor.UpperCenter;
            text.text = "WASD Move   ·   J / LMB Attack   ·   Q Cleave   ·   E Shockwave";
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(900f, 40f);
            rt.anchoredPosition = new Vector2(0f, -24f);
        }

        static void CreateSkillSlot(Transform parent, SkillExecutor executor, int index, string label, Vector2 anchoredPos)
        {
            var slot = new GameObject($"SkillSlot_{index}");
            slot.transform.SetParent(parent, false);
            var rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(84f, 84f);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;

            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.16f, 0.92f);

            var overlayGo = new GameObject("CD");
            overlayGo.transform.SetParent(slot.transform, false);
            var overlay = overlayGo.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.7f);
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = true;
            overlay.fillAmount = 0f;
            Stretch(overlayGo.GetComponent<RectTransform>());

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(slot.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = 18;
            text.text = label;
            Stretch(textGo.GetComponent<RectTransform>());

            var cd = slot.AddComponent<SkillCooldownUI>();
            cd.Bind(executor, index, overlay, text);
        }
    }
}
