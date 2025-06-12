using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Core.Editor.HotUpdateRefSearchTool
{
  public class HotUpdateRefSearchTool : EditorWindow
  {
    private string excludePath = "Assets/Scripts/HotUpdate";
    private static List<string> foundFiles = new List<string>();
    private Vector2 scrollPosition;
    private GUIStyle mFileStyle;
    
    #region Unity菜单与窗口

    [MenuItem("Tools/热更新程序集引用查找工具")]
    public static void ShowWindow() => EditorWindow.GetWindow<HotUpdateRefSearchTool>("热更新程序集引用查找工具");

    #endregion

    private void OnEnable()
    {
      this.mFileStyle = new GUIStyle();
      this.mFileStyle.normal.background = EditorGUIUtility.FindTexture("Folder Icon");
      this.excludePath = EditorPrefs.GetString(typeof (HotUpdateRefSearchTool).ToString());
    }

    private void OnGUI()
    {
      GUILayout.BeginHorizontal();
      GUILayout.Label("排除的热更文件夹:", GUILayout.Width(100f));
      this.excludePath = EditorGUILayout.TextField(this.excludePath, GUILayout.ExpandWidth(true));
      if (GUILayout.Button("", this.mFileStyle, GUILayout.Width(15f)))
      {
        this.excludePath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        GUI.FocusControl((string) null);
        this.Repaint();
        EditorPrefs.SetString(typeof (HotUpdateRefSearchTool).ToString(), this.excludePath);
      }
      GUILayout.EndHorizontal();
      if (GUILayout.Button("查找"))
        this.SearchUseHotfix();
      this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
      foreach (string foundFile in HotUpdateRefSearchTool.foundFiles)
      {
        if (GUILayout.Button(foundFile))
          InternalEditorUtility.OpenFileAtLineExternal(foundFile, 0);
      }
      EditorGUILayout.EndScrollView();
    }

    private void SearchUseHotfix()
    {
      HotUpdateRefSearchTool.foundFiles.Clear();
      HotUpdateRefSearchTool.OnSearch(this.excludePath);
      if (HotUpdateRefSearchTool.foundFiles.Count != 0)
        return;
      EditorUtility.DisplayDialog("", "主工程没有引用热更工程的类", "ok");
    }

    public static bool OnSearch(string path)
    {
      bool flag = false;
      path = path.Replace(Application.dataPath, "Assets");
      string[] patterns = {"HotUpdate(", "HotUpdate,", "HotUpdate;", "HotUpdate."};
      foreach (string file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
      {
        if (!file.Replace("\\", "/").Replace(Application.dataPath, "Assets").StartsWith(path) && !file.Contains("\\Editor"))
        {
          foreach (string readAllLine in File.ReadAllLines(file))
          {
            foreach (var pattern in patterns)
            {
              if (readAllLine.Contains(pattern))
              {
                HotUpdateRefSearchTool.foundFiles.Add(file);
                Debug.LogError((object) ("主工程引用热更工程的类:" + file));
                flag = true;
                break;
              }
            }
            if (flag) break;
          }
        }
      }
      return flag;
    }
  }
}
