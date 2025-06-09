using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Core
{
    [Serializable]
    public class SourceAttribute : Attribute
    {
        public string Path;
        
        public SourceAttribute(string path)
        {
            Path = path;
        }
    }

    public class ComponentItemKey : MonoBehaviour, ISerializationCallbackReceiver
    {
        public Dictionary<string, Object> Dic = new Dictionary<string, Object>();
        [FormerlySerializedAs("componentDatas")] public List<ComponentData> ComponentDatas;
        [FormerlySerializedAs("selectedOfGameObject")]
        [HideInInspector]
        public List<string> SelectedOfGameObject;

        public T GetObject<T>(string key) where T : Component
        {
            T obj = default (T);
            if (key != null && this.Dic.TryGetValue(key, out var value))
                obj = value as T;
            if ((Object) obj == (Object) null)
                Debug.LogError((object) $" ##Error## gameObject.name == {this.gameObject.name}  -- key == {key} ，value == null，");
            return obj;
        }

        private void OnDestroy()
        {
            this.ComponentDatas.Clear();
            this.Dic.Clear();
        }

        public void OnAfterDeserialize()
        {
            this.Dic.Clear();
            if (this.ComponentDatas == null)
                return;
            foreach (var t in this.ComponentDatas)
                this.Dic.Add(t.Key, t.Value);
        }

        public void OnBeforeSerialize()
        {
        }
    }
    [Serializable]
    public class ComponentData
    {
        public string Key;
        public string Type;
        public UnityEngine.Object Value;

        public override string ToString() => $"{this.Key}, {this.Type}, {this.Value}";
    }
}



