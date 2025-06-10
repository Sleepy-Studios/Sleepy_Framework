using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace HotUpdate.GameUtils
{
    /// <summary>
    /// Unity拓展方法
    /// </summary>
    public static class MethodExtend
    {
        #region DOTween拓展

        public static Tween KillTo0(this Tween t)
        {
            if (t != null)
            {
                t.Goto(0, true);
                t.Kill();
            }

            return null;
        }

        public static TweenerCore<float, float, FloatOptions> DOFade(this CanvasGroup target, float endValue,
            float duration)
        {
            // 创建一个 DOTween 动画，将 CanvasGroup 的 alpha 从当前值过渡到 endValue
            TweenerCore<float, float, FloatOptions> t = DOTween.To(() => target.alpha, x => target.alpha = x, endValue,
                duration);
            t.SetTarget(target); // 设置动画的目标为当前 CanvasGroup
            return t; // 返回动画对象
        }

        #endregion

        #region GameObject拓展

        public static GameObject Show(this GameObject target)
        {
            if (target != null)
            {
                target.SetActive(true);
            }

            return target;
        }

        public static GameObject Hide(this GameObject target)
        {
            if (target != null)
            {
                target.SetActive(false);
            }

            return target;
        }

        #endregion
    }
}