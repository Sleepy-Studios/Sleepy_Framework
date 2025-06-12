using System;
using UnityEngine;
using YooAsset;

namespace HotUpdate
{
    /// <summary>
    /// YooAssets 资源加载包装器，自动管理资源句柄的释放
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    public class AssetLoader<T> : IDisposable where T : UnityEngine.Object
    {
        private AssetHandle handle;
        private T asset;
        private bool disposed = false;

        /// <summary>
        /// 获取加载的资源对象
        /// </summary>
        public T Asset => asset;

        /// <summary>
        /// 资源是否有效
        /// </summary>
        public bool IsValid => handle != null && handle.IsValid;

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns>资源加载器实例</returns>
        public static AssetLoader<T> Load(string assetName)
        {
            var loader = new AssetLoader<T>();
            loader.handle = YooAssets.LoadAssetSync<T>(assetName);
            
            if (loader.handle.IsValid)
            {
                loader.asset = loader.handle.AssetObject as T;
            }
            else
            {
                Debug.LogError($"加载资源失败: {assetName}");
            }

            return loader;
        }

        /// <summary>
        /// 析构函数，确保资源被释放
        /// </summary>
        ~AssetLoader()
        {
            Dispose(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        /// <param name="disposing">是否为显式调用</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (handle != null && handle.IsValid)
                {
                    handle.Release();
                    handle = null;
                }
                asset = null;
                disposed = true;
            }
        }
    }
}
