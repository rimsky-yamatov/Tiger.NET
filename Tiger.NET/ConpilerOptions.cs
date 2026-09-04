using System;

namespace Tiger.NET
{
    /// <summary>
    /// 出力バイナリの形式を指定する列挙型
    /// </summary>
    public enum OutputType
    {
        ConsoleApplication,
        WindowsApplication,
        Dll
    }

    /// <summary>
    /// Tiger.NET コンパイラのオプション・設定クラス
    /// </summary>
    public class CompilerOptions
    {
        /// <summary>
        /// ソースコードファイルパス (.tig)
        /// </summary>
        public string SourceFilePath { get; set; } = "";

        /// <summary>
        /// 出力ファイルパス (デフォルト: output.exe)
        /// </summary>
        public string OutputFilePath { get; set; } = "output.exe";

        /// <summary>
        /// ターゲット出力タイプ (コンソールアプリ / Winアプリ / DLL)
        /// </summary>
        public OutputType TargetType { get; set; } = OutputType.ConsoleApplication;

        /// <summary>
        /// 単一ファイル形式で生成するかどうか
        /// </summary>
        public bool IsSingleFile { get; set; } = false;

        /// <summary>
        /// 自己完結型 (Self-Contained) ランタイムを含めて出力するかどうか
        /// </summary>
        public bool IsSelfContained { get; set; } = false;

        /// <summary>
        /// ターゲットフレームワークのバージョン指定 (例: net9.0, net10.0)
        /// デフォルト: net10.0
        /// </summary>
        public string TargetFramework { get; set; } = "net10.0";
    }
}