using System;
using UnityEngine;

namespace ShotGame.Gameplay.Entity
{
    public sealed class EntityUnityObject : IDisposable
    {
        public EntityUnityObject(GameObject gameObject)
        {
            GameObject = gameObject != null ? gameObject : throw new ArgumentNullException(nameof(gameObject));
            Transform = gameObject.transform;
            Rigidbody = gameObject.GetComponent<Rigidbody2D>();
            Colliders = gameObject.GetComponentsInChildren<Collider2D>(true);
        }

        public GameObject GameObject { get; private set; }
        public Transform Transform { get; }
        public Rigidbody2D Rigidbody { get; }
        public Collider2D[] Colliders { get; }

        public void Dispose()
        {
            if (GameObject == null) return;
            UnityEngine.Object.Destroy(GameObject);
            GameObject = null;
        }
    }
}
