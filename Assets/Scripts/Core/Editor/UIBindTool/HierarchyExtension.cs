using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

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

        /// <summary>
        /// 类构造函数，在Unity编辑器加载时调用
        /// </summary>
        static HierarchyExtension()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyChanged += CheckPrefabMode;
            // 注册编辑器事件，确保在场景变更时清理缓存
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
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

            // 获取已选组件，用于显示状态
            bool hasSelectedComponents = UIBindManager.IsGameObjectSelected(go);
            List<Component> selectedComps = hasSelectedComponents ? UIBindManager.GetSelectedComponents()[go] : new List<Component>();

            bool isSelected = selectedComps.Count > 0;
            
            // 使用instanceID作为字典键
            int goID = go.GetInstanceID();
            if (!IsMixedModeEnabled.ContainsKey(goID))
            {
                IsMixedModeEnabled[goID] = selectedComps.Count > 1;
            }
            
            // 创建组件选择按钮区域
            Rect toggleRect = new Rect(selectionRect.xMax - 20, selectionRect.y, 20, selectionRect.height);
            
            // 创建标签显示已选组件信息
            if (isSelected)
            {
                DrawComponentLabel(toggleRect, selectionRect, selectedComps, go);
            }
            
            // 显示勾选框 - 只用于开启/关闭自动绑定状态
            bool newState = EditorGUI.Toggle(toggleRect, isSelected, toggleStyle);
            
            if (newState != isSelected)
            {
                HandleToggleStateChange(newState, go, goID);
            }
        }

        /// <summary>
        /// 检查是否应该为该游戏对象显示UI绑定控件
        /// </summary>
        private static bool ShouldShowUI(GameObject go)
        {
            if (!isInPrefabMode) return false;
            
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || prefabStage.prefabContentsRoot == null) return false;
            
            // 检查当前游戏对象是否是预制体的一部分
            Transform current = go.transform;
            while (current != null)
            {
                // 如果当前对象或其父对象是预制体根节点，且该预制体是当前在UI绑定工具中选择的预制体
                if (current.gameObject == prefabStage.prefabContentsRoot && 
                    (isBindingMode || current.gameObject == UIBindManager.GetCurrentPrefab()))
                {
                    // 如果是预制体根节点但没有ComponentItemKey组件，只在绑定模式下显示
                    if (current.gameObject == prefabStage.prefabContentsRoot &&
                        current.gameObject.GetComponent<ComponentItemKey>() == null &&
                        !isBindingMode)
                    {
                        return false;
                    }
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
        private static void DrawComponentLabel(Rect toggleRect, Rect selectionRect, List<Component> selectedComps, GameObject go)
        {
            string labelText = selectedComps.Count == 1 
                ? selectedComps[0].GetType().Name 
                : "Mixed";
                
            float labelWidth = EditorStyles.miniLabel.CalcSize(new GUIContent(labelText)).x;
            Rect labelRect = new Rect(toggleRect.x - labelWidth - 2, selectionRect.y, labelWidth, selectionRect.height);
            
            // 点击组件名称弹出下拉菜单
            if (GUI.Button(labelRect, labelText, EditorStyles.miniLabel))
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
    }
}
