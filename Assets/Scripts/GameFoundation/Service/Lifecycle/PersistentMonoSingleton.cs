using UnityEngine;

namespace GameFoundation.Service.Lifecycle
{
    /// <summary>
    /// 仅供少量应用级 Unity 边界对象使用。局内实体和 Gameplay Runtime 禁止继承。
    /// </summary>
    public abstract class PersistentMonoSingleton<T> : MonoBehaviour
        where T : PersistentMonoSingleton<T>
    {
        public static T Instance { get; private set; }

        protected bool IsPrimaryInstance => ReferenceEquals(Instance, this);

        protected virtual void Awake()
        {
            if (Instance != null && !ReferenceEquals(Instance, this))
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(Instance, this)) Instance = null;
        }
    }
}
