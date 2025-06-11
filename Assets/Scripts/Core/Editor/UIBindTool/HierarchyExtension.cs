using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using System.Text;
using UnityEngine.UI;
using System.IO;

namespace Core.Editor.UIBindTool
{
    /// <summary>
    /// Unity层级窗口扩展，用于在Hierarchy视图中提供UI组件绑定功能
    /// </summary>
    [InitializeOnLoad]
    public class HierarchyExtension
    {
        private static GUIStyle toggleStyle;
        private static bool isInPrefabMode = false;
        private static bool isBindingMode = false;
        
        // 使用实例ID作为键，以避免GameObject引用问题
        private static readonly Dictionary<int, bool> IsMixedModeEnabled = new Dictionary<int, bool>();
        
        // 缓存常用GUIContent以提高性能
        private static readonly GUIContent MixedModeContent = new GUIContent("Mixed(多选模式)");
        private static readonly GUIContent SelectAllContent = new GUIContent("全选");
        private static readonly GUIContent DeselectAllContent = new GUIContent("取消全选");

        // UI规范检查相关配置
        private static string atlasPathBase = "Assets/GameRes/SpriteAtlas";
        private static string closeBtnImage = "UI_common_windos_close_bg|UI_common_windos_btn_bg";
        private static List<string> closeBtnList = new List<string>();
        private static Vector2 closeBtnPosV2 = new Vector2(25f, 25f);
        private static int prefabSizeLimit = 300; // KB
        
        // 规范检查缓存
        private static Dictionary<int, long> objectSizes = new Dictionary<int, long>();
        private static Dictionary<int, string> objectPaths = new Dictionary<int, string>();
        
        // 浮点数比较容差
        private const float FloatComparisonEpsilon = 0.0001f;
        
        /// <summary>
        /// 类构造函数，在Unity编辑器加载时调用
        /// </summary>
        static HierarchyExtension()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyChanged += CheckPrefabMode;
            // 注册编辑器事件，确保在场景变更时清理缓存
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // 初始化UI规范检查配置
            InitUIStandardizationConfig();
        }
        
        /// <summary>
        /// 初始化UI规范检查配置
        /// </summary>
        private static void InitUIStandardizationConfig()
        {
            string savedAtlasPath = PlayerPrefs.GetString("AtlasPathBase");
            if (!string.IsNullOrEmpty(savedAtlasPath))
                atlasPathBase = savedAtlasPath;
                
            string savedCloseBtnImage = PlayerPrefs.GetString("CloseBtnName");
            if (!string.IsNullOrEmpty(savedCloseBtnImage))
                closeBtnImage = savedCloseBtnImage;
            
            closeBtnList.Clear();
            closeBtnList.AddRange(closeBtnImage.Split('|'));
            
            string savedCloseBtnPos = PlayerPrefs.GetString("CloseBtnPos");
            if (!string.IsNullOrEmpty(savedCloseBtnPos))
            {
                string[] posValues = savedCloseBtnPos.Split('|');
                if (posValues.Length >= 2)
                {
                    if (int.TryParse(posValues[0], out var x) && int.TryParse(posValues[1], out var y))
                    {
                        closeBtnPosV2 = new Vector2(x, y);
                    }
                }
            }
        }

        /// <summary>
        /// 当Unity播放模式状态改变时清理缓存
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 在进入或退出PlayMode时清理缓存
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                IsMixedModeEnabled.Clear();
                objectSizes.Clear();
                objectPaths.Clear();
            }
        }

        /// <summary>
        /// 检查当前是否处于预制体编辑模式
        /// </summary>
        private static void CheckPrefabMode()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            isInPrefabMode = (stage != null);
            
            // 当进入预制体编辑模式时，自动激活绑定模式
            if (isInPrefabMode && stage != null && stage.prefabContentsRoot == UIBindManager.GetCurrentPrefab())
            {
                isBindingMode = true;
            }
            else
            {
                UIBindUtility.isCompileAndAdd = false;
            }
        }
        
        /// <summary>
        /// 设置绑定模式状态
        /// </summary>
        /// <param name="binding">是否启用绑定模式</param>
        public static void SetBindingMode(bool binding)
        {
            if (isBindingMode != binding)
            {
                isBindingMode = binding;
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        /// <summary>
        /// 层级窗口项GUI绘制回调
        /// </summary>
        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;
            if (!ShouldShowUI(go)) return;
            InitStyles();
            bool hasSelectedComponents = UIBindManager.IsGameObjectSelected(go);
            List<Component> selectedComps = hasSelectedComponents ? UIBindManager.GetSelectedComponents()[go] : new List<Component>();
            bool isSelected = selectedComps.Count > 0;
            int goID = go.GetInstanceID();
            if (!IsMixedModeEnabled.ContainsKey(goID))
                IsMixedModeEnabled[goID] = selectedComps.Count > 1;

            // 动态布局参数
            float padding = 0f; // 选择按钮紧贴最右
            float iconWidth = 20f;
            float toggleWidth = 20f;
            float minButtonWidth = 60f;
            float maxButtonWidth = 120f;
            // 固定选择按钮宽度为8个字符宽度
            float charWidth = EditorStyles.label.CalcSize(new GUIContent("W")).x;
            float buttonWidth = charWidth * 8f;
            if (isSelected)
            {
                string labelText = selectedComps.Count == 1 ? selectedComps[0].GetType().Name : "Mixed";
                buttonWidth = Mathf.Clamp(EditorStyles.label.CalcSize(new GUIContent(labelText)).x + 20f, minButtonWidth, maxButtonWidth);
            }

            // 先计算警告/错误图标数量
            int warningCount = 0, errorCount = 0;
            StringBuilder warningsBuilder = new StringBuilder();
            StringBuilder errorsBuilder = new StringBuilder();
            CheckUIStandardization(go, warningsBuilder, errorsBuilder, ref warningCount, ref errorCount);

            // 右对齐布局：选择按钮（最右）- 开关 - 警告/错误图标（自适应）
            float xRight = selectionRect.xMax;
            Rect buttonRect = new Rect();
            Rect toggleRect;
            List<Rect> iconRects = new List<Rect>();

            if (isSelected)
            {
                // 选择按钮在最右
                buttonRect = new Rect(xRight - buttonWidth, selectionRect.y, buttonWidth, selectionRect.height);
                // 开关在按钮左侧
                toggleRect = new Rect(buttonRect.x - toggleWidth, selectionRect.y, toggleWidth, selectionRect.height);
            }
            else
            {
                // 没有选择按钮，开关在最右
                toggleRect = new Rect(xRight - toggleWidth, selectionRect.y, toggleWidth, selectionRect.height);
            }

            // 图标自适应，紧贴开关左侧
            float iconStartX = toggleRect.x - iconWidth;
            if (errorCount > 0)
            {
                iconRects.Add(new Rect(iconStartX, selectionRect.y, iconWidth, selectionRect.height));
                iconStartX -= iconWidth;
            }
            if (warningCount > 0)
            {
                iconRects.Add(new Rect(iconStartX, selectionRect.y, iconWidth, selectionRect.height));
            }

            // 绘制警告/错误图标（顺序：左警告，右错误）
            int iconIdx = iconRects.Count - 1;
            if (warningCount > 0 && iconIdx >= 0)
            {
                GUIContent warningContent = new GUIContent(
                    EditorGUIUtility.FindTexture("d_console.warnicon"), 
                    warningsBuilder.ToString()
                );
                EditorGUI.LabelField(iconRects[iconIdx--], warningContent);
            }
            if (errorCount > 0 && iconIdx >= 0)
            {
                GUIContent errorContent = new GUIContent(
                    EditorGUIUtility.FindTexture("console.erroricon"), 
                    errorsBuilder.ToString()
                );
                EditorGUI.LabelField(iconRects[iconIdx], errorContent);
            }

            // 开关
            bool newState = EditorGUI.Toggle(toggleRect, isSelected, toggleStyle);
            if (newState != isSelected)
                HandleToggleStateChange(newState, go, goID);
            // 选择按钮（最右）
            if (isSelected)
                DrawComponentLabel(buttonRect, selectedComps, go);
        }

        /// <summary>
        /// 检查是否应该为该游戏对象显示UI绑定控件
        /// </summary>
        private static bool ShouldShowUI(GameObject go)
        {
            // 仅在预制体编辑模式下显示
            if (!isInPrefabMode) return false;

            // 获取当前预制体编辑阶段
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.prefabContentsRoot == null) return false;

            // 检查当前游戏对象是否属于当前预制体
            Transform current = go.transform;
            while (current != null)
            {
                // 判断是否为预制体根节点且为当前UI绑定工具选择的预制体
                bool isRoot = current.gameObject == prefabStage.prefabContentsRoot;
                bool isCurrentPrefab = isBindingMode || current.gameObject == UIBindManager.GetCurrentPrefab();
                if (isRoot && isCurrentPrefab)
                {
                    // 仅在绑定模式下显示无ComponentItemKey组件的根节点
                    bool hasComponentItemKey = current.gameObject.GetComponent<ComponentItemKey>() != null;
                    if ( !hasComponentItemKey && !isBindingMode)
                        return false;
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        /// <summary>
        /// 初始化GUI样式
        /// </summary>
        private static void InitStyles()
        {
            toggleStyle ??= new GUIStyle(GUI.skin.toggle)
            {
                margin = new RectOffset(0, 0, 0, 0)
            };
        }
        
        /// <summary>
        /// 绘制组件标签
        /// </summary>
        private static void DrawComponentLabel(Rect buttonRect, List<Component> selectedComps, GameObject go)
        {
            string labelText = selectedComps.Count == 1 
                ? selectedComps[0].GetType().Name 
                : "Mixed";

            // 固定宽度为8个字符宽
            float charWidth = EditorStyles.label.CalcSize(new GUIContent("W")).x;
            float labelWidth = charWidth * 8f;
            Rect labelRect = new Rect(buttonRect.x, buttonRect.y, labelWidth, buttonRect.height);

            // 点击组件名称弹出下拉菜单
            if (GUI.Button(labelRect, labelText, EditorStyles.label))
            {
                ShowComponentSelectionMenu(go, labelRect);
            }
        }

        /// <summary>
        /// 处理组件选择状态变化
        /// </summary>
        private static void HandleToggleStateChange(bool newState, GameObject go, int goID)
        {
            if (newState)
            {
                // 如果勾选了复选框，自动选择第一个组件
                AutoSelectFirstComponent(go);
            }
            else
            {
                // 取消选择
                UIBindManager.UnselectGameObject(go);
                IsMixedModeEnabled.Remove(goID);
            }
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// 自动选择游戏对象上的第一个组件
        /// </summary>
        private static void AutoSelectFirstComponent(GameObject go)
        {
            // 获取游戏对象上的所有组件
            Component[] components = go.GetComponents<Component>();
            if (components.Length == 0) return;
            
            // 优先选择除Transform以外的第一个组件
            Component firstComponent = components.Length > 1 ? components[1] : components[0];
            
            // 清除之前的选择，然后选择第一个组件
            UIBindManager.ClearComponentSelection(go);
            UIBindManager.SelectComponent(go, firstComponent);
            
            // 新选择时默认不启用Mixed模式
            IsMixedModeEnabled[go.GetInstanceID()] = false;
            
            EditorApplication.RepaintHierarchyWindow();
        }

        /// <summary>
        /// 显示组件选择菜单
        /// </summary>
        private static void ShowComponentSelectionMenu(GameObject go, Rect position)
        {
            // 获取游戏对象上的所有组件
            Component[] components = go.GetComponents<Component>();
            if (components.Length == 0) return;

            // 创建菜单
            GenericMenu menu = new GenericMenu();
            
            // 获取当前已选择的组件
            List<Component> selectedComps = UIBindManager.IsGameObjectSelected(go) 
                ? UIBindManager.GetSelectedComponents()[go] 
                : new List<Component>();
            
            int goID = go.GetInstanceID();
            if (!IsMixedModeEnabled.ContainsKey(goID))
            {
                IsMixedModeEnabled[goID] = selectedComps.Count > 1;
            }
            
            bool isMixedMode = IsMixedModeEnabled[goID];
            
            // 添加Mixed模式选项（多选模式开关）
            AddMixedModeMenuItem(menu, go, goID, selectedComps, isMixedMode);
            
            menu.AddSeparator("");
            
            // 如果启用了Mixed模式，添加全选和取消全选选项
            if (isMixedMode)
            {
                AddBatchSelectionMenuItems(menu, go, components);
            }
            
            // 添加所有组件选项
            AddComponentMenuItems(menu, go, components, selectedComps, goID);

            // 在点击位置显示菜单
            menu.DropDown(position);
        }
        
        /// <summary>
        /// 添加混合模式菜单项
        /// </summary>
        private static void AddMixedModeMenuItem(GenericMenu menu, GameObject go, int goID, List<Component> selectedComps, bool isMixedMode)
        {
            menu.AddItem(MixedModeContent, isMixedMode, () => {
                // 切换Mixed模式
                IsMixedModeEnabled[goID] = !isMixedMode;
                
                // 如果取消Mixed模式，但有多个选择，则仅保留第一个
                if (!IsMixedModeEnabled[goID] && selectedComps.Count > 1)
                {
                    Component firstSelected = selectedComps[0];
                    UIBindManager.ClearComponentSelection(go);
                    UIBindManager.SelectComponent(go, firstSelected);
                }
                
                EditorApplication.RepaintHierarchyWindow();
            });
        }
        
        /// <summary>
        /// 添加批量选择菜单项
        /// </summary>
        private static void AddBatchSelectionMenuItems(GenericMenu menu, GameObject go, Component[] components)
        {
            menu.AddItem(SelectAllContent, false, () => {
                // 选择所有组件
                UIBindManager.ClearComponentSelection(go);
                foreach (Component comp in components)
                {
                    if (comp != null)
                        UIBindManager.SelectComponent(go, comp);
                }
                EditorApplication.RepaintHierarchyWindow();
            });
            
            menu.AddItem(DeselectAllContent, false, () => {
                // 清除所有选择
                UIBindManager.ClearComponentSelection(go);
                EditorApplication.RepaintHierarchyWindow();
            });
            
            menu.AddSeparator("");
        }
        
        /// <summary>
        /// 添加组件菜单项
        /// </summary>
        private static void AddComponentMenuItems(GenericMenu menu, GameObject go, Component[] components, 
            List<Component> selectedComps, int goID)
        {
            foreach (Component component in components)
            {
                if (component == null) continue;

                // 获取组件名称
                string componentName = component.GetType().Name;
                
                // 检查当前组件是否已被选择
                bool isSelected = selectedComps.Contains(component);
                
                // 使用局部变量捕获以避免闭包问题
                Component capturedComponent = component;
                bool capturedIsSelected = isSelected;
                
                menu.AddItem(new GUIContent(componentName), isSelected, () => {
                    bool isMixedMode = IsMixedModeEnabled[goID];
                    // 根据当前是否为Mixed模式决定行为
                    if (isMixedMode)
                    {
                        // Mixed模式：切换选择状态
                        if (capturedIsSelected)
                        {
                            UIBindManager.RemoveComponentSelection(go, capturedComponent);
                        }
                        else
                        {
                            UIBindManager.SelectComponent(go, capturedComponent);
                        }
                    }
                    else
                    {
                        // 非Mixed模式：单选
                        UIBindManager.ClearComponentSelection(go);
                        UIBindManager.SelectComponent(go, capturedComponent);
                    }
                    EditorApplication.RepaintHierarchyWindow();
                });
            }
        }
        /// <summary>
        /// 检查游戏对象的UI规范
        /// </summary>
        private static void CheckUIStandardization(
            GameObject go, 
            StringBuilder warningsBuilder, 
            StringBuilder errorsBuilder, 
            ref int warningCount, 
            ref int errorCount)
        {
            // 检查对象名称规范
            CheckNameConvention(go, warningsBuilder, errorsBuilder, ref warningCount, ref errorCount);
            // 检查图像组件规范
            Image image = go.GetComponent<Image>();
            if (image != null)
            {
                CheckImageConvention(go, image, warningsBuilder, errorsBuilder, ref warningCount, ref errorCount);
            }
            // 检查文本组件规范
            Text text = go.GetComponent<Text>();
            if (text != null)
            {
                CheckTextConvention(go, text, errorsBuilder, ref errorCount);
            }
            // 检查RectTransform规范
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                CheckRectTransformConvention(rectTransform, errorsBuilder, ref errorCount);
            }
            // 检查按钮组件规范
            Button button = go.GetComponent<Button>();
            if (button != null)
            {
                CheckButtonConvention(button, warningsBuilder, errorsBuilder, ref warningCount, ref errorCount);
            }
            // 检查遮罩组件规范
            if (go.GetComponent<Mask>() != null)
            {
                warningsBuilder.AppendLine($"{warningCount + 1}.是否使用RectMask2D");
                warningCount++;
            }
            // 检查预制体大小
            CheckPrefabSize(go, errorsBuilder, ref errorCount);
        }

        /// <summary>
        /// 检查对象名称规范
        /// </summary>
        private static void CheckNameConvention(
            GameObject go, 
            StringBuilder warningsBuilder, 
            StringBuilder errorsBuilder, 
            ref int warningCount, 
            ref int errorCount)
        {
            string name = go.name;
            if (name.Contains(" "))
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.命名含空格");
                errorCount++;
            }
            if (IsLowerCase(name[0]))
            {
                warningsBuilder.AppendLine($"{warningCount + 1}.命名首字母小写");
                warningCount++;
            }
            if (IsDigit(name[0]))
            {
                warningsBuilder.AppendLine($"{warningCount + 1}.命名首字符数字");
                warningCount++;
            }
        }

        /// <summary>
        /// 检查图像组件规范
        /// </summary>
        private static void CheckImageConvention(
            GameObject go, 
            Image image, 
            StringBuilder warningsBuilder, 
            StringBuilder errorsBuilder, 
            ref int warningCount, 
            ref int errorCount)
        {
            // 检查空图片
            if (image.sprite == null)
            {
                if (image.color.a != 0)
                {
                    errorsBuilder.AppendLine($"{errorCount + 1}.image为空Alpha不设0会白屏");
                    errorCount++;
                }
                return;
            }

            // 检查默认图片
            string spriteName = image.sprite.name;
            if (spriteName == "Background")
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.image 包含默认Background");
                errorCount++;
            }
            else if (spriteName == "UISprite")
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.image 包含默认UISprite");
                errorCount++;
            }
            else if (spriteName == "UIMask")
            {
                errorsBuilder.AppendLine($"{++errorCount}.image 包含默认UIMask");
            }
            
            // 检查关闭按钮规范
            if (closeBtnList.Contains(spriteName))
            {
                RectTransform rt = image.transform.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (rt.anchorMax != Vector2.one || rt.anchorMin != Vector2.one)
                    {
                        errorsBuilder.AppendLine($"{++errorCount}.close btn锚点不是右上");
                    }
                    else if (!Mathf.Approximately(rt.anchoredPosition3D.x, closeBtnPosV2.x) || !Mathf.Approximately(rt.anchoredPosition3D.y, closeBtnPosV2.y))
                    {
                        errorsBuilder.AppendLine($"{++errorCount}.close btn位置不是右上{closeBtnPosV2}");
                    }
                }
            }
            
            // 检查图集引用
            string assetPath = AssetDatabase.GetAssetPath(image.sprite);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (assetPath.IndexOf(atlasPathBase, StringComparison.Ordinal) != -1)
                {
                    string relativePath = assetPath.Replace(atlasPathBase, "");
                    int slashIndex = relativePath.IndexOf("/", StringComparison.Ordinal);
                    if (slashIndex != -1)
                    {
                        string atlasName = relativePath.Substring(0, slashIndex);
                        string rootName = GetRootGameObjectName(go);
                        string rootNameFirstWordLower = GetRootNameFirstWordLower(rootName);
                        
                        string atlasLower = atlasName.ToLower();
                        if (!atlasLower.Contains("common") && 
                            atlasLower != rootNameFirstWordLower && 
                            !rootNameFirstWordLower.Contains(atlasLower) && 
                            !atlasLower.Contains(rootNameFirstWordLower))
                        {
                            errorsBuilder.AppendLine($"{errorCount + 1}.图片引用了{atlasName}图集资源");
                            errorCount++;
                        }
                    }
                }
                else
                {
                    warningsBuilder.AppendLine($"{warningCount + 1}.引用图片不在图集内");
                    warningCount++;
                }
            }
        }

        /// <summary>
        /// 检查文本组件规范
        /// </summary>
        private static void CheckTextConvention(
            GameObject go, 
            Text text, 
            StringBuilder errorsBuilder, 
            ref int errorCount)
        {
            // 检查字体
            Font font = text.font;
            if (font == null)
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.字体为空");
                errorCount++;
            }
            else if (font.name == "Arial")
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.字体使用了Arial");
                errorCount++;
            }
            
            // 检查非空文本是否使用了语言组件
            if (!string.IsNullOrEmpty(text.text) && go.GetComponent("LanguageComponent") == null)
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.默认文本不为空");
                errorCount++;
            }
        }

        /// <summary>
        /// 检查RectTransform组件规范
        /// </summary>
        private static void CheckRectTransformConvention(RectTransform rectTransform, 
            StringBuilder errorsBuilder, 
            ref int errorCount)
        {
            // 检查坐标是否包含小数点
            Vector3 position = rectTransform.anchoredPosition3D;
            if (HasDecimalPoint(position))
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.Position含小数点");
                errorCount++;
            }
            // 检查z值是否为0（使用容差）
            if (!FloatEquals(position.z, 0f))
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.Pos z值不为零");
                errorCount++;
            }
            // 检查缩放是否规范
            Vector3 scale = rectTransform.localScale;
            if (HasDecimalPoint(scale))
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.localScale含小数点");
                errorCount++;
            }
            if (!FloatEquals(scale.x, 1f) || !FloatEquals(scale.y, 1f) || !FloatEquals(scale.z, 1f))
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.Scale不全为1");
                errorCount++;
            }
        }

        /// <summary>
        /// 检查按钮组件规范
        /// </summary>
        private static void CheckButtonConvention(Button button, 
            StringBuilder warningsBuilder, 
            StringBuilder errorsBuilder, 
            ref int warningCount, 
            ref int errorCount)
        {
            // 检查按钮是否有图像组件
            if (button.image == null)
            {
                warningsBuilder.AppendLine($"{warningCount + 1}.按钮没选择image");
                warningCount++;
                return;
            }
            
            // 检查按钮响应区域是否足够大
            RectTransform rt = button.image.transform.GetComponent<RectTransform>();
            if (rt != null && (rt.rect.width < 40 || rt.rect.height < 40))
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.按钮响应区域过小");
                errorCount++;
            }
        }

        /// <summary>
        /// 检查预制体大小
        /// </summary>
        private static void CheckPrefabSize(
            GameObject go,
            StringBuilder errorsBuilder,
            ref int errorCount)
        {
            // 只对预制体根节点检查大小
            if (!IsPrefabRoot(go)) return;
            
            int goID = go.GetInstanceID();

            // 缓存预制体大小和路径，避免频繁IO操作
            if (!objectSizes.TryGetValue(goID, out var fileSize))
            {
                if (!objectPaths.TryGetValue(goID, out var path))
                {
                    path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    if (string.IsNullOrEmpty(path))
                    {
                        var stage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (stage != null) path = stage.assetPath;
                    }
                    
                    if (!string.IsNullOrEmpty(path))
                    {
                        objectPaths[goID] = path;
                    }
                }
                
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    fileSize = new FileInfo(path).Length;
                    objectSizes[goID] = fileSize;
                }
                else
                {
                    return; // 无法获取文件大小
                }
            }
            
            // 检查预制体大小是否超过限制
            if (fileSize >= prefabSizeLimit * 1024)
            {
                errorsBuilder.AppendLine($"{errorCount + 1}.预制体大小{GetFormatSize(fileSize)},超过{prefabSizeLimit}k,需拆分");
                errorCount++;
            }
        }

        /// <summary>
        /// 获取根游戏对象的名称
        /// </summary>
        private static string GetRootGameObjectName(GameObject go)
        {
            Transform root = go.transform;
            while (root.parent != null)
            {
                root = root.parent;
            }
            return root.name;
        }

        /// <summary>
        /// 获取根对象名称的首个单词（小写）
        /// </summary>
        private static string GetRootNameFirstWordLower(string rootName)
        {
            if (string.IsNullOrEmpty(rootName)) return string.Empty;
            
            StringBuilder result = new StringBuilder();
            
            for (int i = 1; i < rootName.Length && !char.IsUpper(rootName[i]); i++)
            {
                result.Append(rootName[i]);
            }
            
            return result.ToString().ToLower();
        }

        /// <summary>
        /// 检查是否为预制体根节点
        /// </summary>
        private static bool IsPrefabRoot(GameObject go)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null && stage.prefabContentsRoot == go;
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        private static string GetFormatSize(long size)
        {
            if (size > 1048576L) // 1MB = 1024KB = 1048576B
            {
                return ((float)size / 1048576f).ToString("F2") + "m";
            }
            
            if (size > 1024L) // 1KB = 1024B
            {
                return ((float)size / 1024f).ToString("F2") + "k";
            }
            
            return size.ToString();
        }

        /// <summary>
        /// 检查字符是否是小写字母
        /// </summary>
        private static bool IsLowerCase(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        /// <summary>
        /// 检查字符是否是数字
        /// </summary>
        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// 浮点数相等比较（带容差）
        /// </summary>
        private static bool FloatEquals(float a, float b)
        {
            return Mathf.Abs(a - b) < FloatComparisonEpsilon;
        }

        /// <summary>
        /// 检查数值是否包含小数点
        /// </summary>
        private static bool HasDecimalPoint(float value)
        {
            // 优先用容差判断
            return Mathf.Abs(value - Mathf.Round(value)) > FloatComparisonEpsilon;
        }

        /// <summary>
        /// 检查向量是否包含小数点
        /// </summary>
        private static bool HasDecimalPoint(Vector3 vector)
        {
            return HasDecimalPoint(vector.x) || HasDecimalPoint(vector.y) || HasDecimalPoint(vector.z);
        }
    }
}
