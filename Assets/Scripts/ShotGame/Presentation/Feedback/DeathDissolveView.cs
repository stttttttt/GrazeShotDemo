using System.Collections.Generic;
using UnityEngine;

namespace ShotGame.Presentation.Feedback
{
    /// <summary>保存死亡瞬间的 Sprite 快照，并播放完溶解后交还表现对象池。</summary>
    [DisallowMultipleComponent]
    public sealed class DeathDissolveView : MonoBehaviour
    {
        private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private MaterialPropertyBlock _properties;

        public bool Show(EntityFeedbackView source, Material material)
        {
            if (source == null || material == null) return false;
            var sourceRenderers = source.Renderers;
            EnsureRendererCount(sourceRenderers.Length);
            var visibleCount = 0;
            for (var i = 0; i < _renderers.Count; i++)
            {
                var target = _renderers[i];
                var original = i < sourceRenderers.Length ? sourceRenderers[i] : null;
                var visible = original != null && original.enabled && original.sprite != null;
                target.gameObject.SetActive(visible);
                if (!visible) continue;
                visibleCount++;
                target.sprite = original.sprite;
                target.color = original.color;
                target.flipX = original.flipX;
                target.flipY = original.flipY;
                target.drawMode = original.drawMode;
                target.size = original.size;
                target.sortingLayerID = original.sortingLayerID;
                target.sortingOrder = original.sortingOrder + 1;
                target.maskInteraction = original.maskInteraction;
                target.sharedMaterial = material;
                target.transform.SetPositionAndRotation(original.transform.position,
                    original.transform.rotation);
                target.transform.localScale = original.transform.lossyScale;
            }
            if (visibleCount == 0) return false;
            gameObject.SetActive(true);
            return true;
        }

        public void SetDissolve(float normalized, Color edgeColor, float edgeWidth)
        {
            if (_properties == null) _properties = new MaterialPropertyBlock();
            var dissolve = Mathf.Lerp(-0.08f, 1.08f, Mathf.Clamp01(normalized));
            for (var i = 0; i < _renderers.Count; i++)
            {
                var renderer = _renderers[i];
                if (!renderer.gameObject.activeSelf) continue;
                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat(DissolveId, dissolve);
                _properties.SetColor(EdgeColorId, edgeColor);
                _properties.SetFloat(EdgeWidthId, edgeWidth);
                renderer.SetPropertyBlock(_properties);
                _properties.Clear();
            }
        }

        private void EnsureRendererCount(int count)
        {
            while (_renderers.Count < count)
            {
                var child = new GameObject($"DissolveSprite_{_renderers.Count}");
                child.transform.SetParent(transform, false);
                _renderers.Add(child.AddComponent<SpriteRenderer>());
            }
        }
    }
}
