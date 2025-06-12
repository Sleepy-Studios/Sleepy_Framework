using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.GameViewResolutionWindow
{
    public class GameViewResolutionWindow : EditorWindow
    {
        #region 分辨率预设
        private struct ResolutionPreset
        {
            public string Name;
            public int Width;
            public int Height;
            public ResolutionPreset(string name, int width, int height)
            {
                Name = name;
                Width = width;
                Height = height;
            }
        }
        private static readonly ResolutionPreset[] Presets =
        {
            new("720P", 1280, 720),
            new("1080P", 1920, 1080),
            new("2K", 2560, 1440),
            new("4K", 3840, 2160),
            new("16:9", 1920, 1080),
            new("16:10", 1920, 1200),
            new("21:9", 2340, 1080),
            new("20:9", 2400, 1080),
            new("9:21", 1080, 2340),
            new("9:20", 1080, 2400),
        };
        #endregion

        #region 自定义分辨率设置
        private int customWidth = 1920;
        private int customHeight = 1080;
        private Vector2 scrollPosition;
        #endregion

        [MenuItem("Tools/Game视图分辨率快速切换")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameViewResolutionWindow>("分辨率切换");
            window.minSize = new Vector2(300, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Game视图分辨率快速切换", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawPresetResolutions();
            EditorGUILayout.Space(10);
            DrawCustomResolution();
            EditorGUILayout.Space(10);
            DrawCurrentResolution();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPresetResolutions()
        {
            EditorGUILayout.LabelField("预设分辨率", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var preset in Presets)
            {
                if (GUILayout.Button($"{preset.Name} ({preset.Width}x{preset.Height})"))
                {
                    SetGameViewResolution(preset.Width, preset.Height);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCustomResolution()
        {
            EditorGUILayout.LabelField("自定义分辨率", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            customWidth = EditorGUILayout.IntField("宽度", customWidth);
            customHeight = EditorGUILayout.IntField("高度", customHeight);
            if (GUILayout.Button("应用自定义分辨率"))
            {
                SetGameViewResolution(customWidth, customHeight);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentResolution()
        {
            EditorGUILayout.LabelField("当前分辨率", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect gameViewRect = GetMainGameViewRect();
            EditorGUILayout.LabelField($"宽度: {gameViewRect.width}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"高度: {gameViewRect.height}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"宽高比: {(gameViewRect.height > 0 ? (gameViewRect.width / gameViewRect.height).ToString("F2") : "-")}", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 设置游戏视图分辨率
        /// </summary>
        private void SetGameViewResolution(int width, int height)
        {
            try
            {
                var gameViewType = GetGameViewType();
                if (gameViewType == null)
                {
                    Debug.LogError("无法找到GameView类型");
                    return;
                }
                var gameView = GetMainGameView();
                if (gameView == null)
                {
                    Debug.LogError("无法找到GameView窗口");
                    return;
                }
                var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singletonType.GetProperty("instance");
                var gameViewSizesInstance = instanceProp.GetValue(null, null);
                var currentGroupType = sizesType.GetMethod("GetGroup");
                int groupType = EditorUserBuildSettings.activeBuildTarget switch
                {
                    BuildTarget.Android => 2,
                    BuildTarget.iOS => 1,
                    _ => 0
                };
                var group = currentGroupType.Invoke(gameViewSizesInstance, new object[] { groupType });
                var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
                var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
                int foundIndex = Array.FindIndex(displayTexts, t => t.Contains($"{width}x{height}"));
                if (foundIndex == -1)
                {
                    var addCustomSize = group.GetType().GetMethod("AddCustomSize");
                    var gameViewSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
                    var gameViewSizeTypeEnum = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
                    var ctor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                    var fixedType = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                    var newSize = ctor.Invoke(new object[] { fixedType, width, height, $"{width}x{height}" });
                    addCustomSize.Invoke(group, new object[] { newSize });
                    foundIndex = displayTexts.Length;
                    Debug.Log($"[GameViewSizes] 已添加自定义分辨率: {width}x{height}");
                }
                var sizeSelectionCallback = gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (sizeSelectionCallback != null)
                {
                    sizeSelectionCallback.Invoke(gameView, new object[] { foundIndex, null });
                    gameView.Repaint();
                    //Debug.Log($"[GameViewSizes] 成功切换到自定义分辨率: {width}x{height}");
                    return;
                }
                Debug.LogWarning($"无法设置分辨率 {width}x{height}，当前Unity版本可能不支持此操作");
            }
            catch (Exception ex)
            {
                Debug.LogError($"设置游戏视图分辨率时出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 供外部调用：设置Game视图分辨率（静态方法）
        /// </summary>
        public static void SetGameViewResolutionStatic(int width, int height)
        {
            var window = GetWindow<GameViewResolutionWindow>(false, null, false);
            if (window != null)
            {
                window.SetGameViewResolution(width, height);
            }
            else
            {
                // 若未打开窗口也可直接调用静态方法
                var inst = ScriptableObject.CreateInstance<GameViewResolutionWindow>();
                inst.SetGameViewResolution(width, height);
            }
        }

        /// <summary>
        /// 获取Game视图类型
        /// </summary>
        private static Type GetGameViewType() => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");

        /// <summary>
        /// 获取主Game视图
        /// </summary>
        private static EditorWindow GetMainGameView()
        {
            var gameViewType = GetGameViewType();
            if (gameViewType == null) return null;
            var getMainGameViewMethod = gameViewType.GetMethod("GetMainGameView", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            if (getMainGameViewMethod != null)
                return getMainGameViewMethod.Invoke(null, null) as EditorWindow;
            // 兼容不同Unity版本
            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            return windows != null && windows.Length > 0 ? windows[0] as EditorWindow : null;
        }

        /// <summary>
        /// 获取当前Game视图的rect
        /// </summary>
        private static Rect GetMainGameViewRect()
        {
            var gameView = GetMainGameView();
            if (gameView == null) return new Rect(0, 0, 0, 0);
            var gameViewType = gameView.GetType();
            var targetSizeProperty = gameViewType.GetProperty("targetSize", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (targetSizeProperty != null)
            {
                var size = (Vector2)targetSizeProperty.GetValue(gameView);
                return new Rect(0, 0, size.x, size.y);
            }
            return gameView.position;
        }
    }
}
