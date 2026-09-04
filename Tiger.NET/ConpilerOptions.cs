using System;

namespace Tiger.NET
{
    public enum OutputType
    {
        ConsoleApplication,
        WindowsApplication,
        Dll
    }

    public class CompilerOptions
    {
        public string SourceFilePath { get; set; } = "";
        public string OutputFilePath { get; set; } = "output.exe";
        public OutputType TargetType { get; set; } = OutputType.ConsoleApplication;
        public bool SingleFile { get; set; } = false;

        // .NET バージョン指定 (デフォルト: net10.0)
        public string TargetFramework { get; set; } = "net10.0";
    }
}