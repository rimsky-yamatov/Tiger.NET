using System.Collections.Generic;

namespace Tiger.NET
{
    public static class StandardLibrary
    {
        public static readonly List<TigerFunction> Functions = new()
        {
            new TigerFunction(
                "print",
                new List<TigerType> { TigerType.String },
                TigerType.Void),

            new TigerFunction(
                "printline",
                new List<TigerType> { TigerType.String },
                TigerType.Void),

            new TigerFunction(
                "printint",
                new List<TigerType> { TigerType.Int },
                TigerType.Void),

            new TigerFunction(
                "printbool",
                new List<TigerType> { TigerType.Bool },
                TigerType.Void),

            new TigerFunction(
                "flush",
                new List<TigerType>(),
                TigerType.Void),

            new TigerFunction(
                "getchar",
                new List<TigerType>(),
                TigerType.String),

            new TigerFunction(
                "ord",
                new List<TigerType> { TigerType.String },
                TigerType.Int),

            new TigerFunction(
                "chr",
                new List<TigerType> { TigerType.Int },
                TigerType.String),

            new TigerFunction(
                "size",
                new List<TigerType> { TigerType.String },
                TigerType.Int),

            new TigerFunction(
                "substring",
                new List<TigerType>
                {
                    TigerType.String,
                    TigerType.Int,
                    TigerType.Int
                },
                TigerType.String),

            new TigerFunction(
                "concat",
                new List<TigerType>
                {
                    TigerType.String,
                    TigerType.String
                },
                TigerType.String),

            new TigerFunction(
                "not",
                new List<TigerType> { TigerType.Bool },
                TigerType.Bool),

            new TigerFunction(
                "exit",
                new List<TigerType> { TigerType.Int },
                TigerType.Void)
        };
    }
}