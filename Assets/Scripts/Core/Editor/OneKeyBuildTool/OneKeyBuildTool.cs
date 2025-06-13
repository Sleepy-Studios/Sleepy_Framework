using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aot.Runtime;
using HybridCLR.Editor.Commands;
using Renci.SshNet;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using Unity.EditorCoroutines.Editor;
using Debug = UnityEngine.Debug;

namespace ScriptEditor
{
    /// <summary>
    /// 一键打包工具窗口，用于配置热更新、编译打包、服务器设置及资源上传
    /// </summary>
    public class OneKeyBuildTool : EditorWindow
    {
        #region 配置字段

        /// <summary>
        /// 热更新相关配置数据
        /// </summary>
        private static HotUpdateConfig configData;

        /// <summary>
        /// 配置资源文件路径
        /// </summary>
        private const string ConfigAssetPath = "Assets/GameRes/Config/HotUpdateConfig.asset";

        /// <summary>
        /// 平台选择（true为PC，false为Android）
        /// </summary>
        private bool isPcPlatform = true;

        /// <summary>
        /// 是否显示高级设置
        /// </summary>
        private bool showAdvancedSettings;

        /// <summary>
        /// 滚动视图的位置
        /// </summary>
        private Vector2 scrollPosition;

        /// <summary>
        /// 上传状态标识，上传过程中为true
        /// </summary>
        private bool isUploading = false;

        /// <summary>
        /// 当前上传进度（0～1）
        /// </summary>
        private float uploadProgress = 0f;

        /// <summary>
        /// 当前上传文件提示信息
        /// </summary>
        private string uploadProgressMessage = "";

        // DLL替换状态
        private bool isReplacing = false;
        private float replaceProgress = 0f;
        private string replaceProgressMessage = "";
        private bool onlyCompileHotUpdateDll = false;

        private bool cancelUploadRequested = false; // 新增：取消上传请求标志

        // 新增字段
        private bool optimizeAOTDll = false;
        private float stripAOTProgress = 0f;
        private string stripAOTProgressMessage = "";
        private bool isStrippingAOT = false;

        #endregion

        #region 窗口初始化

        /// <summary>
        /// 打开一键打包工具窗口
        /// </summary>
        [MenuItem("Tools/一键打包工具")]
        private static void ShowWindow()
        {
            var window = GetWindow<OneKeyBuildTool>("一键打包工具");
            window.minSize = new Vector2(600, 800);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
            RefreshLocalBundlePathToLatest(); // 新增：刷新本地资源路径为最新
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>
        /// 监听播放模式变化，切换到播放模式时保存配置
        /// </summary>
        /// <param name="state">播放模式状态</param>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                SaveConfig();
        }

        #endregion

        #region 主界面绘制

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHotfixConfigSection(); // 绘制热更新配置区
            DrawBuildSection(); // 绘制编译打包区
            DrawServerConfigSection(); // 绘制服务器配置区
            DrawUploadSection(); // 绘制资源上传区
            EditorGUILayout.EndScrollView();
            DrawProgressBars(); // 同时显示替换和上传进度
            HandleKeyboardEvents();
        }

        #endregion

        #region UI 分区绘制

        /// <summary>
        /// 绘制热更新配置区域
        /// </summary>
        private void DrawHotfixConfigSection()
        {
            GUILayout.Label("程序集配置", EditorStyles.boldLabel);
            var platformFolder = isPcPlatform ? "StandaloneWindows64" : "Android";
            configData.AotSourcePath = $"HybridCLRData\\AssembliesPostIl2CppStrip\\{platformFolder}";
            configData.AotSourcePath = EditorGUILayout.TextField("AOT源路径", configData.AotSourcePath);
            // 动态设置AotStrippedSourcePath
            configData.AotStrippedSourcePath = $"HybridCLRData/StrippedAOTAssembly2/{platformFolder}";
            configData.AotStrippedSourcePath = EditorGUILayout.TextField("AOT优化路径", configData.AotStrippedSourcePath);
            configData.AotTargetPath = EditorGUILayout.TextField("AOT目标路径", configData.AotTargetPath);
            DrawFileList("AOT文件列表（*.dll）", configData.aotFiles);

            GUILayout.Space(10);
            // 热更新配置
            configData.HotUpdateSourcePath = $"HybridCLRData\\HotUpdateDlls\\{platformFolder}";
            configData.HotUpdateSourcePath = EditorGUILayout.TextField("热更新源路径", configData.HotUpdateSourcePath);
            configData.HotUpdateTargetPath = EditorGUILayout.TextField("热更新目标路径", configData.HotUpdateTargetPath);
            DrawFileList("热更新文件列表（*.dll）", configData.hotUpdateFiles);

            if (GUILayout.Button("保存配置", GUILayout.Height(25)))
                SaveConfig();
        }

        /// <summary>
        /// 绘制编译与打包区域
        /// </summary>
        private void DrawBuildSection()
        {
            GUILayout.Space(20);
            GUILayout.Label("编译与打包", EditorStyles.boldLabel);
            // 四个按钮并排，每个20%，间隔5%
            GUILayout.BeginHorizontal();
            {
                float totalWidth = EditorGUIUtility.currentViewWidth;
                float margin = totalWidth * 0.0f; // 不需要边距
                float buttonWidth = totalWidth * 0.20f;
                float spacing = totalWidth * 0.04f;
                // 编译所有DLL
                GUILayout.Space(spacing);
                if (GUILayout.Button("编译所有DLL", GUILayout.Height(30), GUILayout.Width(buttonWidth)))
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteHybridClrBuild());
                }
                GUILayout.Space(spacing);
                // 仅编译热更新DLL
                if (GUILayout.Button("仅编译热更新DLL", GUILayout.Height(30), GUILayout.Width(buttonWidth)))
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteHybridClrHot());
                }
                GUILayout.Space(spacing);
                // 优化AOT大小
                if (GUILayout.Button("优化AOT大小", GUILayout.Height(30), GUILayout.Width(buttonWidth)))
                {
                    StripAOTAssembliesMetadata();
                }
                GUILayout.Space(spacing);
                // DLL替换
                if (GUILayout.Button("DLL替换", GUILayout.Height(30), GUILayout.Width(buttonWidth)))
                {
                    if (!isReplacing)
                        EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteReplaceCoroutine());
                }
                GUILayout.Space(spacing);
            }
            GUILayout.EndHorizontal();

            // 仅编译热更新DLL后面加“是否优化AOT大小”单选框
            GUILayout.BeginHorizontal();
            onlyCompileHotUpdateDll = EditorGUILayout.ToggleLeft("仅编译热更新DLL", onlyCompileHotUpdateDll, GUILayout.Width(180));
            optimizeAOTDll = EditorGUILayout.ToggleLeft("是否优化AOT大小", optimizeAOTDll, GUILayout.Width(180));
            GUILayout.EndHorizontal();

            // 第二行：组合按钮
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("一键编译并替换", GUILayout.Height(32),
                        GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.95f)))
                {
                    if (!isReplacing)
                    {
                        if (onlyCompileHotUpdateDll)
                        {
                            EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteHybridClrHotAndReplace(optimizeAOTDll));
                        }
                        else
                        {
                            EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteFullProcess(optimizeAOTDll));
                        }
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("打开 YooAssets 打包窗口", GUILayout.Height(30),
                        GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.95f)))
                {
                    AssetBundleBuilderWindow.OpenWindow();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制服务器配置区域（宝塔面板）
        /// </summary>
        private void DrawServerConfigSection()
        {
            GUILayout.Space(20);
            GUILayout.Label("服务器配置（宝塔面板）", EditorStyles.boldLabel);

            configData.SshHost = EditorGUILayout.TextField("服务器地址", configData.SshHost);
            configData.SshPort = EditorGUILayout.IntField("SSH端口", configData.SshPort);
            configData.SshUser = EditorGUILayout.TextField("用户名", configData.SshUser);
            configData.KeyFilePath = EditorGUILayout.TextField("Key文件路径", configData.KeyFilePath);

            GUILayout.BeginHorizontal();
            GUI.enabled = !EditorApplication.isPlaying;
            if (GUILayout.Button("测试连接", GUILayout.Width(100)))
                TestSshConnection();
            GUI.enabled = true;
            GUILayout.Label("← 测试服务器连接状态", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制资源上传区域
        /// </summary>
        private void DrawUploadSection()
        {
            GUILayout.Space(20);
            GUILayout.Label("资源部署", EditorStyles.boldLabel);
            DrawPlatformSelector(); // 平台选择
            DrawLocalPathSelector(); // 本地资源路径选择

            // 高级设置
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.TextField("本地资源相对路径", configData.LocalBundlePath);
                EditorGUILayout.LabelField("本地资源绝对路径",
                    Path.GetFullPath(Path.Combine(Application.dataPath, "../", configData.LocalBundlePath)));
                configData.KeyFilePath = EditorGUILayout.TextField("Key文件路径", configData.KeyFilePath);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUI.enabled = !isUploading; // 上传时禁用开始按钮
            if (GUILayout.Button("开始上传", GUILayout.Height(40)))
            {
                cancelUploadRequested = false; // 重置取消标志
                UploadToServer();
            }

            GUI.enabled = true;

            GUI.enabled = isUploading; // 仅在上传时启用取消按钮
            if (GUILayout.Button("取消上传", GUILayout.Height(40)))
            {
                cancelUploadRequested = true;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制文件列表，支持添加和删除文件项
        /// </summary>
        /// <param name="itemTitle">列表标题</param>
        /// <param name="files">文件列表</param>
        private void DrawFileList(string itemTitle, List<string> files)
        {
            GUILayout.Label(itemTitle, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            for (int i = 0; i < files.Count; i++)
            {
                GUILayout.BeginHorizontal();
                files[i] = EditorGUILayout.TextField($"文件 {i + 1}", files[i]);

                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    files.RemoveAt(i--);
                    EditorUtility.SetDirty(configData);
                }

                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 添加新文件", GUILayout.Width(100)))
            {
                files.Add("");
                EditorUtility.SetDirty(configData);
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 绘制本地资源路径选择器
        /// </summary>
        private void DrawLocalPathSelector()
        {
            GUILayout.BeginHorizontal();
            string pathLabel = "路径未初始化";
            if (configData != null && !string.IsNullOrEmpty(configData.LocalBundlePath))
            {
                pathLabel = configData.LocalBundlePath;
            }
            else if (configData != null)
            {
                pathLabel = "未能自动确定路径，请浏览或检查配置";
            }
            else
            {
                pathLabel = "配置数据加载失败";
            }

            EditorGUILayout.LabelField("本地资源路径", pathLabel);

            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
                string initialBrowsePath = projectRoot;

                if (configData != null && !string.IsNullOrEmpty(configData.LocalBundlePath))
                {
                    initialBrowsePath = Path.GetFullPath(Path.Combine(projectRoot, configData.LocalBundlePath));
                }

                if (!Directory.Exists(initialBrowsePath))
                {
                    string parentDir = Path.GetDirectoryName(initialBrowsePath);
                    if (Directory.Exists(parentDir))
                    {
                        initialBrowsePath = parentDir;
                    }
                    else
                    {
                        var platformFolder = isPcPlatform ? "StandaloneWindows64" : "Android";
                        initialBrowsePath =
                            Path.GetFullPath(Path.Combine(projectRoot, "Bundles", platformFolder, "DefaultPackage"));
                        if (!Directory.Exists(initialBrowsePath))
                        {
                            initialBrowsePath = projectRoot;
                        }
                    }
                }

                var path = EditorUtility.OpenFolderPanel("选择资源目录", initialBrowsePath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (configData != null)
                    {
                        configData.LocalBundlePath = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
                        EditorUtility.SetDirty(configData);
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 获取指定路径下最新的文件夹
        /// </summary>
        /// <param name="basePath">基础路径</param>
        /// <returns>最新文件夹的名称</returns>
        private string GetLatestFolder(string basePath)
        {
            if (!Directory.Exists(basePath))
                return string.Empty;

            var directories = Directory.GetDirectories(basePath);
            if (directories.Length == 0)
                return string.Empty;

            return directories.OrderByDescending(d => new DirectoryInfo(d).CreationTime).FirstOrDefault();
        }

        /// <summary>
        /// 绘制平台选择按钮，支持 PC 与 安卓平台切换
        /// </summary>
        private void DrawPlatformSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var prevColor = GUI.backgroundColor;
            bool platformChanged = false;

            GUI.backgroundColor = isPcPlatform ? Color.cyan : prevColor;
            if (GUILayout.Toggle(isPcPlatform, " PC 平台 ", "Button", GUILayout.Width(100), GUILayout.Height(25)))
            {
                if (!isPcPlatform) platformChanged = true;
                isPcPlatform = true;
            }

            GUI.backgroundColor = !isPcPlatform ? Color.green : prevColor;
            if (GUILayout.Toggle(!isPcPlatform, " 安卓平台 ", "Button", GUILayout.Width(100), GUILayout.Height(25)))
            {
                if (isPcPlatform) platformChanged = true;
                isPcPlatform = false;
            }

            GUI.backgroundColor = prevColor;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (platformChanged)
            {
                RefreshLocalBundlePathToLatest();
            }
        }

        private void DrawProgressBars()
        {
            // DLL替换进度
            if (isReplacing)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("替换进度：" + replaceProgressMessage, EditorStyles.boldLabel);
                Rect replaceRect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(replaceRect, replaceProgress, $"{Mathf.RoundToInt(replaceProgress * 100)}%");
            }
            // AOT剥离进度
            if (isStrippingAOT)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("AOT剥离进度：" + stripAOTProgressMessage, EditorStyles.boldLabel);
                Rect stripRect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(stripRect, stripAOTProgress, $"{Mathf.RoundToInt(stripAOTProgress * 100)}%");
            }
            // 上传进度
            if (isUploading)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("上传进度：" + uploadProgressMessage, EditorStyles.boldLabel);
                Rect uploadRect = GUILayoutUtility.GetRect(18, 18, "TextField");
                EditorGUI.ProgressBar(uploadRect, uploadProgress, $"{Mathf.RoundToInt(uploadProgress * 100)}%");
            }
            if (isReplacing || isUploading || isStrippingAOT)
                Repaint();
        }

        #endregion

        #region 核心功能实现

        private IEnumerator ExecuteFullProcess(bool optimizeAOT)
        {
            yield return ExecuteHybridClrBuild();
            if (optimizeAOT)
            {
                // 剥离AOT并等待完成后再替换
                yield return StripAOTAssembliesCoroutine();
                yield return ExecuteReplaceCoroutine(true);
            }
            else
            {
                yield return ExecuteReplaceCoroutine(false);
            }
        }

        private IEnumerator ExecuteHybridClrHotAndReplace(bool optimizeAOT)
        {
            yield return ExecuteHybridClrHot();
            if (optimizeAOT)
            {
                yield return StripAOTAssembliesCoroutine();
                yield return ExecuteReplaceCoroutine(true);
            }
            else
            {
                yield return ExecuteReplaceCoroutine(false);
            }
        }

        private static IEnumerator ExecuteHybridClrBuild()
        {
            PrebuildCommand.GenerateAll();
            yield return null;
        }

        private static IEnumerator ExecuteHybridClrHot()
        {
            CompileDllCommand.CompileDllActiveBuildTarget();
            yield return null;
        }

        // 修改替换流程，支持是否用剥离路径
        private IEnumerator ExecuteReplaceCoroutine(bool useStrippedAOT = false)
        {
            isReplacing = true;
            replaceProgress = 0f;
            // AOT路径选择
            string aotSourcePath = useStrippedAOT
                ? configData.AotStrippedSourcePath
                : configData.AotSourcePath;
            yield return ProcessFilesCoroutine(aotSourcePath, configData.AotTargetPath,
                configData.aotFiles, "AOT");
            yield return ProcessFilesCoroutine(configData.HotUpdateSourcePath, configData.HotUpdateTargetPath,
                configData.hotUpdateFiles, "HotUpdate");

            Debug.Log("<color=#2196F3>ℹ</color> DLL替换完成！");
            ShowNotification(new GUIContent("☝ 程序集替换完成"));
            isReplacing = false;
        }

        private IEnumerator ProcessFilesCoroutine(string source, string target, List<string> files, string category)
        {
            // 因为是协程，所以必须创建集合副本(数据快照)实现数据隔离，避免冲突
            var filesCopy = new List<string>(files);
            Directory.CreateDirectory(target);
            int total = filesCopy.Count;
            int current = 0;

            foreach (var file in filesCopy) // 遍历副本而不是原始集合
            {
                var sourcePath = Path.Combine(source, file);
                var targetPath = Path.Combine(target, $"{Path.GetFileNameWithoutExtension(file)}.dll.bytes");

                replaceProgressMessage = $"[{category}] 处理 {Path.GetFileName(file)}";
                replaceProgress = (float)current / total;

                try
                {
                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, targetPath, true);
                        Debug.Log($"<color=#2196F3>→</color> 已复制：{Path.GetFileName(file)}");
                    }
                    else
                    {
                        Debug.LogWarning($"<color=#FFC107>⚠</color> 文件不存在：{sourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"<color=#F44336>✖</color> 处理文件失败：{ex.Message}");
                }

                current++;
                yield return new WaitForSeconds(0.5f);
            }

            replaceProgress = 1f;
            yield return null;
        }

        /// <summary>
        /// 测试SSH服务器连接，使用私钥认证
        /// </summary>
        private void TestSshConnection()
        {
            try
            {
                var privateKeyFile = new PrivateKeyFile(configData.KeyFilePath);
                var authMethod = new PrivateKeyAuthenticationMethod(configData.SshUser, privateKeyFile);
                var connectionInfo = new ConnectionInfo(configData.SshHost, configData.SshPort, configData.SshUser,
                    authMethod);

                using var client = new SshClient(connectionInfo);
                client.Connect();
                client.RunCommand("echo 'Connection test success!'");
                ShowNotification(new GUIContent("✔服务器连接成功"));
                Debug.Log("<color=#4CAF50>✔</color>服务器连接成功");
                client.Disconnect();
            }
            catch (Exception e)
            {
                HandleSshExceptions(e);
            }
        }

        /// <summary>
        /// 开始资源上传到服务器，采用协程方式异步上传并在主窗口中显示进度及上传速度
        /// </summary>
        private void UploadToServer()
        {
            if (configData == null)
            {
                Debug.LogError("配置数据未加载，无法上传。");
                EditorUtility.DisplayDialog("错误", "配置数据未加载，无法上传。", "确定");
                return;
            }

            if (string.IsNullOrEmpty(configData.LocalBundlePath))
            {
                Debug.LogError("本地资源路径未设置，无法上传。");
                EditorUtility.DisplayDialog("错误", "本地资源路径未设置，请检查配置或浏览选择。", "确定");
                return;
            }

            var localPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", configData.LocalBundlePath));
            if (!Directory.Exists(localPath))
            {
                EditorUtility.DisplayDialog("路径错误", $"本地资源路径不存在：\n{localPath}", "确定");
                Debug.LogError("路径错误：本地资源路径不存在：" + localPath);
                return;
            }

            try
            {
                var platformFolder = isPcPlatform ? "PC" : "Android";
                var remotePath = $"{configData.ServerBasePath}/{platformFolder}/";

                // 1. 加载私钥及建立连接信息
                var privateKeyFile = new PrivateKeyFile(configData.KeyFilePath);
                var authMethod = new PrivateKeyAuthenticationMethod(configData.SshUser, privateKeyFile);
                var connectionInfo = new ConnectionInfo(configData.SshHost, configData.SshPort, configData.SshUser,
                    authMethod);

                // 2. 清理服务器目录
                using (var ssh = new SshClient(connectionInfo))
                {
                    ssh.Connect();
                    ssh.RunCommand($"rm -rf {remotePath} && mkdir -p {remotePath}");
                    ssh.Disconnect();
                    Debug.Log($"清理服务器目录：{remotePath}");
                }

                // 3. 获取待上传文件列表
                var files = Directory.GetFiles(localPath, "*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    EditorUtility.DisplayDialog("提示", "没有找到待上传的资源文件。", "确定");
                    Debug.Log("上传提示：没有找到待上传的资源文件。");
                    return;
                }

                // 4. 设置上传状态标识，开始协程上传文件
                isUploading = true;
                EditorCoroutineUtility.StartCoroutineOwnerless(UploadFilesCoroutine(connectionInfo, localPath,
                    remotePath, files));
            }
            catch (Exception e)
            {
                isUploading = false;
                HandleSshExceptions(e);
            }
        }

        /// <summary>
        /// 协程方法：逐个文件上传并实时更新主窗口中的进度及上传速度
        /// </summary>
        /// <param name="connectionInfo">SSH连接信息</param>
        /// <param name="localPath">本地资源根目录</param>
        /// <param name="remotePath">远程资源根目录</param>
        /// <param name="files">所有待上传文件路径数组</param>
        /// <returns>IEnumerator</returns>
        private IEnumerator UploadFilesCoroutine(ConnectionInfo connectionInfo, string localPath, string remotePath,
            string[] files)
        {
            int totalFiles = files.Length;
            int currentFile = 0;
            bool cancelledInternally = false;

            using (var sftp = new SftpClient(connectionInfo))
            {
                sftp.Connect();
                foreach (var file in files)
                {
                    if (cancelUploadRequested)
                    {
                        cancelledInternally = true;
                        Debug.Log("<color=orange>上传操作已由用户取消。</color>");
                        ShowNotification(new GUIContent("上传已取消"));
                        break;
                    }

                    string relativePath = file.Substring(localPath.Length + 1);
                    string remoteFile = $"{remotePath}{relativePath.Replace('\\', '/')}";
                    string remoteDir = Path.GetDirectoryName(remoteFile);

                    if (!sftp.Exists(remoteDir))
                        sftp.CreateDirectory(remoteDir);

                    using (var stream = File.OpenRead(file))
                    {
                        var stopwatch = Stopwatch.StartNew();
                        sftp.UploadFile(stream, remoteFile);
                        stopwatch.Stop();
                        double speed = stream.Length / 1024.0 / 1024.0 / stopwatch.Elapsed.TotalSeconds;
                        uploadProgressMessage =
                            $"[{currentFile + 1}/{totalFiles}] {Path.GetFileName(file)} 速度：{speed:F2} MB/s";
                        Debug.Log(uploadProgressMessage);
                    }

                    currentFile++;
                    uploadProgress = (float)currentFile / totalFiles;

                    yield return null;
                }

                sftp.Disconnect();
            }

            isUploading = false;
            if (cancelledInternally)
            {
                cancelUploadRequested = false;
            }

            if (!cancelledInternally && totalFiles > 0 && currentFile == totalFiles)
            {
                ShowNotification(new GUIContent($"√√√ 上传完成！共 {currentFile} 个文件 √√√"));
                Debug.Log($"√√√ 上传完成！共 {currentFile} 个文件 √√√");
            }
            else if (!cancelledInternally && totalFiles == 0)
            {
            }

            Repaint();
        }

        private void HandleSshExceptions(Exception e)
        {
            string errorMsg = e.Message;
            if (errorMsg.Contains("invalid private key"))
            {
                EditorUtility.DisplayDialog("私钥错误", "私钥格式无效或路径错误", "确定");
            }
            else if (errorMsg.Contains("Permission denied"))
            {
                EditorUtility.DisplayDialog("权限错误", "服务器公钥未配置或用户无权限", "确定");
            }
            else if (errorMsg.Contains("No such file or directory"))
            {
                EditorUtility.DisplayDialog("路径错误", "服务器目录不存在或无法访问", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("连接错误", $"错误信息：{errorMsg}", "确定");
            }

            Debug.LogError($"SSH Error: {e}");
        }

        #endregion

        #region 配置管理

        private void LoadConfig()
        {
            if (!File.Exists(ConfigAssetPath))
                CreateConfig();

            configData = AssetDatabase.LoadAssetAtPath<HotUpdateConfig>(ConfigAssetPath);
            if (configData == null)
                Debug.LogError("配置文件加载失败！");
        }

        private static void CreateConfig()
        {
            configData = ScriptableObject.CreateInstance<HotUpdateConfig>();
            configData.LocalBundlePath = "Bundles/StandaloneWindows64/DefaultPackage/";
            configData.KeyFilePath = "Assets/GameRes/Config/key";

            AssetDatabase.CreateAsset(configData, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SaveConfig()
        {
            if (configData == null)
                return;

            string projectRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
            configData.LocalBundlePath = Path.GetRelativePath(projectRootPath,
                Path.GetFullPath(Path.Combine(projectRootPath, configData.LocalBundlePath)));

            EditorUtility.SetDirty(configData);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("㊣ 配置保存成功"));
        }

        #endregion

        #region 辅助功能

        private void RefreshLocalBundlePathToLatest()
        {
            if (configData == null)
            {
                LoadConfig();
                if (configData == null)
                {
                    Debug.LogError("HotUpdateConfig无法加载，无法刷新本地资源路径。");
                    return;
                }
            }

            var platformFolder = isPcPlatform ? "StandaloneWindows64" : "Android";
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));

            var packageBaseDirRelative = Path.Combine("Bundles", platformFolder, "DefaultPackage");
            var packageBaseDirAbsolute = Path.GetFullPath(Path.Combine(projectRoot, packageBaseDirRelative));

            string finalRelativePathToSet;

            if (Directory.Exists(packageBaseDirAbsolute))
            {
                var latestFolderFullPath = GetLatestFolder(packageBaseDirAbsolute);
                if (!string.IsNullOrEmpty(latestFolderFullPath))
                {
                    finalRelativePathToSet = Path.GetRelativePath(projectRoot, latestFolderFullPath);
                }
                else
                {
                    finalRelativePathToSet = Path.GetRelativePath(projectRoot, packageBaseDirAbsolute);
                }
            }
            else
            {
                finalRelativePathToSet = packageBaseDirRelative;
                Debug.LogWarning(
                    $"本地资源的基础路径不存在: {packageBaseDirAbsolute}。LocalBundlePath将设置为预期的相对路径: {finalRelativePathToSet}");
            }

            finalRelativePathToSet = finalRelativePathToSet.Replace('\\', '/');

            if (configData.LocalBundlePath != finalRelativePathToSet)
            {
                configData.LocalBundlePath = finalRelativePathToSet;
                EditorUtility.SetDirty(configData);
                Debug.Log($"本地资源路径已自动更新为: {configData.LocalBundlePath}");
            }

            Repaint();
        }

        private void HandleKeyboardEvents()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.control && Event.current.keyCode == KeyCode.S)
                {
                    SaveConfig();
                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// 剥离AOT DLL非泛型元数据，优化补充元数据dll大小
        /// </summary>
        private void StripAOTAssembliesMetadata()
        {
            if (isStrippingAOT) return;
            EditorCoroutineUtility.StartCoroutineOwnerless(StripAOTAssembliesCoroutine());
        }

        private IEnumerator StripAOTAssembliesCoroutine()
        {
            isStrippingAOT = true;
            stripAOTProgress = 0f;
            stripAOTProgressMessage = "开始剥离AOT元数据...";
            string srcDir = $"HybridCLRData/AssembliesPostIl2CppStrip/{(isPcPlatform ? "StandaloneWindows64" : "Android")}";
            string dstDir = $"HybridCLRData/StrippedAOTAssembly2/{(isPcPlatform ? "StandaloneWindows64" : "Android")}";
            Directory.CreateDirectory(dstDir);
            var dlls = Directory.GetFiles(srcDir, "*.dll");
            int total = dlls.Length;
            int count = 0;
            foreach (var src in dlls)
            {
                string dllName = Path.GetFileName(src);
                string dstFile = Path.Combine(dstDir, dllName);
                stripAOTProgressMessage = $"剥离 {dllName} ({count + 1}/{total})";
                HybridCLR.Editor.AOT.AOTAssemblyMetadataStripper.Strip(src, dstFile);
                count++;
                stripAOTProgress = (float)count / total;
                Repaint();
                yield return null;
            }
            stripAOTProgress = 1f;
            stripAOTProgressMessage = $"AOT元数据剥离完成，共处理{count}个DLL";
            ShowNotification(new GUIContent(stripAOTProgressMessage));
            Debug.Log($"AOT元数据剥离完成，共处理{count}个DLL，输出目录: {dstDir}");
            isStrippingAOT = false;
            Repaint();
        }
        #endregion
    }
}

