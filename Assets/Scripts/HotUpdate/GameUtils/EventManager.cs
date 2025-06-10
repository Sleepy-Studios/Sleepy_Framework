using System;
using System.Collections.Generic;
using HotUpdate.Base;

namespace HotUpdate.GameUtils
{
    /// <summary>
    /// 事件管理器
    /// </summary>
    public class EventManager : LazyMonoSingleton<EventManager>
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        /// key 事件名称
        /// value 事件对应委托函数
        Dictionary<EventName, List<Delegate>> eventDic = new Dictionary<EventName, List<Delegate>>();

        ///全局单例实例
        public new static EventManager Instance => LazyMonoSingleton<EventManager>.Instance;


        /// 初始化
        protected override void Awake()
        {
            base.Awake();
        }


        /// <summary>
        /// 监听事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        private void AddListenerBase(EventName eventName, Delegate callback)
        {
            if (eventDic.ContainsKey(eventName))
            {
                eventDic[eventName].Add(callback);
            }
            else
            {
                eventDic.Add(eventName, new List<Delegate>() { callback });
            }
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="callback"></param>
        void RemoveListenerBase(EventName eventName, Delegate callback)
        {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                eventList.Remove(callback);
                if (eventList.Count == 0)
                {
                    eventDic.Remove(eventName);
                }
            }
        }

        /// <summary>
        /// 添加事件监听(不需要参数传递)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        public void AddListener(EventName eventName, Action callback)
        {
            AddListenerBase(eventName, callback);
        }

        /// <summary>
        /// 添加事件监听(1个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T">参数</typeparam>
        public void AddListener<T>(EventName eventName, Action<T> callback)
        {
            AddListenerBase(eventName, callback);
        }

        /// <summary>
        /// 添加事件监听(2个参数)
        /// </summary>  
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T1">参数1</typeparam>
        /// <typeparam name="T2">参数2</typeparam>
        public void AddListener<T1, T2>(EventName eventName, Action<T1, T2> callback)
        {
            AddListenerBase(eventName, callback);
        }

        /// <summary>
        /// 添加事件监听(3个参数)
        /// </summary>  
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T1">参数1</typeparam>
        /// <typeparam name="T2">参数2</typeparam>
        /// <typeparam name="T3">参数3</typeparam>
        public void AddListener<T1, T2, T3>(EventName eventName, Action<T1, T2, T3> callback)
        {
            AddListenerBase(eventName, callback);
        }

        /// <summary>
        /// 添加事件监听(4个参数)
        /// </summary>  
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T1">参数1</typeparam>
        /// <typeparam name="T2">参数2</typeparam>
        /// <typeparam name="T3">参数3</typeparam>
        /// <typeparam name="T4">参数4</typeparam>
        public void AddListener<T1, T2, T3, T4>(EventName eventName, Action<T1, T2, T3, T4> callback)
        {
            AddListenerBase(eventName, callback);
        }


        /// <summary>
        /// 移除事件(没有参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        public void RemoveListener(EventName eventName, Action callback)
        {
            RemoveListenerBase(eventName, callback);
        }

        /// <summary>
        /// 移除事件(1个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T">参数</typeparam>
        public void RemoveListener<T>(EventName eventName, Action<T> callback)
        {
            RemoveListenerBase(eventName, callback);
        }

        /// <summary>
        /// 移除事件(2个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T1">参数1</typeparam>
        /// <typeparam name="T2">参数2</typeparam>
        public void RemoveListener<T1, T2>(EventName eventName, Action<T1, T2> callback)
        {
            RemoveListenerBase(eventName, callback);
        }

        /// <summary>
        /// 移除事件(3个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T1">参数1</typeparam>
        /// <typeparam name="T2">参数2</typeparam>
        /// <typeparam name="T3">参数3</typeparam>
        public void RemoveListener<T1, T2, T3>(EventName eventName, Action<T1, T2, T3> callback)
        {
            RemoveListenerBase(eventName, callback);
        }

        /// <summary>
        /// 移除事件(4个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callback">委托函数</param>
        /// <typeparam name="T1">参数1</typeparam>
        /// <typeparam name="T2">参数2</typeparam>
        /// <typeparam name="T3">参数3</typeparam>
        /// <typeparam name="T4">参数4</typeparam>
        public void RemoveListener<T1, T2, T3, T4>(EventName eventName, Action<T1, T2, T3, T4> callback)
        {
            RemoveListenerBase(eventName, callback);
        }


        /// <summary>
        /// 触发事件(无参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        public void TriggerEvent(EventName eventName)
        {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                foreach (Delegate callback in eventList)
                {
                    (callback as Action)?.Invoke();
                }
            }
        }

        /// <summary>
        /// 触发事件(1个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="info1" >参数</param>
        public void TriggerEvent<T>(EventName eventName, T info1)
        {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                foreach (Delegate callback in eventList)
                {
                    (callback as Action<T>)?.Invoke(info1);
                }
            }
        }

        /// <summary>
        /// 触发事件(2个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="info1" >参数1</param>
        /// <param name="info2" >参数2</param>
        public void TriggerEvent<T1, T2>(EventName eventName, T1 info1, T2 info2)
        {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                foreach (Delegate callback in eventList)
                {
                    (callback as Action<T1, T2>)?.Invoke(info1, info2);
                }
            }
        }

        /// <summary>
        /// 触发事件(3个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <param name="info1" >参数1</param>
        /// <param name="info2" >参数2</param>
        /// <param name="info3" >参数3</param>
        public void TriggerEvent<T1, T2, T3>(EventName eventName, T1 info1, T2 info2, T3 info3)
        {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                foreach (Delegate callback in eventList)
                {
                    (callback as Action<T1, T2, T3>)?.Invoke(info1, info2, info3);
                }
            }
        }

        /// <summary>
        /// 触发事件(4个参数)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="T4">参数4类型</typeparam>
        /// <param name="info1" >参数1</param>
        /// <param name="info2" >参数2</param>
        /// <param name="info3" >参数3</param>
        /// <param name="info4" >参数4</param>
        public void TriggerEvent<T1, T2, T3, T4>(EventName eventName, T1 info1, T2 info2, T3 info3, T4 info4)
        {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList))
            {
                foreach (Delegate callback in eventList)
                {
                    (callback as Action<T1, T2, T3, T4>)?.Invoke(info1, info2, info3, info4);
                }
            }
        }

        public void Clear()
        {
            eventDic.Clear();
        }
    }
}