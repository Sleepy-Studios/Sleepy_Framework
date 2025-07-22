using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace HotUpdate
{
    /// <summary>
    /// Unity拓展方法
    /// </summary>
    public static class MethodExtend
    {
        #region Object拓展,支持链式调用
        
        ///快速实例化
        public static T InstantiateQuick<T>(this T selfObj) where T : Object
        {
            return Object.Instantiate(selfObj);
        }

        ///快速实例化
        public static T InstantiateQuick<T>(this Object selfObj) where T : Object
        {
            return (T)Object.Instantiate(selfObj);
        }

        ///快速实例化,指定父物体
        public static T InstantiateQuick<T>(this T selfObj, Transform parent) where T : Object
        {
            return Object.Instantiate(selfObj, parent);
        }
        
        ///快速实例化,指定父物体
        public static T InstantiateQuick<T>(this Object selfObj, Transform parent) where T : Object
        {
            return (T)Object.Instantiate(selfObj, parent);
        }

        ///重命名
        public static T Name<T>(this T selfObj, string name) where T : Object
        {
            selfObj.name = name;
            return selfObj;
        }

        ///销毁任意对象
        public static void DestroySelf<T>(this T selfObj) where T : Object
        {
            Object.Destroy(selfObj);
        }

        ///安全销毁任意对象
        public static T DestroySelfGracefully<T>(this T selfObj) where T : Object
        {
            if ((bool)(Object)selfObj)
                Object.Destroy(selfObj);
            return selfObj;
        }

        ///在一定时间后销毁任意对象
        public static T DestroySelfAfterDelay<T>(this T selfObj, float afterDelay) where T : Object
        {
            Object.Destroy(selfObj, afterDelay);
            return selfObj;
        }

        ///在一定时间后安全销毁任意对象
        public static T DestroySelfAfterDelayGracefully<T>(this T selfObj, float delay) where T : Object
        {
            if ((bool)(Object)selfObj)
                Object.Destroy(selfObj, delay);
            return selfObj;
        }

        ///任意对象加锁不销毁
        public static T DontDestroyOnLoad<T>(this T selfObj) where T : Object
        {
            Object.DontDestroyOnLoad(selfObj);
            return selfObj;
        }

        ///任意对象转换为指定类型
        public static T As<T>(this Object selfObj) where T : Object
        {
            return selfObj as T;
        }

        #endregion
        
        #region GameObject拓展,支持链式调用

        ///激活GameObject
        public static GameObject Show(this GameObject target)
        {
            target?.SetActive(true);
            return target;
        }

        ///激活组件的GameObject
        public static T Show<T>(this T selfComponent) where T : Component
        {
            selfComponent?.gameObject.Show();
            return selfComponent;
        }

        ///禁用GameObject
        public static GameObject Hide(this GameObject target)
        {
            target?.SetActive(false);
            return target;
        }
        ///禁用组件的GameObject
        public static T Hide<T>(this T selfComponent) where T : Component
        {
            selfComponent?.gameObject.Hide();
            return selfComponent;
        }
        
        ///销毁GameObject 
        public static void DestroyGameObj<T>(this T selfBehaviour) where T : Component
        {
            selfBehaviour.gameObject.DestroySelf();
        }

        ///安全销毁GameObject
        public static void DestroyGameObjGracefully<T>(this T selfBehaviour) where T : Component
        {
            if (!(bool)(Object)selfBehaviour || !(bool)(Object)selfBehaviour.gameObject)
                return;
            selfBehaviour.gameObject.DestroySelfGracefully();
        }

        ///在一定时间后销毁GameObject
        public static T DestroyGameObjAfterDelay<T>(this T selfBehaviour, float delay) where T : Component
        {
            selfBehaviour.gameObject.DestroySelfAfterDelay(delay);
            return selfBehaviour;
        }

        ///在一定时间后安全销毁GameObject
        public static T DestroyGameObjAfterDelayGracefully<T>(this T selfBehaviour, float delay) where T : Component
        {
            if ((bool)(Object)selfBehaviour && (bool)(Object)selfBehaviour.gameObject)
                selfBehaviour.gameObject.DestroySelfAfterDelay(delay);
            return selfBehaviour;
        }

        #endregion
        
        #region DOTween拓展

        public static Tween KillTo0(this Tween t)
        {
            t?.Goto(0, true);
            t?.Kill();
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


    }
}