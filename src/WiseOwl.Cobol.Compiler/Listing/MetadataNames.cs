// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace WiseOwl.Cobol.Compiler.Listing;

/// <summary>
/// The names the object-code map gives the metadata an assembly holds (DESIGN-listing.md section 6.9): a type by its full name (a nested
/// type after its enclosing type and a '/', a generic instance with its arguments in angle brackets), a field or a method as
/// <c>Type::Name</c> (ECMA-335's notation), a generic method instance with its arguments. The one reader of names for
/// <see cref="ObjectCodeMapReader"/> and <see cref="IlDisassembler"/>.
/// </summary>
internal static class MetadataNames
{
    /// <summary>The name of the entity <paramref name="handle"/> names.</summary>
    public static string Of(MetadataReader metadata, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => TypeName(metadata, (TypeDefinitionHandle)handle),
        HandleKind.TypeReference => TypeName(metadata, (TypeReferenceHandle)handle),
        HandleKind.TypeSpecification => metadata.GetTypeSpecification((TypeSpecificationHandle)handle)
            .DecodeSignature(new SignatureNames(metadata), null),
        HandleKind.MethodDefinition => MethodName(metadata, (MethodDefinitionHandle)handle),
        HandleKind.FieldDefinition => FieldName(metadata, (FieldDefinitionHandle)handle),
        HandleKind.MemberReference => MemberName(metadata, (MemberReferenceHandle)handle),
        HandleKind.MethodSpecification => MethodInstance(metadata, (MethodSpecificationHandle)handle),
        HandleKind.StandaloneSignature => "signature",
        _ => handle.Kind.ToString(),
    };

    /// <summary>The full name of type <paramref name="handle"/>.</summary>
    public static string TypeName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var type = metadata.GetTypeDefinition(handle);
        string name = metadata.GetString(type.Name);
        var enclosing = type.GetDeclaringType();
        if (!enclosing.IsNil) return TypeName(metadata, enclosing) + "/" + name;
        string ns = metadata.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    /// <summary>The full name of the method <paramref name="handle"/> defines, as <c>Type::Name</c>.</summary>
    public static string MethodName(MetadataReader metadata, MethodDefinitionHandle handle)
    {
        var method = metadata.GetMethodDefinition(handle);
        return TypeName(metadata, method.GetDeclaringType()) + "::" + metadata.GetString(method.Name);
    }

    private static string TypeName(MetadataReader metadata, TypeReferenceHandle handle)
    {
        var type = metadata.GetTypeReference(handle);
        string name = metadata.GetString(type.Name);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            return TypeName(metadata, (TypeReferenceHandle)type.ResolutionScope) + "/" + name;
        string ns = metadata.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string FieldName(MetadataReader metadata, FieldDefinitionHandle handle)
    {
        var field = metadata.GetFieldDefinition(handle);
        return TypeName(metadata, field.GetDeclaringType()) + "::" + metadata.GetString(field.Name);
    }

    private static string MemberName(MetadataReader metadata, MemberReferenceHandle handle)
    {
        var member = metadata.GetMemberReference(handle);
        string parent = member.Parent.Kind switch
        {
            HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification => Of(metadata, member.Parent),
            HandleKind.MethodDefinition => MethodName(metadata, (MethodDefinitionHandle)member.Parent),
            HandleKind.ModuleReference => metadata.GetString(metadata.GetModuleReference((ModuleReferenceHandle)member.Parent).Name),
            _ => member.Parent.Kind.ToString(),
        };
        return parent + "::" + metadata.GetString(member.Name);
    }

    private static string MethodInstance(MetadataReader metadata, MethodSpecificationHandle handle)
    {
        var instance = metadata.GetMethodSpecification(handle);
        var arguments = instance.DecodeSignature(new SignatureNames(metadata), null);
        return Of(metadata, instance.Method) + "<" + string.Join(", ", arguments) + ">";
    }

    /// <summary>A type signature's name (ECMA-335 II.23.2.12), for a generic instance and a method instance's arguments.</summary>
    private sealed class SignatureNames(MetadataReader metadata) : ISignatureTypeProvider<string, object?>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Boolean => "bool",
            PrimitiveTypeCode.Byte => "uint8",
            PrimitiveTypeCode.Char => "char",
            PrimitiveTypeCode.Double => "float64",
            PrimitiveTypeCode.Int16 => "int16",
            PrimitiveTypeCode.Int32 => "int32",
            PrimitiveTypeCode.Int64 => "int64",
            PrimitiveTypeCode.IntPtr => "native int",
            PrimitiveTypeCode.Object => "object",
            PrimitiveTypeCode.SByte => "int8",
            PrimitiveTypeCode.Single => "float32",
            PrimitiveTypeCode.String => "string",
            PrimitiveTypeCode.TypedReference => "typedref",
            PrimitiveTypeCode.UInt16 => "uint16",
            PrimitiveTypeCode.UInt32 => "uint32",
            PrimitiveTypeCode.UInt64 => "uint64",
            PrimitiveTypeCode.UIntPtr => "native uint",
            PrimitiveTypeCode.Void => "void",
            _ => typeCode.ToString(),
        };

        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => TypeName(metadata, handle);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => TypeName(metadata, handle);
        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            metadata.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetPinnedType(string elementType) => elementType + " pinned";
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
            genericType + "<" + string.Join(", ", typeArguments) + ">";
        public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public string GetFunctionPointerType(MethodSignature<string> signature) => "method " + signature.ReturnType + "*(" + string.Join(", ", signature.ParameterTypes) + ")";
    }
}
