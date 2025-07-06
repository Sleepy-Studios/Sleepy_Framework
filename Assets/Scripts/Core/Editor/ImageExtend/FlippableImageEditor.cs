using Core.Runtime;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Core.Editor
{
    [CustomEditor(typeof(FlippableImage))]
    public class FlippableImageEditor : ImageEditor
    {
        private SerializedProperty spFlipHor;
        private SerializedProperty spFlipVer;
        private GUIContent gcFlipHor;
        private GUIContent gcFlipVer;

        protected override void OnEnable()
        {
            base.OnEnable();
            spFlipHor = serializedObject.FindProperty("FlipHor");
            spFlipVer = serializedObject.FindProperty("FlipVer");
            gcFlipHor = EditorGUIUtility.TrTextContent("水平翻转", null);
            gcFlipVer = EditorGUIUtility.TrTextContent("垂直翻转", null);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
#if UNITY_EDITOR
            var icon = (Texture2D)EditorGUIUtility.IconContent("d_Image Icon").image;
            EditorGUIUtility.SetIconForObject(target, icon);
#endif
            EditorGUILayout.PropertyField(spFlipHor, gcFlipHor);
            EditorGUILayout.PropertyField(spFlipVer, gcFlipVer);
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/UI/Flippable Image", false)]
        private static void CreateFlippableImage(MenuCommand menuCommand)
        {
            var go = new GameObject("Flippable Image");
            go.AddComponent<FlippableImage>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
    
}

