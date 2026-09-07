#if UNITY_EDITOR
using System;
using ShotGame.Gameplay.Config;
using ShotGame.Gameplay.Weapon;
using ShotGame.Presentation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ShotGame.Editor
{
    public static class FourthPhaseSetup
    {
        private const string GameplayConfigPath = "Assets/Res/Config/GameplayContentConfig.asset";
        private const string GameplayUiPath = "Assets/Res/UI/Panel_Gameplay.prefab";
        private const string ShotgunProjectilePath = "Assets/Res/Gameplay/Projectile/PlayerShotgunProjectile.prefab";
        private const string SmgProjectilePath = "Assets/Res/Gameplay/Projectile/PlayerSMGProjectile.prefab";
        private const string SniperProjectilePath = "Assets/Res/Gameplay/Projectile/PlayerSniperProjectile.prefab";
        private const string EnemyProjectilePath = "Assets/Res/Gameplay/Projectile/EnemyProjectile.prefab";
        private const string ShotgunPath = "Assets/Res/Gameplay/Weapon/PlayerShotgun.asset";
        private const string SmgPath = "Assets/Res/Gameplay/Weapon/PlayerSMG.asset";
        private const string SniperPath = "Assets/Res/Gameplay/Weapon/PlayerSniper.asset";
        private const string EnemyGunPath = "Assets/Res/Gameplay/Weapon/TestEnemyGun.asset";
        private const string CompletionMarkerPath = "Assets/Scripts/ShotGame/Editor/FourthPhaseSetupComplete.asset";

        [InitializeOnLoadMethod]
        private static void ExecuteOnceAfterCompile()
        {
            if (Application.isBatchMode) return;
            if (AssetDatabase.LoadAssetAtPath<FourthPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            EditorApplication.delayCall += Execute;
        }

        [MenuItem("Shot Game/Setup Fourth Phase")]
        public static void Execute()
        {
            try
            {
                EnsureFolders();
                var playerProjectileLayer = RequireLayer("PlayerProjectile");
                var enemyProjectileLayer = RequireLayer("EnemyProjectile");
                var playerProjectile = CreateProjectilePrefab(ShotgunProjectilePath,
                    "PlayerShotgunProjectile", new Color(1f, 0.85f, 0.2f), playerProjectileLayer, 0.13f);
                var smgProjectile = CreateProjectilePrefab(SmgProjectilePath,
                    "PlayerSMGProjectile", new Color(0.3f, 0.85f, 1f), playerProjectileLayer, 0.09f);
                var sniperProjectile = CreateProjectilePrefab(SniperProjectilePath,
                    "PlayerSniperProjectile", new Color(1f, 0.3f, 0.9f), playerProjectileLayer, 0.11f,
                    new Vector2(1.2f, 0.12f));
                var enemyProjectile = CreateProjectilePrefab(EnemyProjectilePath,
                    "EnemyProjectile", new Color(1f, 0.25f, 0.2f), enemyProjectileLayer, 0.13f);

                var shotgun = CreateWeapon(ShotgunPath, "散弹枪", WeaponFireMode.SemiAutomatic,
                    6, 12, 0.45f, 1.2f, playerProjectile, 5, 24f, 8f, 16f, 1.2f, 0.13f, 2.2f, 0.55f);
                var smg = CreateWeapon(SmgPath, "冲锋枪", WeaponFireMode.Automatic,
                    24, 48, 0.1f, 1f, smgProjectile, 1, 4f, 5f, 20f, 1.1f, 0.09f, 0.35f, 0.5f);
                var sniper = CreateWeapon(SniperPath, "狙击枪", WeaponFireMode.SemiAutomatic,
                    1, 6, 0.18f, 1.1f, sniperProjectile, 1, 0f, 40f, 32f, 1.5f, 0.11f,
                    1.4f, 0.7f, 5);
                var enemyGun = CreateWeapon(EnemyGunPath, "测试敌弹", WeaponFireMode.Automatic,
                    8, 999, 0.8f, 1.5f, enemyProjectile, 1, 0f, 10f, 6f, 4f, 0.13f, 0f, 0.5f);

                SetShockwaveAmmoConversion(smg, 3);
                SetShockwaveAmmoConversion(shotgun, 1);
                SetShockwaveAmmoConversion(sniper, 1);

                ConfigureGameplayContent(shotgun, smg, sniper, enemyGun);
                CreateGameplayUi();
                CreateCompletionMarker();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("第四阶段武器、弹丸、Gameplay 配置和 HUD 创建完成。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Res/Gameplay", "Weapon");
            EnsureFolder("Assets/Res/Gameplay", "Projectile");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static int RequireLayer(string name)
        {
            var layer = LayerMask.NameToLayer(name);
            if (layer < 0) throw new InvalidOperationException($"缺少 Unity Layer：{name}，请先执行第三阶段配置。");
            return layer;
        }

        private static GameObject CreateProjectilePrefab(string path, string name, Color color,
            int layer, float radius, Vector2 visualSize = default)
        {
            var root = new GameObject(name);
            root.layer = layer;
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            renderer.color = color;
            renderer.sortingOrder = 15;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = visualSize.sqrMagnitude > 0f ? visualSize : Vector2.one * (radius * 2f);
            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static WeaponConfig CreateWeapon(string path, string displayName, WeaponFireMode fireMode,
            int magazineSize, int reserveAmmo, float fireInterval, float reloadDuration,
            GameObject projectilePrefab, int projectileCount, float spreadAngle, float damage,
            float projectileSpeed, float projectileLifetime, float projectileRadius,
            float recoilImpulse, float muzzleOffset, int penetrations = 0)
        {
            var config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<WeaponConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            var serialized = new SerializedObject(config);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_fireMode").enumValueIndex = (int)fireMode;
            serialized.FindProperty("_magazineSize").intValue = magazineSize;
            serialized.FindProperty("_initialReserveAmmo").intValue = reserveAmmo;
            serialized.FindProperty("_fireInterval").floatValue = fireInterval;
            serialized.FindProperty("_reloadDuration").floatValue = reloadDuration;
            serialized.FindProperty("_projectilePrefab").objectReferenceValue = projectilePrefab;
            serialized.FindProperty("_projectileCount").intValue = projectileCount;
            serialized.FindProperty("_spreadAngle").floatValue = spreadAngle;
            serialized.FindProperty("_damage").floatValue = damage;
            serialized.FindProperty("_projectileSpeed").floatValue = projectileSpeed;
            serialized.FindProperty("_projectileLifetime").floatValue = projectileLifetime;
            serialized.FindProperty("_projectileRadius").floatValue = projectileRadius;
            serialized.FindProperty("_penetrations").intValue = penetrations;
            serialized.FindProperty("_recoilImpulse").floatValue = recoilImpulse;
            serialized.FindProperty("_muzzleOffset").floatValue = muzzleOffset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void ConfigureGameplayContent(WeaponConfig shotgun, WeaponConfig smg,
            WeaponConfig sniper, WeaponConfig enemyGun)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameplayContentConfig>(GameplayConfigPath);
            if (config == null) throw new InvalidOperationException("缺少 GameplayContentConfig，请先执行第三阶段配置。");
            var serialized = new SerializedObject(config);
            var weapons = serialized.FindProperty("_playerInitialWeapons");
            weapons.arraySize = 3;
            weapons.GetArrayElementAtIndex(0).objectReferenceValue = smg;
            weapons.GetArrayElementAtIndex(1).objectReferenceValue = shotgun;
            weapons.GetArrayElementAtIndex(2).objectReferenceValue = sniper;
            serialized.FindProperty("_testEnemyWeapon").objectReferenceValue = enemyGun;
            serialized.FindProperty("_playerMaxRecoilSpeed").floatValue = 12f;
            serialized.FindProperty("_playerRecoilRecovery").floatValue = 8f;
            serialized.FindProperty("_playerDashDistance").floatValue = 2.4f;
            serialized.FindProperty("_playerDashDuration").floatValue = 0.12f;
            serialized.FindProperty("_playerDashCooldown").floatValue = 0.65f;
            serialized.FindProperty("_playerTargetMask").intValue = 1 << RequireLayer("Enemy");
            serialized.FindProperty("_enemyTargetMask").intValue = 1 << RequireLayer("Player");
            serialized.FindProperty("_wallMask").intValue = 1 << RequireLayer("Wall");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void SetShockwaveAmmoConversion(WeaponConfig weapon, int amount)
        {
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("_shockwaveAmmoPerProjectile").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
        }

        private static void CreateGameplayUi()
        {
            var root = new GameObject("Panel_Gameplay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.layer = 5;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;
            Stretch(root.GetComponent<RectTransform>());

            var screen = root.AddComponent<GameplayScreen>();
            var screenSerialized = new SerializedObject(screen);
            screenSerialized.FindProperty("_pageId").FindPropertyRelative("_value").stringValue = "Gameplay";
            screenSerialized.FindProperty("_layer").enumValueIndex = 1;
            screenSerialized.FindProperty("_blocksInput").boolValue = false;

            var title = CreateLabel(root.transform, "擦弹射击 Demo", new Vector2(20f, -20f), 24);
            var weapon = CreateLabel(root.transform, "武器", new Vector2(20f, -60f), 22);
            var health = CreateLabel(root.transform, "生命", new Vector2(20f, -96f), 22);
            var help = CreateLabel(root.transform,
                "左键射击并反向移动  |  R 换弹  |  1/2/3、滚轮、Q 切枪  |  Esc 暂停",
                new Vector2(20f, -136f), 16);
            title.color = new Color(0.85f, 0.92f, 1f);
            help.color = new Color(0.72f, 0.76f, 0.82f);
            screenSerialized.FindProperty("_weaponText").objectReferenceValue = weapon;
            screenSerialized.FindProperty("_healthText").objectReferenceValue = health;
            screenSerialized.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, GameplayUiPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Text CreateLabel(Transform parent, string text, Vector2 position, int size)
        {
            var gameObject = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(900f, 32f);
            var label = gameObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateCompletionMarker()
        {
            if (AssetDatabase.LoadAssetAtPath<FourthPhaseSetupMarker>(CompletionMarkerPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FourthPhaseSetupMarker>(), CompletionMarkerPath);
        }
    }
}
#endif
