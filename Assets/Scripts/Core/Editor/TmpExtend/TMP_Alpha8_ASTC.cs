using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.TmpExtend
{
    public class TMP_Alpha8_ASTC_Window : EditorWindow
    {
        private TMP_FontAsset selectedFontAsset;

        [MenuItem("Tools/TextMeshPro相关/TMP-ASTC", false)]
        public static void ShowWindow()
        {
            var window = GetWindow<TMP_Alpha8_ASTC_Window>(true, "TMP字体ASTC转换", true);
            window.minSize = new Vector2(400, 120);
        }

        private void OnGUI()
        {
            GUILayout.Label("选择要转换的TMP字体资源", EditorStyles.boldLabel);
            selectedFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP Font Asset", selectedFontAsset, typeof(TMP_FontAsset), false);

            if (selectedFontAsset != null)
            {
                if (GUILayout.Button("转换为Alpha8 PNG并替换"))
                {
                    ConvertTMPFont(selectedFontAsset);
                }
            }
        }

        private void ConvertTMPFont(TMP_FontAsset targeFontAsset)
        {
            string fontAssetPath = AssetDatabase.GetAssetPath(targeFontAsset);
            string texturePath = fontAssetPath.Replace(".asset", ".png");
            Texture2D srcTex = targeFontAsset.atlasTexture;
            Texture2D texture2D = new Texture2D(srcTex.width, srcTex.height, TextureFormat.Alpha8, false);
            Graphics.CopyTexture(srcTex, texture2D);
            byte[] dataBytes = texture2D.EncodeToPNG();
            FileStream fs = File.Open(texturePath, FileMode.OpenOrCreate);
            fs.Write(dataBytes, 0, dataBytes.Length);
            fs.Flush();
            fs.Close();
            AssetDatabase.Refresh();
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            AssetDatabase.RemoveObjectFromAsset(targeFontAsset.atlasTexture);
            targeFontAsset.atlasTextures[0] = atlas;
            targeFontAsset.material.mainTexture = atlas;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "转换并替换成功！", "OK");
        }
    }
}