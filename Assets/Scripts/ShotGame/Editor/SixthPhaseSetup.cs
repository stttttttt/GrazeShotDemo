#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Service.Resource;
using GameFoundation.Service.UI;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Scene;
using ShotGame.Gameplay.Weapon;
using ShotGame.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShotGame.Editor
{
    /// <summary>生成第六阶段的正式波次、敌人配置、出生点和结算页。</summary>
    public static class SixthPhaseSetup
    {
        private const string GameplayScenePath = "Assets/Scenes/GamePlay.unity";
        private const string EntryScenePath = "Assets/Scenes/Entry.unity";
        private const string GameplayConfigPath = "Assets/Res/Config/GameplayContentConfig.asset";
        private const string ResourceCatalogPath = "Assets/Res/Config/GameResourceCatalog.asset";
        private const string GameplayUiPath = "Assets/Res/UI/Panel_Gameplay.prefab";
        private const string ResultUiPath = "Assets/Res/UI/Panel_GameplayResult.prefab";
        private const string RunPath = "Assets/Res/Gameplay/Run/DemoRun.asset";
        private const string BaseEnemyPrefabPath = "Assets/Res/Gameplay/Enemy/TestEnemy.prefab";
        private const string EnemyProjectilePath = "Assets/Res/Gameplay/Projectile/EnemyProjectile.prefab";
        private const string SlowGunPath = "Assets/Res/Gameplay/Weapon/TestEnemyGun.asset";
        private const string RapidGunPath = "Assets/Res/Gameplay/Weapon/EnemyRapidGun.asset";
        private const string EliteGunPath = "Assets/Res/Gameplay/Weapon/EnemyEliteGun.asset";
        private const string CompletionMarkerPath =
            "Assets/Scripts/ShotGame/Editor/SixthPhaseSetupComplete.asset";

        [InitializeOnLoadMethod]
        private static void ExecuteOnceAfterCompile()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<SixthPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            EditorApplication.delayCall += Execute;
        }

        [MenuItem("Shot Game/Setup Sixth Phase")]
        public static void Execute()
        {
            try
            {
                EnsureFolders();
                var baseEnemy = RequireAsset<GameObject>(BaseEnemyPrefabPath);
                var projectile = RequireAsset<GameObject>(EnemyProjectilePath);
                var slowGun = RequireAsset<WeaponConfig>(SlowGunPath);
                var rapidGun = CreateWeapon(RapidGunPath, "追击连射", 12, 0.42f, 1.4f,
                    projectile, 1, 6f, 7f, 7f, 4f, 0.12f);
                var eliteGun = CreateWeapon(EliteGunPath, "精英散射", 10, 0.9f, 1.5f,
                    projectile, 3, 20f, 12f, 6f, 5f, 0.14f);

                var slowPrefab = CreateEnemyVariant(baseEnemy,
                    "Assets/Res/Gameplay/Enemy/SlowEnemy.prefab", "SlowEnemy",
                    new Color(1f, 0.28f, 0.24f), 1f);
                var chaserPrefab = CreateEnemyVariant(baseEnemy,
                    "Assets/Res/Gameplay/Enemy/ChaserEnemy.prefab", "ChaserEnemy",
                    new Color(1f, 0.58f, 0.16f), 0.9f);
                var elitePrefab = CreateEnemyVariant(baseEnemy,
                    "Assets/Res/Gameplay/Enemy/EliteEnemy.prefab", "EliteEnemy",
                    new Color(0.75f, 0.2f, 1f), 1.35f);

                var slow = CreateEnemy("SlowEnemy", "基础敌人", slowPrefab, slowGun,
                    35f, 2f, 6f, 1f, 1);
                var chaser = CreateEnemy("ChaserEnemy", "追击敌人", chaserPrefab, rapidGun,
                    28f, 3.2f, 4f, 0.8f, 1);
                var elite = CreateEnemy("EliteEnemy", "精英敌人", elitePrefab, eliteGun,
                    120f, 1.6f, 7f, 1.2f, 3);

                var waves = CreateWaves(slow, chaser, elite);
                var run = CreateRun(waves);
                run.Validate();
                ConfigureGameplayContent(run);
                ConfigureGameplayScene();
                ConfigureGameplayHud();
                var resultUi = CreateResultUi();
                ConfigureResourceCatalog(resultUi);
                ConfigureEntryScene();
                CreateCompletionMarker();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("第六阶段 5 波配置、出生点、结算页与流程注册创建完成。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Res/Gameplay", "Run");
            EnsureFolder("Assets/Res/Gameplay", "Wave");
            EnsureFolder("Assets/Res/Gameplay", "Enemy");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"缺少前置资源：{path}");
            return asset;
        }

        private static WeaponConfig CreateWeapon(string path, string name, int magazine,
            float interval, float reload, GameObject projectile, int projectileCount,
            float spread, float damage, float speed, float lifetime, float radius)
        {
            var config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<WeaponConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            var so = new SerializedObject(config);
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_fireMode").enumValueIndex = (int)WeaponFireMode.Automatic;
            so.FindProperty("_magazineSize").intValue = magazine;
            so.FindProperty("_initialReserveAmmo").intValue = 999;
            so.FindProperty("_fireInterval").floatValue = interval;
            so.FindProperty("_reloadDuration").floatValue = reload;
            so.FindProperty("_projectilePrefab").objectReferenceValue = projectile;
            so.FindProperty("_projectileCount").intValue = projectileCount;
            so.FindProperty("_spreadAngle").floatValue = spread;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_projectileSpeed").floatValue = speed;
            so.FindProperty("_projectileLifetime").floatValue = lifetime;
            so.FindProperty("_projectileRadius").floatValue = radius;
            so.FindProperty("_recoilImpulse").floatValue = 0f;
            so.FindProperty("_muzzleOffset").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static GameObject CreateEnemyVariant(GameObject source, string path, string name,
            Color color, float scale)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                instance.name = name;
                instance.transform.localScale = Vector3.one * scale;
                var renderer = instance.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) renderer.color = color;
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static EnemyConfig CreateEnemy(string id, string displayName, GameObject prefab,
            WeaponConfig weapon, float health, float speed, float range, float attackDelay, int cost)
        {
            var path = $"Assets/Res/Gameplay/Enemy/{id}.asset";
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            var so = new SerializedObject(config);
            so.FindProperty("_enemyId").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.FindProperty("_weapon").objectReferenceValue = weapon;
            so.FindProperty("_maxHealth").floatValue = health;
            so.FindProperty("_moveSpeed").floatValue = speed;
            so.FindProperty("_attackRange").floatValue = range;
            so.FindProperty("_initialAttackDelay").floatValue = attackDelay;
            so.FindProperty("_budgetCost").intValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static WaveDefinition[] CreateWaves(EnemyConfig slow, EnemyConfig chaser,
            EnemyConfig elite)
        {
            var waves = new WaveDefinition[5];
            waves[0] = CreateWave(1, "热身", WaveObjectiveType.EliminateAll, 2,
                new[] { Group(slow, "Far", 1, 0f, 1f) }, Array.Empty<RandomItem>(), 0);
            waves[1] = CreateWave(2, "左右夹击", WaveObjectiveType.EliminateAll, 3,
                new[] { Group(slow, "Side", 2, 0f, 0.8f) }, Array.Empty<RandomItem>(), 0);
            waves[2] = CreateWave(3, "追击混战", WaveObjectiveType.EliminateAll, 4,
                new[] { Group(chaser, "Side", 1, 0f, 0.8f) },
                new[] { Random(slow, "Side", 2, 0, 2), Random(chaser, "Side", 1, 0, 1) }, 2);
            waves[3] = CreateWave(4, "关键目标", WaveObjectiveType.KeyTarget, 4,
                new[] { Group(elite, "Elite", 1, 0f, 1f, true, true),
                    Group(slow, "Side", 1, 0.8f, 1f) }, Array.Empty<RandomItem>(), 0);
            waves[4] = CreateWave(5, "最终围攻", WaveObjectiveType.EliminateAll, 4,
                new[] { Group(elite, "Elite", 1, 0f, 1f),
                    Group(chaser, "Side", 2, 0.6f, 0.9f) },
                new[] { Random(slow, "Far", 1, 0, 2) }, 2);
            return waves;
        }

        private static WaveDefinition CreateWave(int number, string name,
            WaveObjectiveType objective, int maxAlive, SpawnItem[] explicitGroups,
            RandomItem[] randomPool, int budget)
        {
            var path = $"Assets/Res/Gameplay/Wave/Wave_{number:00}.asset";
            var wave = AssetDatabase.LoadAssetAtPath<WaveDefinition>(path);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveDefinition>();
                AssetDatabase.CreateAsset(wave, path);
            }
            var so = new SerializedObject(wave);
            so.FindProperty("_waveId").stringValue = $"wave_{number:00}";
            so.FindProperty("_displayName").stringValue = $"第 {number} 波 · {name}";
            so.FindProperty("_objectiveType").enumValueIndex = (int)objective;
            so.FindProperty("_intervalOverride").floatValue = -1f;
            so.FindProperty("_maxAliveEnemies").intValue = maxAlive;
            SetExplicitGroups(so.FindProperty("_explicitGroups"), explicitGroups);
            SetRandomPool(so.FindProperty("_randomPool"), randomPool);
            so.FindProperty("_randomBudget").intValue = budget;
            so.FindProperty("_randomStartDelay").floatValue = 1f;
            so.FindProperty("_randomSpawnInterval").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
            return wave;
        }

        private static void SetExplicitGroups(SerializedProperty array, SpawnItem[] items)
        {
            array.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var property = array.GetArrayElementAtIndex(i);
                property.FindPropertyRelative("_enemy").objectReferenceValue = item.Enemy;
                property.FindPropertyRelative("_spawnGroup").stringValue = item.Group;
                property.FindPropertyRelative("_count").intValue = item.Count;
                property.FindPropertyRelative("_startDelay").floatValue = item.StartDelay;
                property.FindPropertyRelative("_spawnInterval").floatValue = item.Interval;
                property.FindPropertyRelative("_requiredForClear").boolValue = item.Required;
                property.FindPropertyRelative("_isKeyTarget").boolValue = item.KeyTarget;
            }
        }

        private static void SetRandomPool(SerializedProperty array, RandomItem[] items)
        {
            array.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var property = array.GetArrayElementAtIndex(i);
                property.FindPropertyRelative("_enemy").objectReferenceValue = item.Enemy;
                property.FindPropertyRelative("_spawnGroup").stringValue = item.Group;
                property.FindPropertyRelative("_weight").intValue = item.Weight;
                property.FindPropertyRelative("_minCount").intValue = item.Min;
                property.FindPropertyRelative("_maxCount").intValue = item.Max;
            }
        }

        private static RunDefinition CreateRun(WaveDefinition[] waves)
        {
            var run = AssetDatabase.LoadAssetAtPath<RunDefinition>(RunPath);
            if (run == null)
            {
                run = ScriptableObject.CreateInstance<RunDefinition>();
                AssetDatabase.CreateAsset(run, RunPath);
            }
            var so = new SerializedObject(run);
            so.FindProperty("_runId").stringValue = "demo_run";
            so.FindProperty("_initialCountdown").floatValue = 2f;
            so.FindProperty("_defaultWaveInterval").floatValue = 3f;
            so.FindProperty("_fixedSeed").intValue = 20260903;
            so.FindProperty("_endlessAfterLastWave").boolValue = true;
            so.FindProperty("_endlessEnemyIncreasePerWave").intValue = 2;
            var entries = so.FindProperty("_waves");
            entries.arraySize = waves.Length;
            for (var i = 0; i < waves.Length; i++)
                entries.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(run);
            return run;
        }

        private static void ConfigureGameplayContent(RunDefinition run)
        {
            var config = RequireAsset<GameplayContentConfig>(GameplayConfigPath);
            var so = new SerializedObject(config);
            so.FindProperty("_runDefinition").objectReferenceValue = run;
            so.FindProperty("_spawnTestEnemy").boolValue = false;
            so.FindProperty("_testEnemyCount").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            config.Validate();
        }

        private static void ConfigureGameplayScene()
        {
            var scene = SceneManager.GetSceneByPath(GameplayScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var bindings = scene.GetRootGameObjects().SelectMany(root =>
                        root.GetComponentsInChildren<GameplaySceneBindings>(true)).FirstOrDefault();
                if (bindings == null) throw new InvalidOperationException("GamePlay 场景缺少 GameplaySceneBindings。");
                var spawnRoot = bindings.transform.Find("SpawnPoints/EnemySpawns");
                if (spawnRoot == null) throw new InvalidOperationException("GamePlay 场景缺少 SpawnPoints/EnemySpawns。");
                var points = new[]
                {
                    CreateSpawnPoint(spawnRoot, "Spawn_Left", "enemy_left", "Side", new Vector3(-5f, 2f)),
                    CreateSpawnPoint(spawnRoot, "Spawn_Right", "enemy_right", "Side", new Vector3(5f, 2f)),
                    CreateSpawnPoint(spawnRoot, "Spawn_Far", "enemy_far", "Far", new Vector3(0f, 3.5f)),
                    CreateSpawnPoint(spawnRoot, "Spawn_Elite", "enemy_elite", "Elite", new Vector3(0f, 1.5f))
                };
                var so = new SerializedObject(bindings);
                var list = so.FindProperty("_enemySpawnPoints");
                list.arraySize = points.Length;
                for (var i = 0; i < points.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bindings);
                bindings.Validate();
                EditorSceneManager.SaveScene(scene, GameplayScenePath);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static EnemySpawnPoint CreateSpawnPoint(Transform parent, string name, string id,
            string group, Vector3 position)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }
            child.position = position;
            var point = child.GetComponent<EnemySpawnPoint>();
            if (point == null) point = child.gameObject.AddComponent<EnemySpawnPoint>();
            var so = new SerializedObject(point);
            so.FindProperty("_spawnId").stringValue = id;
            so.FindProperty("_group").stringValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();
            return point;
        }

        private static GameObject CreateResultUi()
        {
            var root = new GameObject("Panel_GameplayResult", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.layer = 5;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            Stretch(root.GetComponent<RectTransform>());
            var background = CreateImage(root.transform, "Background", new Color(0.02f, 0.03f, 0.06f, 0.88f));
            Stretch(background.rectTransform);
            var title = CreateLabel(root.transform, "ResultTitle", "挑战结束", new Vector2(0f, 150f), 42);
            var details = CreateLabel(root.transform, "ResultDetails", "结算信息", new Vector2(0f, 35f), 22);
            details.rectTransform.sizeDelta = new Vector2(600f, 180f);
            var restart = CreateButton(root.transform, "RestartButton", "重新开始", new Vector2(-130f, -150f));
            var menu = CreateButton(root.transform, "ReturnMenuButton", "返回主菜单", new Vector2(130f, -150f));
            var screen = root.AddComponent<GameplayResultScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_pageId").FindPropertyRelative("_value").stringValue = "GameplayResult";
            so.FindProperty("_layer").enumValueIndex = 2;
            so.FindProperty("_blocksInput").boolValue = true;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_detailsText").objectReferenceValue = details;
            so.FindProperty("_restartButton").objectReferenceValue = restart;
            so.FindProperty("_returnToMenuButton").objectReferenceValue = menu;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ResultUiPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureGameplayHud()
        {
            var root = PrefabUtility.LoadPrefabContents(GameplayUiPath);
            try
            {
                var screen = root.GetComponent<GameplayScreen>();
                if (screen == null) throw new InvalidOperationException("Gameplay HUD 缺少 GameplayScreen。");
                var wave = FindOrCreateHudLabel(root.transform, "WaveStatus", "开始倒计时", -225f);
                var objective = FindOrCreateHudLabel(root.transform, "WaveObjective", "等待第一波", -260f);
                var so = new SerializedObject(screen);
                so.FindProperty("_waveText").objectReferenceValue = wave;
                so.FindProperty("_objectiveText").objectReferenceValue = objective;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GameplayUiPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Text FindOrCreateHudLabel(Transform parent, string name, string content, float y)
        {
            var child = parent.Find(name);
            if (child != null && child.TryGetComponent<Text>(out var existing)) return existing;
            var label = CreateLabel(parent, name, content, Vector2.zero, 18);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, y);
            rect.sizeDelta = new Vector2(800f, 32f);
            label.alignment = TextAnchor.UpperLeft;
            label.raycastTarget = false;
            return label;
        }

        private static void ConfigureResourceCatalog(GameObject resultUi)
        {
            var catalog = RequireAsset<GameResourceCatalog>(ResourceCatalogPath);
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("_entries");
            var index = FindResource(entries, "UI_GameplayResult");
            if (index < 0)
            {
                index = entries.arraySize;
                entries.arraySize++;
            }
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_id").FindPropertyRelative("_value").stringValue = "UI_GameplayResult";
            entry.FindPropertyRelative("_asset").objectReferenceValue = resultUi;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static int FindResource(SerializedProperty entries, string id)
        {
            for (var i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("_id")
                        .FindPropertyRelative("_value").stringValue == id) return i;
            return -1;
        }

        private static void ConfigureEntryScene()
        {
            var scene = SceneManager.GetSceneByPath(EntryScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Additive);
            try
            {
                var ui = scene.GetRootGameObjects().SelectMany(root =>
                    root.GetComponentsInChildren<UIService>(true)).FirstOrDefault();
                if (ui == null) throw new InvalidOperationException("Entry 场景缺少 UIService。");
                var so = new SerializedObject(ui);
                var pages = so.FindProperty("_pages");
                var index = FindPage(pages, "GameplayResult");
                if (index < 0)
                {
                    index = pages.arraySize;
                    pages.arraySize++;
                }
                var page = pages.GetArrayElementAtIndex(index);
                page.FindPropertyRelative("_pageId").FindPropertyRelative("_value").stringValue = "GameplayResult";
                page.FindPropertyRelative("_resourceId").FindPropertyRelative("_value").stringValue = "UI_GameplayResult";
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ui);
                EditorSceneManager.SaveScene(scene, EntryScenePath);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static int FindPage(SerializedProperty pages, string id)
        {
            for (var i = 0; i < pages.arraySize; i++)
                if (pages.GetArrayElementAtIndex(i).FindPropertyRelative("_pageId")
                        .FindPropertyRelative("_value").stringValue == id) return i;
            return -1;
        }

        private static Text CreateLabel(Transform parent, string name, string content,
            Vector2 position, int fontSize)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            target.layer = 5;
            target.transform.SetParent(parent, false);
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(700f, 70f);
            var label = target.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.layer = 5;
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string content, Vector2 position)
        {
            var image = CreateImage(parent, name, new Color(0.18f, 0.24f, 0.36f, 0.98f));
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 58f);
            var button = image.gameObject.AddComponent<Button>();
            var label = CreateLabel(image.transform, "Label", content, Vector2.zero, 22);
            Stretch(label.rectTransform);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void CreateCompletionMarker()
        {
            if (AssetDatabase.LoadAssetAtPath<SixthPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SixthPhaseSetupMarker>(), CompletionMarkerPath);
        }

        private static SpawnItem Group(EnemyConfig enemy, string group, int count,
            float delay, float interval, bool required = true, bool keyTarget = false) =>
            new SpawnItem(enemy, group, count, delay, interval, required, keyTarget);

        private static RandomItem Random(EnemyConfig enemy, string group, int weight, int min, int max) =>
            new RandomItem(enemy, group, weight, min, max);

        private readonly struct SpawnItem
        {
            public SpawnItem(EnemyConfig enemy, string group, int count, float startDelay,
                float interval, bool required, bool keyTarget)
            { Enemy = enemy; Group = group; Count = count; StartDelay = startDelay; Interval = interval; Required = required; KeyTarget = keyTarget; }
            public EnemyConfig Enemy { get; }
            public string Group { get; }
            public int Count { get; }
            public float StartDelay { get; }
            public float Interval { get; }
            public bool Required { get; }
            public bool KeyTarget { get; }
        }

        private readonly struct RandomItem
        {
            public RandomItem(EnemyConfig enemy, string group, int weight, int min, int max)
            { Enemy = enemy; Group = group; Weight = weight; Min = min; Max = max; }
            public EnemyConfig Enemy { get; }
            public string Group { get; }
            public int Weight { get; }
            public int Min { get; }
            public int Max { get; }
        }
    }
}
#endif
