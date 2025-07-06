using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FileListWindow : EditorWindow
{
    public enum StructState
    {
        Delete = 0,
        UnSelect = 1,
        Select = 2,
    }

    private Dictionary<string, List<string>> messageStruct = new Dictionary<string, List<string>>();
    private static Dictionary<string, bool> toggles = new Dictionary<string, bool>();
    public static string SPLITE = "#";

    private Vector3 mScrollPos;

    public static string InputPath = "";
    private bool toggleAll;

    private bool toggleValue;

    public static void ShowWindow(string path)
    {
        InputPath = path;
        // 显示窗口
        string title = Path.GetFileName(InputPath);
        GetWindow<FileListWindow>(title+"勾选项生成绑定");
    }

    private void OnLostFocus()
    {
        SaveFile();
    }

    public void SaveFile()
    {
        try
        {
            string togglePath = InputPath.Replace(".proto", ".json");
            File.WriteAllText(togglePath, JsonConvert.SerializeObject(toggles));
            Debug.Log("SaveFile:"+togglePath);
            Debug.Log("GetDirectoryName:"+Path.GetDirectoryName(togglePath));
            GitAdd(Path.GetDirectoryName(togglePath));
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
    
    private void GitAdd(string path)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "git";
        startInfo.Arguments = $"add .";
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = path;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        Debug.Log(output);
    }

    private void OnEnable()
    {
        //初始化messageStruct
        var contents = File.ReadAllLines(InputPath);
        var subContents = new List<string>();
        bool begin = false;
        string head = "";
        for (int i = 0; i < contents.Length; i++)
        {
            if (contents[i].Contains("message"))
            {
                head = GetMessageHead(contents[i]);
                messageStruct.Add(head,new List<string>());
                begin = true;
            }

            if (begin)
            {
                subContents.Add(contents[i]);
            }

            if (contents[i].Contains("}") && begin)
            {
                begin = false;
                messageStruct[head]=GetMessageStruct(subContents);
                subContents.Clear();
            }
        }
        
        //初始化toggles
        string togglePath = InputPath.Replace(".proto", ".json");
        toggles.Clear();
        if (File.Exists(togglePath))
        {
            toggles =JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(togglePath));
        }

        //删除toggle中多余的table
        List<string> allTable = new List<string>();
        foreach (var VARIABLE in messageStruct)
        {
            foreach (var str in VARIABLE.Value)
            {
                string arg = VARIABLE.Key + SPLITE + str;
                if (!allTable.Contains(arg))
                {
                    allTable.Add(arg);
                }
            }
        }
        var keysToRemove = toggles.Where(pair => !allTable.Contains(pair.Key)) // 筛选已不存在的 Key 值
            .Select(pair => pair.Key) // 获取它们的 Key
            .ToList(); // 转换为 List 类型
        foreach (var key in keysToRemove) // 遍历筛选出的 Key
        {
            toggles.Remove(key); // 删除它们
        }
    }

    private string GetMessageHead(string input)
    {
        string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words[1].Replace("{","");
    }

    private List<string> GetMessageStruct(List<string> input)
    {
        List<string> list = new List<string>(input.Count);
        for (int i = 1; i < input.Count - 1; i++)
        {
            string[] args = input[i].Split("=");
            if (args.Length > 1)
            {
                args[0] = args[0].Replace("\t", "");
                string[] words = args[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    if (words[0].Contains("//") || words[1].Contains("//"))
                    {
                        Debug.Log("被注释："+input[i]);
                    }
                    else
                    {
                        string value = words.Last().Trim();
                        if (!value.Contains("*") && !value.Contains("/"))
                        {
                            list.Add(value);
                        }
                    }
                }
                else
                {
                    Debug.Log("不能加入："+input[i]);
                }
               
            }
        }
        return list;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        toggleAll = EditorGUILayout.Toggle(toggleAll);
        EditorGUILayout.LabelField("全选",GUILayout.Width(position.width));
        EditorGUILayout.EndHorizontal();
        if (toggleValue != toggleAll)
        {
            toggleValue = toggleAll;
            SelectAll(toggleAll);
        }
        
        mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos);
        foreach (var VARIABLE in messageStruct)
        {
            // 显示头
            EditorGUILayout.LabelField(VARIABLE.Key);
            
            // 显示字段
            for (int i = 0; i < VARIABLE.Value.Count; i++)
            {
                string arg = VARIABLE.Key+SPLITE+VARIABLE.Value[i];
                if (!toggles.ContainsKey(arg))
                {
                    toggles.Add(arg,false);
                }
                EditorGUILayout.BeginHorizontal();
                toggles[arg] = EditorGUILayout.Toggle(toggles[arg]);
                EditorGUILayout.LabelField(GetRelativeName(arg),GUILayout.Width(position.width));
                EditorGUILayout.EndHorizontal();
            }
        }
    
        GUILayout.EndScrollView();
    }

    public static string GetRelativeName(string value)
    {
        string[] args = value.Split(SPLITE);
        if (args.Length == 2)
        {
            return args[1];
        }
        Debug.LogError(value+"不合法");
        return String.Empty;
    }

    public void SelectAll(bool select)
    {
        foreach (var key in toggles.Keys.ToList())
        {
            toggles[key] = select;
        }
    }
}