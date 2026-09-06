namespace Tiger.NET
{
    public enum OutputType
    {
        ConsoleApplication,
        WindowsApplication,
        Dll
    }

    public enum OptimizationLevelKind
    {
        None,
        Debug,
        Release
    }

    public class CompilerOptions
    {
        public string SourceFilePath { get; set; } = "";

        public string OutputFilePath { get; set; }
            = "output.exe";

        public OutputType TargetType { get; set; }
            = OutputType.ConsoleApplication;

        public bool IsSingleFile { get; set; }

        public bool IsSelfContained { get; set; }

        public string TargetFramework { get; set; }
            = "net10.0";

        public OptimizationLevelKind OptimizationLevel { get; set; }
            = OptimizationLevelKind.Release;

        public bool IncludeDebugInfo { get; set; }

        public bool VerboseOutput { get; set; }
    }
}