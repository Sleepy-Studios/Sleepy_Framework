using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Compilation; // 添加编译相关命名空间
using System.Threading.Tasks;
using Core.Runtime.UIBindTool;

namespace Core.Editor.UIBindTool
{
    /// <summary>
    /// UI绑定工具的辅助方法类，提供各种实用功能方法
    /// </summary>
    public class UIBindUtility
    {
        // 添加编译监听状态变量
        public static bool isCompileAndAdd
        {
            get => EditorPrefs.GetBool("UIBindUtility_isCompileAndAdd", false);
            set => EditorPrefs.SetBool("UIBindUtility_isCompileAndAdd", value);
        }
        // 添加编译状态上下文变量，用于保存编译前的状态
        private static string savedPrefabPath = string.Empty;
        private static string savedViewName = string.Empty;
        private static string savedModuleName = string.Empty;

        /// <summary>
        /// 获取游戏对象相对于根对象的层级路径
        /// </summary>
        /// <param name="gameObject">目标游戏对象</param>
        /// <param name="root">根游戏对象</param>
        /// <returns>层级路径字符串</returns>
        public static string GetHierarchyPath(GameObject gameObject, GameObject root)
        {
            if (gameObject == root)
                return root.name;

            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;

            // 向上遍历父级对象直到根对象
            while (parent != null && parent != root.transform)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        /// <summary>
        /// 保存预制体
        /// </summary>
        /// <returns>是否成功保存</returns>
        public static bool SavePrefab()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // 如果在预制体编辑模式下，保存预制体
                string assetPath = prefabStage.assetPath;
                GameObject prefabRoot = prefabStage.prefabContentsRoot;
                // 直接标记预制体为已修改
                EditorUtility.SetDirty(prefabRoot);
                // 将修改应用到预制体资源
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                // 保存所有资源，确保修改被持久化
                AssetDatabase.SaveAssets();
                // 刷新Asset数据库，确保所有更改都被应用
                AssetDatabase.Refresh();
                // 使用更明显的日志输出方式
                Debug.LogWarning("【UI绑定工具】预制体已成功保存: " + assetPath);
                return true;
            }

            // 如果没有保存成功，输出一条信息
            Debug.LogWarning("【UI绑定工具】当前不在预制体编辑模式，无法保存预制体");
            return false;
        }

        /// <summary>
        /// 更新预制体的ComponentItemKey组件数据
        /// </summary>
        /// <param name="prefab">目标预制体</param>
        /// <param name="selectedComponents">已选择的组件字典</param>
        public static void UpdateComponentItemKeyData(GameObject prefab,
            Dictionary<GameObject, List<Component>> selectedComponents)
        {
            ComponentItemKey componentItemKey = prefab.GetComponent<ComponentItemKey>();
            if (componentItemKey == null)
                return;

            // 清空现有数据
            componentItemKey.Dic.Clear();

            // 确保序列化数据列表已初始化
            if (componentItemKey.ComponentDatas == null)
            {
                componentItemKey.ComponentDatas = new List<ComponentData>();
            }
            else
            {
                componentItemKey.ComponentDatas.Clear();
            }

            // 确保selectedOfGameObject已初始化
            if (componentItemKey.SelectedOfGameObject == null)
            {
                componentItemKey.SelectedOfGameObject = new List<string>();
            }
            else
            {
                componentItemKey.SelectedOfGameObject.Clear();
            }

            // 添加新选择的组件
            foreach (var entry in selectedComponents)
            {
                GameObject go = entry.Key;
                List<Component> components = entry.Value;

                bool gameObjectAddedToSelectedList = false;
                foreach (var component in components)
                {
                    if (component == null) continue;

                    // 使用 组件类型_游戏对象名 作为键
                    string gameObjectName = UIBindCodeGenerator.SanitizeVariableName(go.name);
                    string componentType = component.GetType().Name;
                    string key = $"{componentType}_{gameObjectName}";

                    componentItemKey.Dic[key] = component;

                    ComponentData componentData = new ComponentData
                    {
                        Key = key,
                        Type = component.GetType().FullName,
                        Value = component
                    };

                    componentItemKey.ComponentDatas.Add(componentData);

                    // 记录包含此组件的游戏对象名称，用于ComponentSelectWindow的预选中
                    if (!gameObjectAddedToSelectedList && !componentItemKey.SelectedOfGameObject.Contains(go.name))
                    {
                        componentItemKey.SelectedOfGameObject.Add(go.name);
                        gameObjectAddedToSelectedList = true;
                    }
                }
            }

            // 强制触发序列化
            EditorUtility.SetDirty(componentItemKey);

            // 如果在预制体编辑模式，则标记为已修改
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(componentItemKey);
            }
        }

        /// <summary>
        /// 从预制体的ComponentItemKey中加载组件选择状态
        /// </summary>
        public static void LoadComponentSelections()
        {
            if (UIBindData.CurrentPrefab == null)
                return;

            // 清空当前UIBindManager中的选择，确保正确的状态
            UIBindManager.ClearAllSelections();

            ComponentItemKey componentItemKey = UIBindData.CurrentPrefab.GetComponent<ComponentItemKey>();
            if (componentItemKey == null)
            {
                // 如果没有ComponentItemKey组件，记录日志但不抛出异常
                Debug.Log($"【UI绑定工具】预制体 {UIBindData.CurrentPrefab.name} 没有ComponentItemKey组件，无法加载组件选择状态");
                return;
            }

            // 从componentDatas加载所有绑定的组件
            if (componentItemKey.ComponentDatas != null && componentItemKey.ComponentDatas.Count > 0)
            {
                //Debug.Log($"从ComponentItemKey加载了{componentItemKey.ComponentDatas.Count}个组件数据");

                // 遍历所有保存的组件数据
                foreach (ComponentData componentData in componentItemKey.ComponentDatas)
                {
                    if (componentData.Value == null) continue;

                    try
                    {
                        // 检查是否是一个有效的组件实例
                        Component comp = componentData.Value as Component;
                        if (comp != null)
                        {
                            GameObject go = comp.gameObject;
                            if (go != null)
                            {
                                // 将这个组件添加到UIBindManager的选择中
                                UIBindManager.SelectComponent(go, comp);
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // 捕获任何异常，防止因一个组件的错误导致整个工具无法使用
                        Debug.LogError($"【UI绑定工具】加载组件时出错: {ex.Message}");
                    }
                }
            }

            // 刷新层级窗口，使组件选择状态显示出来
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.prefabContentsRoot == UIBindData.CurrentPrefab)
            {
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        /// <summary>
        /// 检查当前打开或选择的预制体
        /// 如果预制体变化，会更新相关状态并加载组件选择
        /// </summary>
        /// <returns>预制体是否发生变化</returns>
        public static bool CheckCurrentPrefab()
        {
            bool prefabChanged = false;

            // 检查是否在预制体编辑模式
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.prefabContentsRoot != null)
            {
                GameObject prefabRoot = prefabStage.prefabContentsRoot;
                if (UIBindData.CurrentPrefab != prefabRoot)
                {
                    UIBindData.CurrentPrefab = prefabRoot;
                    prefabChanged = true;
                    HierarchyExtension.SetBindingMode(true);
                }
            }

            // 如果预制体发生变化，更新相关状态
            if (prefabChanged)
            {
                UIBindManager.SetCurrentPrefab(UIBindData.CurrentPrefab);

                // 加载预制体的ComponentItemKey数据
                LoadComponentSelections();
                UIBindData.ViewName = UIBindData.CurrentPrefab.name + "View";

                // 通过查找已存在的同名脚本来推断模块名
                string viewName = UIBindData.ViewName;
                string viewScriptPath = FindViewScriptPath(viewName);

                if (!string.IsNullOrEmpty(viewScriptPath))
                {
                    // 从脚本路径提取模块名
                    string relativePath = viewScriptPath.Replace(UIBindData.OutputPath, "");
                    string[] pathParts = relativePath.Split('/');

                    if (pathParts.Length >= 1)
                    {
                        UIBindData.ModuleName = pathParts[0];
                    }
                }
            }

            return prefabChanged;
        }

        /// <summary>
        /// 扫描所有UI视图，填充uiModules字典
        /// </summary>
        public static void ScanUIViews()
        {
            UIBindData.UIModules.Clear();

            // 扫描所有的View脚本
            string[] viewScriptGuids =
                AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts/HotUpdate/Module" });

            foreach (string guid in viewScriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // 检查是否是View脚本
                if (fileName.EndsWith("View") && !fileName.EndsWith("ViewComponent"))
                {
                    // 提取模块名和视图名
                    string relativePath = path.Replace(UIBindData.OutputPath, "");
                    string[] pathParts = relativePath.Split('/');

                    if (pathParts.Length >= 2)
                    {
                        string moduleName = pathParts[0];
                        string viewBaseName = fileName.Replace("View", "");

                        // 确保模块数据存在
                        if (!UIBindData.UIModules.ContainsKey(moduleName))
                        {
                            UIBindData.UIModules[moduleName] = new UIBindData.UIModuleData { ModuleName = moduleName };
                        }

                        // 寻找对应的ViewComponent脚本
                        string viewComponentPath = path.Replace($"{fileName}.cs", $"{fileName}Component.cs");
                        bool isAutoGenerated = false;

                        if (File.Exists(viewComponentPath))
                        {
                            // 检查是否包含自动生成标记
                            string content = File.ReadAllText(viewComponentPath);
                            isAutoGenerated = content.Contains("<auto-generated>") ||
                                              content.Contains("// <auto-generated>");
                        }

                        // 尝试查找预制体
                        string prefabPath = FindPrefabPathByName(viewBaseName);

                        // 获取最后修改时间
                        DateTime lastModified = File.Exists(viewComponentPath)
                            ? File.GetLastWriteTime(viewComponentPath)
                            : File.GetLastWriteTime(path);

                        // 创建视图数据
                        UIBindData.UIViewData viewData = new UIBindData.UIViewData
                        {
                            ViewName = fileName,
                            ViewScriptPath = path,
                            ViewComponentPath = viewComponentPath,
                            PrefabPath = prefabPath,
                            IsAutoGenerated = isAutoGenerated,
                            LastModifiedTime = lastModified
                        };

                        // 添加到模块数据
                        UIBindData.UIModules[moduleName].Views[viewBaseName] = viewData;
                    }
                }
            }

            // 对模块按名称排序
            var sortedModules = UIBindData.UIModules
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // 清除原始字典并添加排序后的内容
            UIBindData.UIModules.Clear();
            foreach (var kvp in sortedModules)
            {
                UIBindData.UIModules.Add(kvp.Key, kvp.Value);
            }

            // 对每个模块内的视图按名称排序
            foreach (var moduleData in UIBindData.UIModules.Values)
            {
                var sortedViews = moduleData.Views
                    .OrderBy(kvp => kvp.Key)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                moduleData.Views = sortedViews;
            }
        }

        /// <summary>
        /// 通过名称查找预制体路径
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        /// <returns>预制体路径，未找到则返回空字符串</returns>
        public static string FindPrefabPathByName(string prefabName)
        {
            // 尝试在所有资源中查找预制体
            string[] prefabGuids = AssetDatabase.FindAssets($"t:Prefab {prefabName}");

            foreach (string prefabGuid in prefabGuids)
            {
                string tempPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                string foundPrefabName = Path.GetFileNameWithoutExtension(tempPath);

                if (string.Equals(foundPrefabName, prefabName, StringComparison.OrdinalIgnoreCase))
                {
                    return tempPath;
                }
            }

            return "";
        }

        /// <summary>
        /// 为视图查找对应的预制体
        /// </summary>
        /// <param name="viewData">视图数据</param>
        /// <returns>是否找到预制体</returns>
        public static bool FindPrefabForView(UIBindData.UIViewData viewData)
        {
            string viewBaseName = viewData.ViewName.Replace("View", "");
            string prefabPath = FindPrefabPathByName(viewBaseName);

            if (!string.IsNullOrEmpty(prefabPath))
            {
                viewData.PrefabPath = prefabPath;
                EditorUtility.DisplayDialog("成功", $"为 {viewData.ViewName} 找到预制体: {Path.GetFileName(prefabPath)}", "确定");
                return true;
            }
            else
            {
                // 打开文件选择对话框让用户手动选择预制体
                string selectedPath = EditorUtility.OpenFilePanel("选择预制体", "Assets", "prefab");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 将完整路径转换为项目相对路径
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        viewData.PrefabPath = selectedPath;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 生成绑定代码
        /// 包括验证数据、更新组件、保存预制体和生成代码文件等步骤
        /// </summary>
        /// <returns>是否成功生成代码</returns>
        public static bool GenerateBindingCode()
        {
            // 验证视图名
            if (string.IsNullOrEmpty(UIBindData.ViewName))
            {
                if (UIBindData.CurrentPrefab != null)
                    UIBindData.ViewName = UIBindData.CurrentPrefab.name + "View";
                else
                {
                    EditorUtility.DisplayDialog("错误", "请输入视图名或选择一个预制体", "确定");
                    return false;
                }
            }

            //验证模块名
            if (string.IsNullOrEmpty(UIBindData.ModuleName))
            {
                EditorUtility.DisplayDialog("错误", "请输入模块名再进行生成", "确定");
                return false;
            }

            // 验证是否选择了组件
            var selectedComponents = UIBindManager.GetSelectedComponents();
            if (selectedComponents.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择需要绑定的组件", "确定");
                return false;
            }

            // 保存当前预制体的路径和引用，确保后续能找到它
            GameObject prefabReference = UIBindData.CurrentPrefab;

            // 在预制体编辑模式下，从prefabStage获取路径
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
            {
                EditorUtility.DisplayDialog("错误", "请在预制体编辑模式下使用此功能", "确定");
                return false;
            }

            string prefabPath = prefabStage.assetPath;

            // 显示进度对话框
            EditorUtility.DisplayProgressBar("UI绑定工具", "正在处理UI绑定...", 0.1f);
            Debug.Log("【UI绑定工具】开始生成绑定代码，预制体: " + UIBindData.CurrentPrefab.name);

            try
            {
                // 第一步：检查和添加ComponentItemKey组件
                bool needComponentItemKey = UIBindData.CurrentPrefab.GetComponent<ComponentItemKey>() == null;

                if (needComponentItemKey)
                {
                    if (EditorUtility.DisplayDialog("添加组件",
                            "预制体需要ComponentItemKey组件才能正常工作，是否自动添加？",
                            "添加", "取消"))
                    {
                        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                        {
                            // 如果是在预制体编辑模式
                            Undo.AddComponent<ComponentItemKey>(UIBindData.CurrentPrefab);
                            Debug.Log("【UI绑定工具】向预制体添加了ComponentItemKey组件");
                        }
                        else
                        {
                            // 如果是预制体资源
                            string assetPath = AssetDatabase.GetAssetPath(UIBindData.CurrentPrefab);
                            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                            prefabRoot.AddComponent<ComponentItemKey>();
                            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                            PrefabUtility.UnloadPrefabContents(prefabRoot);

                            // 重新加载预制体
                            UIBindData.CurrentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            UIBindManager.SetCurrentPrefab(UIBindData.CurrentPrefab);
                            Debug.Log("【UI绑定工具】向预制体添加了ComponentItemKey组件并重新加载");
                        }
                    }
                    else
                    {
                        EditorUtility.ClearProgressBar();
                        return false;
                    }
                }

                // 第二步：更新预制体的 ComponentItemKey 组件数据
                EditorUtility.DisplayProgressBar("UI绑定工具", "正在更新组件数据...", 0.3f);
                UpdateComponentItemKeyData(UIBindData.CurrentPrefab, selectedComponents);
                Debug.Log("【UI绑定工具】已更新预制体组件数据");

                // 第三步：保存预制体
                EditorUtility.DisplayProgressBar("UI绑定工具", "正在保存预制体...", 0.5f);
                SavePrefab();
                Debug.Log("【UI绑定工具】已保存预制体");
                isCompileAndAdd = true;

                // 第四步：生成代码
                EditorUtility.DisplayProgressBar("UI绑定工具", "正在生成绑定代码...", 0.7f);
                UIBindCodeGenerator.GenerateCode(
                    UIBindData.CurrentPrefab,
                    UIBindData.NamespaceName,
                    UIBindData.ModuleName,
                    UIBindData.ViewName,
                    UIBindData.OutputPath,
                    selectedComponents
                );
                Debug.Log("【UI绑定工具】已生成绑定代码");

                // 再次保存预制体
                SavePrefab();
                // 确保UI绑定模式重新激活
                HierarchyExtension.SetBindingMode(true);

                // 再次确认预制体引用和保存状态
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    // 如果预制体引用丢失，尝试重新获取
                    if (UIBindData.CurrentPrefab == null)
                    {
                        UIBindData.CurrentPrefab = prefabReference;
                        if (UIBindData.CurrentPrefab == null)
                        {
                            UIBindData.CurrentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                            UIBindManager.SetCurrentPrefab(UIBindData.CurrentPrefab);
                        }
                    }

                    Debug.Log("【UI绑定工具】已确认预制体保存状态: " + prefabPath);
                }

                // 强制重新绘制所有视图
                SceneView.RepaintAll();
                EditorApplication.RepaintHierarchyWindow();
                EditorWindow.GetWindow<UIBindIntegratedWindow>().Repaint();

                // 刷新UI管理器视图
                ScanUIViews();

                Debug.Log("【UI绑定工具】流程完成: 已刷新资源和视图。");
                Debug.Log("UI绑定代码已成功生成");
                SimulateCtrlS();

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("【UI绑定工具】生成绑定代码时发生错误: " + ex.Message);
                EditorUtility.DisplayDialog("错误", "生成绑定代码失败: " + ex.Message, "确定");
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void AddView()
        {
            // 自动挂载View脚本到预制体
            string viewClassName = UIBindData.ViewName;
            string viewNamespace = UIBindData.NamespaceName;
            string fullClassName = string.IsNullOrEmpty(viewNamespace)
                ? viewClassName
                : $"{viewNamespace}.{viewClassName}";

            // 给预制体添加View脚本组件
            var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject prefabRoot = currentPrefabStage.prefabContentsRoot;
            // 检查是否已经添加了View组件
            Component existingView = null;
            Component[] components = prefabRoot.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component != null && component.GetType().ToString() == fullClassName)
                {
                    existingView = component;
                    break;
                }
            }

            if (existingView == null)
            {
                Debug.Log($"【UI绑定工具】编译完成后将尝试添加组件，当前正在等待编译...");
                // 尝试查找并添加组件
                string scriptPath =
                    $"{UIBindData.OutputPath}{(!string.IsNullOrEmpty(UIBindData.ModuleName) ? UIBindData.ModuleName + "/" : "")}{viewClassName.Replace("View", "")}/View/{viewClassName}.cs";
                Debug.Log($"【UI绑定工具】将在编译完成后查找脚本: {scriptPath}");
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (script != null)
                {
                    System.Type scriptType = script.GetClass();
                    if (scriptType != null)
                    {
                        // 确保预制体根对象引用依然有效
                        var stage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (stage != null && stage.prefabContentsRoot != null)
                        {
                            Debug.Log($"【UI绑定工具】找到脚本类型: {scriptType.FullName}，编译完成后将添加组件");
                            stage.prefabContentsRoot.AddComponent(scriptType);
                            // 再次保存预制体
                            SavePrefab();
                            SimulateCtrlS();
                            Debug.LogWarning($"【UI绑定工具】已自动添加 {viewClassName} 组件到预制体，此操作发生在编译完成后");
                        }
                    }
                    else
                    {
                        Debug.LogError($"【UI绑定工具】无法获取脚本类型: {viewClassName}，请稍后手动添加组件");
                    }
                }
            }
            else
            {
                Debug.Log($"【UI绑定工具】预制体已存在 {viewClassName} 组件，无需重复添加");
                Debug.Log("编译完成");
            }
        }

        /// <summary>
        /// 通过视图名查找对应的视图脚本路径
        /// </summary>
        /// <param name="viewName">视图名称，如"LoginView"</param>
        /// <returns>视图脚本路径，未找到则返回空字符串</returns>
        public static string FindViewScriptPath(string viewName)
        {
            // 在项目中查找对应名称的脚本文件
            string[] scriptGuids = AssetDatabase.FindAssets($"t:Script {viewName}");

            foreach (string guid in scriptGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // 检查文件名是否完全匹配
                if (string.Equals(fileName, viewName, StringComparison.OrdinalIgnoreCase))
                {
                    // 检查是否在HotUpdate/Module目录下
                    if (path.Contains(UIBindData.OutputPath) && !path.Contains("Component"))
                    {
                        return path;
                    }
                }
            }

            return "";
        }

        public static void SimulateCtrlS()
        {
#if UNITY_EDITOR_WIN
            SimulateCtrlAndKey(KEY_S);
#else
        Debug.LogWarning("当前平台不支持模拟快捷键操作（仅限 Windows）");
#endif
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private const int KEYEVENTF_KEYDOWN = 0x0;
        private const int KEYEVENTF_KEYUP = 0x2;
        private const byte VK_CONTROL = 0x11;
        private const byte KEY_S = 0x53;

        /// <summary>
        /// /// 模拟按下 Ctrl + 指定按键（仅限 Windows）
        /// </summary>
        /// <param name="key">需要配合 Ctrl 的按键（例如 KEY_S）</param>
        public static void SimulateCtrlAndKey(byte key)
        {
            // 模拟按下 Ctrl + Key
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0); // 按下 Ctrl
            keybd_event(key, 0, KEYEVENTF_KEYDOWN, 0); // 按下指定键
            keybd_event(key, 0, KEYEVENTF_KEYUP, 0); // 释放指定键
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0); // 释放 Ctrl
        }
#endif


        /// <summary>
        /// 注册编译完成事件，并在编译完成后执行指定操作
        /// </summary>
        /// <param name="action">编译完成后要执行的操作</param>
        public static void ExecuteAfterCompilation(Action action)
        {
            // 保存重要状态，用于编译后恢复
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                savedPrefabPath = prefabStage.assetPath;
                savedViewName = UIBindData.ViewName;
                savedModuleName = UIBindData.ModuleName;
                Debug.Log($"【UI绑定工具】保存编译前状态 - 预制体路径:{savedPrefabPath}, 视图名:{savedViewName}, 模块名:{savedModuleName}");
            }
            // 如果不是在编译中，直接执行
            if (!EditorApplication.isCompiling)
            {
                Debug.Log("【UI绑定工具】当前不在编译中，直接执行操作");
                action?.Invoke();
                isCompileAndAdd = false;
            }
        }
    }
    public class UnityScriptCompiling:AssetPostprocessor
    {
        [UnityEditor.Callbacks.DidReloadScripts]
        static void AllScriptsReloaded()
        {
            
            if (UIBindUtility.isCompileAndAdd)
            {
                UIBindUtility.ExecuteAfterCompilation(UIBindUtility.AddView);
            }
        }
    }
}
