#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Service.Config;
using GameFoundation.Service.Resource;
using GameFoundation.Service.UI;
using ShotGame.Entry;
using ShotGame.GameFlow;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Entity;
using ShotGame.Gameplay.Scene;
using ShotGame.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShotGame.Editor
{
    public static class ThirdPhaseSetup
    {
        private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
        private const string EntryScenePath = "Assets/Scenes/Entry.unity";
        private const string ConfigPath = "Assets/Res/Config/GameplayContentConfig.asset";
        private const string PlayerPrefabPath = "Assets/Res/Gameplay/Player/Player.prefab";
        private const string EnemyPrefabPath = "Assets/Res/Gameplay/Enemy/TestEnemy.prefab";
        private const string GameplayUiPath = "Assets/Res/UI/Panel_Gameplay.prefab";
        private const string PauseUiPath = "Assets/Res/UI/Panel_Pause.prefab";
        private const string CompletionMarkerPath = "Assets/Scripts/ShotGame/Editor/ThirdPhaseSetupComplete.asset";

        [InitializeOnLoadMethod]
        private static void ExecuteOnceAfterCompile()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<ThirdPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            EditorApplication.delayCall += Execute;
        }

        [MenuItem("Shot Game/Setup Third Phase")]
        public static void Execute()
        {
            try
            {
                EnsureFolders();
                var layers = EnsureLayers("Player", "Enemy", "PlayerProjectile", "EnemyProjectile", "Wall", "Pickup");
                var playerPrefab = CreateEntityPrefab(PlayerPrefabPath, "Player", new Color(0.2f, 0.65f, 1f), layers["Player"]);
                var enemyPrefab = CreateEntityPrefab(EnemyPrefabPath, "TestEnemy", new Color(1f, 0.25f, 0.25f), layers["Enemy"]);
                var config = CreateGameplayConfig(playerPrefab, enemyPrefab, layers);
                var gameplayUi = CreateGameplayUi();
                var pauseUi = CreatePauseUi();
                ConfigureMainMenuUi();
                ConfigureResourceCatalog(gameplayUi, pauseUi);
                ConfigureFoundationConfig();
                CreateGameplayScene(layers);
                AssetDatabase.SaveAssets();
                config = AssetDatabase.LoadAssetAtPath<GameplayContentConfig>(ConfigPath);
                ConfigureEntryScene(config);
                ConfigureBuildSettings();
                CreateCompletionMarker();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("第三部分基础场景、配置和预制体创建完成。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Res", "Gameplay");
            EnsureFolder("Assets/Res/Gameplay", "Player");
            EnsureFolder("Assets/Res/Gameplay", "Enemy");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static Dictionary<string, int> EnsureLayers(params string[] names)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            var result = new Dictionary<string, int>();
            foreach (var name in names)
            {
                var existing = LayerMask.NameToLayer(name);
                if (existing >= 0) { result[name] = existing; continue; }
                var slot = -1;
                for (var i = 8; i < layers.arraySize; i++)
                {
                    if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                    slot = i;
                    break;
                }
                if (slot < 0) throw new InvalidOperationException($"没有可用的 Unity Layer 槽位：{name}");
                layers.GetArrayElementAtIndex(slot).stringValue = name;
                result[name] = slot;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return result;
        }

        private static GameObject CreateEntityPrefab(string path, string name, Color color, int layer)
        {
            var root = new GameObject(name);
            root.layer = layer;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = color;
            renderer.sortingOrder = 10;
            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameplayContentConfig CreateGameplayConfig(GameObject playerPrefab, GameObject enemyPrefab,
            IReadOnlyDictionary<string, int> layers)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayContentConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameplayContentConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_playerPrefab").objectReferenceValue = playerPrefab;
            serialized.FindProperty("_testEnemyPrefab").objectReferenceValue = enemyPrefab;
            serialized.FindProperty("_spawnTestEnemy").boolValue = true;
            serialized.FindProperty("_testEnemyCount").intValue = 1;
            serialized.FindProperty("_testEnemyAttackRange").floatValue = 5f;
            serialized.FindProperty("_playerTargetMask").intValue = Mask(layers, "Enemy", "Wall");
            serialized.FindProperty("_enemyTargetMask").intValue = Mask(layers, "Player", "Wall");
            serialized.FindProperty("_grazeProjectileMask").intValue = Mask(layers, "EnemyProjectile");
            serialized.FindProperty("_wallMask").intValue = Mask(layers, "Wall");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static int Mask(IReadOnlyDictionary<string, int> layers, params string[] names)
        {
            var mask = 0;
            for (var i = 0; i < names.Length; i++) mask |= 1 << layers[names[i]];
            return mask;
        }

        private static GameObject CreateGameplayUi()
        {
            var root = CreateCanvasRoot("Panel_Gameplay", 0);
            var screen = root.AddComponent<GameplayScreen>();
            ConfigureScreen(screen, "Gameplay", 1, false);
            CreateLabel(root.transform, "Gameplay Demo", new Vector2(20f, -20f), TextAnchor.UpperLeft, 24);
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, GameplayUiPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePauseUi()
        {
            var root = CreateCanvasRoot("Panel_Pause", 100);
            var background = CreateImage(root.transform, "Background", new Color(0f, 0f, 0f, 0.65f));
            Stretch(background.rectTransform);
            CreateLabel(root.transform, "Paused", new Vector2(0f, 120f), TextAnchor.MiddleCenter, 40);
            var resume = CreateButton(root.transform, "ResumeButton", "继续游戏", new Vector2(0f, 20f));
            var menu = CreateButton(root.transform, "ReturnMenuButton", "返回主菜单", new Vector2(0f, -60f));
            var screen = root.AddComponent<PauseScreen>();
            ConfigureScreen(screen, "Pause", 2, true);
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_resumeButton").objectReferenceValue = resume;
            serialized.FindProperty("_returnToMenuButton").objectReferenceValue = menu;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PauseUiPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCanvasRoot(string name, int sortingOrder)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.layer = 5;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static void ConfigureScreen(UIScreen screen, string pageId, int layer, bool blocksInput)
        {
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_pageId").FindPropertyRelative("_value").stringValue = pageId;
            serialized.FindProperty("_layer").enumValueIndex = layer;
            serialized.FindProperty("_blocksInput").boolValue = blocksInput;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateLabel(Transform parent, string text, Vector2 position, TextAnchor alignment, int size)
        {
            var gameObject = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = alignment == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rect.pivot = alignment == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(500f, 70f);
            var label = gameObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            var image = CreateImage(parent, name, new Color(0.18f, 0.22f, 0.3f, 0.95f));
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(240f, 56f);
            var button = image.gameObject.AddComponent<Button>();
            var label = CreateLabel(image.transform, text, Vector2.zero, TextAnchor.MiddleCenter, 22);
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

        private static void ConfigureMainMenuUi()
        {
            var root = PrefabUtility.LoadPrefabContents("Assets/Res/UI/Panel_MainMenu.prefab");
            var screen = root.GetComponent<MainMenuScreen>();
            if (screen != null) ConfigureScreen(screen, "MainMenu", 1, true);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Res/UI/Panel_MainMenu.prefab");
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void ConfigureResourceCatalog(GameObject gameplayUi, GameObject pauseUi)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameResourceCatalog>("Assets/Res/Config/GameResourceCatalog.asset");
            var mainMenu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Res/UI/Panel_MainMenu.prefab");
            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("_entries");
            entries.arraySize = 3;
            SetResourceEntry(entries.GetArrayElementAtIndex(0), "UI_MainMenu", mainMenu);
            SetResourceEntry(entries.GetArrayElementAtIndex(1), "UI_Gameplay", gameplayUi);
            SetResourceEntry(entries.GetArrayElementAtIndex(2), "UI_Pause", pauseUi);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void SetResourceEntry(SerializedProperty entry, string id, GameObject asset)
        {
            entry.FindPropertyRelative("_id").FindPropertyRelative("_value").stringValue = id;
            entry.FindPropertyRelative("_asset").objectReferenceValue = asset;
        }

        private static void ConfigureFoundationConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameFoundationConfig>("Assets/Res/Config/GameFoundationConfig.asset");
            var serialized = new SerializedObject(config);
            serialized.FindProperty("_gameplayActionMap").stringValue = "Gameplay";
            serialized.FindProperty("_uiActionMap").stringValue = "UI";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void CreateGameplayScene(IReadOnlyDictionary<string, int> layers)
        {
            var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);

            var gameplayRoot = new GameObject("GameplayRoot");
            var worldRoot = Child(gameplayRoot.transform, "WorldRoot");
            var presentationRoot = Child(gameplayRoot.transform, "PresentationRoot");
            var spawnPoints = Child(gameplayRoot.transform, "SpawnPoints");
            var playerSpawn = Child(spawnPoints, "PlayerSpawn");
            playerSpawn.position = new Vector3(0f, -2f, 0f);
            var enemySpawnRoot = Child(spawnPoints, "EnemySpawns");
            var enemySpawn = Child(enemySpawnRoot, "EnemySpawn_01");
            enemySpawn.position = new Vector3(0f, 2.5f, 0f);
            var dropRoot = Child(gameplayRoot.transform, "DropRoot");

            var boundsObject = new GameObject("ArenaBounds");
            boundsObject.transform.SetParent(gameplayRoot.transform, false);
            var bounds = boundsObject.AddComponent<BoxCollider2D>();
            bounds.isTrigger = true;
            bounds.size = new Vector2(14f, 9f);

            // Entry 场景常驻并负责全局音频监听，Gameplay 相机只负责画面，避免叠加场景出现双 AudioListener。
            var cameraObject = new GameObject("GameplayCamera", typeof(Camera));
            cameraObject.transform.SetParent(gameplayRoot.transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f);

            var sceneEntities = new List<SceneEntityAuthoring>();
            sceneEntities.Add(CreateWall(worldRoot, "Wall_Top", new Vector2(0f, 4.5f), new Vector2(14f, 0.5f), layers["Wall"]));
            sceneEntities.Add(CreateWall(worldRoot, "Wall_Bottom", new Vector2(0f, -4.5f), new Vector2(14f, 0.5f), layers["Wall"]));
            sceneEntities.Add(CreateWall(worldRoot, "Wall_Left", new Vector2(-7f, 0f), new Vector2(0.5f, 9f), layers["Wall"]));
            sceneEntities.Add(CreateWall(worldRoot, "Wall_Right", new Vector2(7f, 0f), new Vector2(0.5f, 9f), layers["Wall"]));

            var bindings = gameplayRoot.AddComponent<GameplaySceneBindings>();
            var serialized = new SerializedObject(bindings);
            serialized.FindProperty("_worldRoot").objectReferenceValue = worldRoot;
            serialized.FindProperty("_presentationRoot").objectReferenceValue = presentationRoot;
            serialized.FindProperty("_dropRoot").objectReferenceValue = dropRoot;
            serialized.FindProperty("_playerSpawn").objectReferenceValue = playerSpawn;
            var enemySpawns = serialized.FindProperty("_enemySpawns");
            enemySpawns.arraySize = 1;
            enemySpawns.GetArrayElementAtIndex(0).objectReferenceValue = enemySpawn;
            serialized.FindProperty("_gameplayCamera").objectReferenceValue = camera;
            serialized.FindProperty("_arenaBounds").objectReferenceValue = bounds;
            var entities = serialized.FindProperty("_sceneEntities");
            entities.arraySize = sceneEntities.Count;
            for (var i = 0; i < sceneEntities.Count; i++) entities.GetArrayElementAtIndex(i).objectReferenceValue = sceneEntities[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, GamePlayScenePath);
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static SceneEntityAuthoring CreateWall(Transform parent, string name, Vector2 position,
            Vector2 size, int layer)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.layer = layer;
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = new Color(0.3f, 0.34f, 0.42f);
            renderer.size = size;
            renderer.drawMode = SpriteDrawMode.Sliced;
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
            var authoring = wall.AddComponent<SceneEntityAuthoring>();
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("_category").enumValueIndex = (int)EntityCategory.Wall;
            serialized.FindProperty("_team").enumValueIndex = (int)EntityTeam.Neutral;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return authoring;
        }

        private static void ConfigureEntryScene(GameplayContentConfig config)
        {
            var scene = EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Single);
            var entry = Resources.FindObjectsOfTypeAll<GameEntry>().FirstOrDefault(item => item.gameObject.scene == scene);
            if (entry == null) throw new InvalidOperationException("Entry 场景中找不到 GameEntry。");
            var entrySerialized = new SerializedObject(entry);
            entrySerialized.FindProperty("_gameplayContentConfig").objectReferenceValue = config;
            entrySerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);

            var ui = Resources.FindObjectsOfTypeAll<UIService>().FirstOrDefault(item => item.gameObject.scene == scene);
            if (ui == null) throw new InvalidOperationException("Entry 场景中找不到 UIService。");
            var uiSerialized = new SerializedObject(ui);
            var pages = uiSerialized.FindProperty("_pages");
            pages.arraySize = 3;
            SetPage(pages.GetArrayElementAtIndex(0), "MainMenu", "UI_MainMenu");
            SetPage(pages.GetArrayElementAtIndex(1), "Gameplay", "UI_Gameplay");
            SetPage(pages.GetArrayElementAtIndex(2), "Pause", "UI_Pause");
            uiSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, EntryScenePath);
        }

        private static void SetPage(SerializedProperty page, string pageId, string resourceId)
        {
            page.FindPropertyRelative("_pageId").FindPropertyRelative("_value").stringValue = pageId;
            page.FindPropertyRelative("_resourceId").FindPropertyRelative("_value").stringValue = resourceId;
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            EnsureBuildScene(scenes, EntryScenePath, true);
            EnsureBuildScene(scenes, GamePlayScenePath, false);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureBuildScene(List<EditorBuildSettingsScene> scenes, string path, bool insertFirst)
        {
            var existing = scenes.FindIndex(item => item.path == path);
            if (existing >= 0)
            {
                scenes[existing].enabled = true;
                return;
            }

            var scene = new EditorBuildSettingsScene(path, true);
            if (insertFirst) scenes.Insert(0, scene);
            else scenes.Add(scene);
        }

        private static void CreateCompletionMarker()
        {
            if (AssetDatabase.LoadAssetAtPath<ThirdPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ThirdPhaseSetupMarker>(), CompletionMarkerPath);
        }
    }
}
#endif
