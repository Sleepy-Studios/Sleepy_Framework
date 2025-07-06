using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;


namespace Platform.Editor
{
    public class ProtoUtils
    {
        /// <summary>
        /// 对生成的Cs文件进一步处理
        /// </summary>
        /// <param name="csPath">Cs文件路径</param>
        /// <param name="togglePath">保存Dictionary<string, bool>的路径</param>
        public static void Convert(string csPath,string togglePath)
        {
            if (!File.Exists(togglePath))
            {
                File.WriteAllText(csPath, File.ReadAllText(csPath));
                return;
            }
            string input =  File.ReadAllText(csPath);
            Dictionary<string, bool> toggles = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(togglePath));
            List<string> propers = toggles.Where(pair => !pair.Value).Select(pair => pair.Key.Replace("_", "")).ToList();
            // propers.Add("Token");
            // propers.Add("AccountId");
            Debug.Log("这些属性将会生成简单类型："+String.Join(",",propers));
            string news = OnConvert(input, propers);
            File.WriteAllText(csPath,news);
        }
        
        private static string OnConvert(string input,List<string> propers)
        {
            string outPuts = input;
            foreach (string proper in propers)
            {
                string[] args = proper.Split(FileListWindow.SPLITE);
                string oldText = FindString(outPuts, args[0]+" :", "FillOtherCommands");
                string newText = oldText;
                
                string relva = "";
                //PropertyValue
                if (MatchPropertyValue(newText, args[1],ref relva))
                {
                    newText = FillPropertyValue(newText,relva);
                    newText = ReplaceValue(newText, relva);
                    newText = ReplaceBindMethod(newText, relva);
                    newText = ReplaceFillPropeMethod(newText, relva);
                }
                //Collection
                else if (MatchSimpleCollection(newText, args[1],ref relva))
                {
                    newText = FillSimpleCollection(newText, relva);
                    newText = ReplaceColBindMethod(newText, relva);
                    newText = ReplaceFillPropeMethod(newText, relva);
                }

                outPuts = outPuts.Replace(oldText, newText);
            }

            return outPuts;
        }

        /// <summary>
        /// 是否匹配到了PropertyValue的属性
        /// </summary>
        private static bool MatchPropertyValue(string input,string proper,ref string rel)
        {
            string pattern = $@"PropertyValue<(.+)> {proper};";
            Match m = Regex.Match(input, pattern, RegexOptions.IgnoreCase); // 忽略大小写
            if (m.Success)
            {
                rel = m.Value.Split(" ")[1].Replace(";","");;
                Debug.Log($"match {rel}");
            }
            return m.Success;
        }
        

        /// <summary>
        /// 字段PropertyValue改为简单类型
        /// </summary>
        private static string FillPropertyValue(string input,string proper)
        {
            string pattern = $@"PropertyValue<(.+)> {proper};";
            // if (Regex.IsMatch(input, pattern))
            // {
            //     Debug.Log($"match {proper}");
            // }
            string replacement = $"$1 {proper};";
            string output = Regex.Replace(input, pattern, replacement);
            return output;
        }
        
        /// <summary>
        /// 去除字段PropertyValue。Value取值方式
        /// </summary>
        private static string ReplaceValue(string input,string proper)
        {
            string pattern = $@"(?<![a-zA-Z]){proper}\.Value";
            string output = Regex.Replace(input, pattern, proper);
            return output;
        }
        
        /// <summary>
        ///   注释new PropertyValue(proper)
        /// </summary>
        private static string ReplaceBindMethod(string input,string proper)
        {
            string output = input.Replace($" {proper} = new PropertyValue", $" //{proper} = new PropertyValue");
            return output;
        }
        
        /// <summary>
        ///   注释new ViewModelPropertyInfo(proper)
        /// </summary>
        private static string ReplaceFillPropeMethod(string input,string proper)
        {
            string output = input.Replace($" list.Add(new ViewModelPropertyInfo({proper}));", $" //list.Add(new ViewModelPropertyInfo({proper}));");
            return output;
        }
        
        /// <summary>
        /// 是否匹配到了ModelCollection的字段
        /// </summary>
        private static bool MatchSimpleCollection(string input,string proper,ref string rel)
        {
            string pattern = $@"ModelCollection<(.+)> {proper};";
            Match m = Regex.Match(input, pattern, RegexOptions.IgnoreCase); // 忽略大小写
            if (m.Success)
            {
                rel = m.Value.Split(" ")[1].Replace(";","");
                Debug.Log($"match {rel}");
            }
            return m.Success;
        }
        
        
        /// <summary>
        /// 将ModelCollection类型替换为SimpleCollection类型
        /// </summary>
        private static string FillSimpleCollection(string input,string proper)
        {
            string pattern = $@"ModelCollection<(.+)> {proper};";
            // if (Regex.IsMatch(input, pattern))
            // {
            //     Debug.Log($"match {proper}");
            //     success = true;
            // }
            string replacement = $"SimpleCollection<$1> {proper};";
            string output = Regex.Replace(input, pattern, replacement);
            
            return output;
        }
        
        /// <summary>
        /// 将new ModelCollection字符部分替换为SimpleCollection
        /// </summary>
        private static string ReplaceColBindMethod(string input,string proper)
        {
            string pattern = $"new ModelCollection<(.+)>(.+)\"{proper}\"";
            if (Regex.IsMatch(input, pattern))
            {
                Debug.Log($"match collection {proper}");
            }
            string replacement = $"new SimpleCollection<$1>(";
            string output = Regex.Replace(input, pattern, replacement);
            return output;
        }
        
        public static string FindString(string input, string start, string end)
        {
            int startIndex = input.IndexOf(start);
            if (startIndex == -1)
            {
                return null;
            }

            int endIndex = input.IndexOf(end, startIndex + start.Length);
            if (endIndex == -1)
            {
                return null;
            }

            return input.Substring(startIndex, endIndex - startIndex + end.Length);
        }
    }
}