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

        public static TigerType Array(TigerType elementType)
        {
            if (elementType == null)
                throw new ArgumentNullException(nameof(elementType));

            return new TigerType(
                TigerTypeKind.Array,
                elementType: elementType);
        }

        public static TigerType Struct(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(
                    "Struct name cannot be empty.",
                    nameof(name));

            return new TigerType(
                TigerTypeKind.Struct,
                structName: name);
        }

        public bool Equals(TigerType? other)
        {
            if (other == null)
                return false;

            if (Kind != other.Kind)
                return false;

            return Kind switch
            {
                TigerTypeKind.Array =>
                    ElementType!.Equals(other.ElementType),

                TigerTypeKind.Struct =>
                    string.Equals(
                        StructName,
                        other.StructName,
                        StringComparison.Ordinal),

                _ => true
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is TigerType other && Equals(other);
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
                TigerTypeKind.Int => "int",
                TigerTypeKind.String => "string",
                TigerTypeKind.Bool => "bool",
                TigerTypeKind.Void => "void",

                TigerTypeKind.Array =>
                    $"{ElementType}[]",

                TigerTypeKind.Struct =>
                    StructName ?? "struct",

                _ => "unknown"
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

            Parameters = parameters?.ToList()
                ?? throw new ArgumentNullException(nameof(parameters));

            ReturnType = returnType
                ?? throw new ArgumentNullException(nameof(returnType));
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

            Fields = new Dictionary<string, TigerType>(
                fields ?? throw new ArgumentNullException(nameof(fields)),
                StringComparer.Ordinal);
        }

        public bool HasField(string name)
        {
            return Fields.ContainsKey(name);
        }

        public TigerType GetField(string name)
        {
            if (!Fields.TryGetValue(name, out var type))
            {
                throw new Exception(
                    $"Unknown field '{name}' in struct '{Name}'.");
            }

            return type;
        }
    }

    public sealed class ScopeStack
    {
        private readonly Stack<
            Dictionary<string, TigerType>
        > _variableScopes = new();

        private readonly Stack<
            Dictionary<string, TigerFunction>
        > _functionScopes = new();

        private readonly Stack<
            Dictionary<string, TigerStruct>
        > _structScopes = new();

        public ScopeStack()
        {
            Push();
        }

        public int Depth =>
            _variableScopes.Count;

        public void Push()
        {
            _variableScopes.Push(
                new Dictionary<string, TigerType>(
                    StringComparer.Ordinal));

            _functionScopes.Push(
                new Dictionary<string, TigerFunction>(
                    StringComparer.Ordinal));

            _structScopes.Push(
                new Dictionary<string, TigerStruct>(
                    StringComparer.Ordinal));
        }

        public void Pop()
        {
            if (_variableScopes.Count <= 1)
            {
                throw new InvalidOperationException(
                    "Cannot pop the global scope.");
            }

            _variableScopes.Pop();
            _functionScopes.Pop();
            _structScopes.Pop();
        }

        public void DeclareVariable(
            string name,
            TigerType type)
        {
            var scope = _variableScopes.Peek();

            if (scope.ContainsKey(name))
            {
                throw new Exception(
                    $"Variable '{name}' is already declared in this scope.");
            }

            scope[name] = type;
        }

        public void DeclareFunction(
            TigerFunction function)
        {
            var scope = _functionScopes.Peek();

            if (scope.ContainsKey(function.Name))
            {
                throw new Exception(
                    $"Function '{function.Name}' is already declared in this scope.");
            }

            scope[function.Name] = function;
        }

        public void DeclareStruct(
            TigerStruct structure)
        {
            var scope = _structScopes.Peek();

            if (scope.ContainsKey(structure.Name))
            {
                throw new Exception(
                    $"Struct '{structure.Name}' is already declared in this scope.");
            }

            scope[structure.Name] = structure;
        }

        public TigerType? LookupVariable(
            string name)
        {
            foreach (var scope in _variableScopes)
            {
                if (scope.TryGetValue(name, out var type))
                    return type;
            }

            return null;
        }

        public TigerFunction? LookupFunction(
            string name)
        {
            foreach (var scope in _functionScopes)
            {
                if (scope.TryGetValue(name, out var function))
                    return function;
            }

            return null;
        }

        public TigerStruct? LookupStruct(
            string name)
        {
            foreach (var scope in _structScopes)
            {
                if (scope.TryGetValue(name, out var structure))
                    return structure;
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