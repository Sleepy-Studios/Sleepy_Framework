using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Platform.Editor
{
    [System.Serializable]
    public class ProtobufSelectData
    {
        public List<SelectItem> selectItems = new List<SelectItem>();

        [System.Serializable]
        public class SelectItem
        {
            public string Name;
            [SerializeField]
            public List<string> NotSelectType = new List<string> ();
        }

        public void Save()
        {
            var str = JsonUtility.ToJson(this);
            File.WriteAllText("Assets/Editor/ProtoCache.json", str);
        }
    }

    public class ProtobufTypeSelect : EditorWindow
    {
        private List<string> typeList = new List<string>();
        private List<string> selectType = new List<string>();

        private string originalText;

        private System.Action<string> getText;

        Vector2 vector2;
        string searchStr;

        ProtobufSelectData protobufSelectData;
        ProtobufSelectData.SelectItem selectItem;
        TaskCompletionSource<string> gAsync;

        private void OnGUI()
        {
            searchStr = EditorGUILayout.TextField(searchStr, (GUIStyle)"SearchTextField");

            vector2 = GUILayout.BeginScrollView(vector2);
            for (int i = 0; i < typeList.Count; i++)
            {
                var current = typeList[i];

                if (!string.IsNullOrEmpty(searchStr))
                {
                    if (!current.ToLower().Contains(searchStr.ToLower()))
                    {
                        continue;
                    }
                }

                var select = GUILayout.Toggle(selectType.Contains(current), current, (GUIStyle)"BoldToggle");
                if (select && !selectType.Contains(current))
                {
                    selectType.Add(current);
                }
                if (!select && selectType.Contains(current))
                {
                    selectType.Remove(current);
                }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Next"))
            {
                var ignoreList = typeList.Except(selectType).ToList();

                for (int i = 0; i < ignoreList.Count; i++)
                {
                    Regex reg = GetRegex($"message\\b{ignoreList[i]}", "}");
                    originalText = reg.Replace(originalText, "");
                }
                gAsync.SetResult(originalText);

                selectItem.NotSelectType = ignoreList;
                protobufSelectData.Save();
            }
        }

        private void OnLostFocus()
        {
            this.Close();
        }

        Regex GetRegex(string s, string e)
        {
            return new Regex("((" + s + "))[.\\s\\S]*?((" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline);
        }

        List<string> GetRegex(string str, string s, string e)
        {
            return Regex.Matches(str, "(?<=(" + s + "))[.\\s\\S]*?(?=(" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(x => x.Value).ToList();
        }

        public Task <string> InitTypeList(string typeText, string protoName)
        {
            gAsync = new TaskCompletionSource<string>();
            if (protobufSelectData == null)
            {
                if (File.Exists("Assets/Editor/ProtoCache.json"))
                {
                    protobufSelectData = JsonUtility.FromJson<ProtobufSelectData>(File.ReadAllText("Assets/Editor/ProtoCache.json"));
                }
                else
                {
                    protobufSelectData = new ProtobufSelectData();
                }
            }

            typeList.Clear();
            selectType.Clear();
            FindType(typeText);

            this.originalText = typeText;

            selectItem = protobufSelectData.selectItems.Find(x => x.Name.Equals(protoName));
            if (selectItem == null)
            {
                selectItem = new ProtobufSelectData.SelectItem();
                selectItem.Name = protoName;
                protobufSelectData.selectItems.Add(selectItem);
            }
            for (int i = 0; i < selectItem.NotSelectType.Count; i++)
            {
                selectType.Remove(selectItem.NotSelectType[i]);
            }

            EditorUtility.ClearProgressBar();

            return gAsync.Task;
        }

        void FindType(string typeText)
        {
            var allType = GetRegex(typeText, $"message","}");
            for (int i = 0; i < allType.Count; i++)
            {
                var typeStr = allType[i];
                var endIndex = typeStr.IndexOf("{");
                var typeName = typeStr.Substring(0, endIndex).TrimEnd();
                typeList.Add(typeName);
                selectType.Add(typeName);
            }
        }

    }
}