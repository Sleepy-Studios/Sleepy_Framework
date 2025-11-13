using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace HotUpdate
{
    public partial class Tables : LazyMonoSingleton<Tables>
    {
        private cfg.Tables loadTables;
        private Dictionary<string, string> fileDict;
        public new static Tables Instance => LazyMonoSingleton<Tables>.Instance;
        
        /// 静态转发属性，直接访问单例的 LoadTables
        public static cfg.Tables Config => Instance.loadTables;

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
            loadTables = new cfg.Tables(file => SimpleJSON.JSON.Parse(fileDict[file]));
        }
    }

 
}
