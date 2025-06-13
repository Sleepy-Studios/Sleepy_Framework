using System.Collections;
using Core;
using Core.Runtime.Log;
using HotUpdate.Base;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using YooAsset;

namespace HotUpdate
{
    public class LoadScenesManager : EagerMonoSingleton<LoadScenesManager>
    {
        ///全局单例实例
        public new static LoadScenesManager Instance => EagerMonoSingleton<LoadScenesManager>.Instance;

        // UI元素引用
        public string LoadUIPrefabPath = "Assets/GameRes/Prefabs/UI/LoadUI"; // 加载场景UI的资源路径
        private GameObject loadPanel; // 动态加载的加载面板
        private Slider loadSlider; // 进度条
        private TextMeshProUGUI loadText; // 进度文本
        private RawImage loadSceneBg; // 背景图像
        private TextMeshProUGUI loadDesText; // 描述背景图像

        public const float MinLoadTime = 0.4f; // 最少加载时间(默认0.4秒)
        public const string DefaultBgPath = "Assets/GameRes/Textures/BackGround/BG_default"; // 默认背景路径
        public const string DefaultDescription = "正在加载下一场景中"; // 默认描述文本

        /// <summary>
        /// 加载场景（统一方法）
        /// </summary>
        /// <param name="scenePath">场景路径(可以直接填名字)</param>
        /// <param name="minLoadTime">最少加载时间(默认为0.4f)</param>
        /// <param name="bgPath">背景图路径</param>
        /// <param name="description">背景描述</param>
        public void LoadScene(string scenePath, float minLoadTime = MinLoadTime, string bgPath = null,
            string description = null)
        {
            InitializeLoadingUI(bgPath, description); // 初始化加载UI
            StartCoroutine(LoadSceneAsync(scenePath, minLoadTime)); // 异步加载场景
        }

        /// <summary>
        /// 初始化加载UI，加载并绑定相应的组件
        /// </summary>
        /// <param name="bgPath">背景图路径</param>
        /// <param name="description">背景描述</param>
        private void InitializeLoadingUI(string bgPath = null, string description = null)
        {
            if (loadPanel != null) return; // 如果UI已经初始化，不重复实例化

            // 加载加载UI预制体
            var loadUIPrefabHandle = YooAssets.LoadAssetSync<GameObject>(LoadUIPrefabPath);
            if (loadUIPrefabHandle.AssetObject == null)
            {
                Log.Error("加载UI预制体失败，请确保路径正确: " + LoadUIPrefabPath);
                return;
            }

            loadPanel = Instantiate(loadUIPrefabHandle.AssetObject as GameObject);
            loadSlider = loadPanel.transform.Find("LoadSlider").GetComponent<Slider>();
            loadText = loadPanel.transform.Find("LoadText").GetComponent<TextMeshProUGUI>();
            loadSceneBg = loadPanel.transform.Find("LoadSceneBg").GetComponent<RawImage>();
            loadDesText = loadPanel.transform.Find("LoadDesText").GetComponent<TextMeshProUGUI>();

            // 加载背景图片
            LoadBackgroundImage(bgPath);

            // 设置背景描述文本
            loadDesText.text = description ?? DefaultDescription;

            loadPanel.SetActive(true); // 显示加载面板
        }

        /// <summary>
        /// 根据给定的路径加载背景图像
        /// </summary>
        /// <param name="path">背景图路径</param>
        private void LoadBackgroundImage(string path)
        {
            string backgroundPath = string.IsNullOrEmpty(path) ? DefaultBgPath : path;

            // 异步加载背景图像
            var backgroundHandle = YooAssets.LoadAssetSync<Texture2D>(backgroundPath);
            if (backgroundHandle.AssetObject != null)
            {
                loadSceneBg.texture = backgroundHandle.AssetObject as Texture2D;
            }
            else
            {
                Log.Error("加载背景图像失败，路径：" + backgroundPath);
            }
        }

        /// <summary>
        /// 异步加载场景的协程
        /// </summary>
        /// <param name="scenePath">场景路径</param>
        /// <param name="minLoadTime">最少加载时间</param>
        private IEnumerator LoadSceneAsync(string scenePath, float minLoadTime)
        {
            float actualLoadTime = 0f; // 实际加载时间
            float displayProgress = 0f;
            var sceneHandle = YooAssets.LoadSceneAsync(location: scenePath, suspendLoad: true);
            while (sceneHandle.Progress < 0.9f || actualLoadTime < minLoadTime || displayProgress < 1)
            {
                actualLoadTime += Time.deltaTime;
                float actualProgress = Mathf.Clamp01(sceneHandle.Progress / 0.9f);
                float timeProgress = Mathf.Clamp01(actualLoadTime / minLoadTime);
                displayProgress = Mathf.Min(actualProgress, timeProgress);
                UpdateLoadingUI(displayProgress);
                yield return new WaitForEndOfFrame();
            }

            if (displayProgress >= 1)
            {
                CleanupLoadingUI();
                sceneHandle.UnSuspend();
                var package = YooAssets.GetPackage("DefaultPackage");
                yield return package.UnloadUnusedAssetsAsync();
            }
        }

        /// <summary>
        /// 更新加载UI元素（进度条和文本）
        /// </summary>
        /// <param name="progress">加载进度</param>
        private void UpdateLoadingUI(float progress)
        {
            loadText.text = Mathf.FloorToInt(progress * 100) + "%";
            loadSlider.value = progress;
        }

        /// <summary>
        /// 清理加载UI引用
        /// </summary>
        private void CleanupLoadingUI()
        {
            loadPanel = null; // 重置面板引用
        }
    }
}