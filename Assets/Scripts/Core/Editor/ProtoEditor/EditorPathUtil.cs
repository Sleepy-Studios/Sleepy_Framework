using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Platform.Editor
{
    public static class EditorPathUtil
    {
        private static string mRealPackagePath;

        static EditorPathUtil()
        {
            StackTrace stackTrace = new StackTrace(true);
            var frame = stackTrace.GetFrames();
            mRealPackagePath = frame[0].GetFileName().Replace("\\EditorPathUtil.cs", "");
        }

        public static string PackPath { get { return mRealPackagePath; } }
    }
}
