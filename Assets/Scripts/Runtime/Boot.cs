using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using YooAsset;

public class Boot : MonoBehaviour
{
    public EPlayMode PlayMode = EPlayMode.HostPlayMode;
    [Header("日志显示等级设置")]
    public LogLevel LOGLevel = LogLevel.All;
    [Header("最大保存日志条数")]
    public int MaxLogCount = 1000;
    [Header("是否显示堆栈信息")]
    public bool ShowStackTrace = true;
    [Header("是否清理日志缓存")]
    public bool IsCleanLogCache = true;
    /// 进度条
    [Header("UI组件")]
    public Slider ProgressSlider;
    /// 下载速度
    public TextMeshProUGUI SpeedText;
    /// 总体进度
    public TextMeshProUGUI ProgressText;
    public ResourcePackage YooAssetsPackage;
    /// 热更新资源的远端地址
    [Header("基础下载地址设置")]
    public string BaseServerURL = "http://20.2.148.139:9000/Test";
    ///真正下载地址
    string hostServerURL;
    [Header("热更配置")]
    public HotUpdateConfig HotUpdateConfig;

    /// 缓存加载的资源数据
    private static Dictionary<string, TextAsset> sAssetDatas = new();
    /// 总下载大小
    private long totalDownloadBytes;
    /// 当前已下载大小
    private long currentDownloadBytes;
    /// 总下载文件数
    private int totalDownloadCount;
    /// 当前已下载文件数
    private int currentDownloadCount;

    private void Awake()
    {
#if UNITY_ANDROID
         hostServerURL = $"{baseServerURL}/Android";
#elif UNITY_STANDALONE
        hostServerURL = $"{BaseServerURL}/PC";
#endif
    }

    void Start()
    {
        Log.Init(LOGLevel, MaxLogCount, ShowStackTrace, IsCleanLogCache);
        StartCoroutine(InitYooAssets(StartGame));
    }

    #region YooAsset初始化

    /// <summary>
    /// 初始化YooAsset资源系统并加载资源包
    /// </summary>
    IEnumerator InitYooAssets(Action onDownloadComplete)
    {
        if (!YooAssets.Initialized)
        {
            YooAssets.Initialize();
        }

        string packageName = "DefaultPackage";
        var package = YooAssets.TryGetPackage(packageName) ?? YooAssets.CreatePackage(packageName);
        YooAssets.SetDefaultPackage(package);

        InitializationOperation initializationOperation = InitializePackage(package);
        yield return initializationOperation;

        if (initializationOperation.Status != EOperationStatus.Succeed)
        {
            Log.Error($"资源包初始化失败：{initializationOperation.Error}");
            yield break;
        }

        Log.Info("资源包初始化成功！");

        // 更新资源版本
        var operation = package.RequestPackageVersionAsync();
        yield return operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            Log.Error(operation.Error);
            Log.Error("网络问题,切换至离线模式");
            // 先销毁资源包，再移除
            var destroyOperation = package.DestroyAsync();
            yield return destroyOperation;
            YooAssets.RemovePackage(package);
            PlayMode = EPlayMode.OfflinePlayMode;
            StartCoroutine(InitYooAssets(StartGame));
            yield break;
        }

        string packageVersion = operation.PackageVersion;
        Log.Info($"更新后的资源包版本 : {packageVersion}");

        // 更新补丁清单
        var operation2 = package.UpdatePackageManifestAsync(packageVersion);
        yield return operation2;

        if (operation2.Status != EOperationStatus.Succeed)
        {
            Log.Error(operation2.Error);
            yield break;
        }

        // 下载补丁包并更新 UI
        yield return DownloadAndUpdateUI();

        var configHandle = package.LoadAssetAsync<HotUpdateConfig>("Assets/GameRes/Config/HotUpdateConfig");
        yield return configHandle;
        HotUpdateConfig = configHandle.AssetObject as HotUpdateConfig;
        //加载必要的资源
        var assets = new List<string>(HotUpdateConfig.hotUpdateFiles).Concat(HotUpdateConfig.aotFiles);
        foreach (var asset in assets)
        {
            var handle = package.LoadAssetAsync<TextAsset>(asset);
            yield return handle;
            var assetObj = handle.AssetObject as TextAsset;
            sAssetDatas[asset] = assetObj;
            Log.Info($"用YooAssets加载Dll:{asset}   {assetObj != null}");
        }

        YooAssetsPackage = package;
        onDownloadComplete();
    }

    /// <summary>
    /// 初始化资源包
    /// </summary>
    private InitializationOperation InitializePackage(ResourcePackage package)
    {
        if (PlayMode == EPlayMode.EditorSimulateMode)
        {
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(package.PackageName);
            var packageRoot = simulateBuildResult.PackageRootDirectory;

            var initParameters = new EditorSimulateModeParameters
            {
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
            };

            return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.OfflinePlayMode)
        {
            var initParameters = new OfflinePlayModeParameters
            {
                BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
            };

            return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.HostPlayMode)
        {
            string packagePath = Application.streamingAssetsPath + "/DefaultPackage";
            bool useLocalCache = Directory.Exists(packagePath) && new DirectoryInfo(packagePath).GetFiles().Length > 0;
            var remoteServices = new RemoteServices(hostServerURL, hostServerURL);
            var initParameters = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = useLocalCache
                    ? FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    : null,
                CacheFileSystemParameters =
                    FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
            };
            return package.InitializeAsync(initParameters);
        }

        return null;
    }

    #endregion

    #region 下载热更资源

    /// <summary>
    /// 下载并更新资源包
    /// </summary>
    IEnumerator DownloadAndUpdateUI()
    {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        var package = YooAssets.GetPackage("DefaultPackage");
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

        if (downloader.TotalDownloadCount == 0)
        {
            SpeedText.text = "无需下载更新";
            ProgressSlider.value = 1f;
            ProgressText.text = "100%";
            Log.Info("无需下载更新！");
            yield break;
        }

        totalDownloadBytes = downloader.TotalDownloadBytes;
        totalDownloadCount = downloader.TotalDownloadCount;

        downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
        downloader.BeginDownload();

        while (!downloader.IsDone)
        {
            UpdateUI();
            yield return null;
        }

        if (downloader.Status == EOperationStatus.Succeed)
        {
            Log.Info("资源更新完成！");
        }
        else
        {
            Log.Error("资源更新失败！");
        }
    }

    /// <summary>
    /// 下载进度更新回调
    /// </summary>
    private void OnDownloadProgressUpdateFunction(DownloadUpdateData data)
    {
        currentDownloadBytes = data.CurrentDownloadBytes;
        currentDownloadCount = data.CurrentDownloadCount;
    }

    /// <summary>
    /// 更新UI显示进度
    /// </summary>
    private void UpdateUI()
    {
        float progress = currentDownloadBytes / (float)totalDownloadBytes;
        ProgressSlider.value = progress;

        SpeedText.text = $"{FormatBytes(currentDownloadBytes)}/{FormatBytes(totalDownloadBytes)}";
        ProgressText.text = $"{progress * 100:F2}%";
    }

    /// <summary>
    /// 格式化字节单位
    /// </summary>
    /// <param name="bytes">字节</param>
    private string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
        return $"{bytes / 1024f / 1024f:F2} MB";
    }

    #endregion

    #region 补充元数据

    /// <summary>
    /// 加载元数据
    /// </summary>
    private void LoadMetadataForAOTAssemblies()
    {
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in HotUpdateConfig.aotFiles)
        {
            byte[] dllBytes = ReadBytesFromStreamingAssets(aotDllName);
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Log.Info($"加载AOT元数据:{aotDllName}. mode:{mode} 返回值:{err}, 大小:{FormatBytes(dllBytes.Length)}字节");
        }
    }

    static byte[] ReadBytesFromStreamingAssets(string dllName)
    {
        if (sAssetDatas.ContainsKey(dllName))
        {
            return sAssetDatas[dllName].bytes;
        }

        return Array.Empty<byte>();
    }

    #endregion

    #region 游戏启动

    /// <summary>
    /// 游戏启动时执行
    /// </summary>
    void StartGame()
    {
        LoadMetadataForAOTAssemblies();
#if !UNITY_EDITOR
        Assembly hotUpdateAss = Assembly.Load(ReadBytesFromStreamingAssets("HotUpdate.dll"));
#else
        Assembly hotUpdateAss = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "HotUpdate");
#endif
        Log.Info("热更新完成");
        // 添加图集监听器
        SpriteAtlasManager.atlasRequested += OnAtlasRequested;
        StartCoroutine(LoadMainScene());
    }

    private void OnAtlasRequested(string atlasName, System.Action<SpriteAtlas> callback)
    {
        // 使用YooAsset加载图集
        var handle = YooAssets.LoadAssetSync<SpriteAtlas>(atlasName);
        if (handle != null && handle.AssetObject != null)
        {
            callback(handle.AssetObject as SpriteAtlas);
        }
    }

    /// <summary>
    /// 实例化资源
    /// </summary>
    IEnumerator LoadMainScene()
    {
        var handle = YooAssets.LoadSceneAsync("Main");
        yield return handle;
    }

    #endregion

    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string defaultHostServer;
        private readonly string fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            this.defaultHostServer = defaultHostServer;
            this.fallbackHostServer = fallbackHostServer;
        }

        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{defaultHostServer}/{fileName}";
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{fallbackHostServer}/{fileName}";
        }
    }
}