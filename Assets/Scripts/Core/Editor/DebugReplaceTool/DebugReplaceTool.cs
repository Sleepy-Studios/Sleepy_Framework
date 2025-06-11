using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.DebugReplaceTool
{
    public class DebugReplaceTool : EditorWindow
    {
        #region 字段定义

        private List<string> folderPaths = new List<string>(); // 存储用户选择的文件夹路径
        private Vector2 scrollPosition; // 用于文件夹列表的滚动视图
        private Vector2 scrollPosition2; // 用于待替换文件列表的滚动视图
        private List<string> csFiles = new List<string>(); // 存储所有找到的cs文件路径
        private List<string> needReplace = new List<string>(); // 存储需要替换的cs文件路径
        private List<string> doNotReplayList = new List<string>(); // 存储不需要替换的文件名关键字

        #endregion

        #region Unity菜单与窗口

        [MenuItem("Tools/Log替换工具")]
        public static void ShowWindow() => EditorWindow.GetWindow<DebugReplaceTool>("Debug Replace To Log Tool");

        #endregion

        #region 初始化与辅助方法

        private void Reset()
        {
            this.doNotReplayList.Add("UnityEngine_Debug_Binding"); // 排除Unity自动生成的Debug绑定
            this.doNotReplayList.Add("Log"); // 排除自定义Log类
        }

        private bool ContainsDoNotReplayList(string path)
        {
            foreach (string doNotReplay in this.doNotReplayList)
            {
                if (path.Contains(doNotReplay))
                    return true; // 如果路径包含排除关键字则返回true
            }

            return false;
        }

        #endregion

        #region 主界面绘制

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Add Folder", GUILayout.Height(30f)))
            {
                string str = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (!string.IsNullOrEmpty(str))
                    this.folderPaths.Add(str);
            }

            // 显示已选择的文件夹
            EditorGUILayout.LabelField("Selected Folders:", EditorStyles.boldLabel);
            this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition, GUILayout.MinHeight(60f),
                GUILayout.MaxHeight(150f));
            int removeIndex = -1;
            for (int index = 0; index < this.folderPaths.Count; ++index)
            {
                EditorGUILayout.BeginHorizontal();
                this.folderPaths[index] = EditorGUILayout.TextField(this.folderPaths[index]);
                if (GUILayout.Button("Remove", GUILayout.Width(80f)))
                {
                    removeIndex = index;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0 && removeIndex < this.folderPaths.Count)
            {
                this.folderPaths.RemoveAt(removeIndex);
            }

            EditorGUILayout.EndScrollView();
            GUILayout.Space(10f);
            if (GUILayout.Button("Search CS Files", GUILayout.Height(30f)))
            {
                this.csFiles.Clear();
                this.needReplace.Clear();
                foreach (string folderPath in this.folderPaths)
                    this.csFiles.AddRange(
                        (IEnumerable<string>)Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories));
                foreach (string csFile in this.csFiles)
                {
                    if (this.CheckCodeInFile(csFile) && !this.ContainsDoNotReplayList(csFile))
                        this.needReplace.Add(csFile);
                }
            }

            GUILayout.Space(10f);
            if (this.needReplace.Any<string>())
            {
                EditorGUILayout.LabelField($"CS Files: 数量 = {this.needReplace.Count}", EditorStyles.boldLabel);
                this.scrollPosition2 = EditorGUILayout.BeginScrollView(this.scrollPosition2, GUILayout.MinHeight(100f),
                    GUILayout.MaxHeight(300f));
                foreach (string str in this.needReplace)
                {
                    if (this.CheckCodeInFile(str) && !this.ContainsDoNotReplayList(str))
                        EditorGUILayout.LabelField(str, EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.EndScrollView();
                GUILayout.Space(10f);
                // 一键替换按钮
                if (GUILayout.Button("Replace All", GUILayout.Height(40f)))
                {
                    foreach (string filePath in this.needReplace)
                        this.ReplaceDebugInFile(filePath);
                    this.needReplace.Clear();
                    EditorUtility.DisplayDialog("", "全局替换成功", "OK");
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 文件检查与替换

        public bool CheckCodeInFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;
            using StreamReader streamReader = new StreamReader(filePath);
            string str;
            while ((str = streamReader.ReadLine()) != null)
            {
                // 检查是否包含Debug日志相关代码
                if (str.Contains("UnityEngine.Debug.LogError(") || str.Contains("UnityEngine.Debug.LogWarning(") ||
                    str.Contains("UnityEngine.Debug.Log(") || str.Contains("Debug.LogError(") ||
                    str.Contains("Debug.LogWarning(") || str.Contains("Debug.Log("))
                    return true;
            }

            return false;
        }

        private void ReplaceDebugInFile(string filePath)
        {
            // 替换Debug日志为自定义Log方法
            string contents = File.ReadAllText(filePath)
                .Replace("UnityEngine.Debug.LogError(", "Log.Error(")
                .Replace("UnityEngine.Debug.LogWarning(", "Log.Warning(")
                .Replace("UnityEngine.Debug.Log(", "Log.Info(")
                .Replace("Debug.LogError(", "Log.Error(")
                .Replace("Debug.LogWarning(", "Log.Warning(")
                .Replace("Debug.Log(", "Log.Info(");
            File.WriteAllText(filePath, contents, Encoding.UTF8);
        }

        #endregion
    }
}