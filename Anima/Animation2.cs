using System.Collections.Generic;
using UnityEngine;

namespace DesingerTables
{
    /// <summary>
    /// 使用 IAnimDriver2 的动画表，value 既可以是 Animator 动画，也可以是 DOTween 动画。
    /// </summary>
    public static class UnitAnimInfo2
    {
        /// <summary>
        /// 逻辑名 -> Driver。
        /// 示例只写了少数几条，你可以按需要扩展。
        /// </summary>
        public static readonly Dictionary<string, IAnimDriver2> data = new Dictionary<string, IAnimDriver2>
        {
            // 仅示例：Stand 用 Animator，MoveForward 用 DOTween
            { "Stand",       new AnimatorDriver2("Stand",      "Stand",      1.0f, 0) },
            { "MoveForward", new DotweenDriver2("MoveForward", "MoveForward",1.0f, 0) },
        };
    }
}