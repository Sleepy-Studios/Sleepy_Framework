using Core;
using HotUpdate.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HotUpdate.GameUtils
{
    public class GlobalDataManager : LazyMonoSingleton<GlobalDataManager>
    {
        /// 全局单例实例
        public new static GlobalDataManager Instance => LazyMonoSingleton<GlobalDataManager>.Instance;

        /// 主角引用
        public GameObject Player;
        /// 主摄像机引用
        public Camera MainCamera;
        /// 当前活跃场景
        public string CurrentSceneName;
        public GameObject RangedEnemyTartGameObject;

        /// <summary>
        /// 初始化时调用
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            RefreshReferences();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        /// <summary>
        /// 场景切换事件回调
        /// </summary>
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            OnSceneChanged();
        }

        /// <summary>
        /// 刷新所有引用
        /// </summary>
        void RefreshReferences()
        {
            FindMainCamera();
            FindPlayer();
            FindRangedEnemyTarget();
            CurrentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// 查找主摄像机
        /// </summary>
        private void FindMainCamera()
        {
            MainCamera = Camera.main;
            if (MainCamera == null)
            {
                Log.Warning("未找到主摄像机");
            }
        }

        /// <summary>
        /// 查找玩家对象
        /// </summary>
        private void FindPlayer()
        {
            Player = GameObject.FindGameObjectWithTag("Player");
            if (Player == null)
            {
                Log.Warning("未找到标签为Player的玩家对象");
            }
        }

        /// <summary>
        /// 查找远程敌人子弹目标
        /// </summary>
        void FindRangedEnemyTarget()
        {
            RangedEnemyTartGameObject = GameObject.FindGameObjectWithTag("RangedEnemyTarget");
            if (RangedEnemyTartGameObject == null)
            {
                Log.Warning("未找到标签为RangedEnemyTarget的玩家对象");
            }
        }

        /// <summary>
        /// 设置玩家引用
        /// </summary>
        /// <param name="playerObject">玩家GameObject</param>
        public void SetPlayer(GameObject playerObject)
        {
            if (playerObject != null)
            {
                Player = playerObject;
                Log.Info("已手动设置玩家引用");
            }
        }

        /// <summary>
        /// 获取主摄像机
        /// </summary>
        /// <returns>主摄像机</returns>
        public Camera GetMainCamera()
        {
            if (MainCamera == null)
            {
                FindMainCamera();
            }

            return MainCamera;
        }

        /// <summary>
        /// 获取玩家对象
        /// </summary>
        /// <returns>玩家GameObject</returns>
        public GameObject GetPlayer()
        {
            if (Player == null)
            {
                FindPlayer();
            }

            return Player;
        }

        /// <summary>
        /// 当场景切换时更新管理器数据
        /// </summary>
        public void OnSceneChanged()
        {
            RefreshReferences();
            Log.Info($"场景切换为：{CurrentSceneName}，已刷新全局引用");
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
    }
}