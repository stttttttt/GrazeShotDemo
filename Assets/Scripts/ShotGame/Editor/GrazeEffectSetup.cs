#if UNITY_EDITOR
using System;
using ShotGame.Gameplay.Config;
using ShotGame.Presentation.Feedback;
using UnityEditor;
using UnityEngine;

namespace ShotGame.Editor
{
    /// <summary>创建擦弹圆环材质，并将表现节点接入玩家预制体。</summary>
    public static class GrazeEffectSetup
    {
        private const string ShaderPath = "Assets/Shaders/GrazeRing.shader";
        private const string MaterialFolder = "Assets/Res/Gameplay/Feel/Materials";
        private const string MaterialPath = MaterialFolder + "/GrazeRing.mat";
        private const string PlayerPrefabPath = "Assets/Res/Gameplay/Player/Player.prefab";
        private const string FeelConfigPath = "Assets/Res/Gameplay/Feel/GameplayFeelConfig.asset";
        private const string GrazeConfigPath = "Assets/Res/Gameplay/Graze/GrazeConfig.asset";

        [MenuItem("Shot Game/Setup Graze Effect")]
        public static void Execute()
        {
            try
            {
                EnsureFolder("Assets/Res/Gameplay/Feel", "Materials");
                var material = CreateOrUpdateMaterial();
                ConfigurePlayerPrefab(material);
                ConfigureFeelConfig();
                ConfigureGrazeConfig();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("擦弹窗口 Shader、材质、表现配置与玩家预制体接入完成。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static Material CreateOrUpdateMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) shader = Shader.Find("ShotGame/GrazeRing");
            if (shader == null) throw new InvalidOperationException($"无法加载擦弹 Shader：{ShaderPath}");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "GrazeRing" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else material.shader = shader;

            material.SetColor("_BaseColor", new Color(0.35f, 1f, 0.72f, 1f));
            material.SetFloat("_Alpha", 1f);
            material.SetFloat("_Thickness", 0.018f);
            material.SetFloat("_Softness", 0.006f);
            material.SetFloat("_Brightness", 1f);
            material.SetFloat("_ArcAmount", 0f);
            material.SetFloat("_NoiseStrength", 0f);
            material.SetFloat("_Rotation", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigurePlayerPrefab(Material material)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
                throw new InvalidOperationException($"缺少玩家预制体：{PlayerPrefabPath}");

            var player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var previousRoot = player.transform.Find("GrazeEffectRoot");
                if (previousRoot != null) UnityEngine.Object.DestroyImmediate(previousRoot.gameObject);

                var effectRoot = new GameObject("GrazeEffectRoot").transform;
                effectRoot.SetParent(player.transform, false);
                effectRoot.localPosition = Vector3.zero;

                var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                var main = CreateRenderer(effectRoot, "GrazeRing", material, sprite, 8);
                var pulse = CreateRenderer(effectRoot, "PerfectPulse", material, sprite, 9);

                var view = player.GetComponent<PlayerGrazeEffectView>();
                if (view == null) view = player.AddComponent<PlayerGrazeEffectView>();
                var serializedView = new SerializedObject(view);
                serializedView.FindProperty("_mainRenderer").objectReferenceValue = main;
                serializedView.FindProperty("_pulseRenderer").objectReferenceValue = pulse;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                main.enabled = false;
                pulse.enabled = false;
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static SpriteRenderer CreateRenderer(Transform parent, string name, Material material,
            Sprite sprite, int sortingOrder)
        {
            var target = new GameObject(name, typeof(SpriteRenderer));
            target.transform.SetParent(parent, false);
            var renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static void ConfigureFeelConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayFeelConfig>(FeelConfigPath);
            if (config == null) throw new InvalidOperationException($"缺少手感配置：{FeelConfigPath}");
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("_grazeRingMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            serializedConfig.FindProperty("_grazeStartupColor").colorValue = new Color(0.25f, 0.7f, 1f, 1f);
            serializedConfig.FindProperty("_grazePerfectColor").colorValue = new Color(0.35f, 1f, 0.72f, 1f);
            serializedConfig.FindProperty("_grazeActiveColor").colorValue = new Color(1f, 0.82f, 0.25f, 1f);
            serializedConfig.FindProperty("_grazeCooldownColor").colorValue = new Color(0.3f, 0.38f, 0.48f, 1f);
            serializedConfig.FindProperty("_grazeStartupScale").floatValue = 1.45f;
            serializedConfig.FindProperty("_grazeStartupVisualDuration").floatValue = 0.16f;
            serializedConfig.FindProperty("_grazeRingThickness").floatValue = 0.018f;
            serializedConfig.FindProperty("_grazeRingSoftness").floatValue = 0.006f;
            serializedConfig.FindProperty("_grazeActiveAlpha").floatValue = 0.3f;
            serializedConfig.FindProperty("_grazePerfectBrightness").floatValue = 2.5f;
            serializedConfig.FindProperty("_grazePerfectPulseScale").floatValue = 1.8f;
            serializedConfig.FindProperty("_grazePerfectPulseDuration").floatValue = 0.28f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            config.Validate();
        }

        private static void ConfigureGrazeConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GrazeConfig>(GrazeConfigPath);
            if (config == null) throw new InvalidOperationException($"缺少擦弹配置：{GrazeConfigPath}");
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("_totalCooldown").floatValue = 0.5f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            config.Validate();
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
