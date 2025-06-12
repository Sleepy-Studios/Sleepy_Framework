using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Core.Editor.SceneLoaderEditor
{
    /// <summary>
    /// Unity编辑器工具栏扩展工具
    /// 通过反射获取Unity内部工具栏，并提供添加自定义控件的功能
    /// </summary>
    public static class ToolbarExtension
    {
        // 通过反射获取Unity编辑器工具栏类型
        static Type mToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type mGUIViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");

        // 当前工具栏实例
        static ScriptableObject mCurrentToolbar;

        // 提供给外部注册回调的委托，用于在工具栏左侧和右侧添加控件
        public static Action<VisualElement> ToolbarZoneLeftAlign; // 工具栏左侧区域
        public static Action<VisualElement> ToolbarZoneRightAlign; // 工具栏右侧区域

        // 静态构造函数，在类首次加载时注册更新回调
        static ToolbarExtension()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// 编辑器更新回调，用于检测和初始化工具栏
        /// </summary>
        static void OnUpdate()
        {
            // 当工具栏为空时，尝试查找并初始化（布局改变时ToolBar会���删除重建）
            if (mCurrentToolbar == null)
            {
                // 查找工具栏实例
                var toolbars = Resources.FindObjectsOfTypeAll(mToolbarType);
                mCurrentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (mCurrentToolbar != null)
                {
                    // 通过反射获取工具栏的根视图
                    var root = mCurrentToolbar.GetType()
                        .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rawRoot = root.GetValue(mCurrentToolbar);
                    var mRoot = rawRoot as VisualElement;

                    // 注册左侧和右侧区域的回调
                    RegisterVisualElementCallback("ToolbarZoneLeftAlign", ToolbarZoneLeftAlign);
                    RegisterVisualElementCallback("ToolbarZoneRightAlign", ToolbarZoneRightAlign);

                    // 本地函数：注册可视化元素回调
                    void RegisterVisualElementCallback(string root, Action<VisualElement> cb)
                    {
                        // 获取指定名称的工具栏区域
                        var toolbarZone = mRoot.Q(root);
                        if (toolbarZone == null) return;

                        // 创建一个新的容器元素
                        var parent = new VisualElement()
                        {
                            style =
                            {
                                flexGrow = 1,
                                marginLeft = 2,
                                flexDirection = FlexDirection.Row,
                            }
                        };

                        // 调用外部注册的回调，添加自定义控件
                        cb?.Invoke(parent);

                        // 将容器添加到工具栏区域
                        toolbarZone.Add(parent);
                    }
                }
            }
        }
    }
}