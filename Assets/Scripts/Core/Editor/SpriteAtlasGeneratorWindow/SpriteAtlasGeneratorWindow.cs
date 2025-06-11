using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Core.Editor.SpriteAtlasGeneratorWindow
{
    /// <summary>
    /// 自动生成SpriteAtlas图集的编辑器工具（支持参数中文解释）
    /// </summary>
    public class SpriteAtlasGeneratorWindow : EditorWindow
    {
        // 根路径：要扫描的文件夹
        private string rootPath = "Assets/GameRes/SpriteAtlas";
        // 允许旋转：打包时是否允许图片旋转以节省空间
        private bool allowRotation = false;
        // 紧密打包：是否使用紧密打包（更贴合图片轮廓）
        private bool tightPacking = false;
        // 间距：图片之间的像素间距
        private int padding = 2;
        
        [MenuItem("Tools/自动生成图集(可配置)")]
        public static void ShowWindow()
        {
            GetWindow<SpriteAtlasGeneratorWindow>("自动生成图集");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("图集生成设置", EditorStyles.boldLabel);
        
            // 根路径输入
            rootPath = EditorGUILayout.TextField("根路径 (Root Path)", rootPath);
            EditorGUILayout.HelpBox("要扫描生成图集的根文件夹路径，例如：Assets/GameRes/SpriteAtlas", MessageType.Info);
        
            // 允许旋转
            allowRotation = EditorGUILayout.Toggle("允许旋转 (Allow Rotation)", allowRotation);
            EditorGUILayout.HelpBox("是否允许图片在图集中旋转以节省空间。", MessageType.None);
        
            // 紧密打包
            tightPacking = EditorGUILayout.Toggle("紧密打包 (Tight Packing)", tightPacking);
            EditorGUILayout.HelpBox("是否使用紧密打包，更贴合图片轮廓，减少空白。", MessageType.None);
        
            // 间距
            padding = EditorGUILayout.IntField("间距 (Padding)", padding);
            EditorGUILayout.HelpBox("图片之间的像素间距，防止图集边缘出现缝隙。", MessageType.None);
        
            GUILayout.Space(10f);
        
            if (GUILayout.Button("生成图集", GUILayout.Height(35f)))
            {
                CreateAllSpriteAtlas();
            }
        }
        
        private void CreateAllSpriteAtlas()
        {
            if (!Directory.Exists(rootPath))
            {
                EditorUtility.DisplayDialog("错误", "根路径不存在，请检查输入路径。", "OK");
                return;
            }
        
            string[] subFolders = Directory.GetDirectories(rootPath);
        
            foreach (string folder in subFolders)
            {
                string folderName = Path.GetFileName(folder);
                string atlasPath = $"{rootPath}/{folderName}.spriteatlas";
        
                if (File.Exists(atlasPath))
                    continue;
        
                SpriteAtlas atlas = new SpriteAtlas();
        
                // 配置图集参数（v2）
                SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings
                {
                    blockOffset = padding, // 间距
                    enableRotation = allowRotation, // 允许旋转
                    enableTightPacking = tightPacking, // 紧密打包
                    padding = padding, // 间距（兼容字段）
                    enableAlphaDilation = true, // 启用Alpha膨胀（防止透明边缘问题）
                    
                };
                atlas.SetPackingSettings(packingSettings);
        
                AssetDatabase.CreateAsset(atlas, atlasPath);

                // 直接添加文件夹到图集
                var folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder.Replace("\\", "/"));
                if (folderAsset != null)
                {
                    atlas.Add(new Object[] { folderAsset });
                }
                else
                {
                    Debug.LogWarning($"未能加载文件夹Asset: {folder}");
                }
                EditorUtility.SetDirty(atlas);
                Debug.Log($"已创建图集: {atlasPath}");
            }
        
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "所有图集已自动生成", "OK");
        }
    }
}