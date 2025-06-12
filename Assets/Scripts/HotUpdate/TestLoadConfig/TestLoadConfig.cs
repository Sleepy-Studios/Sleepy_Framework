using cfg;
using Core;
using SimpleJSON;
using UnityEngine;
using YooAsset;

namespace HotUpdate.TestLoadConfig
{
    public class TestLoadConfig : MonoBehaviour
    {
        // 可以声明为类字段，方便在不同方法中使用
        private AssetLoader<TextAsset> configLoader;
        
        private void Start()
        {
            //LoadConfigNormal();
            LoadConfigWithWrapper();
        }

        // 原始方式：手动释放资源
        private void LoadConfigNormal()
        {
            Debug.Log("===使用原始方式加载配置===");
            // 使用 YooAssets 直接加载 demo_tbitem 配置
            var handle = YooAssets.LoadAssetSync<TextAsset>("demo_tbitem");
            if (!handle.IsValid)
            {
                Debug.LogError("加载 demo_tbitem 失败！");
                return;
            }

            var textAsset = handle.AssetObject as TextAsset;
            if (textAsset == null)
            {
                Debug.LogError("无法获取 demo_tbitem 的 TextAsset！");
                return;
            }

            // 解析 JSON 数据到 Luban 生成的 Tables 中
            var tables = new cfg.Tables(file => JSON.Parse(textAsset.text));
            
            // 读取配置数据
            ReadConfigData(tables);

            // 必须手动释放资源句柄
            handle.Release();
        }

        // 使用包装器方式：自动释放资源
        private void LoadConfigWithWrapper()
        {
            Debug.Log("===使用资源包装器加载配置===");

            //使用类字段存储加载器，以便在其他地方使用
            configLoader = AssetLoader<TextAsset>.Load("tbfight_test1");
            if (configLoader.IsValid)
            {
                var tables = new cfg.Tables(file => JSON.Parse(configLoader.Asset.text));
                ReadConfigData(tables);
            }
        }

        // 公共的配置读取逻辑
        private void ReadConfigData(cfg.Tables tables)
        {
            // 通过遍历方式访问所有物品
            Debug.Log("===通过遍历访问所有物品===");
            foreach (var item in tables.TbTest1.DataList)
            {
                Debug.Log($"物品: ID={item.Id}, 名称={item.Name}, 描述={item.Desc}, 数量={item.Count}");
            }

            // 通过 ID 直接访问特定物品
            Debug.Log("===通过ID访问特定物品===");
            int targetId = 1001;
            var targetItem = tables.TbTest1.Get(targetId);
            if (targetItem != null)
            {
                Debug.Log($"找到物品 {targetId}: 名称={targetItem.Name}, 描述={targetItem.Desc}, 数量={targetItem.Count}");
            }

            // 使用 GetOrDefault 安全地获取物品（如果不存在则返回 null）
            int unknownId = 9999;
            var unknownItem = tables.TbTest1.GetOrDefault(unknownId);
            if (unknownItem != null)
            {
                Debug.Log($"找到物品 {unknownId}: {unknownItem.Name}");
            }
            else
            {
                Debug.Log($"物品 ID {unknownId} 不存在");
            }
        }

        private void OnDestroy()
        {
            // 如果使用了类字段存储加载器，需要在组件销毁时释放
            configLoader?.Dispose();
        }
    }
}