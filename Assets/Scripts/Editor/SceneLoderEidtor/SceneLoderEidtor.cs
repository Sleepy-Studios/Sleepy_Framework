using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    /// <summary>
    /// 场景加载器工具栏按钮，用于在Unity编辑器工具栏上显示场景加载按钮
    /// </summary>
    [EditorToolbarElement("SceneLoaderToolbar")]
    public class SceneLoaderToolbarButton : EditorToolbarButton
    {
        public SceneLoaderToolbarButton()
        {
            text = "SceneLoader";
            tooltip = "选择并加载场景";
            clicked += OnSceneButtonClicked;
        }

        /// <summary>
        /// 按钮点击事件处理
        /// </summary>
        private void OnSceneButtonClicked()
        {
            GenericMenu menu = new GenericMenu();

            // 添加管理选项
            menu.AddItem(new GUIContent("刷新场景列表"), false, SceneLoader.RefreshSceneList);
            menu.AddItem(new GUIContent("场景加载器设置"), false, () => EditorWindow.GetWindow<SceneLoaderSettings>());
            menu.AddSeparator("");

            // 添加所有可用场景到菜单
            foreach (var scene in SceneLoader.GetScenes())
            {
                string path = scene.Value;
                string name = scene.Key;
                menu.AddItem(new GUIContent(name), false, () =>
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(path);
                    }
                });
            }

            menu.ShowAsContext();
        }
    }

    /// <summary>
    /// 场景加载器显示控制开关
    /// </summary>
    [EditorToolbarElement("NeedShowSceneLoader")]
    public class SceneLoaderToggle : EditorToolbarToggle
    {
        public SceneLoaderToggle()
        {
            text = "显示场景加载器";
            tooltip = "是否在工具栏显示场景加载器";
            value = EditorPrefs.GetBool("SceneLoader_ShowToolbar", true);

            // 注册值变化事件
            this.RegisterValueChangedCallback(evt => { EditorPrefs.SetBool("SceneLoader_ShowToolbar", evt.newValue); });
        }
    }

    /// <summary>
    /// 场景加载器核心功能类
    /// 管理场景列表、UI注册和场景加载操作
    /// </summary>
    public static class SceneLoader
    {
        // 场景路径默认值
        private static string scenesPath = "Assets/GameRes/Scenes";

        // 存储场景名称和路径的字典
        private static Dictionary<string, string> scenes = new Dictionary<string, string>();

        /// <summary>
        /// 场景文件夹路径属性，带有持久化存储
        /// </summary>
        private static string ScenesFolderPath
        {
            get => EditorPrefs.GetString("SceneLoader_ScenesPath", scenesPath);
            set
            {
                EditorPrefs.SetString("SceneLoader_ScenesPath", value);
                scenesPath = value;
                RefreshSceneList();
            }
        }

        /// <summary>
        /// 初始化方法，在编辑器启动时自动调用
        /// </summary>
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                RefreshSceneList();
                RegisterToolbar();
            };
        }

        /// <summary>
        /// 注册工具栏UI
        /// </summary>
        private static void RegisterToolbar()
        {
            ToolbarExtension.ToolbarZoneRightAlign += OnToolbarGUI;
        }

        /// <summary>
        /// 工具栏UI绘制回调
        /// </summary>
        private static void OnToolbarGUI(VisualElement rootVisualElement)
        {
            // 如果用户禁用了显示，则不添加UI
            if (!EditorPrefs.GetBool("SceneLoader_ShowToolbar", true))
                return;

            // 创建下拉按钮
            var dropdown = new EditorToolbarDropdown();
            dropdown.text = "SceneLoader";
            dropdown.clicked += () =>
            {
                GenericMenu menu = new GenericMenu();

                // 添加管理选项
                menu.AddItem(new GUIContent("刷新场景列表"), false, RefreshSceneList);
                menu.AddItem(new GUIContent("场景加载器设置"), false,
                    () => EditorWindow.GetWindow<SceneLoaderSettings>());
                menu.AddSeparator("");

                // 添加所有可用场景到菜单
                foreach (var scene in scenes)
                {
                    string path = scene.Value;
                    string name = scene.Key;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(path);
                        }
                    });
                }

                menu.ShowAsContext();
            };

            // 设置UI样式
            dropdown.style.unityTextAlign = TextAnchor.MiddleCenter;

            // 添加到根元素
            rootVisualElement.Add(dropdown);
        }

        /// <summary>
        /// 刷新场景列表，更新可用场景
        /// </summary>
        public static void RefreshSceneList()
        {
            scenes.Clear();

            // 确保目录存在
            if (Directory.Exists(ScenesFolderPath))
            {
                // 搜索所有.unity文件，包括子目录
                string[] sceneFiles = Directory.GetFiles(ScenesFolderPath, "*.unity", SearchOption.AllDirectories);

                foreach (string scenePath in sceneFiles)
                {
                    // 转换为Unity项目路径格式 (使用正斜杠)
                    string unityPath = scenePath.Replace('\\', '/');

                    // 确保路径以Assets开头
                    if (!unityPath.StartsWith("Assets"))
                    {
                        int assetsIndex = unityPath.IndexOf("Assets", StringComparison.Ordinal);
                        if (assetsIndex >= 0)
                            unityPath = unityPath.Substring(assetsIndex);
                    }

                    // 提取场景名称并添加到字典
                    string sceneName = Path.GetFileNameWithoutExtension(unityPath);
                    scenes[sceneName] = unityPath;
                }
            }
        }

        /// <summary>
        /// 获取所有可用场景
        /// </summary>
        public static Dictionary<string, string> GetScenes()
        {
            return scenes;
        }

        /// <summary>
        /// 设置场景文件夹路径
        /// </summary>
        public static void SetScenesPath(string path)
        {
            ScenesFolderPath = path;
        }
    }

    /// <summary>
    /// 场景加载器设置窗口
    /// 提供UI界面配置场景加载器参数
    /// </summary>
    public class SceneLoaderSettings : EditorWindow
    {
        [MenuItem("Tools/Scene Loader Settings")]
        private static void ShowWindow()
        {
            var window = GetWindow<SceneLoaderSettings>();
            window.titleContent = new GUIContent("场景加载器设置");
            window.Show();
        }

        // 滚动位置和路径输入
        private Vector2 scrollPosition;
        private string newPath = "";

        /// <summary>
        /// 窗口启用时加载当前设置
        /// </summary>
        private void OnEnable()
        {
            newPath = EditorPrefs.GetString("SceneLoader_ScenesPath", "Assets/GameRes/Scenes");
        }

        /// <summary>
        /// 绘制设置窗口UI
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("场景加载器设置", EditorStyles.boldLabel);

            // 路径设置区域
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("场景文件夹路径:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            newPath = EditorGUILayout.TextField(newPath);

            // 文件夹浏览按钮
            if (GUILayout.Button("浏览...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("选择场景文件夹", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换绝对路径为项目相对路径
                    string projectPath = Application.dataPath;
                    projectPath = projectPath.Substring(0, projectPath.Length - 6); // 去掉 "Assets"

                    if (path.StartsWith(projectPath))
                    {
                        newPath = "Assets" + path.Substring(projectPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("错误", "请选择项目内的文件夹", "确定");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // 保存按钮
            if (GUILayout.Button("保存路径并刷新场景列表"))
            {
                SceneLoader.SetScenesPath(newPath);
            }

            // 工具栏显示设置
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("工具栏设置:", EditorStyles.boldLabel);

            bool showToolbar = EditorPrefs.GetBool("SceneLoader_ShowToolbar", true);
            bool newShowToolbar = EditorGUILayout.Toggle("在工具栏显示场景加载器", showToolbar);
            if (newShowToolbar != showToolbar)
            {
                EditorPrefs.SetBool("SceneLoader_ShowToolbar", newShowToolbar);
            }

            // 场景列表区域
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当前可用场景:", EditorStyles.boldLabel);

            // 使用滚动视图显示场景列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var scene in SceneLoader.GetScenes())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(scene.Key);

                // 加载按钮
                if (GUILayout.Button("加载", GUILayout.Width(80)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene.Value);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}