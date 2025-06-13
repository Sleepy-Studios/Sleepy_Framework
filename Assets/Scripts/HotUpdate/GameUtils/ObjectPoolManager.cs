using System.Collections.Generic;
using Core;
using Core.Runtime.Log;
using Cysharp.Threading.Tasks;
using HotUpdate.Base;
using UnityEngine;
using YooAsset;

namespace HotUpdate.GameUtils
{
    public class ObjectPoolManager : LazyMonoSingleton<ObjectPoolManager>
    {
        /// 全局单例实例
        public new static ObjectPoolManager Instance => LazyMonoSingleton<ObjectPoolManager>.Instance;

        /// 对象池，存储每种预制体对应的对象队列
        private Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
        /// 预制体缓存，存储路径与预制体的映射
        private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        /// 对象与其对应预制体的映射
        private Dictionary<GameObject, GameObject> objectPrefabMap = new Dictionary<GameObject, GameObject>();
        /// 每种预制体对应的父级Transform，用于组织对象池中的对象
        private Dictionary<GameObject, Transform> poolParents = new Dictionary<GameObject, Transform>();
        /// 当前在池中的所有对象集合，用于快速检测对象是否在池中
        private HashSet<GameObject> pooledObjects = new HashSet<GameObject>();
        /// 每个预制体对象池的最大容量
        private const int MaxPoolSize = 400;
        /// 批量处理数量（创建或销毁）
        private const int BatchSize = 20;

        /// <summary>
        /// 从路径异步加载预制体
        /// </summary>
        /// <param name="prefabPath">预制体路径</param>
        /// <returns>加载的预制体</returns>
        private async UniTask<GameObject> LoadPrefab(string prefabPath)
        {
            // 检查预制体是否已缓存
            if (prefabCache.TryGetValue(prefabPath, out GameObject prefab))
            {
                return prefab;
            }

            // 异步加载预制体资源
            var handle = YooAssets.LoadAssetAsync<GameObject>(prefabPath);
            await handle;

            if (handle.AssetObject != null)
            {
                prefab = handle.AssetObject as GameObject;
                prefabCache[prefabPath] = prefab;
                return prefab;
            }
            else
            {
                Log.Error($"无法从路径异步加载预制体: {prefabPath}");
                return null;
            }
        }

        /// <summary>
        /// 确保为预制体创建父级对象
        /// </summary>
        /// <param name="prefab">预制体对象</param>
        private void EnsurePoolParent(GameObject prefab)
        {
            if (!poolParents.ContainsKey(prefab))
            {
                // 只有在需要时才创建父级对象
                GameObject parent = new GameObject(prefab.name + "Pool");
                parent.transform.SetParent(this.transform);
                poolParents[prefab] = parent.transform;
            }
        }

        /// <summary>
        /// 异步初始化对象池（通过预制体路径）
        /// </summary>
        /// <param name="prefabPath">预制体路径</param>
        /// <param name="initialSize">初始池大小</param>
        public async UniTask InitializePool(string prefabPath, int initialSize)
        {
            GameObject prefab = await LoadPrefab(prefabPath);
            if (prefab != null)
            {
                await InitializePool(prefab, initialSize);
            }
        }

        /// <summary>
        /// 异步初始化对象池（通过预制体对象）
        /// </summary>
        /// <param name="prefab">预制体对象</param>
        /// <param name="initialSize">初始池大小</param>
        public async UniTask InitializePool(GameObject prefab, int initialSize)
        {
            // 限制初始大小不超过最大池容量
            initialSize = Mathf.Min(initialSize, MaxPoolSize);

            if (!pool.ContainsKey(prefab))
            {
                // 确保创建父级对象
                EnsurePoolParent(prefab);
                pool[prefab] = new Queue<GameObject>();

                // 计算每帧创建的对象数量
                int objectsPerFrame = BatchSize;


                if (initialSize <= BatchSize)
                {
                    for (int i = 0; i < initialSize; i++)
                    {
                        CreatePooledObject(prefab);
                    }

                    await UniTask.CompletedTask;
                }
                else
                {
                    int created = 0;
                    while (created < initialSize)
                    {
                        int batchSize = Mathf.Min(objectsPerFrame, initialSize - created);
                        for (int i = 0; i < batchSize; i++)
                        {
                            CreatePooledObject(prefab);
                            created++;
                        }

                        Log.Info($"对象池 {prefab.name} 批次初始化完成，批次数量{batchSize}");
                        await UniTask.Yield(); // 等待下一帧
                    }
                }
            }

            // 验证最终创建数量
            Log.Info($"对象池 {prefab.name} 初始化完成，目标数量: {initialSize}，实际数量: {pool[prefab].Count}");
        }

        /// <summary>
        /// 异步获取池中对象（通过预制体路径）
        /// </summary>
        /// <param name="prefabPath">预制体路径</param>
        /// <returns>池中对象</returns>
        public async UniTask<GameObject> GetPooledObject(string prefabPath)
        {
            GameObject prefab = await LoadPrefab(prefabPath);
            return prefab != null ? await GetPooledObject(prefab) : null;
        }

        /// <summary>
        /// 异步获取池中对象（通过预制体对象）
        /// </summary>
        /// <param name="prefab">预制体对象</param>
        /// <returns>池中对象</returns>
        public async UniTask<GameObject> GetPooledObject(GameObject prefab)
        {
            if (!pool.ContainsKey(prefab))
            {
                Log.Warning($"未初始化 {prefab.name} 的对象池，正在创建新池。");
                await InitializePool(prefab, 1);
            }

            if (pool[prefab].Count > 0)
            {
                // 从池中取出对象
                GameObject obj = pool[prefab].Dequeue();
                // 从已入池集合中移除
                pooledObjects.Remove(obj);
                obj.SetActive(true);
                return obj;
            }

            // 如果池中没有对象，则创建新对象
            GameObject newObj = CreatePooledObject(prefab);
            // 创建后立即从池中取出
            pooledObjects.Remove(newObj);
            newObj.SetActive(true);
            return newObj;
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        /// <param name="obj">归还的对象</param>
        public void ReturnPooledObject(GameObject obj)
        {
            // 检查对象是否已在池中
            if (IsInPool(obj))
            {
                Log.Warning($"对象 {obj.name} 已经在池中，不需要重复归还");
                return;
            }

            if (objectPrefabMap.TryGetValue(obj, out GameObject prefab))
            {
                // 检查对象池是否已达到容量上限
                if (pool[prefab].Count >= MaxPoolSize)
                {
                    Log.Info($"对象池 {prefab.name} 已达到上限 {MaxPoolSize}，销毁对象");
                    objectPrefabMap.Remove(obj);
                    Destroy(obj);
                    return;
                }

                obj.SetActive(false);
                // 父物体改回
                obj.transform.SetParent(poolParents[prefab]);
                pool[prefab].Enqueue(obj);
                // 添加到已入池集合
                pooledObjects.Add(obj);
            }
            else
            {
                Log.Warning($"对象 {obj.name} 不是由对象池创建的，正在销毁。");
                Destroy(obj);
            }
        }

        /// <summary>
        /// 检查对象是否已在池中
        /// </summary>
        /// <param name="obj">要检查的对象</param>
        /// <returns>是否在池中</returns>
        public bool IsInPool(GameObject obj)
        {
            return pooledObjects.Contains(obj);
        }

        /// <summary>
        /// 创建池中对象
        /// </summary>
        /// <param name="prefab">预制体对象</param>
        /// <returns>创建的对象</returns>
        private GameObject CreatePooledObject(GameObject prefab)
        {
            // 确保已创建父级对象
            EnsurePoolParent(prefab);

            // 实例化对象并设置为池的子对象
            GameObject obj = Instantiate(prefab, poolParents[prefab]);
            obj.SetActive(false);
            pool[prefab].Enqueue(obj);
            objectPrefabMap[obj] = prefab;
            // 添加到已入池集合
            pooledObjects.Add(obj);
            return obj;
        }

        /// <summary>
        /// 异步清除特定预制体的对象池
        /// </summary>
        /// <param name="prefab">要清除的预制体</param>
        public async UniTask ClearPool(GameObject prefab)
        {
            if (!pool.TryGetValue(prefab, out var objectQueue))
            {
                Log.Warning($"对象池中不存在预制体 {prefab.name}，无需清除");
                return;
            }

            // 分批销毁这个预制体池中的所有对象
            List<GameObject> objectsToDestroy = new List<GameObject>(objectQueue);
            objectQueue.Clear(); // 清空队列

            // 分批处理销毁
            for (int i = 0; i < objectsToDestroy.Count; i += BatchSize)
            {
                int batchCount = Mathf.Min(BatchSize, objectsToDestroy.Count - i);
                for (int j = 0; j < batchCount; j++)
                {
                    GameObject obj = objectsToDestroy[i + j];
                    if (obj != null)
                    {
                        // 从已入池集合中移除
                        pooledObjects.Remove(obj);
                        // 从对象-预制体映射中移除
                        objectPrefabMap.Remove(obj);
                        // 销毁对象
                        Destroy(obj);
                    }
                }

                //Log.Info($"对象池 {prefab.name} 批次销毁完成，批次数量 {batchCount}");
                await UniTask.Yield(); // 等待下一帧
            }

            // 销毁父级容器
            if (poolParents.TryGetValue(prefab, out Transform parent) && parent != null)
            {
                GameObject parentObj = parent.gameObject;
                poolParents.Remove(prefab);  // 先从字典中移除
                if (parentObj != null)
                {
                    Destroy(parentObj);  // 然后再销毁对象
                }
            }
            else
            {
                // 如果父级已经不存在，只需从字典中移除
                poolParents.Remove(prefab);
            }

            // 从池中移除这个预制体的队列
            pool.Remove(prefab);
        }

        /// <summary>
        /// 通过预制体路径异步清除对象池
        /// </summary>
        /// <param name="prefabPath">预制体路径</param>
        public async UniTask ClearPool(string prefabPath)
        {
            if (prefabCache.TryGetValue(prefabPath, out GameObject prefab))
            {
                await ClearPool(prefab);
                // 从预制体缓存中移除
                prefabCache.Remove(prefabPath);
            }
        }

        /// <summary>
        /// 异步清除所有对象池
        /// </summary>
        public async UniTask ClearAllPools()
        {
            // 创建预制体的副本列表，避免在迭代过程中修改集合
            List<GameObject> prefabs = new List<GameObject>(pool.Keys);

            // 逐个清除每个预制体的对象池
            foreach (GameObject prefab in prefabs)
            {
                await ClearPool(prefab);
            }

            // 确保所有集合都被清空
            pool.Clear();
            objectPrefabMap.Clear();
            poolParents.Clear();
            pooledObjects.Clear();
            prefabCache.Clear();

            Log.Info("已清除所有对象池");
        }

        /// <summary>
        /// 获取指定预制体对象池的当前大小
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <returns>当前池大小</returns>
        public int GetPoolSize(GameObject prefab)
        {
            if (pool.TryGetValue(prefab, out Queue<GameObject> objectQueue))
            {
                return objectQueue.Count;
            }

            return 0;
        }

        /// <summary>
        /// 获取指定预制体路径对象池的当前大小
        /// </summary>
        /// <param name="prefabPath">预制体路径</param>
        /// <returns>当前池大小</returns>
        public int GetPoolSize(string prefabPath)
        {
            if (prefabCache.TryGetValue(prefabPath, out GameObject prefab))
            {
                return GetPoolSize(prefab);
            }

            return 0;
        }
    }
}