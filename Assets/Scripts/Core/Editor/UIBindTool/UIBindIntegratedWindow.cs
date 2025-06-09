using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Core.Editor.UIBindTool
{
    /// <summary>
    /// UI绑定集成工具窗口
    /// 提供UI组件绑定和代码生成功能，帮助快速创建UI视图并管理UI资源
    /// </summary>
    public class UIBindIntegratedWindow : EditorWindow
    {
        #region 窗口变量与数据结构

        /// <summary>
        /// 标签页类型枚举
        /// </summary>
        private enum TabType
        {
            BindTool, // 绑定工具页签
            Manager   // 管理页签
        }

        // 当前激活的标签页
        private TabType currentTab = TabType.BindTool;
        
        // 搜索相关
        private string searchString = "";

        // 滚动位置
        private Vector2 bindToolScrollPosition;
        private Vector2 managerScrollPosition;

        #endregion

        #region 窗口生命周期与初始化

        /// <summary>
        /// 打开UI绑定工具窗口
        /// </summary>
        [MenuItem("Tools/UI绑定工具")]
        public static void ShowWindow()
        {
            UIBindIntegratedWindow window = GetWindow<UIBindIntegratedWindow>("UI绑定工具");
            window.minSize = new Vector2(600, 500);
            window.OnEnable();
        }

        /// <summary>
        /// 窗口启用时调用
        /// </summary>
        private void OnEnable()
        {
            // 注册编辑器更新回调，用于检测预制体打开状态
            EditorApplication.update += OnEditorUpdate;

            // 扫描UI视图
            UIBindUtility.ScanUIViews();

            // 检查当前预制体
            UIBindUtility.CheckCurrentPrefab();
        }

        /// <summary>
        /// 窗口禁用时调用
        /// </summary>
        private void OnDisable()
        {
            // 移除更新回调
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 编辑器更新回调
        /// </summary>
        private void OnEditorUpdate()
        {
            // 检查预制体变化
            bool prefabChanged = UIBindUtility.CheckCurrentPrefab();
            if (prefabChanged)
            {
                // 自动切换到绑定工具页签
                currentTab = TabType.BindTool;
                Repaint();
            }
        }

        #endregion

        #region GUI主入口

        /// <summary>
        /// GUI绘制主入口
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            // 绘制标签页
            EditorGUILayout.Space();
            DrawTabs();

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 根据当前标签页绘制相应内容
            switch (currentTab)
            {
                case TabType.BindTool:
                    DrawBindToolTab();
                    break;
                case TabType.Manager:
                    DrawManagerTab();
                    break;
            }
        }

        /// <summary>
        /// 绘制顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 标题
            GUILayout.Label("UI绑定工具", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // 根据当前标签页显示不同工具栏选项
            if (currentTab == TabType.Manager)
            {
                // 搜索框
                GUI.SetNextControlName("SearchField");
                string newSearch = EditorGUILayout.TextField(searchString, EditorStyles.toolbarSearchField,
                    GUILayout.Width(200));
                if (newSearch != searchString)
                {
                    searchString = newSearch;
                }

                if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    searchString = "";
                    GUI.FocusControl(null);
                }
            }

            // 刷新按钮
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (currentTab == TabType.Manager)
                    UIBindUtility.ScanUIViews();
                else
                    UIBindUtility.CheckCurrentPrefab();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制标签页切换按钮
        /// </summary>
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            // 创建标签页样式
            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton);
            tabStyle.fontSize = 12;
            tabStyle.fontStyle = FontStyle.Bold;
            tabStyle.fixedHeight = 30;

            // 组件绑定标签
            if (GUILayout.Toggle(currentTab == TabType.BindTool, "组件绑定", tabStyle))
                currentTab = TabType.BindTool;

            // UI管理标签
            if (GUILayout.Toggle(currentTab == TabType.Manager, "UI 管理", tabStyle))
                currentTab = TabType.Manager;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 绑定工具标签页

        /// <summary>
        /// 绘制绑定工具标签页内容
        /// </summary>
        private void DrawBindToolTab()
        {
            // 如果没有选择预制体，显示选择预制体的界面
            if (UIBindData.CurrentPrefab == null)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.HelpBox("请先打开或选择一个UI预制体", MessageType.Info);

                if (GUILayout.Button("选择预制体", GUILayout.Height(30)))
                {
                    string prefabPath = EditorUtility.OpenFilePanelWithFilters(
                        "选择UI预制体",
                        "Assets",
                        new[] { "预制体", "prefab" });

                    if (!string.IsNullOrEmpty(prefabPath) && prefabPath.StartsWith(Application.dataPath))
                    {
                        string assetPath = "Assets" + prefabPath.Substring(Application.dataPath.Length);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        if (prefab != null)
                        {
                            UIBindData.CurrentPrefab = prefab;
                            UIBindManager.SetCurrentPrefab(prefab);
                            UIBindData.ViewName = prefab.name + "View";
                        }
                    }
                }

                EditorGUILayout.EndVertical();
                return;
            }

            // 开始滚动视图
            bindToolScrollPosition = EditorGUILayout.BeginScrollView(bindToolScrollPosition);

            // 绘制预制体信息区域
            DrawPrefabInfo();
            
            // 绘制配置区域
            DrawConfigArea();
            
            // 绘制已选组件列表
            DrawSelectedComponents();
            
            // 绘制操作按钮区域
            DrawBindToolButtons();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制预制体信息区域
        /// </summary>
        private void DrawPrefabInfo()
        {
            EditorGUILayout.BeginVertical("box");
            
            // 显示预制体名称
            EditorGUILayout.LabelField($"当前预制体: {UIBindData.CurrentPrefab.name}", EditorStyles.boldLabel);
            
            // 在预制体编辑模式下，从prefabStage获取路径
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            string prefabPath = prefabStage != null ? prefabStage.assetPath : "";
            
            // 显示路径（只读）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("预制体路径", prefabPath);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制配置区域
        /// </summary>
        private void DrawConfigArea()
        {
            EditorGUILayout.BeginVertical("box");
            
            // 代码生成配置
            UIBindData.NamespaceName = EditorGUILayout.TextField("命名空间", UIBindData.NamespaceName);
            UIBindData.ModuleName = EditorGUILayout.TextField("模块名", UIBindData.ModuleName);
            UIBindData.ViewName = EditorGUILayout.TextField("视图名", UIBindData.ViewName);
            UIBindData.OutputPath = EditorGUILayout.TextField("输出路径", UIBindData.OutputPath);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制已选组件列表
        /// </summary>
        private void DrawSelectedComponents()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("已选择的组件:", EditorStyles.boldLabel);

            var selectedComponents = UIBindManager.GetSelectedComponents();
            if (selectedComponents.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未选择任何组件。在Hierarchy中点击游戏对象右侧的复选框来选择组件。", MessageType.Info);
            }
            else
            {
                // 遍历显示每个游戏对象及其选中的组件
                foreach (var entry in selectedComponents)
                {
                    EditorGUILayout.BeginVertical("helpBox");

                    // 显示游戏对象路径
                    string hierarchyPath = UIBindUtility.GetHierarchyPath(entry.Key, UIBindData.CurrentPrefab);
                    EditorGUILayout.LabelField(hierarchyPath, EditorStyles.boldLabel);

                    // 显示该游戏对象上选中的所有组件
                    foreach (var component in entry.Value.ToArray())
                    {
                        if (component == null) continue;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(component, component.GetType(), true);

                        // 移除按钮
                        if (GUILayout.Button("移除", GUILayout.Width(60)))
                        {
                            UIBindManager.RemoveComponentSelection(entry.Key, component);
                            break; // 防止修改集合时的迭代错误
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制操作按钮区域
        /// </summary>
        private void DrawBindToolButtons()
        {
            EditorGUILayout.BeginVertical();

            // 生成绑定代码按钮
            if (GUILayout.Button("生成绑定代码", GUILayout.Height(30)))
            {
                UIBindUtility.GenerateBindingCode();
            }

            // 清除所有选择按钮
            if (GUILayout.Button("清除所有选择", GUILayout.Height(30)))
            {
                UIBindManager.ClearAllSelections();
                EditorApplication.RepaintHierarchyWindow();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region UI管理标签页

        /// <summary>
        /// 绘制UI管理标签页内容
        /// </summary>
        private void DrawManagerTab()
        {
            // 顶部控制区域
            EditorGUILayout.BeginHorizontal();

            // 展开/折叠所有按钮
            if (GUILayout.Button("全部展开", GUILayout.Width(80)))
            {
                foreach (var key in UIBindData.ModulesFoldout.Keys.ToList())
                {
                    UIBindData.ModulesFoldout[key] = true;
                }

                foreach (var key in UIBindData.ViewsFoldout.Keys.ToList())
                {
                    UIBindData.ViewsFoldout[key] = true;
                }
            }

            if (GUILayout.Button("全部折叠", GUILayout.Width(80)))
            {
                foreach (var key in UIBindData.ModulesFoldout.Keys.ToList())
                {
                    UIBindData.ModulesFoldout[key] = false;
                }

                foreach (var key in UIBindData.ViewsFoldout.Keys.ToList())
                {
                    UIBindData.ViewsFoldout[key] = false;
                }
            }

            // 显示全部UI选项
            bool newShowAllUI = EditorGUILayout.ToggleLeft("显示全部UI", UIBindData.ShowAllUI, GUILayout.Width(100));
            if (newShowAllUI != UIBindData.ShowAllUI)
            {
                UIBindData.ShowAllUI = newShowAllUI;
            }

            EditorGUILayout.EndHorizontal();

            // 开始滚动视图
            managerScrollPosition = EditorGUILayout.BeginScrollView(managerScrollPosition);

            // 过滤搜索结果和自动生成的UI
            var filteredModules = UIBindData.UIModules
                .Where(m => UIBindData.ShowAllUI || m.Value.Views.Any(v => v.Value.IsAutoGenerated))
                .Where(m => string.IsNullOrEmpty(searchString) ||
                            m.Key.ToLower().Contains(searchString.ToLower()) ||
                            m.Value.Views.Any(v => v.Key.ToLower().Contains(searchString.ToLower())))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 显示提示信息（如果没有找到UI视图）
            if (filteredModules.Count == 0)
            {
                if (UIBindData.ShowAllUI)
                    EditorGUILayout.HelpBox("没有找到UI视图", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("没有找到使用UI绑定自动生成的UI视图", MessageType.Info);
            }

            // 遍历并绘制每个模块
            foreach (var moduleKvp in filteredModules)
            {
                string moduleName = moduleKvp.Key;
                UIBindData.UIModuleData moduleData = moduleKvp.Value;

                // 根据选项过滤视图
                var filteredViews = UIBindData.ShowAllUI
                    ? moduleData.Views
                    : moduleData.Views.Where(v => v.Value.IsAutoGenerated)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (filteredViews.Count == 0)
                    continue;

                // 确保字典中有该模块的折叠状态
                UIBindData.ModulesFoldout.TryAdd(moduleName, true);

                // 绘制模块折叠标题
                EditorGUILayout.BeginVertical("box");

                // 模块标题行
                EditorGUILayout.BeginHorizontal();
                UIBindData.ModulesFoldout[moduleName] = EditorGUILayout.Foldout(UIBindData.ModulesFoldout[moduleName], moduleName, true,
                    EditorStyles.foldoutHeader);
                GUILayout.FlexibleSpace();

                // 显示视图数量信息
                int autoGenCount = moduleData.Views.Count(v => v.Value.IsAutoGenerated);
                int totalCount = moduleData.Views.Count;
                EditorGUILayout.LabelField($"自动绑定: {autoGenCount}/{totalCount}", EditorStyles.miniLabel,
                    GUILayout.Width(120));

                EditorGUILayout.EndHorizontal();

                if (UIBindData.ModulesFoldout[moduleName])
                {
                    // 绘制每个View项
                    foreach (var viewKvp in filteredViews)
                    {
                        string viewName = viewKvp.Key;
                        UIBindData.UIViewData viewData = viewKvp.Value;

                        // 过滤搜索结果
                        if (!string.IsNullOrEmpty(searchString) &&
                            !viewName.ToLower().Contains(searchString.ToLower()) &&
                            !moduleName.ToLower().Contains(searchString.ToLower()))
                        {
                            continue;
                        }

                        // 确保字典中有该视图的折叠状态
                        UIBindData.ViewsFoldout.TryAdd(viewName, false);

                        EditorGUILayout.BeginVertical("helpBox");

                        // 视图标题行
                        EditorGUILayout.BeginHorizontal();
                        UIBindData.ViewsFoldout[viewName] = EditorGUILayout.Foldout(UIBindData.ViewsFoldout[viewName], viewName, true);

                        // 显示状态标签
                        if (viewData.IsAutoGenerated)
                        {
                            GUIStyle autoGenStyle = new GUIStyle(EditorStyles.miniLabel);
                            autoGenStyle.normal.textColor = Color.green;
                            EditorGUILayout.LabelField("自动绑定", autoGenStyle, GUILayout.Width(60));
                        }
                        else
                        {
                            GUIStyle manualStyle = new GUIStyle(EditorStyles.miniLabel);
                            manualStyle.normal.textColor = Color.gray;
                            EditorGUILayout.LabelField("手动创建", manualStyle, GUILayout.Width(60));
                        }

                        // 显示最后修改时间
                        EditorGUILayout.LabelField(viewData.LastModifiedTime.ToString("yyyy-MM-dd HH:mm"),
                            EditorStyles.miniLabel, GUILayout.Width(140));

                        EditorGUILayout.EndHorizontal();

                        if (UIBindData.ViewsFoldout[viewName])
                        {
                            EditorGUI.indentLevel++;

                            // View脚本
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("View脚本", GUILayout.Width(100));
                            EditorGUILayout.LabelField(Path.GetFileName(viewData.ViewScriptPath));
                            if (GUILayout.Button("打开", GUILayout.Width(50)))
                            {
                                AssetDatabase.OpenAsset(
                                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(viewData.ViewScriptPath));
                            }

                            EditorGUILayout.EndHorizontal();

                            // ViewComponent脚本
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("组件脚本", GUILayout.Width(100));
                            if (File.Exists(viewData.ViewComponentPath))
                            {
                                EditorGUILayout.LabelField(Path.GetFileName(viewData.ViewComponentPath));
                                if (GUILayout.Button("打开", GUILayout.Width(50)))
                                {
                                    AssetDatabase.OpenAsset(
                                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(viewData.ViewComponentPath));
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("未找到组件脚本", EditorStyles.boldLabel);
                            }

                            EditorGUILayout.EndHorizontal();

                            // 预制体
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("预制体", GUILayout.Width(100));
                            if (!string.IsNullOrEmpty(viewData.PrefabPath) && File.Exists(viewData.PrefabPath))
                            {
                                EditorGUILayout.LabelField(Path.GetFileName(viewData.PrefabPath));
                                if (GUILayout.Button("打开", GUILayout.Width(50)))
                                {
                                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(viewData.PrefabPath);
                                    if (prefab != null)
                                    {
                                        AssetDatabase.OpenAsset(prefab);
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog("错误", $"无法打开预制体: {viewData.PrefabPath}", "确定");
                                    }
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("未找到预制体", EditorStyles.boldLabel);
                                if (GUILayout.Button("查找", GUILayout.Width(50)))
                                {
                                    if (UIBindUtility.FindPrefabForView(viewData))
                                    {
                                        Repaint();
                                    }
                                }
                            }

                            EditorGUILayout.EndHorizontal();

                            EditorGUI.indentLevel--;
                        }

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}
