using UnityEditor;
using UnityEngine;

namespace Core.Editor.LubanConfigGenerator
{
    public class LubanConfigGenerator : EditorWindow
    {
        [MenuItem("Luban/生成Luban配置")]
        private static void ShowWindow()
        {
            RunLubanGenScript();
        }

        private static void RunLubanGenScript()
        {
            string projectPath = Application.dataPath;
            string lubanDir = System.IO.Path.Combine(System.IO.Directory.GetParent(projectPath).FullName, "Luban");
            string scriptFile;
            string platform;

#if UNITY_EDITOR_WIN
            platform = "Windows";
            scriptFile = System.IO.Path.Combine(lubanDir, "gen.bat");
#elif UNITY_EDITOR_OSX
            platform = "macOS";
            scriptFile = System.IO.Path.Combine(lubanDir, "gen.sh");
#else
            Debug.LogError("当前平台暂不支持Luban配置生成");
            return;
#endif

            if (!System.IO.File.Exists(scriptFile))
            {
                Debug.LogError($"未找到Luban生成脚本: {scriptFile}");
                return;
            }

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = scriptFile;
            process.StartInfo.WorkingDirectory = lubanDir;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (_, args) => { if (!string.IsNullOrEmpty(args.Data)) Debug.Log($"[Luban] {args.Data}"); };
            process.ErrorDataReceived += (_, args) => { if (!string.IsNullOrEmpty(args.Data)) Debug.LogError($"[Luban] {args.Data}"); };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                Debug.Log($"Luban配置生成完成（平台: {platform}）");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"执行Luban生成脚本失败: {ex.Message}");
            }
        }
    }
}