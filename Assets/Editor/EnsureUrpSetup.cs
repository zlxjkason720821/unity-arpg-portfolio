#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ARPG.EditorTools
{
    /// <summary>
    /// 首次打开工程时把 Graphics/Quality 切到 URP，消除 Hub「Built-In 已弃用」黄标。
    /// 菜单：ARPG Tools > Ensure URP Setup
    /// </summary>
    public static class EnsureUrpSetup
    {
        const string Folder = "Assets/Settings";
        const string RendererPath = Folder + "/URP_Renderer.asset";
        const string PipelinePath = Folder + "/URP_Pipeline.asset";

        [InitializeOnLoadMethod]
        static void AutoRun()
        {
            // 延迟一帧，等 Package/AssetDatabase 就绪
            EditorApplication.delayCall += () =>
            {
                if (GraphicsSettings.defaultRenderPipeline == null)
                    Setup(silent: true);
            };
        }

        [MenuItem("ARPG Tools/Ensure URP Setup")]
        public static void SetupFromMenu() => Setup(silent: false);

        /// <summary>供 batchmode：Unity.exe -executeMethod ARPG.EditorTools.EnsureUrpSetup.SetupBatch</summary>
        public static void SetupBatch()
        {
            Setup(silent: true);
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        static void Setup(bool silent)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                // 确保 renderer 列表非空
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                if (list != null && list.arraySize == 0)
                {
                    list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pipeline);
                }
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // 所有 Quality Level 都挂上同一 URP Asset
            var qualityCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityCount; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();

            if (!silent)
                Debug.Log($"[EnsureUrpSetup] URP 已启用：{PipelinePath}");
            else
                Debug.Log($"[EnsureUrpSetup] Auto: GraphicsSettings → {pipeline.name}");
        }
    }
}
#endif
