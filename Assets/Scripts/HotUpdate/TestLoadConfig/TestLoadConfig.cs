using UnityEngine;

namespace HotUpdate.TestLoadConfig
{
    public class TestLoadConfig : MonoBehaviour
    {
        
        private void Start()
        {
            LoadConfigWithWrapper();
        }

        // 使用包装器方式：自动释放资源
        private void LoadConfigWithWrapper()
        {
            ReadConfigData(GlobalLubanConfig.Tables);
            
        }

        // 公共的配置读取逻辑
        private void ReadConfigData(cfg.Tables tables)
        {
            // 通过遍历方式访问所有物品
            Debug.Log("===通过遍历访问所有物品===");
            foreach (var item in tables.TbTest1.DataList)
            {
                Debug.Log($"物品: ID={item.Id}, 名称={item.Name}, 描述={item.Desc}, 数量={item.Count}," +
                          $"奖励id={item.Award.ItemId},奖励数量={item.Award.Num},奖励2id={item.Award2.ItemId},奖励数量={item.Award2.Num}" +
                          $"奖励列表={item.Awardlist1.Count}");
            }

            // 通过 ID 直接访问特定物品
            Debug.Log("===通过ID访问特定物品===");
            int targetId = 1;
            var targetItem = tables.TbTest1.Get(targetId);
            if (targetItem != null)
            {
                Debug.Log($"找到物品 {targetId}: 名称={targetItem.Name}, 描述={targetItem.Desc}, 数量={targetItem.Count}");
            }

            // 使用 GetOrDefault 安全地获取物品（如果不存在则返回 null）
            int unknownId = 9999;
            var unknownItem = tables.TbTest1.GetOrDefault(unknownId);
            Debug.Log(unknownItem != null ? $"找到物品 {unknownId}: {unknownItem.Name}" : $"物品 ID {unknownId} 不存在");
        }
    }
}