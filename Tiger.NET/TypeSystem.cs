using System;
using System.Collections.Generic;
using System.Linq;

namespace Tiger.NET
{
    public enum TigerTypeKind
    {
        Int,
        String,
        Bool,
        Void,
        Array,
        Struct
    }

    public sealed class TigerType : IEquatable<TigerType>
    {
        public TigerTypeKind Kind { get; }

        public TigerType? ElementType { get; }

        public string? StructName { get; }

        private TigerType(
            TigerTypeKind kind,
            TigerType? elementType = null,
            string? structName = null)
        {
            Kind = kind;
            ElementType = elementType;
            StructName = structName;
        }

        public static readonly TigerType Int =
            new TigerType(TigerTypeKind.Int);

        public static readonly TigerType String =
            new TigerType(TigerTypeKind.String);

        public static readonly TigerType Bool =
            new TigerType(TigerTypeKind.Bool);

        public static readonly TigerType Void =
            new TigerType(TigerTypeKind.Void);

        public static TigerType Array(
            TigerType elementType)
        {
            if (elementType == null)
                throw new ArgumentNullException(
                    nameof(elementType));

            return new TigerType(
                TigerTypeKind.Array,
                elementType: elementType);
        }

        public static TigerType ArrayOf(
            TigerType elementType)
        {
            return Array(elementType);
        }

        public static TigerType Struct(
            string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Struct name cannot be empty.",
                    nameof(name));

            return new TigerType(
                TigerTypeKind.Struct,
                structName: name);
        }

        public bool IsArray =>
            Kind == TigerTypeKind.Array;

        public string Name =>
            StructName ?? "";

        public bool Equals(
            TigerType? other)
        {
            if (other == null)
                return false;

            if (Kind != other.Kind)
                return false;

            return Kind switch
            {
                TigerTypeKind.Array =>
                    ElementType!.Equals(
                        other.ElementType),

                TigerTypeKind.Struct =>
                    string.Equals(
                        StructName,
                        other.StructName,
                        StringComparison.Ordinal),

                _ => true
            };
        }

        public override bool Equals(
            object? obj)
        {
            return obj is TigerType other &&
                   Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Kind,
                ElementType,
                StructName);
        }

        public override string ToString()
        {
            return Kind switch
            {
                TigerTypeKind.Int =>
                    "int",

                TigerTypeKind.String =>
                    "string",

                TigerTypeKind.Bool =>
                    "bool",

                TigerTypeKind.Void =>
                    "void",

                TigerTypeKind.Array =>
                    $"{ElementType}[]",

                TigerTypeKind.Struct =>
                    StructName ?? "struct",

                _ =>
                    "unknown"
            };
        }
    }

    public sealed class TigerFunction
    {
        public string Name { get; }

        public IReadOnlyList<TigerType> Parameters { get; }

        public TigerType ReturnType { get; }

        public TigerFunction(
            string name,
            IEnumerable<TigerType> parameters,
            TigerType returnType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Function name cannot be empty.",
                    nameof(name));

            Name = name;

            Parameters =
                parameters?.ToList()
                ?? throw new ArgumentNullException(
                    nameof(parameters));

            ReturnType =
                returnType
                ?? throw new ArgumentNullException(
                    nameof(returnType));
        }
    }

    public sealed class TigerStruct
    {
        public string Name { get; }

        public Dictionary<string, TigerType> Fields { get; }

        public TigerStruct(
            string name,
            Dictionary<string, TigerType> fields)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Struct name cannot be empty.",
                    nameof(name));

            Name = name;

            Fields =
                new Dictionary<string, TigerType>(
                    fields
                    ?? throw new ArgumentNullException(
                        nameof(fields)),
                    StringComparer.Ordinal);
        }

        public bool HasField(
            string name)
        {
            return Fields.ContainsKey(name);
        }

        public TigerType GetField(
            string name)
        {
            if (!Fields.TryGetValue(
                    name,
                    out var type))
            {
                throw new Exception(
                    $"Unknown field '{name}' in struct '{Name}'.");
            }

            return type;
        }
    }

    public sealed class Scope
    {
        public Dictionary<string, TigerType> Variables { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, TigerFunction> Functions { get; } =
            new(StringComparer.Ordinal);

        public Dictionary<string, TigerStruct> Structs { get; } =
            new(StringComparer.Ordinal);
    }

    public sealed class ScopeStack
    {
        private readonly Stack<Scope> _scopes =
            new();

        public ScopeStack()
        {
            Push();
        }

        public Scope Current =>
            _scopes.Peek();

        public int Depth =>
            _scopes.Count;

        public void Push()
        {
            _scopes.Push(
                new Scope());
        }

        public void Pop()
        {
            if (_scopes.Count <= 1)
            {
                throw new InvalidOperationException(
                    "Cannot pop the global scope.");
            }

            _scopes.Pop();
        }

        public void DeclareVariable(
            string name,
            TigerType type)
        {
            var scope =
                Current.Variables;

            if (scope.ContainsKey(name))
            {
                throw new Exception(
                    $"Variable '{name}' is already declared in this scope.");
            }

            scope[name] = type;
        }

        public void DeclareFunction(
            string name,
            TigerFunction function)
        {
            var scope =
                Current.Functions;

            if (scope.ContainsKey(name))
            {
                throw new Exception(
                    $"Function '{name}' is already declared in this scope.");
            }

            scope[name] = function;
        }

        public void DeclareFunction(
            TigerFunction function)
        {
            DeclareFunction(
                function.Name,
                function);
        }

        public void DeclareStruct(
            string name,
            TigerStruct structure)
        {
            var scope =
                Current.Structs;

            if (scope.ContainsKey(name))
            {
                throw new Exception(
                    $"Struct '{name}' is already declared in this scope.");
            }

            scope[name] = structure;
        }

        public void DeclareStruct(
            TigerStruct structure)
        {
            DeclareStruct(
                structure.Name,
                structure);
        }

        public TigerType? LookupVariable(
            string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Variables.TryGetValue(
                        name,
                        out var type))
                {
                    return type;
                }
            }

            return null;
        }

        public TigerFunction? LookupFunction(
            string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Functions.TryGetValue(
                        name,
                        out var function))
                {
                    return function;
                }
            }

            return null;
        }

        public TigerStruct? LookupStruct(
            string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.Structs.TryGetValue(
                        name,
                        out var structure))
                {
                    return structure;
                }
            }

            return null;
        }

        public bool ContainsVariable(
            string name)
        {
            return LookupVariable(name) != null;
        }

        public bool ContainsFunction(
            string name)
        {
            return LookupFunction(name) != null;
        }

        public bool ContainsStruct(
            string name)
        {
            return LookupStruct(name) != null;
        }
    }
}