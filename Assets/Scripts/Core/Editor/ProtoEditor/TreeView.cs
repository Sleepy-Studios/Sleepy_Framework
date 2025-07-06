using System.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VisualElementExtension
{
    public abstract class TreeView : EditorWindow
    {
        protected List<TreeNode> items;
        protected ListView container;
        protected VisualElement toolBar;
        string filter;

        protected VisualElement errorContent;
        protected Label errorLabel;
        protected string err;
        protected string Error { 
            get => err; 
            set 
            {
                if(!string.IsNullOrEmpty(value))
                {
                    errorContent.style.display = DisplayStyle.Flex;
                    container.style.display = DisplayStyle.None;
                    errorLabel.text = value;
                }
                else
                {
                    errorContent.style.display = DisplayStyle.None;
                    container.style.display = DisplayStyle.Flex;
                }
                err = value;
            } 
        }

        protected abstract bool ToggleVisible { get; }
        protected abstract string ContentTitle { get; }

        protected class TreeNode
        {
            public string title;
            public string mark;
            public bool isTitle;
            public bool opened;
            public Action<bool> onSelected;
            public string fullName;
            public int level;
            public TreeNode parent;

            /// <summary>
            /// path with '/', like /proto/base.proto.
            /// </summary>
            /// <returns></returns>
            public string PathWithParent => MakeWithParent(this);

            private string MakeWithParent(TreeNode node)
            {
                if(node == null)
                {
                    return "";
                }
                return MakeWithParent(node.parent) + "/" + node.title;
            }

        }
 
        protected virtual void OnEnable()
        {
            rootVisualElement.style.borderLeftWidth = 5;
            rootVisualElement.style.borderTopWidth = 5;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        protected virtual void OnGUI()
        {
            VisualElement just = container;
            if(!string.IsNullOrEmpty(Error))
            {
                just = errorLabel;
            }

            if (just != null)
            {
                just.style.height = just.style.maxHeight = rootVisualElement.layout.height - 16 - 32 - toolBar.layout.height;
            }
        }

        private VisualElement MakeError()
        {
            errorContent = new VisualElement();
            //errorContent.style.alignItems = Align.Center;
            //errorContent.style.justifyContent = Justify.Center;

            errorLabel = new Label();
            errorLabel.style.fontSize = 32;
            errorLabel.style.color = Color.red;
            errorLabel.style.alignSelf = Align.Center;
            errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            errorContent.Add(errorLabel);
            errorContent.style.display = DisplayStyle.None;

            return errorContent;
        }

        private void CreateGUI()
        {

            if (container == null)
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(CreateSearchBar());
                rootVisualElement.Add(CreateContentTitle());

                errorContent = MakeError();
                container = new ListView();
                container.fixedItemHeight = 25;
                
                rootVisualElement.Add(errorContent);
                rootVisualElement.Add(container);

                InitData();
                InitView();

                CreateToolsBar();

                EditorApplication.playModeStateChanged += (state) =>
                {
                    if (state == PlayModeStateChange.EnteredPlayMode)
                    {
                        EditorApplication.update += OnUpdate;
                    }
                    else if (state == PlayModeStateChange.ExitingPlayMode)
                    {
                        EditorApplication.update -= OnUpdate;
                    }
                };
            }
        }

        protected abstract void OnDoubleClick(IEnumerable<object> itor);
        protected abstract void OnSelected(IEnumerable<object> itor);
        protected abstract void InitData(string filter = null);



        private VisualElement CreateContentTitle()
        {
            return new Label() { text = ContentTitle };
        }

        void InitView()
        {
            container.Clear();
            container.style.height = rootVisualElement.layout.height - 16 - 32;
            container.makeItem = MakeItem;
            container.bindItem = BindItem;
            container.itemsSource = items;
            container.selectionType = SelectionType.Single;

            // Callback invoked when the user double clicks an item
            container.onItemsChosen -= OnDoubleClick;
            container.onItemsChosen += OnDoubleClick;

            // Callback invoked when the user changes the selection inside the ListView
            container.onSelectionChange -= OnSelected;
            container.onSelectionChange += OnSelected;
        }

        protected void Refresh()
        {
            container.RefreshItems();
        }

        protected virtual void OnUpdate()
        {

        }
        public virtual VisualElement MakeItem()
        {
            var container = new VisualElement();
            container.style.justifyContent = Justify.Center;
            var titleContainer = new VisualElement();
            titleContainer.style.flexDirection = FlexDirection.Row;
            var toggle = new Toggle();
            var running = new VisualElement() { name = "running", tooltip = "运行中" };
            var titleContent = new VisualElement() { name = "title-content" };
            titleContent.style.flexDirection = FlexDirection.Row;
            var title = new Label();

            container.Add(titleContainer);
            titleContainer.Add(running);
            titleContainer.Add(toggle);
            titleContent.Add(title);
            titleContainer.Add(titleContent);
            return container;
        }
        Dictionary<string, Clickable> directoryOpenEvnet;
        public virtual void BindItem(VisualElement e, int i)
        {
            if (i >= items.Count) return;
            var data = items[i];

            // e.style.backgroundColor = i % 2 == 1 ? new Color(56f/256,56f/256,56f/256) : new Color(63f/256,63f/256,63f/256);
            var title = e.Q<Label>();
            if (!string.IsNullOrEmpty(data.mark))
            {
                title.text = $"{data.title}({data.mark})";
            }
            else
            {
                title.text = $"{data.title}";
            }
            var toggle = e.Q<Toggle>();
            var running = e.Query<VisualElement>().Where(x => x.name == "running").First();
            running.visible = false;
            running.style.color = Color.green;
            var length = container.fixedItemHeight / 4;
            running.style.height = length;
            running.style.width = length;
            running.style.borderTopLeftRadius = length / 2;
            running.style.borderTopRightRadius = length / 2;
            running.style.borderBottomLeftRadius = length / 2;
            running.style.borderBottomRightRadius = length / 2;
            running.style.alignSelf = Align.Center;
            running.style.left = new StyleLength(15 - length);

            toggle.style.borderLeftWidth = 15 * data.level;
            if(data.isTitle)
            {
                toggle.AddToClassList("unity-foldout__toggle");
                toggle.RemoveFromClassList("unity-input__toggle");
            }
            else
            {
                toggle.AddToClassList("unity-input__toggle");
                toggle.RemoveFromClassList("unity-foldout__toggle");
            }
            //toggle.AddToClassList("unity-foldout__toggle");
            //toggle.AddToClassList("unity-input__toggle");
            //toggle.EnableInClassList("unity-foldout__toggle", data.isTitle);
            //toggle.EnableInClassList("unity-input__toggle", data.isTitle);

            toggle.UnregisterCallback<ChangeEvent<bool>, TreeNode>(ToggleChangeEvent);
            toggle.value = data.opened;
            if (data.isTitle)
            {
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                title.style.unityFontStyleAndWeight = FontStyle.Normal;
                toggle.visible = ToggleVisible;

                var circle = new Texture2D(10, 10);
                for (int j = 0; j < circle.width; j++)
                {
                    for (int k = 0; k < circle.height; k++)
                    {
                        circle.SetPixel(j, k, Color.green);
                    }
                }
                circle.Apply();
                running.style.backgroundImage = new StyleBackground(circle);
            }

            DoSearch(title);
            
            data.onSelected = (selected) => toggle.value = selected;

            toggle.RegisterCallback<ChangeEvent<bool>, TreeNode>(ToggleChangeEvent, data);

            if(menus.TryGetValue(e.GetHashCode(), out IManipulator lastMenu))
            {
                lastMenu.target = null;
                lastMenu = null;
                menus.Remove(e.GetHashCode());
            }
            var menu = AddManipulator(i);
            menus.Add(e.GetHashCode(), menu);
            menu.target = e;
        }

        Dictionary<int, IManipulator> menus = new Dictionary<int, IManipulator>();

        protected virtual IManipulator AddManipulator(int i)
        {
            return null;
        }

        void ToggleChangeEvent(ChangeEvent<bool> evt, TreeNode data)
        {
            if(!string.IsNullOrEmpty(filter))
            {
                return;
            }

            data.opened = evt.newValue;
            data.onSelected(data.opened);
            if(data.isTitle)
            {
                InitData();
                Refresh();
            }
        }

        protected virtual VisualElement CreateSearchBar()
        {
            TextField search = new TextField();
            search.value = "Search by type";
            search.Query<VisualElement>().Where(x => x.name == "unity-text-input").First().style.color = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1));
            search.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                if (evt.newValue == "Search by type") return;

                filter = evt.newValue;
                InitData(evt.newValue);
                Refresh();
            });

            search.RegisterCallback<FocusEvent>((evt) =>
            {
                search.Query<VisualElement>().Where(x => x.name == "unity-text-input").First().style.color = Color.white;
                if (search.value != "" && search.value == "Search by type")
                {
                    search.value = "";
                }
            });
            search.RegisterCallback<BlurEvent>((evt) =>
            {
                if (!string.IsNullOrEmpty(search.value)) return;
                search.Query<VisualElement>().Where(x => x.name == "unity-text-input").First().style.color = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1));
                search.value = "Search by type";
                filter = null;
                InitData();
            });

            return search;
        }

        protected virtual void CreateToolsBar()
        {
            if (toolBar == null)
            {
                toolBar = new VisualElement();
                toolBar.style.minHeight = 16;
                toolBar.style.position = Position.Absolute;
                toolBar.style.bottom = 0;
                toolBar.StretchToParentWidth();
            }

            rootVisualElement.Add(toolBar);
        }

        void DoSearch(Label title)
        {
            var parent = title.parent;
            parent.Clear();            
            if (!string.IsNullOrEmpty(filter))
            {
                var startIndex = title.text.IndexOf(filter, StringComparison.OrdinalIgnoreCase);
                if (startIndex >= 0)
                {
                    var checkeStr = title.text.Substring(startIndex, filter.Length);
                    var highLight = new VisualElement();
                    highLight.AddToClassList("unity-debugger-highlight");
                    int fontSize = 12;

                    var left = 0;
                    Font sample = Font.CreateDynamicFontFromOSFont("Inter-Regular", fontSize);

                    sample.RequestCharactersInTexture(title.text, fontSize, FontStyle.Normal);

                    for (int j = 0; j < startIndex; j++)
                    {
                        sample.GetCharacterInfo(title.text[j], out CharacterInfo info);
                        left += info.advance;
                    }

                    var width = 0;
                    for (int j = 0; j < checkeStr.Length; j++)
                    {
                        sample.GetCharacterInfo(checkeStr[j], out CharacterInfo info);
                        width += info.advance;
                    }

                    highLight.style.width = width;
                    highLight.style.left = left + 1;
                    highLight.style.height = 16;
                    highLight.style.position = Position.Absolute;
                    highLight.style.backgroundColor = new StyleColor(new Color(0.882f, 0.271f, 0.271f, 1));
                    parent.Add(highLight);
                }
            }
            parent.Add(title);
        }
    }
    
    public static class UIElementsUtils
    {
        internal static VisualElement CreateListContainer<T>(string title, IList<T> source, Func<VisualElement> createfunc, Action<VisualElement, int> binder, Action onSourceChange = null) where T : new()
        {
            VisualElement body = new VisualElement();
            body.AddDefaultStyleSheet();
            body.AddToClassList("list-container");
            VisualElement titleContent = new VisualElement();
            titleContent.AddToClassList("title-content");
            Button buttonTitle = new Button() { text = title };

            // buttonTitle.RegisterCallback<MouseOverEvent>(evt =>
            // {
            //     buttonTitle.style.backgroundColor = new Color(0.2705882f, 0.2705882f, 0.2705882f, 1);
            // });
            // buttonTitle.RegisterCallback<MouseOutEvent>(evt =>
            // {
            //     buttonTitle.style.backgroundColor = new Color(0.2705882f, 0.2705882f, 0.2705882f, 0);
            // });

            ListView listContent = new ListView();
            listContent.AddToClassList("list-content");
            listContent.fixedItemHeight = 24;
            listContent.itemsSource = (IList)source;
            listContent.bindItem = binder;
            listContent.makeItem = createfunc;
 
            Toggle toggleExpand = new Toggle();
            toggleExpand.AddToClassList("unity-foldout__toggle");

            VisualElement toolContainer = new VisualElement();
            toolContainer.AddToClassList("tool-container");
            VisualElement toolContent = new VisualElement();
            toolContent.AddToClassList("tool-locator");

            Button toolAdd = new Button();
            toolAdd.AddToClassList("tool-add");
            toolAdd.style.backgroundImage = EditorGUIUtility.FindTexture("d_Toolbar Plus");
            Button toolDel = new Button();
            toolDel.AddToClassList("tool-add");
            toolDel.style.backgroundImage = EditorGUIUtility.FindTexture("d_Toolbar Minus");

            toolAdd.clicked += () =>
            {
                source.Add(new T());
                listContent.Rebuild();
                onSourceChange?.Invoke();
            };
            toolDel.clicked += () =>
            {
                source.RemoveAt(listContent.selectedIndex);
                listContent.Rebuild();
                onSourceChange?.Invoke();
            };

            buttonTitle.clicked += () =>
            {
                
            };

            toolAdd.RegisterCallback<MouseOverEvent>(evt => toolAdd.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f));
            toolAdd.RegisterCallback<MouseOutEvent>(evt => toolAdd.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0));

            toolDel.RegisterCallback<MouseOverEvent>(evt => toolDel.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f));
            toolDel.RegisterCallback<MouseOutEvent>(evt => toolDel.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0));

            body.Add(titleContent);
            body.Add(listContent);

            titleContent.Add(buttonTitle);
            titleContent.Add(toggleExpand);
            body.Add(toolContainer);
            toolContainer.Add(toolContent);
            toolContent.Add(toolAdd);
            toolContent.Add(toolDel);

            if(string.IsNullOrEmpty(title))
            {
                titleContent.style.display = DisplayStyle.None;
            }

            return body;
        }

        public static void Add(this VisualElement self, params VisualElement[] children)
        {
            if(children == null)
            {
                return;
            }
            foreach (var child in children)
            {
                self.Add(child);
            }
        }

        internal static void ClearlyVisualElement(this VisualElement e)
        {
            e.style.borderTopColor = e.style.borderRightColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.backgroundColor = new Color(1, 1, 1, 0);
        }

        internal static bool AddDefaultStyleSheet(this VisualElement self)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Core/Editor/ProtoEditor/DefaultStyle.uss");
            if(styleSheet != null)
            {
                self.styleSheets.Add(styleSheet);
                return true;
            }
            Debug.LogError("无法加载默认风格");
            return false;
        }
    }
}

