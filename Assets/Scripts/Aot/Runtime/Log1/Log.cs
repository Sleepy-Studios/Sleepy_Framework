using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Aot.Runtime.Log
{
    public enum LogLevel
    {
        All, // 显示所有日志
        None, // 不显示日志
        OnlyError, // 仅显示错误日志
        ErrorAndWarning // 显示错误和警告日志
    }

    public static class Log
    {
        // 配置项
        private static LogLevel currentLogLevel = LogLevel.All; // 默认显示所有日志
        private static bool showStackTrace = true;
        private static bool isCleanLogCache = true;
        private static string logFilePath;
        private static int maxLogCount; // 默认最大日志条数为1000
        private static List<string> logList = new List<string>(); // 用于缓存日志

        private static bool isInitialized = false;

        /// StringBuilder 对象池
        private static readonly Stack<StringBuilder> StringBuilderPool = new Stack<StringBuilder>();
        private const int PoolSize = 10; // 池的大小
        
        

        /// <summary>
        /// 初始化日志系统，生成日志文件并记录设备信息
        /// </summary>
        /// <param name="logLevel">日志级别</param>
        /// <param name="maxLogCount">最大日志条数</param>
        /// <param name="showStack">是否显示堆栈信息</param>
        /// <param name="cleanCache">是否清理日志缓存</param>
        public static void Init(LogLevel logLevel = LogLevel.All, int maxLogCount = 1000, bool showStack = true,
            bool cleanCache = true)
        {
            if (isInitialized)
            {
                return;
            }

            // 设置日志级别、最大日志条数、是否显示堆栈信息、是否清理日志缓存
            currentLogLevel = logLevel;
            Log.maxLogCount = maxLogCount;
            showStackTrace = showStack;
            isCleanLogCache = cleanCache;

            // 如果需要清理日志缓存，清空 Log 文件夹中的所有日志文件
            if (isCleanLogCache)
            {
                CleanLogDirectory();
            }

            // 设置日志文件保存路径
#if UNITY_ANDROID
            /// 安卓平台：使用 Application.persistentDataPath
            string logDirectory = Path.Combine(Application.persistentDataPath, "Log");
#else
            // PC端及其他平台：使用 Application.dataPath 上层目录
            string logDirectory = Path.Combine(Application.dataPath, "../Log");
#endif
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            logFilePath = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            // 获取设备信息
            string deviceInfo =
                $"设备信息: {SystemInfo.deviceModel}, {SystemInfo.operatingSystem}, {SystemInfo.deviceType}, {SystemInfo.graphicsDeviceName}";

            // 写入设备信息到日志文件
            WriteLogToFile($"日志文件初始化 - {DateTime.Now}");
            WriteLogToFile(deviceInfo);

            // 输出到控制台
            Debug.Log("日志系统初始化完成");

            isInitialized = true;

            // 初始化对象池
            for (int i = 0; i < PoolSize; i++)
            {
                StringBuilderPool.Push(new StringBuilder());
            }
        }

        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="logLevel">日志级别</param>
        public static void SetLogLevel(LogLevel logLevel)
        {
            currentLogLevel = logLevel;
        }

        /// <summary>
        /// 设置最大日志条数，超过则删除最旧的日志
        /// </summary>
        public static void SetMaxLogCount(int count)
        {
            maxLogCount = count;
        }

        /// <summary>
        /// 打印普通日志信息
        /// </summary>
        /// <param name="msg">要打印的日志信息</param>
        /// <param name="showStack">保存日志是否显示堆栈信息</param>
        public static void Info(object msg, bool showStack = true)
        {
            if (currentLogLevel == LogLevel.All || currentLogLevel == LogLevel.ErrorAndWarning)
            {
                StringBuilder sb = GetStringBuilder(); // 从对象池获取 StringBuilder
                sb.Append($"[Info] {msg}");
                if (showStack && showStackTrace)
                {
                    sb.Append(GetStackTrace());
                }

                WriteLogToFile(sb.ToString());
                Debug.Log($"<color=white>[Info]</color> {msg}");

                ReturnStringBuilder(sb); // 将 StringBuilder 返回到对象池
            }
        }

        /// <summary>
        /// 打印警告信息
        /// </summary>
        /// <param name="msg">要打印的日志信息</param>
        /// <param name="showStack">保存日志是否显示堆栈信息</param>
        public static void Warning(object msg, bool showStack = true)
        {
            if (currentLogLevel == LogLevel.All || currentLogLevel == LogLevel.ErrorAndWarning)
            {
                StringBuilder sb = GetStringBuilder(); // 从对象池获取 StringBuilder
                sb.Append($"[Warning] {msg}");
                if (showStack && showStackTrace)
                {
                    sb.Append(GetStackTrace());
                }

                WriteLogToFile(sb.ToString());
                Debug.LogWarning($"<color=yellow>[Warning]</color> {msg}");

                ReturnStringBuilder(sb); // 将 StringBuilder 返回到对象池
            }
        }

        /// <summary>
        /// 打印错误信息
        /// </summary>
        /// <param name="msg">要打印的日志信息</param>
        /// <param name="showStack">保存日志是否显示堆栈信息</param>
        public static void Error(object msg, bool showStack = true)
        {
            if (currentLogLevel == LogLevel.All || currentLogLevel == LogLevel.OnlyError ||
                currentLogLevel == LogLevel.ErrorAndWarning)
            {
                StringBuilder sb = GetStringBuilder(); // 从对象池获取 StringBuilder
                sb.Append($"[Error] {msg}");
                if (showStack && showStackTrace)
                {
                    sb.Append(GetStackTrace());
                }

                WriteLogToFile(sb.ToString());
                Debug.LogError($"<color=red>[Error]</color> {msg}");

                ReturnStringBuilder(sb); // 将 StringBuilder 返回到对象池
            }
            else
            {
                // 如果错误日志被禁用，依然打印前20条日志方便追踪
                if (logList.Count > 0)
                {
                    int count = Math.Min(20, logList.Count);
                    for (int i = 0; i < count; i++)
                    {
                        Debug.LogError(logList[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 获取堆栈信息
        /// </summary>
        private static string GetStackTrace()
        {
            return Environment.NewLine + Environment.NewLine + UnityEngine.StackTraceUtility.ExtractStackTrace();
        }


        /// <summary>
        /// 将日志写入文件并保持最大日志条数
        /// </summary>
        /// <param name="log">要写入的日志信息</param>
        private static void WriteLogToFile(string log)
        {
            // 如果设置了清理缓存，则删除最旧的日志
            if (isCleanLogCache && logList.Count >= maxLogCount)
            {
                logList.RemoveAt(0); // 删除最早的日志
            }

            // 将日志添加到缓存列表
            logList.Add(log);

            // 写入文件
            File.AppendAllText(logFilePath, log + Environment.NewLine);
        }

        /// <summary>
        /// 清理日志目录下的所有日志文件
        /// </summary>
        private static void CleanLogDirectory()
        {
#if UNITY_ANDROID
            /// 安卓平台：使用 Application.persistentDataPath
            string logDirectory = Path.Combine(Application.persistentDataPath, "Log");
#else
            // PC端及其他平台：使用 Application.dataPath 上层目录
            string logDirectory = Path.Combine(Application.dataPath, "../Log");
#endif
            if (Directory.Exists(logDirectory))
            {
                // 获取该目录下所有的日志文件并删除
                string[] files = Directory.GetFiles(logDirectory, "*.txt");
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                Debug.Log("日志文件夹已清空");
            }
        }

        /// <summary>
        /// 从对象池获取一个 StringBuilder 实例
        /// </summary>
        /// <returns>StringBuilder 实例</returns>
        private static StringBuilder GetStringBuilder()
        {
            lock (StringBuilderPool) // 确保线程安全
            {
                return StringBuilderPool.Count > 0 ? StringBuilderPool.Pop() : new StringBuilder();
            }
        }

        /// <summary>
        /// 将 StringBuilder 实例返回到对象池
        /// </summary>
        /// <param name="sb">要返回的 StringBuilder 实例</param>
        private static void ReturnStringBuilder(StringBuilder sb)
        {
            sb.Clear(); // 清空内容以便重用
            lock (StringBuilderPool) // 确保线程安全
            {
                StringBuilderPool.Push(sb);
            }
        }
    }
}