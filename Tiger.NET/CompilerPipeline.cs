using System;
using System.Diagnostics;
using System.IO;

namespace Tiger.NET
{
    public class CompilerPipeline
    {
        private readonly CompilerOptions _options;

        public CompilerPipeline(CompilerOptions options) => _options = options;

        public void Execute()
        {
            if (!File.Exists(_options.SourceFilePath))
            {
                Console.WriteLine($"[Error] Source file not found: {_options.SourceFilePath}");
                return;
            }

            string tigerCode = File.ReadAllText(_options.SourceFilePath);

            // 1. Lexer
            var lexer = new Lexer(tigerCode);
            var tokens = lexer.Tokenize();

            // 2. Parser
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            // 3. Code Generator (C# Emitting under Tiger.NET.Runtime)
            string csCode = CodeGenerator.EmitCSharp(ast);

            // 4. Primary Assembly Compilation
            bool ok = CodeGenerator.CompileToAssembly(csCode, _options);
            if (!ok) return;

            // 5. Advanced Publishing (SelfContained / SingleFile Options)
            if (_options.IsSelfContained || _options.IsSingleFile)
            {
                BuildNativeStandaloneProject(csCode);
            }
        }

        private void BuildNativeStandaloneProject(string csCode)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "TigerNetTemp_" + Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            try
            {
                string csPath = Path.Combine(tempDir, "Program.cs");
                File.WriteAllText(csPath, csCode);

                string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>{(_options.TargetType == OutputType.Dll ? "Library" : "Exe")}</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Tiger.NET.Runtime</RootNamespace>
    <PublishSingleFile>{(_options.IsSingleFile ? "true" : "false")}</PublishSingleFile>
    <SelfContained>{(_options.IsSelfContained ? "true" : "false")}</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishReadyToRun>true</PublishReadyToRun>
  </PropertyGroup>
</Project>";
                File.WriteAllText(Path.Combine(tempDir, "TempProj.csproj"), csprojContent);

                string outDir = Path.GetDirectoryName(Path.GetFullPath(_options.OutputFilePath)) ?? ".";
                string args = $"publish \"{tempDir}/TempProj.csproj\" -c Release -o \"{outDir}\"";

                Console.WriteLine("[Publish] Generating Standalone/SingleFile Executable via Native .NET Engine...");

                var psi = new ProcessStartInfo("dotnet", args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    Console.WriteLine($"[Success] Native Executable Published to Output Path.");
                }
                else
                {
                    Console.WriteLine("[Error] Native Publish Failed:\n" + proc.StandardError.ReadToEnd());
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}