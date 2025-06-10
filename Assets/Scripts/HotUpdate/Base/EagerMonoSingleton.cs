using UnityEngine;

namespace HotUpdate.Base
{
    /// <summary>
    /// 饿汉单例Mono脚本 初始即加载 需挂载到物体上
    /// </summary>
    /// <typeparam name="T">单例脚本类</typeparam>
    public class EagerMonoSingleton<T> : MonoBehaviour where T : EagerMonoSingleton<T>
    {
        private static T instance;

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;
            DontDestroyOnLoad(gameObject); // 跨场景存活
        }

        public static T Instance => instance;
    }
}