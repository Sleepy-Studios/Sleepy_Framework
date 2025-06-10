using UnityEngine;

namespace HotUpdate.Base
{
    /// <summary>
    /// 懒汉单例Mono脚本 即用即加载
    /// </summary>
    /// <typeparam name="T">单例脚本类</typeparam>
    public class LazyMonoSingleton<T> : MonoBehaviour where T : LazyMonoSingleton<T>
    {
        private static T instance;
        private static readonly object Lock = new object();

        protected static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (Lock)
                    {
                        if (instance == null)
                        {
                            // 自动创建GameObject并挂载脚本
                            GameObject obj = new GameObject($"{typeof(T).Name}");
                            instance = obj.AddComponent<T>();
                            DontDestroyOnLoad(obj); // 跨场景存活
                        }
                    }
                }

                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
                Destroy(gameObject); // 防止重复实例
        }
    }
}