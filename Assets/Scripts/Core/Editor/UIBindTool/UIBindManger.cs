using System.Collections.Generic;
using UnityEngine;

namespace Core.Editor.UIBindTool
{
    public static class UIBindManager
    {
        // 存储已选择的游戏物体和组件
        private static Dictionary<GameObject, List<Component>> selectedComponents = new Dictionary<GameObject, List<Component>>();

        // 当前编辑的预制体
        private static GameObject currentPrefab;

        // 检查游戏物体是否被选中
        public static bool IsGameObjectSelected(GameObject gameObject)
        {
            return selectedComponents.ContainsKey(gameObject) && selectedComponents[gameObject].Count > 0;
        }

        // 选择组件
        public static void SelectComponent(GameObject gameObject, Component component)
        {
            if (!selectedComponents.ContainsKey(gameObject))
            {
                selectedComponents[gameObject] = new List<Component>();
            }

            if (!selectedComponents[gameObject].Contains(component))
            {
                selectedComponents[gameObject].Add(component);
            }
        }

        // 移除单个组件的选择
        public static void RemoveComponentSelection(GameObject gameObject, Component component)
        {
            if (selectedComponents.ContainsKey(gameObject))
            {
                selectedComponents[gameObject].Remove(component);
            }
        }

        // 取消选择游戏物体
        public static void UnselectGameObject(GameObject gameObject)
        {
            selectedComponents.Remove(gameObject);
        }

        // 清除游戏物体的组件选择
        public static void ClearComponentSelection(GameObject gameObject)
        {
            if (selectedComponents.ContainsKey(gameObject))
            {
                selectedComponents[gameObject].Clear();
            }
        }

        // 获取所有选择的组件
        public static Dictionary<GameObject, List<Component>> GetSelectedComponents()
        {
            return selectedComponents;
        }

        // 设置当前编辑的预制体
        public static void SetCurrentPrefab(GameObject prefab)
        {
            currentPrefab = prefab;
        }

        // 获取当前编辑的预制体
        public static GameObject GetCurrentPrefab()
        {
            return currentPrefab;
        }

        // 清除所有选择
        public static void ClearAllSelections()
        {
            selectedComponents.Clear();
        }
    }
}

