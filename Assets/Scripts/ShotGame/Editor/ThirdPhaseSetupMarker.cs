#if UNITY_EDITOR
using UnityEngine;

namespace ShotGame.Editor
{
    /// <summary>标记第三阶段自动搭建已经完整执行；手动菜单命令仍可随时重建。</summary>
    internal sealed class ThirdPhaseSetupMarker : ScriptableObject
    {
    }
}
#endif
