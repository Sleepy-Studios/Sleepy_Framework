using System;

namespace Core
{
    /// <summary>
    /// 视图路径工具类，用于获取UI视图对应的预制体路径
    /// </summary>
    public static class ViewPathUtility
    {
        /// <summary>
        /// 获取视图类对应的预制体路径
        /// </summary>
        /// <param name="viewType">视图类型</param>
        /// <returns>预制体路径</returns>
        public static string GetViewPrefabPath(Type viewType)
        {
            if (viewType == null)
                return null;
                
            // 获取类上的SourceAttribute
            var attributes = viewType.GetCustomAttributes(typeof(SourceAttribute), false);
            if (attributes.Length > 0)
            {
                var sourceAttr = attributes[0] as SourceAttribute;
                return sourceAttr?.Path;
            }
            
            return null;
        }
        
        /// <summary>
        /// 获取视图类对应的预制体路径(泛型方法)
        /// </summary>
        /// <typeparam name="T">视图类型</typeparam>
        /// <returns>预制体路径</returns>
        public static string GetViewPrefabPath<T>()
        {
            return GetViewPrefabPath(typeof(T));
        }
        
        /// <summary>
        /// 获取视图实例对应的预制体路径
        /// </summary>
        /// <param name="viewInstance">视图实例</param>
        /// <returns>预制体路径</returns>
        public static string GetViewPrefabPath(object viewInstance)
        {
            if (viewInstance == null)
                return null;
                
            return GetViewPrefabPath(viewInstance.GetType());
        }
    }
}
