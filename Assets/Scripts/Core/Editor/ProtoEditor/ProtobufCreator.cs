using Platform.Editor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VisualElementExtension;
using Debug = UnityEngine.Debug;
public class ProtobufCreator : VisualElementExtension.TreeView
{
    class CsOutPath
    {
        public string Path
        {
            get => path;
            set
            {
                path = value;
                if(string.IsNullOrEmpty(path)) return;
                var itor = PathWithNamespace.GetEnumerator();
                while (itor.MoveNext())
                {
                    if (path.Contains(itor.Current.Key))
                    {
                        NameSpace = itor.Current.Value;
                        break;
                    }
                }
            }
        }
        private string path;
        public string NameSpace;

        public CsOutPath() { }

        public CsOutPath(string path)
        {
            this.Path = path;
            NameSpace = "";
            var itor = PathWithNamespace.GetEnumerator();
            while (itor.MoveNext())
            {
                if (path.Contains(itor.Current.Key))
                {
                    NameSpace = itor.Current.Value;
                    break;
                }
            }
        }
    }

    static readonly Dictionary<string, string> PathWithNamespace = new Dictionary<string, string>()
    {
        {"Hotfix", "HotUpdate"},
        {"Main", "Core"}
    };

    static string protoSavaPath = Environment.CurrentDirectory + "\\Proto";
    static readonly string CsOutPathKey = "Proto-CsOutPath";
    static readonly string NamespaceKey = "Proto-Namespace"; // 新增命名空间保存键

    string nameSpace;
    bool isNormal;

    event Action OnGUIEvent;
    List<TreeNode> allFiles = new List<TreeNode>();
    protected override bool ToggleVisible => true;

    protected override string ContentTitle => "Protos";

    List<CsOutPath> csOutPaths = new List<CsOutPath>();
    int SelectIndex
    {
        get => selectIndex;
        set
        {
            selectIndex = value;
            onSelectIndexChange?.Invoke(selectIndex);
        }
    }
    Action<int> onSelectIndexChange;
    int selectIndex = -1;
    Action<int> onSelectorClicked;

    [MenuItem("Tools/Protobuf生成", false, 1)]
    public static void ShowWindow()
    {
        var window = GetWindow<ProtobufCreator>();
        window.minSize = new Vector2(100, 100);

        window.titleContent = new GUIContent("Proto2cs");
    }

    protected override void OnGUI()
    {
        base.OnGUI();
        OnGUIEvent?.Invoke();
    }

    void InitCsOutPaths()
    {
        csOutPaths ??= new List<CsOutPath>();
        csOutPaths.Clear();

        int count = EditorPrefs.GetInt(CsOutPathKey);
        for (int i = 0; i < count; i++)
        {
            var path = EditorPrefs.GetString(CsOutPathKey + i);
            if (!string.IsNullOrEmpty(path) && !csOutPaths.Exists((x) => x.Path == path))
            {
                var csOutPath = new CsOutPath(path);
                // 加载保存的命名空间
                var savedNamespace = EditorPrefs.GetString(NamespaceKey + i, "");
                if (!string.IsNullOrEmpty(savedNamespace))
                {
                    csOutPath.NameSpace = savedNamespace;
                }
                csOutPaths.Add(csOutPath);
            }
            else
            {
                EditorPrefs.DeleteKey(CsOutPathKey + i);
                EditorPrefs.DeleteKey(NamespaceKey + i);
            }
        }
        if (csOutPaths.Count != count)
        {
            EditorPrefs.SetInt(CsOutPathKey, csOutPaths.Count);
        }
        
        // 加载全局命名空间设置
        if (string.IsNullOrEmpty(nameSpace))
        {
            nameSpace = EditorPrefs.GetString(NamespaceKey + "_current", "");
        }
    }

    protected override void InitData(string filter = null)
    {
        if (items == null)
        {
            items = new List<TreeNode>();
        }
        else
        {
            items.Clear();
        }

        if (allFiles.Count == 0)
        {
            if (!string.IsNullOrEmpty(protoSavaPath))
            {
                if (!Directory.Exists(protoSavaPath))
                {
                    this.ShowNotification(new GUIContent("请先选择存放Proto文件的路径"));
                    EditorPrefs.SetString("ProtoKitSavePath", "");
                }
                else
                {
                    SearchDirectoryFiles(protoSavaPath, 0, null, allFiles);
                    if (allFiles.Count == 0)
                    {
                        this.ShowNotification(new GUIContent("目录下不存在Proto文件"));
                    }
                }
            }
            else
            {
                this.ShowNotification(new GUIContent("请先选择存放Proto文件的路径"));
            }
        }

        if (allFiles.Count > 0)
        {
            InitCsOutPaths();
            bool opened = true;
            int parentLevel = 0;
            foreach (var file in allFiles)
            {
                if (file.isTitle)
                {
                    opened = file.opened;
                    parentLevel = file.level;
                    items.Add(file);
                    continue;
                }
                if (!opened && parentLevel == file.level)
                {
                    opened = true;
                }

                if (!string.IsNullOrEmpty(filter))
                {
                    if (file.title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (file.parent != null)
                        {
                            file.parent.opened = true;
                        }

                        items.Add(file);
                    }
                }
                else
                {
                    if (opened)
                    {
                        items.Add(file);
                    }
                }

            }
        }
    }

    bool SearchDirectoryFiles(string directoryPath, int level, TreeNode parent, List<TreeNode> result)
    {
        DirectoryInfo directory = new DirectoryInfo(directoryPath);
        var directories = directory.GetDirectories();
        TreeNode node;
        foreach (var d in directories)
        {
            node = result.Find(x => x.PathWithParent == d.Name) ?? new TreeNode() { title = d.Name, isTitle = true, level = level, opened = true };
            int parentIndex = result.Count;
            result.Add(node);
            if (node.opened && !SearchDirectoryFiles(d.FullName, level + 1, node, result))
            {
                result.RemoveAt(parentIndex);
            }
        }
        bool exist = false;
        var fileInfo = directory.GetFiles();
        foreach (var file in fileInfo)
        {
            if (!file.Name.EndsWith(".proto"))
                continue;
            node = result.Find(x => x.PathWithParent == file.Name) ?? new TreeNode() { title = file.Name, isTitle = false, level = level, opened = false, fullName = directoryPath + "/" + file.Name, parent = parent };
            result.Add(node);
            exist = true;
        }
        return exist;
    }

    protected override IManipulator AddManipulator(int i)
    {
        var menu = new ContextualMenuManipulator((evt) =>
        {
            evt.menu.AppendAction("Create", (action) => CreateSelectList(items[i].PathWithParent));
            evt.menu.AppendAction($"Open", (action) => System.Diagnostics.Process.Start(items[i].fullName));
        });
        return menu;
    }

    protected override void OnDoubleClick(IEnumerable<object> itor)
    {

    }

    protected override void OnSelected(IEnumerable<object> itor)
    {
        // foreach (var o in itor)
        // {
        //     if (o is TreeNode)
        //     {
        //         TreeNode treeNode = (TreeNode)o;
        //         if (!treeNode.isTitle)
        //         {
        //             FileListWindow.ShowWindow(treeNode.fullName);
        //         }
        //     }
        //     break;
        // }
    }

    public class LabelText
    {

    }

    protected override void CreateToolsBar()
    {
        base.CreateToolsBar();

        var horizontal = new VisualElement();
        horizontal.style.flexDirection = FlexDirection.Row;
        horizontal.style.marginBottom = 4;
        var selectFirstLay = new Toggle() { text = "SelectFirstLayer" };
        selectFirstLay.RegisterCallback<ChangeEvent<bool>>(OnSelectFirstLay);
        var selectAll = new Toggle() { text = "SelectAll" };
        selectAll.RegisterCallback<ChangeEvent<bool>>(OnSelectAll);
        var normal = new Label() { text = "NAMESPACE" };
        var normalNamesapce = new TextField();
        Color color;
        ColorUtility.TryParseHtmlString("#4FB9FE", out color);
        normalNamesapce.Query<VisualElement>(name = "unity-text-input").First().style.color = color;
        normal.style.color = color;
        normal.style.borderLeftWidth = 1;
        normal.style.paddingLeft = 4;
        normal.style.borderTopWidth = normal.style.borderBottomWidth = 2;
        ColorUtility.TryParseHtmlString("#646464", out color);
        normal.style.borderLeftColor = color;
        normalNamesapce.style.width = 200;
        normalNamesapce.RegisterCallback<ChangeEvent<string>>((evt) => {
            nameSpace = evt.newValue;
            // 更新当前选择路径的命名空间
            if (selectIndex >= 0 && selectIndex < csOutPaths.Count)
            {
                csOutPaths[selectIndex].NameSpace = nameSpace;
                SaveCsOutPath();
            }
        });
        // 初始化时显示已保存的命名空间
        if (selectIndex >= 0 && selectIndex < csOutPaths.Count)
        {
            nameSpace = csOutPaths[selectIndex].NameSpace;
            normalNamesapce.value = nameSpace;
        }
        onSelectIndexChange += (index) =>
        {
            if (index >= 0 && index < csOutPaths.Count)
            {
                nameSpace = csOutPaths[index].NameSpace;
                normalNamesapce.value = nameSpace;
            }
        };

        //var createBar = new VisualElement();
        var createBtn = new Button() { text = "Create", tooltip = "to create all selected files or right click to create directly." };
        createBtn.AddToClassList("--unity-icons-foldout");
        createBtn.clicked += () => CreateSelectList();
        createBtn.style.right = 5;
        createBtn.style.position = Position.Absolute;
        var pbBar = CreateDirectoryBar("PB保存路径", protoSavaPath, true, false, (newPath) =>
        {
            Error = !IsValidPath(newPath) ? "不能包含中文路径" : null;
            EditorPrefs.SetString("ProtoKitSavePath", newPath);
            allFiles.Clear();
            InitData();
            Refresh();
        });
        pbBar.SetEnabled(false);

        var directoryBar = VisualElementExtension.UIElementsUtils.CreateListContainer("CS保存路径", csOutPaths, CreateDirectoryBar, BindDirectoryBar, SaveCsOutPath);
        horizontal.Add(selectFirstLay);
        horizontal.Add(selectAll);
        horizontal.Add(normal);
        horizontal.Add(normalNamesapce);
        horizontal.Add(createBtn);
        toolBar.Add(horizontal);
        toolBar.Add(pbBar);
        toolBar.Add(directoryBar);
    }

    bool IsValidPath(string path)
    {
        //var gb18030 = "[\u4e00-\u9fa5]";
        //Regex regex = new Regex(gb18030);
        //return !regex.IsMatch(path);
        return true;
    }

    void OnSelectAll(ChangeEvent<bool> evt)
    {
        items.Clear();
        foreach (var item in allFiles)
        {
            items.Add(item);
            if (item.isTitle && item.opened)
            {
                continue;
            }
            item.opened = evt.newValue;
        }
        container.Rebuild();
    }
    
    void OnSelectFirstLay(ChangeEvent<bool> evt)
    {
        items.Clear();
        foreach (var item in allFiles)
        {
            
            items.Add(item);
            if (item.isTitle && item.opened)
            {
                continue;
            }

            if (item.level > 0)
            {
                continue;
            }
            item.opened = evt.newValue;
        }
        container.Rebuild();
    }

    VisualElement CreateDirectoryBar(string label, string val = null, bool blurEvent = false, bool toggled = false, Action<string> action = null)
    {
        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;

        var path = new TextField() { label = label, value = val };
        path.Q<Label>().style.minWidth = 20;
        path.style.minWidth = Length.Percent(90);

        if (blurEvent)
        {
            path.RegisterCallback<BlurEvent>((evt) =>
            {
                action?.Invoke(path.value);
            });
        }

        var icon = new Button();
        icon.style.width = 20;
        icon.style.height = 16;
        icon.style.maxWidth = 20;
        icon.style.backgroundImage = EditorGUIUtility.FindTexture("Folder Icon");
        //icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        icon.clicked += () =>
        {
            var tPath = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
            if (!string.IsNullOrEmpty(tPath))
            {
                path.value = tPath;
                action?.Invoke(tPath);
            }
        };

        var toggle = new Toggle();
        toggle.style.display = toggled ? DisplayStyle.Flex : DisplayStyle.None;
        // OnGUIEvent += () =>
        // {
        //     path.style.width = rootVisualElement.layout.width - 34;
        // };

        body.Add(path, icon, toggle);

        return body;
    }

    void SaveCsOutPath()
    {
        for (int i = 0; i < csOutPaths.Count; i++)
        {
            EditorPrefs.SetString(CsOutPathKey + i, csOutPaths[i].Path);
            // 保存对应的命名空间
            EditorPrefs.SetString(NamespaceKey + i, csOutPaths[i].NameSpace);
        }
        EditorPrefs.SetInt(CsOutPathKey, csOutPaths.Count);
        
        // 保存当前全局命名空间
        EditorPrefs.SetString(NamespaceKey + "_current", nameSpace);
    }
    VisualElement CreateDirectoryBar()
    {

        var body = new VisualElement();
        body.style.flexDirection = FlexDirection.Row;

        var path = new TextField();
        path.style.minWidth = Length.Percent(90);

        var icon = new Button();
        icon.style.width = 20;
        icon.style.height = 16;
        icon.style.maxWidth = 20;
        icon.style.backgroundImage = EditorGUIUtility.FindTexture("Folder Icon");
        //icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

        var toggle = new Toggle();

        // OnGUIEvent += () =>
        // {
        //     path.style.width = rootVisualElement.layout.width - 34;
        // };

        body.Add(path, icon, toggle);

        return body;
    }

    void BindDirectoryBar(VisualElement element, int index)
    {
        string saveKey = CsOutPathKey + index;
        var path = element.Query<TextField>().First();
        path.label = $"element {index}";
        path.value = csOutPaths[index].Path;
        path.RegisterCallback<BlurEvent>((evt) =>
        {
            csOutPaths[index].Path = path.value;
            SaveCsOutPath();
        });

        var icon = element.Query<Button>().First();
        icon.clicked += () =>
        {
            var tPath = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
            if (!string.IsNullOrEmpty(tPath))
            {
                path.value = tPath.StartsWith(Application.dataPath) ? tPath.Remove(0, Application.dataPath.Length) : tPath;
                csOutPaths[index].Path = path.value;
                SaveCsOutPath();
            }
        };

        var toggle = element.Query<Toggle>().First();
        onSelectorClicked += (x) =>
        {
            toggle.value = index == x;
            if (toggle.value)
            {
                SelectIndex = x;
            }
        };
        toggle.RegisterValueChangedCallback((evt) =>
        {
            if (selectIndex == index && !evt.newValue)
            {
                toggle.value = true;
            }
            if (evt.newValue)
            {
                onSelectorClicked(index);
            }
        });
        if (csOutPaths.Count == 1 || selectIndex > csOutPaths.Count || SelectIndex == -1)
        {
            SelectIndex = 0;
        }
        if (index == SelectIndex)
        {
            toggle.value = true;
        }
    }

    void CreateSelectList(string singleFile = null)
    {

        if (string.IsNullOrEmpty(nameSpace))
        {
            if (EditorUtility.DisplayDialog("错误", "命名空间为空", "确认"))
            {

            }
            return;
        }
        var tempPath = System.Environment.CurrentDirectory + "~";
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }

        for (int i = 0; i < allFiles.Count; i++)
        {
            if (allFiles[i].isTitle) continue;
            if (!string.IsNullOrEmpty(nameSpace))
            {
                var allText = File.ReadAllText($"{allFiles[i].fullName}");
                Regex reg = new Regex("(\\npackage\\b .+);");
                string modified;
                if (!reg.IsMatch(allText))
                {
                    Regex makeWithPackage = new Regex("syntax.*;");
                    modified = makeWithPackage.Replace(allText, (m) => m.Value + ($"\npackage {nameSpace};\n"));
                }
                else
                {
                    modified = reg.Replace(allText, $"\npackage {nameSpace};");
                }

                reg = new Regex("(\\noption csharp_namespace\\b .+);");
                modified = reg.Replace(modified, "");

                //reg = new Regex("import\\s*(.*/)");
                //modified = reg.Replace(modified, (m) => m.Value.Replace(m.Groups[1].Value, "\""));

                if (allText.Equals(modified))
                {
                    var index = allText.IndexOf("\n");
                    modified = allText.Insert(index + 1, $"package {nameSpace};");
                }

                string newFilePath = $"{tempPath}{allFiles[i].PathWithParent}";
                string directory = Path.GetDirectoryName(newFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(newFilePath, modified);
            }
            else
            {
                FileInfo file = new FileInfo($"{allFiles[i].fullName}");
                file.CopyTo($"{tempPath}/{allFiles[i].title}", true);
            }
        }

        var tempExe = PathWithNamespace.ContainsValue(nameSpace) ? "/protoc.exe" : "/protocNormal.exe";
        //var tempExe =  "/protoc.exe";
        var exePath = EditorPathUtil.PackPath + tempExe;
        try
        {
            if (singleFile != null)
            {
                Run(exePath, singleFile, protoSavaPath);
            }
            else
            {
                for (int tIndex = 0; tIndex < allFiles.Count; tIndex++)
                {
                    if (allFiles[tIndex].isTitle || !allFiles[tIndex].opened) continue;
                    EditorUtility.DisplayProgressBar("等待创建", allFiles[tIndex].fullName, (float)tIndex / (float)allFiles.Count);
                    exePath = exePath.Replace('\\', '/');
                    Run(exePath, allFiles[tIndex].PathWithParent, protoSavaPath);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.Refresh();
    }

    void Run(string exe, string pb, string directory)
    {
        Process p = new Process();
        p.StartInfo.FileName = "cmd.exe";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardInput = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.CreateNoWindow = true;
        p.Start();

        var tempPath = System.Environment.CurrentDirectory + "~";
        var textPath2 = $"{tempPath}{pb}";
        var fullFilePath = csOutPaths[SelectIndex].Path;
        if(fullFilePath.StartsWith("\\") || fullFilePath.StartsWith("/"))
        {
            fullFilePath = Application.dataPath + fullFilePath;
        }
        p.StandardInput.WriteLine($"\"{exe}\" \"{textPath2}\" --csharp_out=\"{fullFilePath}\" --proto_path=\"{textPath2.Substring(0, textPath2.LastIndexOf('/'))}\"&exit");
        p.StandardInput.AutoFlush = true;

        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            Debug.LogError(p.StandardError.ReadToEnd());
        }
        else
        {
            var name = Path.GetFileNameWithoutExtension(pb);
            var underlineName = name.Split('_');
            if (underlineName.Length > 0)
            {
                name = "";
                for (int i = 0; i < underlineName.Length; i++)
                {
                    name += underlineName[i].Substring(0, 1).ToUpper() + underlineName[i].Substring(1);
                }
            }
            else
                name = name.Substring(0, 1).ToUpper() + name.Substring(1);
            var filePath = $"{csOutPaths[SelectIndex].Path}/{name}.cs";
            if (File.Exists(filePath))
            {
                ProtoUtils.Convert(filePath,protoSavaPath + pb.Replace(".proto",".json"));
                
                // var content = File.ReadAllText(filePath);
                // File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
            } else
            {
                filePath = Application.dataPath + filePath;
                if (File.Exists(filePath))
                {
                    ProtoUtils.Convert(filePath, protoSavaPath + pb.Replace(".proto", ".json"));
                }
            }
            Debug.Log(pb + "生成成功");
        }

        p.Close();
    }
}

