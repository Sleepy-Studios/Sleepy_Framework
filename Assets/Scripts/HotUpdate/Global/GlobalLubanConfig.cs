using System.Collections.Generic;
using HotUpdate.Base;
using UnityEngine;
using YooAsset;

namespace HotUpdate
{
    public class GlobalLubanConfig : LazyMonoSingleton<GlobalLubanConfig>
    {
        private cfg.Tables tables;
        private Dictionary<string, string> fileDict;
        
        public new static GlobalLubanConfig Instance => LazyMonoSingleton<GlobalLubanConfig>.Instance;
        public static cfg.Tables Tables => Instance.tables;

        protected override void Awake()
        {
            base.Awake();
            LoadAllConfig();
        }

        private void LoadAllConfig()
        {
            var assetInfos = YooAssets.GetAssetInfos("LubanConfig");
            fileDict = new Dictionary<string, string>();
            foreach (var assetInfo in assetInfos)
            {
                var handle = YooAssets.LoadAssetSync<TextAsset>(assetInfo.AssetPath);
                if (handle.IsValid)
                {
                    var textAsset = handle.AssetObject as TextAsset;
                    if (textAsset != null) fileDict[textAsset.name] = textAsset.text;
                }
                handle.Release();
            }
            tables = new cfg.Tables(file => SimpleJSON.JSON.Parse(fileDict[file]));
        }
    }    
}

