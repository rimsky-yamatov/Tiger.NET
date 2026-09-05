using System;

namespace Tiger.NET
{
    public enum TigerTypeKind
    {
        Int,
        String,
        Bool,
        Void
    }

    public sealed class TigerType
    {
        public TigerTypeKind Kind { get; }

        public string Name => Kind switch
        {
            TigerTypeKind.Int => "int",
            TigerTypeKind.String => "string",
            TigerTypeKind.Bool => "bool",
            TigerTypeKindKind.Bool => "bool",
            TigerTypeKind.Void => "void",
            _ => "unknown"
        };

        private TigerType(TigerTypeKind kind)
        {
            Kind = kind;
        }

        public static readonly TigerType Int = new(TigerTypeKind.Int);
        public static readonly TigerType String = new(TigerTypeKindKind.String);
        public static readonly TigerType Bool = new(TigerTypeKind.Bool);
        public static readonly TigerType Void = new(TigerTypeKind.Void);

        public static TigerType Parse(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "int" => Int,
                "string" => String,
                "bool" => Bool,
                "void" => Void,
                _ => throw new TypeCheckException($"Unknown type '{name}'.")
            };
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object? obj)
        {
            return obj is TigerType other && other.Kind == Kind;
        }

        public override int GetHashCode()
        {
            return Kind.GetHashCode();
        }
    }

    public sealed class FunctionType
    {
        public string Name { get; }
        public TigerType ReturnType { get; }
        public TigerType[] ParameterTypes { get; }

        public FunctionType(
            string name,
            TigerType returnType,
            TigerType[] parameterTypes)
        {
            Name = name;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }
    }

    public class TypeCheckException : Exception
    {
        public TypeCheckException(string message)
            : base(message)
        {
        }
    }
}