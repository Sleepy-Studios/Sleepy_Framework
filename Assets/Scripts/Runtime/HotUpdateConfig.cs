using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HotUpdateConfig", menuName = "Config/热更程序集配置")]
public class HotUpdateConfig : ScriptableObject
{
    // AOT 配置
    [Header("AOT 源路径")]
    public string AotSourcePath;
    [Header("AOT 优化路径")]
    public string AotStrippedSourcePath;
    [Header("AOT 目标路径")]
    public string AotTargetPath = "Assets/GameRes/Codes/Aot";
    [Header("AOT 文件列表（带.dll后缀）")]
    public List<string> aotFiles = new List<string>();

    // 热更新配置
    [Header("热更新源路径")]
    public string HotUpdateSourcePath;
    [Header("热更新目标路径")]
    public string HotUpdateTargetPath = "Assets/GameRes/Codes/HotUpdate";
    [Header("热更新文件列表（带.dll后缀）")]
    public List<string> hotUpdateFiles = new List<string>();

    // SSH 服务器配置
    [Header("SSH服务器地址")]
    public string SshHost = "20.2.148.139";
    [Header("SSH服务器端口")]
    public int SshPort = 22;
    [Header("SSH登录用户名")]
    public string SshUser = "root";
    [Header("SSH登录密码")]
    public string KeyFilePath = "Assets/GameRes/Config/key";
    [Header("服务器资源根路径")]
    public string ServerBasePath = "/www/wwwroot/20.2.148.139_9000/Test";

    // 本地路径配置
    [Header("本地资源包路径（相对工程目录）")]
    public string LocalBundlePath = "Bundles/StandaloneWindows64/DefaultPackage/2025-03-29-1156";
}