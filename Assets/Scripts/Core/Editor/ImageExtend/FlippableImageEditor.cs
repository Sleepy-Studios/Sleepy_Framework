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

            EditorGUILayout.PropertyField(spFlipHor, gcFlipHor);
            EditorGUILayout.PropertyField(spFlipVer, gcFlipVer);

            serializedObject.ApplyModifiedProperties();
        }
    }
    
}



