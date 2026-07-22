#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ARPG.Skills;
using UnityEditor;
using UnityEngine;

namespace ARPG.EditorTools
{
    /// <summary>
    /// 技能可视化配置窗口：列表 + Slider 编辑 + 伤害等级柱状预览。
    /// 菜单：ARPG Tools > Skill Editor
    /// </summary>
    public class SkillEditorWindow : EditorWindow
    {
        Vector2 _listScroll;
        Vector2 _detailScroll;
        SkillData _selected;
        string _search = string.Empty;
        List<SkillData> _skills = new();
        float _previewAttackPower = 10f;

        [MenuItem("ARPG Tools/Skill Editor")]
        public static void Open()
        {
            var window = GetWindow<SkillEditorWindow>("Skill Editor");
            window.minSize = new Vector2(780, 480);
            window.RefreshSkillList();
        }

        void OnEnable() => RefreshSkillList();
        void OnFocus() => RefreshSkillList();

        void RefreshSkillList()
        {
            _skills = AssetDatabase.FindAssets("t:SkillData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SkillData>)
                .Where(s => s != null)
                .OrderBy(s => s.displayName)
                .ToList();

            if (_selected != null && !_skills.Contains(_selected))
                _selected = null;
        }

        void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSkillList();
                DrawSkillDetail();
            }
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    RefreshSkillList();

                if (GUILayout.Button("Create Skill", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    CreateSkillAsset();

                GUILayout.Space(8);
                _search = GUILayout.TextField(_search ?? string.Empty, EditorStyles.toolbarSearchField);
            }
        }

        void DrawSkillList()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(230)))
            {
                EditorGUILayout.LabelField($"Skills ({_skills.Count})", EditorStyles.boldLabel);
                if (_skills.Count == 0)
                {
                    EditorGUILayout.HelpBox("项目里还没有 SkillData。\n点 Create Skill，或菜单 ARPG Tools > Create Sample Data。", MessageType.Warning);
                }

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                foreach (var skill in FilteredSkills())
                {
                    bool selected = skill == _selected;
                    var bg = selected ? new Color(0.24f, 0.48f, 0.90f, 0.35f) : new Color(0f, 0f, 0f, 0f);
                    var rect = GUILayoutUtility.GetRect(210, 24);
                    EditorGUI.DrawRect(rect, bg);
                    if (GUI.Button(rect, $"  {skill.displayName}", EditorStyles.label))
                        _selected = skill;
                }
                EditorGUILayout.EndScrollView();
            }
        }

        IEnumerable<SkillData> FilteredSkills()
        {
            if (string.IsNullOrWhiteSpace(_search))
                return _skills;
            string q = _search.Trim().ToLowerInvariant();
            return _skills.Where(s =>
                (!string.IsNullOrEmpty(s.displayName) && s.displayName.ToLowerInvariant().Contains(q)) ||
                (!string.IsNullOrEmpty(s.skillId) && s.skillId.ToLowerInvariant().Contains(q)));
        }

        void DrawSkillDetail()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                if (_selected == null)
                {
                    EditorGUILayout.HelpBox(
                        "请选择左侧技能，或点击 Create Skill。\n右侧不会在未选中时抛错——这是编辑器容错的基本要求。",
                        MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(_selected.displayName, EditorStyles.boldLabel);
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                Undo.RecordObject(_selected, "Edit SkillData");

                _selected.skillId = EditorGUILayout.TextField("Skill Id", _selected.skillId);
                _selected.displayName = EditorGUILayout.TextField("Display Name", _selected.displayName);
                EditorGUILayout.LabelField("Description");
                _selected.description = EditorGUILayout.TextArea(_selected.description, GUILayout.MinHeight(48));

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Combat (Sliders)", EditorStyles.boldLabel);
                _selected.baseDamage = EditorGUILayout.Slider("Base Damage", _selected.baseDamage, 0f, 200f);
                _selected.attackPowerScale = EditorGUILayout.Slider("Attack Power Scale", _selected.attackPowerScale, 0f, 3f);
                _selected.cooldown = EditorGUILayout.Slider("Cooldown", _selected.cooldown, 0f, 20f);
                _selected.castTime = EditorGUILayout.Slider("Cast Time", _selected.castTime, 0f, 5f);
                _selected.range = EditorGUILayout.Slider("Range", _selected.range, 0f, 15f);
                _selected.radius = EditorGUILayout.Slider("Radius", _selected.radius, 0.1f, 10f);

                EditorGUILayout.Space(4);
                _selected.hitMask = LayerMaskField("Hit Mask", _selected.hitMask);
                _selected.damageFalloff = EditorGUILayout.CurveField("Damage Falloff", _selected.damageFalloff);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Feedback", EditorStyles.boldLabel);
                _selected.vfxPrefab = (GameObject)EditorGUILayout.ObjectField("VFX Prefab", _selected.vfxPrefab, typeof(GameObject), false);
                _selected.sfxClip = (AudioClip)EditorGUILayout.ObjectField("SFX Clip", _selected.sfxClip, typeof(AudioClip), false);
                _selected.vfxSocketName = EditorGUILayout.TextField("VFX Socket", _selected.vfxSocketName);

                EditorGUILayout.Space(4);
                _selected.isBasicAttack = EditorGUILayout.Toggle("Is Basic Attack", _selected.isBasicAttack);
                _selected.canMoveWhileCasting = EditorGUILayout.Toggle("Can Move While Casting", _selected.canMoveWhileCasting);

                EditorGUILayout.Space(10);
                DrawDamageLevelPreview();

                if (GUI.changed)
                    EditorUtility.SetDirty(_selected);

                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ping Asset"))
                        EditorGUIUtility.PingObject(_selected);
                    if (GUILayout.Button("Select in Project"))
                        Selection.activeObject = _selected;
                }
            }
        }

        void DrawDamageLevelPreview()
        {
            EditorGUILayout.LabelField("Damage Preview by Level", EditorStyles.boldLabel);
            _previewAttackPower = EditorGUILayout.Slider("Preview Attack Power", _previewAttackPower, 0f, 50f);

            // 固定纵轴上限，避免“全体同比例变高→归一化后看起来没变化”
            const float axisMax = 200f;
            const int levels = 10;
            float[] values = new float[levels];
            for (int i = 0; i < levels; i++)
            {
                int level = i + 1;
                float levelScale = 1f + 0.1f * (level - 1);
                float falloff = _selected.damageFalloff != null
                    ? _selected.damageFalloff.Evaluate(Mathf.Clamp01((level - 1) / 9f))
                    : 1f;
                values[i] = (_selected.baseDamage + _previewAttackPower * _selected.attackPowerScale) * levelScale * falloff;
            }

            Rect rect = GUILayoutUtility.GetRect(16, 140);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.14f, 1f));

            // 纵轴参考线
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            for (int t = 1; t <= 4; t++)
            {
                float y = rect.yMax - 20f - (rect.height - 36f) * (t / 4f);
                Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
            }
            Handles.EndGUI();

            float barW = rect.width / levels;
            float plotH = rect.height - 36f;
            for (int i = 0; i < levels; i++)
            {
                float h = Mathf.Clamp01(values[i] / axisMax) * plotH;
                var bar = new Rect(rect.x + i * barW + 3f, rect.yMax - 18f - h, barW - 6f, h);
                EditorGUI.DrawRect(bar, new Color(0.95f, 0.55f, 0.15f, 0.95f));

                var valueRect = new Rect(rect.x + i * barW, bar.y - 14f, barW, 14f);
                GUI.Label(valueRect, values[i].ToString("0"), EditorStyles.centeredGreyMiniLabel);

                var lvRect = new Rect(rect.x + i * barW, rect.yMax - 16f, barW, 16f);
                GUI.Label(lvRect, (i + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.LabelField($"纵轴固定 0–{axisMax:0}（拖 Base Damage 会明显变高/变矮；柱顶有伤害数字）");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Cooldown / Range readout", EditorStyles.boldLabel);
            DrawMeter("Cooldown", _selected.cooldown, 20f, new Color(0.3f, 0.7f, 1f));
            DrawMeter("Range", _selected.range, 15f, new Color(0.4f, 0.9f, 0.5f));
            DrawMeter("Radius", _selected.radius, 10f, new Color(0.9f, 0.4f, 0.7f));

            EditorGUILayout.HelpBox(
                $"Lv1≈{values[0]:0.#}  Lv5≈{values[4]:0.#}  Lv10≈{values[9]:0.#}  |  CD={_selected.cooldown:0.##}s  Range={_selected.range:0.##}",
                MessageType.None);
        }

        static void DrawMeter(string label, float value, float max, Color color)
        {
            Rect row = GUILayoutUtility.GetRect(16, 18);
            var labelRect = new Rect(row.x, row.y, 70f, row.height);
            GUI.Label(labelRect, label);

            var track = new Rect(row.x + 74f, row.y + 4f, row.width - 120f, 10f);
            EditorGUI.DrawRect(track, new Color(0.2f, 0.2f, 0.22f));
            float fill = Mathf.Clamp01(value / Mathf.Max(0.01f, max));
            EditorGUI.DrawRect(new Rect(track.x, track.y, track.width * fill, track.height), color);

            var num = new Rect(track.xMax + 6f, row.y, 40f, row.height);
            GUI.Label(num, value.ToString("0.##"));
        }

        static LayerMask LayerMaskField(string label, LayerMask selected)
        {
            var layers = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                layers.Add(string.IsNullOrEmpty(name) ? $"Layer {i}" : name);
            }
            return EditorGUILayout.MaskField(label, selected.value, layers.ToArray());
        }

        void CreateSkillAsset()
        {
            string folder = "Assets/Data/Skills";
            EnsureFolder(folder);

            var skill = CreateInstance<SkillData>();
            skill.skillId = "skill_new";
            skill.displayName = "New Skill";
            skill.baseDamage = 15f;
            skill.cooldown = 1f;
            skill.range = 2f;
            skill.radius = 1.2f;
            skill.hitMask = ~0;
            skill.damageFalloff = AnimationCurve.Linear(0f, 1f, 1f, 1f);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NewSkill.asset");
            AssetDatabase.CreateAsset(skill, path);
            AssetDatabase.SaveAssets();
            RefreshSkillList();
            _selected = skill;
            Selection.activeObject = skill;
            EditorGUIUtility.PingObject(skill);
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string[] parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
