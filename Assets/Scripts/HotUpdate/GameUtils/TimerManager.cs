using System;
using System.Collections.Generic;
using HotUpdate.Base;
using UnityEngine;

namespace HotUpdate.GameUtils
{
    /// <summary>
    /// 时间管理器
    /// </summary>
    public class TimerManager : LazyMonoSingleton<TimerManager>
    {
        ///全局单例实例
        public new static TimerManager Instance => LazyMonoSingleton<TimerManager>.Instance;

        private List<Timer> timers = new List<Timer>();
        private Stack<Timer> timerPool = new Stack<Timer>();

        protected override void Awake()
        {
            base.Awake();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            // 倒序遍历，避免删除元素时索引错乱
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                Timer timer = timers[i];
                if (timer.IsActive)
                {
                    timer.UpdateTime(deltaTime);
                    if (!timer.IsActive)
                    {
                        timers.RemoveAt(i);
                        ReturnTimerToPool(timer);
                    }
                }
            }
        }

        /// <summary>
        /// 创建一次性秒计时器
        /// </summary>
        /// <param name="time">秒数</param>
        /// <param name="callback">回调方法</param>
        /// <param name="autoStart">是否自动启动</param>
        public Timer CreateOneTimeTimer(float time, Action callback, bool autoStart = true)
        {
            Timer timer = GetTimerFromPool();
            timer.Initialize(time, callback, autoStart, false);
            timers.Add(timer);
            return timer;
        }

        /// <summary>
        /// 创建一次性帧计时器
        /// </summary>
        /// <param name="frameInterval">帧数</param>
        /// <param name="callback">回调方法</param>
        /// <param name="autoStart">是否自动启动</param>
        public Timer CreateOneTimeFrameTimer(int frameInterval, Action callback, bool autoStart = true)
        {
            Timer timer = GetTimerFromPool();
            timer.Initialize(frameInterval, callback, autoStart, false);
            timers.Add(timer);
            return timer;
        }

        /// <summary>
        /// 创建永久秒计时器
        /// </summary>
        /// <param name="interval">秒数</param>
        /// <param name="callback">回调方法</param>
        /// <param name="autoStart">是否自动启动</param>
        /// <param name="execImmediately">是否立即执行一次</param>
        public Timer CreateRepeatingTimer(float interval, Action callback, bool autoStart = true,
            bool execImmediately = false)
        {
            Timer timer = GetTimerFromPool();
            timer.Initialize(interval, callback, autoStart, true, execImmediately);
            timers.Add(timer);
            return timer;
        }

        /// <summary>
        /// 创建永久帧计时器
        /// </summary>
        /// <param name="frameInterval">帧数</param>
        /// <param name="callback">回调方法</param>
        /// <param name="autoStart">是否自动启动</param>
        /// <param name="execImmediately">是否立即执行一次</param>
        public Timer CreateRepeatingFrameTimer(int frameInterval, Action callback, bool autoStart = true,
            bool execImmediately = false)
        {
            Timer timer = GetTimerFromPool();
            timer.Initialize(frameInterval, callback, autoStart, true, execImmediately);
            timers.Add(timer);
            return timer;
        }

        /// <summary>
        /// 手动停止计时器，并立即从管理列表中移除回收
        /// </summary>
        public void StopTimer(Timer timer)
        {
            if (timer == null) return;
            if (timers.Contains(timer))
            {
                timers.Remove(timer);
                timer.Stop();
                ReturnTimerToPool(timer);
            }
        }

        private Timer GetTimerFromPool()
        {
            if (timerPool.Count > 0)
            {
                return timerPool.Pop();
            }

            return new Timer();
        }

        public void ReturnTimerToPool(Timer timer)
        {
            timer.Reset();
            timerPool.Push(timer);
        }
    }

    public class Timer
    {
        private float duration; // 秒计时器的原始时长
        private float time; // 当前剩余秒数
        private int frameInterval; // 帧计时器的原始帧数
        private int frameCount; // 当前剩余帧数
        private Action callback;
        private bool isRepeating;
        private bool isActive;
        private bool execImmediately;
        private bool isFrameTimer;

        public bool IsActive => isActive;

        /// <summary>
        /// 秒计时器初始化方法
        /// </summary>
        public void Initialize(float time, Action callback, bool autoStart, bool isRepeating,
            bool execImmediately = false)
        {
            this.duration = time;
            this.time = time;
            this.callback = callback;
            this.isRepeating = isRepeating;
            this.execImmediately = execImmediately;
            this.isFrameTimer = false;
            this.frameCount = 0;
            this.isActive = autoStart;

            if (this.execImmediately)
            {
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 帧计时器初始化方法
        /// </summary>
        public void Initialize(int frameInterval, Action callback, bool autoStart, bool isRepeating,
            bool execImmediately = false)
        {
            this.frameInterval = frameInterval;
            this.frameCount = frameInterval;
            this.callback = callback;
            this.isRepeating = isRepeating;
            this.execImmediately = execImmediately;
            this.isFrameTimer = true;
            this.duration = 0;
            this.isActive = autoStart;

            if (execImmediately)
            {
                callback?.Invoke();
            }
        }

        /// <summary>
        /// 每帧更新计时器状态
        /// </summary>
        public void UpdateTime(float deltaTime)
        {
            if (isFrameTimer)
            {
                // 帧计时器，每帧减少1
                frameCount--;
                if (frameCount <= 0)
                {
                    callback?.Invoke();
                    if (isRepeating)
                    {
                        frameCount = frameInterval;
                    }
                    else
                    {
                        isActive = false;
                    }
                }
            }
            else
            {
                time -= deltaTime;
                if (time <= 0)
                {
                    callback?.Invoke();
                    if (isRepeating)
                    {
                        // 为避免累计误差，将多余时间累计
                        time += duration;
                    }
                    else
                    {
                        isActive = false;
                    }
                }
            }
        }

        /// <summary>
        /// 手动停止计时器
        /// </summary>
        public void Stop()
        {
            isActive = false;
        }

        /// <summary>
        /// 重置计时器，方便回收再利用
        /// </summary>
        public void Reset()
        {
            duration = 0;
            time = 0;
            frameInterval = 0;
            frameCount = 0;
            callback = null;
            isRepeating = false;
            isActive = false;
            execImmediately = false;
            isFrameTimer = false;
        }
    }
}