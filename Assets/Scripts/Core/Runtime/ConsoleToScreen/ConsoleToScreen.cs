using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Runtime.ConsoleToScreen
{
    public class ConsoleToScreen : MonoBehaviour
    {
        /// 最大日志行数
        private const int MaxLines = 30;
        /// 单行最大字符数
        private const int MaxLineLength = 120;
        /// 存储所有日志内容
        private string logContent = "";
        /// 日志分行列表
        private readonly List<string> logLines = new();
        /// 字体大小
        [FormerlySerializedAs("fontSize")]
        public int FontSize = 15;

        /// <summary>
        /// 启用脚本时绑定日志回调
        /// </summary>
        void OnEnable()
        {
            Application.logMessageReceived += LogHandler;
        }

        /// <summary>
        /// 禁用脚本时解绑日志回调
        /// </summary>
        void OnDisable()
        {
            Application.logMessageReceived -= LogHandler;
        }

        /// <summary>
        /// 日志回调处理方法
        /// </summary>
        /// <param name="logString">日志内容</param>
        /// <param name="stackTrace">堆栈跟踪信息</param>
        /// <param name="type">日志类型</param>
        public void LogHandler(string logString, string stackTrace, LogType type)
        {
            // 分割日志内容并处理超长行
            foreach (var line in logString.Split('\n'))
            {
                ProcessLogLine(line);
            }

            // 限制日志行数
            if (logLines.Count > MaxLines)
            {
                logLines.RemoveRange(0, logLines.Count - MaxLines);
            }

            // 将日志列表合并为字符串
            logContent = string.Join("\n", logLines);
        }

        /// <summary>
        /// 处理单行日志，拆分超长的日志行
        /// </summary>
        /// <param name="line">单行日志</param>
        private void ProcessLogLine(string line)
        {
            if (line.Length <= MaxLineLength)
            {
                logLines.Add(line);
                return;
            }

            // 拆分超长行
            for (int i = 0; i < line.Length; i += MaxLineLength)
            {
                int remainingLength = Mathf.Min(MaxLineLength, line.Length - i);
                logLines.Add(line.Substring(i, remainingLength));
            }
        }

        /// <summary>
        /// 使用 OnGUI 绘制日志内容到屏幕上
        /// </summary>
        void OnGUI()
        {
            // 动态计算屏幕缩放矩阵，适配不同分辨率
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
            // 定义 GUI 样式
            var guiStyle = new GUIStyle
            {
                fontSize = FontSize,
                normal = { textColor = Color.black }
            };
            // 绘制日志内容
            GUI.Label(new Rect(10, 10, 800, 370), logContent, guiStyle);
        }
    }
}