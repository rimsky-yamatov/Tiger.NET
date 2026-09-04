using System;

namespace Tiger.NET
{
    public class CompilerOptions
    {
        public string SourceFilePath { get; set; } = "";
        public string OutputFilePath { get; set; } = "output.exe";
        public OutputType TargetType { get; set; } = OutputType.ConsoleApplication;

        // SingleFile / SelfContained プロパティを追加
        public bool IsSingleFile { get; set; } = false;
        public bool IsSelfContained { get; set; } = false;

        // .NET バージョン指定 (デフォルト: net10.0)
        public string TargetFramework { get; set; } = "net10.0";
    }
}