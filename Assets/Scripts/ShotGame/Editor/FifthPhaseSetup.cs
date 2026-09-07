#if UNITY_EDITOR
using System;
using ShotGame.Gameplay.Config;
using ShotGame.Presentation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Editor
{
    public static class FifthPhaseSetup
    {
        private const string GrazeConfigPath = "Assets/Res/Gameplay/Graze/GrazeConfig.asset";
        private const string GameplayConfigPath = "Assets/Res/Config/GameplayContentConfig.asset";
        private const string GameplayUiPath = "Assets/Res/UI/Panel_Gameplay.prefab";
        private const string CompletionMarkerPath =
            "Assets/Scripts/ShotGame/Editor/FifthPhaseSetupComplete.asset";

        [InitializeOnLoadMethod]
        private static void ExecuteOnceAfterCompile()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<FifthPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            EditorApplication.delayCall += Execute;
        }

        [MenuItem("Shot Game/Setup Fifth Phase")]
        public static void Execute()
        {
            try
            {
                EnsureFolder("Assets/Res/Gameplay", "Graze");
                var grazeConfig = AssetDatabase.LoadAssetAtPath<GrazeConfig>(GrazeConfigPath);
                if (grazeConfig == null)
                {
                    grazeConfig = ScriptableObject.CreateInstance<GrazeConfig>();
                    AssetDatabase.CreateAsset(grazeConfig, GrazeConfigPath);
                }
                ConfigureGameplayContent(grazeConfig);
                ConfigureGameplayUi();
                CreateCompletionMarker();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("第五阶段擦弹配置、充能与子弹时间 HUD 创建完成。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void ConfigureGameplayContent(GrazeConfig grazeConfig)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameplayContentConfig>(GameplayConfigPath);
            if (content == null) throw new InvalidOperationException("缺少 GameplayContentConfig。");
            var serialized = new SerializedObject(content);
            serialized.FindProperty("_grazeConfig").objectReferenceValue = grazeConfig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);
        }

        private static void ConfigureGameplayUi()
        {
            var root = PrefabUtility.LoadPrefabContents(GameplayUiPath);
            try
            {
                var screen = root.GetComponent<GameplayScreen>();
                if (screen == null) throw new InvalidOperationException("Gameplay HUD 缺少 GameplayScreen。");
                var graze = FindOrCreateLabel(root.transform, "GrazeStatus", "擦弹  Idle", -120f);
                var charge = FindOrCreateLabel(root.transform, "ChargeStatus", "充能  Lv.0  连段 0", -155f);
                var time = FindOrCreateLabel(root.transform, "TimeStatus", "子弹时间  ×1.00", -190f);
                var serialized = new SerializedObject(screen);
                serialized.FindProperty("_grazeText").objectReferenceValue = graze;
                serialized.FindProperty("_chargeText").objectReferenceValue = charge;
                serialized.FindProperty("_timeText").objectReferenceValue = time;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GameplayUiPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Text FindOrCreateLabel(Transform parent, string name, string text, float y)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                var existing = child.GetComponent<Text>();
                if (existing != null) return existing;
            }

            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, y);
            rect.sizeDelta = new Vector2(700f, 32f);
            var label = gameObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = 18;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateCompletionMarker()
        {
            if (AssetDatabase.LoadAssetAtPath<FifthPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FifthPhaseSetupMarker>(),
                CompletionMarkerPath);
        }
    }
}
#endif
