// using指令
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Editor.RectTransformEditorWindow
{
    [CustomEditor(typeof(RectTransform))]
    public class RectTransformEditorWindow : UnityEditor.Editor
    {
        #region 字段定义区域
        private UnityEditor.Editor mTarget;
        private RectTransform targetTransform;
        private Transform realRoot;
        private bool floatIncludeChildren;
        private bool textIncludeChildren = true;
        private bool beautifyPrefabRoot;
        private const string DefaultTextPath = "DefaultText/DefaultText.txt";
        #endregion
        
        #region Unity 生命周期方法
        private void Awake()
        {
            mTarget = UnityEditor.Editor.CreateEditor(target, Assembly.GetAssembly(typeof(UnityEditor.Editor)).GetType("UnityEditor.RectTransformEditor", true));
            targetTransform = target as RectTransform;
            beautifyPrefabRoot = false;
            if (targetTransform.root != null)
            {
                if (!targetTransform.root.name.Contains("Canvas ("))
                {
                    if (targetTransform.root == targetTransform)
                    {
                        beautifyPrefabRoot = true;
                        realRoot = targetTransform.transform;
                    }
                }
                else
                {
                    if (targetTransform.root.childCount > 0 && targetTransform.root.GetChild(0) == targetTransform)
                    {
                        beautifyPrefabRoot = true;
                        realRoot = targetTransform.root.GetChild(0);
                    }
                }
            }
        }
        #endregion
        
        #region Inspector面板绘制
        public override void OnInspectorGUI()
        {
            mTarget.OnInspectorGUI();
            GUI.color = Color.green;
            if (!beautifyPrefabRoot) return;

            // 规范美化预制体
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("规范美化预制体", GUILayout.Height(24f)))
            {
                ResetTransformFloat(true);
                ResetTransformText(true);
                ResetTransformName();
                HandleImageShake();
            }
            EditorGUILayout.EndHorizontal();

            // 导入默认文本
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("导入默认文本", GUILayout.Height(24f)))
                ReimportDefaultText(realRoot);
            EditorGUILayout.EndHorizontal();

            // 清理文本默认值
            EditorGUILayout.BeginHorizontal();
            textIncludeChildren = EditorGUILayout.Toggle(textIncludeChildren, GUILayout.Width(15f));
            GUILayout.Label("保存默认值配置", GUILayout.Width(80f));
            if (GUILayout.Button("清理文本默认值", GUILayout.Height(24f)))
                ResetTransformText(textIncludeChildren);
            EditorGUILayout.EndHorizontal();

            // 清理浮点数位
            EditorGUILayout.BeginHorizontal();
            floatIncludeChildren = EditorGUILayout.Toggle(floatIncludeChildren, GUILayout.Width(15f));
            GUILayout.Label("包含子节点", GUILayout.Width(80f));
            if (GUILayout.Button("清理浮点数位(4舍5入)", GUILayout.Height(24f)))
                ResetTransformFloat(floatIncludeChildren);
            EditorGUILayout.EndHorizontal();

            // 清理资源
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清理资源(spr=null&alpha!=0&size!=0)", GUILayout.Height(24f)))
                HandleImageShake();
            EditorGUILayout.EndHorizontal();

            // 勾选CullTransparentMesh
            if (GUILayout.Button("勾选CullTransparentMesh", GUILayout.Height(24f)))
                CullTransparentMesh();

            // 查找Alpha为1
            if (GUILayout.Button("FindAlpha 1", GUILayout.Height(24f)))
                FindAlpha0();
        }
        #endregion

       
        #region 功能方法
        // 查找Alpha为0但不为极小值的Image
        public void FindAlpha0()
        {
            bool changed = false;
            foreach (Image img in targetTransform.GetComponentsInChildren<Image>(true))
            {
                if (img.color.a <= 0.0117647061f && img.color.a > 0.0001f)
                {
                    Debug.LogError($"{img.name} alpha: {img.color.a}");
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                    changed = true;
                }
            }
            if (changed)
                EditorUtility.SetDirty(target);
        }

        // 勾选所有CanvasRenderer的cullTransparentMesh
        public void CullTransparentMesh()
        {
            bool changed = false;
            foreach (CanvasRenderer cr in targetTransform.GetComponentsInChildren<CanvasRenderer>(true))
            {
                cr.cullTransparentMesh = true;
                changed = true;
            }
            if (changed)
                EditorUtility.SetDirty(target);
        }

        // 清理RectTransform的浮点数位
        public void ResetTransformFloat(bool includeChildren)
        {
            bool changed = false;
            if (includeChildren)
            {
                foreach (RectTransform rt in targetTransform.GetComponentsInChildren<RectTransform>(true))
                {
                    rt.anchoredPosition = new Vector2(Mathf.Round(rt.anchoredPosition.x), Mathf.Round(rt.anchoredPosition.y));
                    rt.sizeDelta = new Vector2(Mathf.Round(rt.sizeDelta.x), Mathf.Round(rt.sizeDelta.y));
                    changed = true;
                }
            }
            else
            {
                targetTransform.anchoredPosition = new Vector2(Mathf.Round(targetTransform.anchoredPosition.x), Mathf.Round(targetTransform.anchoredPosition.y));
                targetTransform.sizeDelta = new Vector2(Mathf.Round(targetTransform.sizeDelta.x), Mathf.Round(targetTransform.sizeDelta.y));
                changed = true;
            }
            if (changed)
                EditorUtility.SetDirty(target);
        }

        // 清理Text内容并可选保存默认值
        public void ResetTransformText(bool saveFile)
        {
            bool changed = false;
            if (saveFile)
            {
                string path = Application.dataPath.Replace("Assets", "DefaultText/DefaultText.txt");
                if (File.Exists(path) && EditorUtility.DisplayDialog("保存默认值配置？", "是否需要保存当前默认值配置？", "保存", "关闭"))
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path));
                    var defaultDict = new Dictionary<string, string>();
                    SetTextKey(realRoot, realRoot.name, ref defaultDict);
                    dict[realRoot.name] = defaultDict;
                    string contents = JsonConvert.SerializeObject(dict);
                    File.Delete(path);
                    File.WriteAllText(path, contents);
                    if (EditorUtility.DisplayDialog("导出完成", $"导出完成,是否打开{path}", "打开", "关闭"))
                        InternalEditorUtility.OpenFileAtLineExternal(path, 1);
                }
            }
            foreach (Text txt in targetTransform.GetComponentsInChildren<Text>(true))
            {
                if (txt.transform.GetComponent("LanguageComponent") == null)
                {
                    txt.text = string.Empty;
                    changed = true;
                }
            }
            if (changed)
                EditorUtility.SetDirty(target);
        }

        // 清理命名
        public void ResetTransformName()
        {
            bool changed = false;
            foreach (Transform t in targetTransform.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Contains(" "))
                    {
                        changed = true;
                        name = name.Replace(" ", "");
                        t.name = name;
                    }
                    if (CompareChar(name[0]))
                    {
                        string newName = name.Substring(0, 1).ToUpper() + name.Substring(1);
                        Debug.LogError(newName);
                        t.name = newName;
                    }
                }
            }
            if (changed)
                EditorUtility.SetDirty(target);
        }

        // 清理Image资源异常
        public void HandleImageShake()
        {
            bool changed = false;
            foreach (Image img in targetTransform.GetComponentsInChildren<Image>(true))
            {
                RectTransform rt = img.GetComponent<RectTransform>();
                if (img.sprite == null && img.color.a != 0f && (rt.sizeDelta.x > 0.1f || rt.sizeDelta.y > 0.1f))
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                    CanvasRenderer cr = rt.GetComponent<CanvasRenderer>();
                    if (cr != null)
                        cr.cullTransparentMesh = true;
                    changed = true;
                }
            }
            if (changed)
                EditorUtility.SetDirty(target);
        }

        // 导入默认文本
        private void ReimportDefaultText(Transform root)
        {
            string path = Application.dataPath.Replace("Assets", "DefaultText/DefaultText.txt");
            if (!File.Exists(path)) return;
            if (!JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path)).TryGetValue(root.name, out var dict))
                return;
            foreach (var kv in dict)
            {
                string[] strArray = kv.Key.Split('|');
                if (strArray[0] != root.name)
                {
                    Debug.LogError($"root 节点不正确 name = {root.name}");
                    break;
                }
                Transform trans = root;
                for (int i = 1; i < strArray.Length; ++i)
                {
                    int idx = int.Parse(strArray[i].Split('/')[1]);
                    if (trans.childCount != 0)
                    {
                        Transform child = trans.GetChild(idx);
                        if (i == strArray.Length - 1 && child != null && child.GetComponent<Text>() != null && string.IsNullOrEmpty(child.GetComponent<Text>().text) && child.GetComponent("LanguageComponent") == null)
                            child.GetComponent<Text>().text = kv.Value;
                        else if (child != null && child.childCount != 0)
                            trans = child;
                    }
                }
            }
        }
        #endregion
        
        #region 静态工具方法
        private static void SetTextKey(Transform root, string rootPath, ref Dictionary<string, string> defaultDict)
        {
            if (root.childCount == 0) return;
            string prefix = rootPath + "|";
            for (int i = 0; i < root.childCount; ++i)
            {
                Text txt = root.GetChild(i).GetComponent<Text>();
                string key = $"{prefix}{root.GetChild(i).name}/{i}";
                if (txt != null && !string.IsNullOrEmpty(txt.text) && root.GetChild(i).GetComponent("LanguageComponent") == null)
                {
                    defaultDict.Add(key, txt.text);
                    if (root.childCount != 0)
                        SetTextKey(txt.transform, key, ref defaultDict);
                }
                else if (root.childCount != 0)
                    SetTextKey(root.GetChild(i), key, ref defaultDict);
            }
        }

        private static bool CompareChar(char c) => c >= 'a' && c <= 'z';
        #endregion
    }
}
