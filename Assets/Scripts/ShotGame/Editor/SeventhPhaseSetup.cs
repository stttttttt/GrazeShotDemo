#if UNITY_EDITOR
using System;
using System.Linq;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Scene;
using ShotGame.Gameplay.Weapon;
using ShotGame.Presentation.Feedback;
using ShotGame.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShotGame.Editor
{
    /// <summary>建立第七阶段手感配置、被动表现绑定、正式 HUD 和占位资源。</summary>
    public static class SeventhPhaseSetup
    {
        private const string FeelFolder = "Assets/Res/Gameplay/Feel";
        private const string FeelConfigPath = FeelFolder + "/GameplayFeelConfig.asset";
        private const string WorldEffectPath = FeelFolder + "/WorldEffect.prefab";
        private const string DamageNumberPath = FeelFolder + "/DamageNumber.prefab";
        private const string HealthBarPath = FeelFolder + "/WorldHealthBar.prefab";
        private const string GameplayConfigPath = "Assets/Res/Config/GameplayContentConfig.asset";
        private const string GameplayUiPath = "Assets/Res/UI/Panel_Gameplay.prefab";
        private const string GameplayScenePath = "Assets/Scenes/GamePlay.unity";
        private const string MuzzleSheetPath =
            "Assets/Art/New_All_Fire_Bullet_Pixel_16x16/All_Fire_Bullet_Pixel_16x16_00.png";
        private const string EnemyDissolveMaterialPath =
            "Assets/Res/Gameplay/Feel/Materials/EnemyDissolve.mat";
        private const string MarkerPath = "Assets/Scripts/ShotGame/Editor/SeventhPhaseSetupComplete.asset";
        private const string ExpansionMarkerPath =
            "Assets/Scripts/ShotGame/Editor/GameplayExpansionSetupComplete.asset";
        private const string ReloadUiMarkerPath =
            "Assets/Scripts/ShotGame/Editor/ReloadUiSetupComplete.asset";

        [InitializeOnLoadMethod]
        private static void ExecuteOnceAfterCompile()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<SeventhPhaseSetupMarker>(MarkerPath) != null) return;
            EditorApplication.delayCall += Execute;
        }

        [InitializeOnLoadMethod]
        private static void RefreshGameplayHudOnce()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<SeventhPhaseSetupMarker>(ExpansionMarkerPath) != null) return;
            EditorApplication.delayCall += () =>
            {
                var damage = AssetDatabase.LoadAssetAtPath<DamageNumberView>(DamageNumberPath);
                var health = AssetDatabase.LoadAssetAtPath<WorldHealthBarView>(HealthBarPath);
                if (damage == null || health == null) return;
                ConfigureGameplayHud(damage, health);
                CreateMarker(ExpansionMarkerPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Gameplay HUD 已更新：精简冲击波指示器并加入按键说明。");
            };
        }

        [InitializeOnLoadMethod]
        private static void AddReloadUiOnce()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<SeventhPhaseSetupMarker>(ReloadUiMarkerPath) != null) return;
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPath) == null) return;
                var root = PrefabUtility.LoadPrefabContents(HealthBarPath);
                try
                {
                    var view = root.GetComponent<WorldHealthBarView>();
                    if (view == null) return;
                    view.SetReloadProgress(true, 0f);
                    view.SetReloadProgress(false, 0f);
                    PrefabUtility.SaveAsPrefabAsset(root, HealthBarPath);
                    CreateMarker(ReloadUiMarkerPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log("玩家头顶血条已加入换弹进度 UI。");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            };
        }

        [MenuItem("Shot Game/Setup Seventh Phase")]
        public static void Execute()
        {
            try
            {
                EnsureFolder("Assets/Res/Gameplay", "Feel");
                var worldEffect = CreateWorldEffectPrefab();
                var damageNumber = CreateDamageNumberPrefab();
                var healthBar = CreateHealthBarPrefab();
                var feel = CreateFeelConfig(worldEffect);
                ConfigureContent(feel);
                ConfigureEntityPrefab("Assets/Res/Gameplay/Player/Player.prefab", 0.65f);
                ConfigureEntityPrefab("Assets/Res/Gameplay/Enemy/TestEnemy.prefab", 0.65f);
                ConfigureEntityPrefab("Assets/Res/Gameplay/Enemy/SlowEnemy.prefab", 0.65f);
                ConfigureEntityPrefab("Assets/Res/Gameplay/Enemy/ChaserEnemy.prefab", 0.65f);
                ConfigureEntityPrefab("Assets/Res/Gameplay/Enemy/EliteEnemy.prefab", 0.95f);
                ConfigureProjectilePrefab("Assets/Res/Gameplay/Projectile/PlayerShotgunProjectile.prefab",
                    new Color(1f, 0.78f, 0.22f, 0.9f), 0.14f);
                ConfigureProjectilePrefab("Assets/Res/Gameplay/Projectile/PlayerSMGProjectile.prefab",
                    new Color(0.25f, 0.9f, 1f, 0.9f), 0.09f);
                ConfigureProjectilePrefab("Assets/Res/Gameplay/Projectile/EnemyProjectile.prefab",
                    new Color(1f, 0.2f, 0.22f, 0.9f), 0.12f);
                ConfigureGameplayHud(damageNumber, healthBar);
                ConfigureGameplayScene();
                CreateMarker(MarkerPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("第七阶段手感配置、表现绑定、正式 HUD 与占位资源创建完成。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static GameplayFeelConfig CreateFeelConfig(GameObject worldEffect)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayFeelConfig>(FeelConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameplayFeelConfig>();
                AssetDatabase.CreateAsset(config, FeelConfigPath);
            }
            var weapons = new[]
            {
                AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/Res/Gameplay/Weapon/PlayerShotgun.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/Res/Gameplay/Weapon/PlayerSMG.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/Res/Gameplay/Weapon/PlayerSniper.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/Res/Gameplay/Weapon/TestEnemyGun.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/Res/Gameplay/Weapon/EnemyRapidGun.asset"),
                AssetDatabase.LoadAssetAtPath<WeaponConfig>("Assets/Res/Gameplay/Weapon/EnemyEliteGun.asset")
            }.Where(item => item != null).ToArray();
            var so = new SerializedObject(config);
            so.FindProperty("_worldEffectPrefab").objectReferenceValue = worldEffect;
            var muzzleFrames = AssetDatabase.LoadAllAssetsAtPath(MuzzleSheetPath)
                .OfType<Sprite>()
                .Where(sprite => IsSelectedMuzzleFrame(sprite.name))
                .OrderBy(sprite => sprite.name)
                .ToArray();
            if (muzzleFrames.Length != 5)
                throw new InvalidOperationException($"{MuzzleSheetPath} 缺少枪口焰序列帧 168-172。");
            var muzzleFrameProperty = so.FindProperty("_muzzleFrames");
            muzzleFrameProperty.arraySize = muzzleFrames.Length;
            for (var i = 0; i < muzzleFrames.Length; i++)
                muzzleFrameProperty.GetArrayElementAtIndex(i).objectReferenceValue = muzzleFrames[i];
            so.FindProperty("_muzzleFrameDuration").floatValue = 0.035f;
            var hitFrames = LoadSpriteFrames(457, 443);
            var hitFrameProperty = so.FindProperty("_hitEffectFrames");
            hitFrameProperty.arraySize = hitFrames.Length;
            for (var i = 0; i < hitFrames.Length; i++)
                hitFrameProperty.GetArrayElementAtIndex(i).objectReferenceValue = hitFrames[i];
            so.FindProperty("_hitEffectFrameDuration").floatValue = 0.055f;
            var dissolveMaterial = AssetDatabase.LoadAssetAtPath<Material>(EnemyDissolveMaterialPath);
            if (dissolveMaterial == null)
                throw new InvalidOperationException($"缺少敌人死亡溶解材质：{EnemyDissolveMaterialPath}");
            so.FindProperty("_enemyDissolveMaterial").objectReferenceValue = dissolveMaterial;
            var entries = so.FindProperty("_weaponFeedback");
            entries.arraySize = weapons.Length;
            for (var i = 0; i < weapons.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_weapon").objectReferenceValue = weapons[i];
                var shotgun = weapons[i].name.Contains("Shotgun");
                var sniper = weapons[i].name.Contains("Sniper");
                var enemy = weapons[i].name.Contains("Enemy") || weapons[i].name.Contains("TestEnemy");
                entry.FindPropertyRelative("_muzzleSize").floatValue = sniper ? 1.05f : shotgun ? 0.72f : enemy ? 0.35f : 0.42f;
                entry.FindPropertyRelative("_cameraKick").floatValue = sniper ? 0.18f : shotgun ? 0.16f : enemy ? 0f : 0.045f;
                entry.FindPropertyRelative("_shakeStrength").floatValue = sniper ? 0.18f : shotgun ? 0.1f : enemy ? 0f : 0.025f;
                entry.FindPropertyRelative("_shakeDuration").floatValue = sniper ? 0.18f : shotgun ? 0.12f : 0.055f;
                entry.FindPropertyRelative("_hitStopDuration").floatValue = sniper ? 0.035f : shotgun ? 0.022f : enemy ? 0f : 0.006f;
                entry.FindPropertyRelative("_hitStopCooldown").floatValue = sniper ? 0.2f : shotgun ? 0.08f : 0.07f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            config.Validate();
            return config;
        }

        private static bool IsSelectedMuzzleFrame(string spriteName)
        {
            for (var index = 168; index <= 172; index++)
                if (spriteName.EndsWith("_" + index, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static Sprite[] LoadSpriteFrames(params int[] indices)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(MuzzleSheetPath).OfType<Sprite>().ToArray();
            var result = new Sprite[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                var expectedName = $"All_Fire_Bullet_Pixel_16x16_00_{indices[i]}";
                result[i] = sprites.FirstOrDefault(sprite => sprite.name == expectedName);
                if (result[i] == null)
                    throw new InvalidOperationException($"{MuzzleSheetPath} 缺少序列帧 {expectedName}。");
            }
            return result;
        }

        private static void ConfigureContent(GameplayFeelConfig feel)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameplayContentConfig>(GameplayConfigPath);
            if (content == null) throw new InvalidOperationException($"缺少 {GameplayConfigPath}");
            var so = new SerializedObject(content);
            so.FindProperty("_feelConfig").objectReferenceValue = feel;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);
        }

        private static GameObject CreateWorldEffectPrefab()
        {
            var root = new GameObject("WorldEffect", typeof(SpriteRenderer), typeof(WorldEffectView));
            try
            {
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                renderer.sortingOrder = 50;
                var so = new SerializedObject(root.GetComponent<WorldEffectView>());
                so.FindProperty("_renderer").objectReferenceValue = renderer;
                so.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(false);
                return PrefabUtility.SaveAsPrefabAsset(root, WorldEffectPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static DamageNumberView CreateDamageNumberPrefab()
        {
            var root = new GameObject("DamageNumber", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Text), typeof(DamageNumberView));
            try
            {
                root.layer = 5;
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(120f, 46f);
                var text = root.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 23;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                var so = new SerializedObject(root.GetComponent<DamageNumberView>());
                so.FindProperty("_text").objectReferenceValue = text;
                so.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(false);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, DamageNumberPath);
                return prefab.GetComponent<DamageNumberView>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static WorldHealthBarView CreateHealthBarPrefab()
        {
            var root = new GameObject("WorldHealthBar", typeof(RectTransform), typeof(CanvasGroup),
                typeof(WorldHealthBarView));
            try
            {
                root.layer = 5;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(76f, 10f);
                var background = CreateImage(root.transform, "Background", new Color(0.04f, 0.05f, 0.08f, 0.92f));
                Stretch(background.rectTransform, 0f);
                var delayed = CreateFill(root.transform, "DelayedDamage", new Color(1f, 0.78f, 0.2f, 1f), 2f);
                var immediate = CreateFill(root.transform, "Immediate", new Color(0.9f, 0.2f, 0.2f, 1f), 2f);
                var so = new SerializedObject(root.GetComponent<WorldHealthBarView>());
                so.FindProperty("_immediateFill").objectReferenceValue = immediate;
                so.FindProperty("_delayedDamageFill").objectReferenceValue = delayed;
                so.FindProperty("_canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
                so.ApplyModifiedPropertiesWithoutUndo();
                var view = root.GetComponent<WorldHealthBarView>();
                view.SetReloadProgress(true, 0f);
                view.SetReloadProgress(false, 0f);
                root.SetActive(false);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, HealthBarPath);
                return prefab.GetComponent<WorldHealthBarView>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ConfigureEntityPrefab(string path, float anchorHeight)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visualRoot = root.transform.Find("VisualRoot");
                if (visualRoot == null)
                {
                    visualRoot = new GameObject("VisualRoot").transform;
                    visualRoot.SetParent(root.transform, false);
                    var rootRenderer = root.GetComponent<SpriteRenderer>();
                    if (rootRenderer != null)
                    {
                        var copy = visualRoot.gameObject.AddComponent<SpriteRenderer>();
                        copy.sprite = rootRenderer.sprite;
                        copy.color = rootRenderer.color;
                        copy.flipX = rootRenderer.flipX;
                        copy.flipY = rootRenderer.flipY;
                        copy.sortingLayerID = rootRenderer.sortingLayerID;
                        copy.sortingOrder = rootRenderer.sortingOrder;
                        copy.sharedMaterial = rootRenderer.sharedMaterial;
                        UnityEngine.Object.DestroyImmediate(rootRenderer);
                    }
                }
                var anchor = root.transform.Find("HealthBarAnchor");
                if (anchor == null)
                {
                    anchor = new GameObject("HealthBarAnchor").transform;
                    anchor.SetParent(root.transform, false);
                }
                anchor.localPosition = new Vector3(0f, anchorHeight, 0f);
                var view = root.GetComponent<EntityFeedbackView>();
                if (view == null) view = root.AddComponent<EntityFeedbackView>();
                var so = new SerializedObject(view);
                so.FindProperty("_visualRoot").objectReferenceValue = visualRoot;
                so.FindProperty("_healthBarAnchor").objectReferenceValue = anchor;
                var renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
                var array = so.FindProperty("_renderers");
                array.arraySize = renderers.Length;
                for (var i = 0; i < renderers.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureProjectilePrefab(string path, Color color, float width)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var trail = root.GetComponent<TrailRenderer>();
                if (trail == null) trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.1f;
                trail.minVertexDistance = 0.04f;
                trail.startWidth = width;
                trail.endWidth = 0f;
                trail.startColor = color;
                var end = color;
                end.a = 0f;
                trail.endColor = end;
                trail.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
                trail.sortingOrder = 20;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureGameplayHud(DamageNumberView damagePrefab,
            WorldHealthBarView healthBarPrefab)
        {
            var root = PrefabUtility.LoadPrefabContents(GameplayUiPath);
            try
            {
                var old = root.transform.Find("SeventhPhaseHud");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var hud = new GameObject("SeventhPhaseHud", typeof(RectTransform)).transform;
                hud.SetParent(root.transform, false);
                Stretch((RectTransform)hud, 0f);

                var health = CreatePlayerHealth(hud);
                var graze = CreateGrazeIndicator(hud);
                var controlsHelp = CreateControlsHelp(hud);
                var worldRoot = new GameObject("WorldUIRoot", typeof(RectTransform)).GetComponent<RectTransform>();
                worldRoot.SetParent(hud, false);
                Stretch(worldRoot, 0f);

                var screen = root.GetComponent<GameplayScreen>();
                if (screen == null) throw new InvalidOperationException("Gameplay HUD 缺少 GameplayScreen。");
                var so = new SerializedObject(screen);
                so.FindProperty("_playerHealthView").objectReferenceValue = health;
                so.FindProperty("_grazeIndicatorView").objectReferenceValue = graze;
                so.FindProperty("_worldUiRoot").objectReferenceValue = worldRoot;
                so.FindProperty("_damageNumberPrefab").objectReferenceValue = damagePrefab;
                so.FindProperty("_worldHealthBarPrefab").objectReferenceValue = healthBarPrefab;
                so.FindProperty("_controlsHelpText").objectReferenceValue = controlsHelp;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GameplayUiPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static PlayerHealthView CreatePlayerHealth(Transform parent)
        {
            var root = new GameObject("PlayerHealth", typeof(RectTransform), typeof(PlayerHealthView));
            root.layer = 5;
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(28f, -26f);
            rect.sizeDelta = new Vector2(400f, 72f);
            var background = CreateImage(root.transform, "Background", new Color(0.04f, 0.05f, 0.08f, 0.9f));
            Stretch(background.rectTransform, 0f);
            var delayed = CreateFill(root.transform, "DelayedDamage", new Color(0.7f, 1f, 0.35f, 1f), 6f);
            var immediate = CreateFill(root.transform, "Immediate", new Color(0.18f, 0.9f, 0.34f, 1f), 6f);
            var text = CreateLabel(root.transform, "HealthText", "HP  100 / 100", 20, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 0f);
            var low = CreateImage(root.transform, "LowHealthFrame", new Color(1f, 0.08f, 0.08f, 0.35f));
            Stretch(low.rectTransform, 0f);
            low.enabled = false;
            var so = new SerializedObject(root.GetComponent<PlayerHealthView>());
            so.FindProperty("_immediateFill").objectReferenceValue = immediate;
            so.FindProperty("_delayedDamageFill").objectReferenceValue = delayed;
            so.FindProperty("_healthText").objectReferenceValue = text;
            so.FindProperty("_lowHealthFrame").objectReferenceValue = low;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root.GetComponent<PlayerHealthView>();
        }

        private static GrazeIndicatorView CreateGrazeIndicator(Transform parent)
        {
            var root = new GameObject("GrazeIndicator", typeof(RectTransform), typeof(GrazeIndicatorView));
            root.layer = 5;
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 26f);
            rect.sizeDelta = new Vector2(520f, 98f);
            var background = CreateImage(root.transform, "Background", new Color(0.04f, 0.05f, 0.08f, 0.82f));
            Stretch(background.rectTransform, 0f);
            var phase = CreateFill(root.transform, "PhaseFill", new Color(0.3f, 0.75f, 1f), 8f);
            var perfect = CreateImage(root.transform, "PerfectMarker", new Color(0.35f, 1f, 0.55f, 0.7f));
            var perfectRect = perfect.rectTransform;
            perfectRect.anchorMin = new Vector2(0.2f, 0.1f);
            perfectRect.anchorMax = new Vector2(0.35f, 0.9f);
            perfectRect.offsetMin = perfectRect.offsetMax = Vector2.zero;
            var phaseText = CreateLabel(root.transform, "PhaseText", "IDLE", 16, TextAnchor.UpperLeft);
            PlaceLabel(phaseText, new Vector2(12f, -8f), new Vector2(180f, 28f), new Vector2(0f, 1f));
            var chargeText = CreateLabel(root.transform, "ChargeText", "CHARGE  Lv.0", 16, TextAnchor.UpperRight);
            PlaceLabel(chargeText, new Vector2(-12f, -8f), new Vector2(180f, 28f), new Vector2(1f, 1f));
            var result = CreateLabel(root.transform, "ResultText", "完美擦弹", 26, TextAnchor.MiddleCenter);
            PlaceLabel(result, new Vector2(0f, 45f), new Vector2(240f, 42f), new Vector2(0.5f, 1f));
            result.gameObject.SetActive(false);
            var so = new SerializedObject(root.GetComponent<GrazeIndicatorView>());
            so.FindProperty("_phaseFill").objectReferenceValue = phase;
            so.FindProperty("_perfectMarker").objectReferenceValue = perfect;
            so.FindProperty("_phaseText").objectReferenceValue = phaseText;
            so.FindProperty("_chargeText").objectReferenceValue = chargeText;
            so.FindProperty("_resultText").objectReferenceValue = result;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root.GetComponent<GrazeIndicatorView>();
        }

        private static Text CreateControlsHelp(Transform parent)
        {
            var text = CreateLabel(parent, "ControlsHelp", "", 16, TextAnchor.UpperRight);
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(470f, 190f);
            text.color = new Color(1f, 1f, 1f, 0.86f);
            text.text = "操作说明\n" +
                        "WASD  移动     鼠标  瞄准\n" +
                        "左键  开火     右键按住  蓄力冲击波\n" +
                        "Space  冲刺（无无敌）     R  换弹\n" +
                        "1  冲锋枪     2  霰弹枪     3  狙击枪\n" +
                        "滚轮  切换武器     Q  上一把     Esc  暂停";
            return text;
        }

        private static void ConfigureGameplayScene()
        {
            var scene = SceneManager.GetSceneByPath(GameplayScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var sceneBindings = scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<GameplaySceneBindings>(true)).FirstOrDefault();
                if (sceneBindings == null) throw new InvalidOperationException("GamePlay 场景缺少 GameplaySceneBindings。");
                var presentationRoot = sceneBindings.transform.Find("PresentationRoot");
                if (presentationRoot == null) throw new InvalidOperationException("GamePlay 场景缺少 PresentationRoot。");
                var temporary = GetOrCreate(presentationRoot, "TemporaryEffects");
                var persistent = GetOrCreate(presentationRoot, "PersistentEffects");
                var audio = presentationRoot.GetComponent<AudioSource>();
                if (audio == null) audio = presentationRoot.gameObject.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0f;
                var bindings = presentationRoot.GetComponent<GameplayPresentationBindings>();
                if (bindings == null) bindings = presentationRoot.gameObject.AddComponent<GameplayPresentationBindings>();
                var so = new SerializedObject(bindings);
                so.FindProperty("_temporaryEffectRoot").objectReferenceValue = temporary;
                so.FindProperty("_persistentEffectRoot").objectReferenceValue = persistent;
                so.FindProperty("_audioSource").objectReferenceValue = audio;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bindings);
                EditorSceneManager.SaveScene(scene, GameplayScenePath);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Transform GetOrCreate(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.layer = 5;
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateFill(Transform parent, string name, Color color, float margin)
        {
            var image = CreateImage(parent, name, color);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            Stretch(image.rectTransform, margin);
            return image;
        }

        private static Text CreateLabel(Transform parent, string name, string value, int size,
            TextAnchor alignment)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            target.layer = 5;
            target.transform.SetParent(parent, false);
            var text = target.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void PlaceLabel(Text text, Vector2 position, Vector2 size, Vector2 anchor)
        {
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * margin;
            rect.offsetMax = Vector2.one * -margin;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static void CreateMarker(string path)
        {
            var marker = AssetDatabase.LoadAssetAtPath<SeventhPhaseSetupMarker>(path);
            if (marker != null) return;
            marker = ScriptableObject.CreateInstance<SeventhPhaseSetupMarker>();
            AssetDatabase.CreateAsset(marker, path);
        }
    }
}
#endif
