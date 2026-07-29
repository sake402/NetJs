using NetJs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

#if TRANSLATOR
[JsonConverter(typeof(HandleSerializer))]
public struct Handle
{
    public string Expression { get; set; }
    public Handle Or(Handle handle)
    {
        if (ulong.TryParse(Expression, out var e1) && ulong.TryParse(handle.Expression, out var e2))
            return new Handle() { Expression = $"{e1 | e2}" };
        //Number(BigInt(this.$h)|0x200000000n)
        return new Handle() { Expression = $"Number(BigInt({Expression})|{handle.Expression}n)" };
    }
    public Handle ShiftLeft(int n) { return new Handle() { Expression = $"({Expression}) << {n}" }; }
    public static implicit operator string(Handle handle) => handle.Expression;
    public static implicit operator ulong(Handle handle) => ulong.TryParse(handle.Expression, out var v) ? v : 0;
    public static implicit operator Handle(ulong handle) => new Handle() { Expression = handle.ToString() };
    public static implicit operator Handle(string handle) => new Handle() { Expression = handle };
    public override string ToString()
    {
        return Expression;
    }
}
public class HandleSerializer : JsonConverter<Handle>
{
    public override Handle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Handle value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value.Expression ?? "null", skipInputValidation: true);
    }
}
#else
using Handle = ulong;
#endif

namespace NetJs
{
    // --- Enums ---

    [InlineConst]
    [External]
    public enum KnownTypeHandle
    {
        SystemDynamic,
        SystemVoid,
        SystemObject,
        SystemValueType,
        SystemBool,
        SystemSByte,
        SystemByte,
        SystemChar,
        SystemInt16,
        SystemUInt16,
        SystemInt32,
        SystemUint32,
        SystemIntPtr,
        SystemUIntPtr,
        SystemInt64,
        SystemUint64,
        SystemEnum,
        SystemSingle,
        SystemDouble,
        SystemArray,
        SystemString,
        SystemPointer,
        SystemReference,

        GenericType1Placeholder,
        GenericType2Placeholder,
        GenericType3Placeholder,
        GenericType4Placeholder,
        GenericType5Placeholder,
        GenericType6Placeholder,
        GenericType7Placeholder,
        GenericType8Placeholder,
        GenericType9Placeholder,
        GenericType10Placeholder,
        GenericType11Placeholder,
        GenericType12Placeholder,
        GenericType13Placeholder,
        GenericType14Placeholder,
        GenericType15Placeholder,
        GenericType16Placeholder,
        GenericType17Placeholder,
        GenericType18Placeholder,
        GenericType19Placeholder,
        GenericType20Placeholder,
        GenericType21Placeholder,
        GenericType22Placeholder,
        GenericType23Placeholder,
        GenericType24Placeholder,
        GenericType25Placeholder,
        GenericType26Placeholder,
        GenericType27Placeholder,
        GenericType28Placeholder,
        GenericType29Placeholder,
        GenericType30Placeholder,
        GenericType31Placeholder,
        GenericType32Placeholder,
        GenericTypeMaxPlaceholder = GenericType32Placeholder,

        GenericMethodType1Placeholder,
        GenericMethodType2Placeholder,
        GenericMethodType3Placeholder,
        GenericMethodType4Placeholder,
        GenericMethodType5Placeholder,
        GenericMethodType6Placeholder,
        GenericMethodType7Placeholder,
        GenericMethodType8Placeholder,
        GenericMethodType9Placeholder,
        GenericMethodType10Placeholder,
        GenericMethodType11Placeholder,
        GenericMethodType12Placeholder,
        GenericMethodType13Placeholder,
        GenericMethodType14Placeholder,
        GenericMethodType15Placeholder,
        GenericMethodType16Placeholder,
        GenericMethodType17Placeholder,
        GenericMethodType18Placeholder,
        GenericMethodType19Placeholder,
        GenericMethodType20Placeholder,
        GenericMethodType21Placeholder,
        GenericMethodType22Placeholder,
        GenericMethodType23Placeholder,
        GenericMethodType24Placeholder,
        GenericMethodType25Placeholder,
        GenericMethodType26Placeholder,
        GenericMethodType27Placeholder,
        GenericMethodType28Placeholder,
        GenericMethodType29Placeholder,
        GenericMethodType30Placeholder,
        GenericMethodType31Placeholder,
        GenericMethodType32Placeholder,
        GenericMethodTypeMaxPlaceholder = GenericMethodType32Placeholder,

    }

    [InlineConst]
    [External]
    [CLSCompliant(true)]
    public enum TypeHandleFlags : ulong
    {
        //First 16 bits is assembly handle
        //Next 16 bit is type handle
        //Next 16 bit is member handle
        Array = 1UL << 48 //Array flag signifies array of the type 
    }

    [InlineConst]
    [External]
    public enum TypeKindModel
    {
        Unknown = 0,
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
        Array,
        Pointer,
    }

    [Flags]
    [InlineConst]
    [External]
    public enum TypeFlagsModel : uint
    {
        None = 0,
        IsPublic = 1 << 0,
        IsStatic = 1 << 1,
        IsInterface = 1 << 2,
        IsEnum = 1 << 3,
        IsClass = 1 << 4,
        IsAbstract = 1 << 5,
        IsGenericType = 1 << 6,
        IsSealed = 1 << 7,
        IsRecord = 1 << 8,
        IsValueType = 1 << 9,
        IsPrimitive = 1 << 10,
        HasElementType = 1 << 11,
        IsArray = 1 << 12,
        IsInternal = 1 << 13,
        IsByRef = 1 << 14,
        IsPointer = 1 << 15,
        IsNested = 1 << 16,
        IsFlags = 1 << 17,
        IsNestedPublic = 1 << 18,
        IsSerializable = 1 << 19,
        IsStructLayout = 1 << 20,
        IsInlineArray = 1 << 21,
        IsPureStruct = 1 << 22, //A struct whose all members are primitive numeric, safe to use with js DataView
    }

    [Flags]
    [InlineConst]
    [External]
    public enum MemberFlagsModel : uint
    {
        None = 0,
        IsPublic = 1 << 0,
        IsPrivate = 1 << 1,
        IsFamily = 1 << 2, // protected
        IsAssembly = 1 << 3, // internal
        IsFamilyOrAssembly = 1 << 4, // protected internal
        IsStatic = 1 << 5,
        IsFinal = 1 << 6,
        IsVirtual = 1 << 7,
        IsAbstract = 1 << 8,
        IsSpecialName = 1 << 9, // e.g., property get/set methods
        IsHideBySig = 1 << 10,
        IsExtensionMethod = 1 << 11,
        IsAsync = 1 << 12,
        IsOperator = 1 << 13,
        IsIndexer = 1 << 14,
        IsOverride = 1 << 15,
        IsSealed = 1 << 16,
        IsGeneric = 1 << 17,
        HasDefaultValue = 1 << 18,
        ReturnTypeIsCovariantOut = 1 << 19,
        IsAnonymous = 1 << 20,
        IsFamilyAndAssembly = IsFamily | IsAssembly,
    }

    [Flags]
    [InlineConst]
    [External]
    public enum GenericConstraintFlagsModel
    {
        None = 0,
        HasClassConstraint = 1 << 0,
        HasStructConstraint = 1 << 1,
        HasNewConstraint = 1 << 2,
        HasUnmanagedConstraint = 1 << 3,
        HasInConstraint = 1 << 4,
        HasOutConstraint = 1 << 5,
        HasNotNullConstraint = 1 << 6
    }

    [Flags]
    [InlineConst]
    [External]
    public enum ParameterFlagsModel
    {
        None,
        Optional = 1 << 0,
        Out = 1 << 1,
        Ref = 1 << 2,
        In = 1 << 3,
        Params = 1 << 4,
        ContravariantIn = 1 << 5,
        HasDefaultValue = 1 << 6
    }

    [Flags]
    [InlineConst]
    [External]
    public enum AssemblyFlags
    {
        None,
        Entry = 1 << 0
    }

    // --- Core Models ---
    [ObjectLiteral]
    public class AssemblyModel
    {
        [JsonPropertyName("g")][Name("g")] public AssemblyFlags AssemblyFlags { get; set; } = default!;
        [JsonPropertyName("h")][Name("h")] public Handle Handle { get; set; } = default!;
        [JsonPropertyName("f")][Name("f")] public string FullName { get; set; } = default!;
        [JsonPropertyName("v")][Name("v")] public string Version { get; set; } = default!;
        //[JsonPropertyName("n")][Name("n")] public string[] TypeNames { get; set; } = default!;
        //[JsonPropertyName("t")][Name("t")] public TypeModel[]? Types { get; set; }
        [JsonPropertyName("a")][Name("a")] public AttributeModel[]? Attributes { get; set; }
        [JsonPropertyName("m")][Name("m")] public AssemblyManifestModel[]? Manifests { get; set; }
        [JsonPropertyName("r")][Name("r")] public ulong[] ReferencedAssembliesHandle { get; set; } = default!;
        [JsonPropertyName("e")][Name("e")] public Handle Entry { get; set; } = default!;
    }

    [ObjectLiteral]
    public class AssemblyManifestModel
    {
        [JsonPropertyName("n")][Name("n")] public string Name { get; set; } = default!;
        [JsonPropertyName("d")][Name("d")] public string? Data { get; set; } = default!;
        [JsonPropertyName("r")][Name("r")] public object StringResourceData { get; set; } = default!;
    }

    [ObjectLiteral]
    public class TypeModel : MemberModel
    {
        // We can derive this name from fullname at runtime
        //[JsonPropertyName("n")][Name("n")] public string Name { get; set; } = default!;
        //[JsonPropertyName("h")][Name("h")] public ReflectionHandleModel Handle { get; set; }
        //[JsonPropertyName("aqn")][Name("aqn")] public string AssemblyQualifiedName { get; set; } = default!;
        [JsonPropertyName("b")][Name("b")] public Handle? BaseType { get; set; }
        //[JsonPropertyName("d")][Name("d")] public ulong? DeclaringType { get; set; }
        [JsonPropertyName("u")][Name("u")] public Handle UnderlyingType { get; set; }
        //[JsonPropertyName("k")][Name("k")] public TypeKindModel Kind { get; set; }
        [JsonPropertyName("kt")][Name("kt")] public KnownTypeHandle KnownType { get; set; }
        //[JsonPropertyName("fg")][Name("fg")] public new TypeFlagsModel Flags { get; set; }
        //[JsonPropertyName("y")][Name("y")] public TypeAttributes TypeAttributes { get; set; }
        [JsonPropertyName("p")][Name("p")] public PropertyModel[]? Properties { get; set; }
        [JsonPropertyName("m")][Name("m")] public MethodModel[]? Methods { get; set; }
        [JsonPropertyName("l")][Name("l")] public FieldModel[]? Fields { get; set; }
        [JsonPropertyName("c")][Name("c")] public ConstructorModel[]? Constructors { get; set; }
        [JsonPropertyName("e")][Name("e")] public EventModel[]? Events { get; set; }
        [JsonPropertyName("i")][Name("i")] public Handle[]? Interfaces { get; set; }
        //[JsonPropertyName("a")][Name("a")] public AttributeModel[]? Attributes { get; set; }
        [JsonPropertyName("g")][Name("g")] public Handle[]? GenericArguments { get; set; }
        [JsonPropertyName("s")][Name("s")] public GenericParameterConstraintModel[]? GenericConstraints { get; set; }
        [JsonPropertyName("j")][Name("j")] public Handle[]? NestedTypes { get; set; }
        [JsonPropertyName("r")][Name("r")] public int GenericParameterCount { get; set; }
        //[JsonPropertyName("sz")][Name("sz")] public int? Size { get; set; }

        //// --- Helper properties for transpiler ---
        //[JsonIgnore][Name("(f & 1L) != 0")] public extern bool IsPublic { get; }
        //[JsonIgnore][Name("(f & 4L) != 0")] public extern bool IsAbstract { get; }
        //[JsonIgnore][Name("(f & 8L) != 0")] public extern bool IsSealed { get; }
        //[JsonIgnore][Name("(f & 16L) != 0")] public extern bool IsStatic { get; }
        //[JsonIgnore][Name("(f & 32L) != 0")] public extern bool IsInterface { get; }
        //[JsonIgnore][Name("(f & 64L) != 0")] public extern bool IsEnum { get; }
        //[JsonIgnore][Name("(f & 128L) != 0")] public extern bool IsValueType { get; }
        //[JsonIgnore][Name("(f & 256L) != 0")] public extern bool IsGenericType { get; }
        //[JsonIgnore][Name("(f & 65536L) != 0")] public extern bool IsClass { get; }
        //[JsonIgnore][Name("(f & 131072L) != 0")] public extern bool IsRecord { get; }
        //[JsonIgnore][Name("(f & 32768L) != 0")] public extern bool IsNested { get; }
        //[JsonIgnore][Name("(f & 262144) != 0")] public extern bool IsFlags { get; }
        //[JsonIgnore][Name("(f & 2048) != 0")] public extern bool IsArray { get; }
    }

    //[External]
    //public interface IHasAssemblyModel
    //{
    //    //Used by runtime to link back to assembly
    //    [Name("$")] public AssemblyModel AssemblyModel { get; }
    //}

    [ObjectLiteral]
    public abstract class MemberModel //: IHasAssemblyModel
    {
        [JsonPropertyName("n")][Name("n")] public string Name { get; set; } = default!;
        [JsonPropertyName("o")][Name("o")] public string? OutputName { get; set; }
        [JsonPropertyName("d")][Name("d")] public Handle DeclaringType { get; set; } = default!;
        [JsonPropertyName("h")][Name("h")] public Handle Handle { get; set; } = default!;
        [JsonPropertyName("f")][Name("f")] public MemberFlagsModel Flags { get; set; }
        [JsonPropertyName("a")][Name("a")] public AttributeModel[]? Attributes { get; set; }
        //Used by runtime to link back to assembly
        //[JsonIgnore][Name("$")] public AssemblyModel AssemblyModel { get; set; } = default!;
        // --- Helper properties for transpiler ---
        //[JsonIgnore][Name("(f & 1) != 0")] public extern bool IsPublic { get; }
        //[JsonIgnore][Name("(f & 32) != 0")] public extern bool IsStatic { get; }
        //[JsonIgnore][Name("(f & 256) != 0")] public extern bool IsAbstract { get; }
        //[JsonIgnore][Name("(f & 128) != 0")] public extern bool IsVirtual { get; }
    }

    [ObjectLiteral]
    public class PropertyModel : MemberModel
    {
        [JsonPropertyName("p")][Name("p")] public Handle PropertyType { get; set; }
        [JsonPropertyName("i")][Name("i")] public ParameterModel[]? IndexParameters { get; set; }
        [JsonPropertyName("g")][Name("g")] public MethodModel? GetMethod { get; set; }
        [JsonPropertyName("s")][Name("s")] public MethodModel? SetMethod { get; set; }
        //[JsonIgnore][Name("(f & 16384) != 0")] public extern bool IsIndexer { get; }
    }

    //public class AccessorModel
    //{
    //    [JsonPropertyName("f")][Name("f")] public MemberFlags Flags { get; set; }
    //}

    [ObjectLiteral]
    public class MethodModel : MemberModel
    {
#if TRANSLATOR
        [JsonPropertyName("r")][Name("r")] public Handle? ReturnType { get; set; }
#else
        //Generic method return type may be runtime computed
        [JsonPropertyName("r")][Name("r")] public NetJs.Union<Handle, NetJs.NativeFunction<Handle>> ReturnType { get; set; } = default!;
#endif
        [JsonPropertyName("t")][Name("t")] public AttributeModel[]? ReturnAttributes { get; set; }
        [JsonPropertyName("p")][Name("p")] public ParameterModel[]? Parameters { get; set; }
        [JsonPropertyName("g")][Name("g")] public string[]? GenericArguments { get; set; }
        [JsonPropertyName("c")][Name("c")] public GenericParameterConstraintModel[]? GenericConstraints { get; set; }

        //[JsonIgnore][Name("(f & 2048) != 0")] public extern bool IsExtensionMethod { get; }
        //[JsonIgnore][Name("(f & 4096) != 0")] public extern bool IsAsync { get; }
        //[JsonIgnore][Name("(f & 8192) != 0")] public extern bool IsOperator { get; }
    }

    [ObjectLiteral]
    public class FieldModel : MemberModel
    {
        [JsonPropertyName("s")][Name("s")] public int? Offset { get; set; }
        [JsonPropertyName("t")][Name("t")] public Handle FieldType { get; set; } = default!;
    }

    [ObjectLiteral]
    public class ConstructorModel : MethodModel
    {
    }

    [ObjectLiteral]
    public class EventModel : MemberModel
    {
        [JsonPropertyName("e")][Name("e")] public Handle EventHandlerType { get; set; }
        [JsonPropertyName("m")][Name("m")] public MethodModel? AddMethod { get; set; }
        [JsonPropertyName("r")][Name("r")] public MethodModel? RemoveMethod { get; set; }
        [JsonPropertyName("y")][Name("y")] public MethodModel? RaiseMethod { get; set; }
    }

    [ObjectLiteral]
    public class ParameterModel// : IHasAssemblyModel
    {
        [JsonPropertyName("n")][Name("n")] public string Name { get; set; } = default!;
#if TRANSLATOR
        [JsonPropertyName("p")][Name("p")] public Handle ParameterType { get; set; } = default!;
#else
        //Generic method parameter may be runtime computed
        [JsonPropertyName("p")][Name("p")] public NetJs.Union<Handle, NetJs.NativeFunction<Handle>> ParameterType { get; set; } = default!;
#endif
        //[JsonPropertyName("o")][Name("o")] public int Position { get; set; }
        [JsonPropertyName("f")][Name("f")] public ParameterFlagsModel Flags { get; set; }
        [JsonPropertyName("v")][Name("v")] public object? DefaultValue { get; set; }
        //[JsonIgnore][Name("$")] public AssemblyModel AssemblyModel { get; set; } = default!;
        [JsonPropertyName("a")][Name("a")] public AttributeModel[]? Attributes { get; set; }
    }

    [ObjectLiteral]
    public class AttributeConstructorArgumentModel
    {
        [JsonPropertyName("v")][Name("v")] public object? Value { get; set; }
        [JsonPropertyName("t")][Name("t")] public Handle Type { get; set; }
    }

    [ObjectLiteral]
    public class AttributeNamedArgumentModel
    {
        [JsonPropertyName("n")][Name("n")] public string Name { get; set; } = default!;
        [JsonPropertyName("v")][Name("v")] public object? Value { get; set; }
        [JsonPropertyName("t")][Name("t")] public Handle Type { get; set; }
    }

    [ObjectLiteral]
    public class AttributeModel
    {
        [JsonPropertyName("t")][Name("t")] public Handle TypeHandle { get; set; } = default!;
        [JsonPropertyName("c")][Name("c")] public Handle ConstructorHandle { get; set; } = default!;
        [JsonPropertyName("a")][Name("a")] public AttributeConstructorArgumentModel[]? ConstructorArguments { get; set; } = default!;
        [JsonPropertyName("n")][Name("n")] public AttributeNamedArgumentModel[]? NamedArguments { get; set; } = default!;
    }

    [ObjectLiteral]
    public class GenericParameterConstraintModel
    {
        [JsonPropertyName("n")][Name("n")] public string ParameterName { get; set; } = default!;
        [JsonPropertyName("f")][Name("f")] public GenericConstraintFlagsModel Flags { get; set; }
        [JsonPropertyName("c")][Name("c")] public Handle[]? TypeConstraints { get; set; }
    }

}