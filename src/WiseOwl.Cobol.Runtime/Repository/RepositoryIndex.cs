// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace WiseOwl.Cobol.Runtime.Repository;

/// <summary>One row of an assembly's <see cref="CobolRepositoryAttribute.Units"/> index (DESIGN-external-repository §5.4
/// item 3): a published unit's kind, externalized name and user-defined word.</summary>
/// <param name="Kind">What the unit is.</param>
/// <param name="ExternalizedName">The unit's externalized name as formed (DOC-A.1-68); compared by the Annex C fold.</param>
/// <param name="Word">The unit's user-defined word as declared; equals its <see cref="CobolUnitAttribute.Name"/>.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct RepositoryUnitRow(RepositoryUnitKind Kind, string ExternalizedName, string Word);

/// <summary>Why a repository entry could not be read (DESIGN-external-repository §15.2, §15.3): the typed outcome the
/// compiler's REPO diagnostics (slice 7) and the run-time probe (slice 5) report from, never an exception.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public enum RepositoryReadFailure
{
    /// <summary>The entry was read.</summary>
    None = 0,
    /// <summary>The file is not a managed PE image with metadata.</summary>
    NotManagedAssembly = 1,
    /// <summary>The assembly carries no <see cref="CobolRepositoryAttribute"/>: it is not a repository entry.</summary>
    NoRepositoryRecord = 2,
    /// <summary>The record or an attribute of it uses an encoding the schema does not (an argument that is not a
    /// primitive, a string, an <c>int</c>-backed enum or an array of those), or a row is malformed (REPO-4).</summary>
    Malformed = 3,
    /// <summary>The <c>Units</c> index lists more than <see cref="RepositoryIndex.MaxUnits"/> units (REPO-4).</summary>
    TooManyUnits = 4,
    /// <summary>The module was built with <c>--repository-update off</c>: it carries no signature attributes, so no
    /// compilation can bind against it (REPO-8, DESIGN-external-repository §7.1).</summary>
    NoSignatures = 5,
    /// <summary>The record follows another schema version than the one the reader implements (REPO-7, §6.6).</summary>
    OtherSchemaVersion = 6,
}

/// <summary>The result of <see cref="RepositoryIndex.Open(Stream)"/>: the index, or the typed reason there is none.</summary>
/// <param name="Index">The opened index, or null when <paramref name="Failure"/> is not <see cref="RepositoryReadFailure.None"/>.</param>
/// <param name="Failure">Why the entry could not be read.</param>
/// <param name="Detail">The reader's own words for a <see cref="RepositoryReadFailure.Malformed"/> entry; null otherwise.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct RepositoryIndexOutcome(RepositoryIndex? Index, RepositoryReadFailure Failure, string? Detail);

/// <summary>
/// ⛔ THE ONE READER OF A COMPILED MODULE'S REPOSITORY HEADER AND UNIT INDEX (DESIGN-external-repository §7.4, §15.1; kb/Work
/// PB2100), without loading the assembly: one prefetched <see cref="PEReader"/> and <see cref="MetadataReader"/> read of the
/// assembly-level <see cref="CobolRepositoryAttribute"/> — its schema, runtime version, call ABI, registrar, compiler version,
/// edition and <see cref="Units"/> rows — and of the <c>AssemblyRef</c> table's names (the stale-expansion scan of §10.5).
/// The file is read into memory and closed, so no handle lingers (a repository entry can be replaced by the next build while
/// a compilation holds its index, §7.5).
/// <para>It is public because the runtime grants no <c>InternalsVisibleTo</c> to the compiler, whose
/// <c>RepositoryReader</c> opens the SAME reader here (the header is never decoded twice) and decodes the unit and
/// description attributes through <see cref="Metadata"/>; and the run-time probe (slice 5) reads the same header. It is the
/// compiler's and the probe's ABI, not an API for a hand-written caller. <see cref="CobolRepositoryAttribute.Units"/> is
/// written by the compiler's <c>RepositoryAttributeRenderer</c> through <see cref="EncodeUnit"/>, and this class is the
/// one reader of that encoding (<see cref="TryDecodeUnit"/>).</para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RepositoryIndex : IDisposable
{
    /// <summary>The most units one index may list (DESIGN-external-repository §15.2); more is
    /// <see cref="RepositoryReadFailure.TooManyUnits"/>.</summary>
    public const int MaxUnits = 65_536;

    /// <summary>The namespace every attribute of the repository schema is declared in (this one).</summary>
    private const string SchemaNamespace = "WiseOwl.Cobol.Runtime.Repository";

    private readonly PEReader _image;

    private RepositoryIndex(PEReader image, MetadataReader metadata)
    {
        _image = image;
        Metadata = metadata;
    }

    /// <summary>The metadata of the entry. Valid until <see cref="Dispose"/>; the compiler's reader decodes the unit and
    /// description attributes through it.</summary>
    public MetadataReader Metadata { get; }

    /// <summary>The module version id of the assembly (its <c>Mvid</c>): two files with one are the same build, two with
    /// different ones are two assemblies (§7.4's collision test).</summary>
    public Guid Mvid { get; private init; }

    /// <summary>The repository schema the record follows (<see cref="CobolRepositoryAttribute.SchemaVersion"/>).</summary>
    public int SchemaVersion { get; private init; }

    /// <summary>The runtime version the module was compiled against.</summary>
    public string RuntimeVersion { get; private init; } = "";

    /// <summary>The boundary layout the module was compiled for.</summary>
    public int CallAbi { get; private init; }

    /// <summary>The full CLR name of the module's registrar type, or null when the record names none.</summary>
    public string? Registrar { get; private init; }

    /// <summary>The version of the compiler that wrote the module, or null when the record does not say.</summary>
    public string? CompilerVersion { get; private init; }

    /// <summary>The ISO edition the module was compiled for, or 0 when the record does not say.</summary>
    public int Edition { get; private init; }

    /// <summary>Whether the module carries the signature attributes a later compilation binds against.</summary>
    public bool HasSignatures { get; private init; }

    /// <summary>The units the assembly publishes, in the order the compiler wrote them.</summary>
    public IReadOnlyList<RepositoryUnitRow> Units { get; private init; } = [];

    /// <summary>The simple names of the assemblies this one references, in <c>AssemblyRef</c> table order.</summary>
    public IReadOnlyList<string> AssemblyReferences { get; private init; } = [];

    /// <summary>Open the repository entry at <paramref name="path"/>: the file is read into memory and closed.</summary>
    /// <param name="path">The assembly file.</param>
    public static RepositoryIndexOutcome Open(string path)
    {
        // Shared for read AND write/delete: a build that replaces the entry while another compilation reads it must not fail (§7.5).
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Open(stream);
    }

    /// <summary>Open the repository entry in <paramref name="stream"/>; the stream is left open and is not needed after the
    /// call returns.</summary>
    /// <param name="stream">A seekable stream over the assembly's bytes.</param>
    public static RepositoryIndexOutcome Open(Stream stream)
    {
        PEReader? image = null;
        try
        {
            image = new PEReader(stream, PEStreamOptions.PrefetchMetadata | PEStreamOptions.LeaveOpen);
            if (!image.HasMetadata) return Fail(image, RepositoryReadFailure.NotManagedAssembly, null);
            MetadataReader md = image.GetMetadataReader();
            if (!md.IsAssembly) return Fail(image, RepositoryReadFailure.NotManagedAssembly, null);

            CustomAttribute? record = null;
            foreach (CustomAttributeHandle h in md.GetAssemblyDefinition().GetCustomAttributes())
            {
                CustomAttribute candidate = md.GetCustomAttribute(h);
                if (SchemaAttributeName(md, candidate) != nameof(CobolRepositoryAttribute)) continue;
                record = candidate;
                break;
            }
            if (record is null) return Fail(image, RepositoryReadFailure.NoRepositoryRecord, null);

            CustomAttributeValue<string> value = DecodeSchemaAttribute(record.Value);
            if (value.FixedArguments.Length != 3
                || value.FixedArguments[0].Value is not int schema
                || value.FixedArguments[1].Value is not string runtimeVersion
                || value.FixedArguments[2].Value is not int callAbi)
                return Fail(image, RepositoryReadFailure.Malformed, "the repository record's constructor arguments are not (int, string, int)");

            string? registrar = null, compiler = null;
            int edition = 0;
            bool signatures = false;
            ImmutableArray<CustomAttributeTypedArgument<string>>? unitRows = null;
            foreach (CustomAttributeNamedArgument<string> named in value.NamedArguments)
            {
                switch (named.Name)
                {
                    case nameof(CobolRepositoryAttribute.Registrar): registrar = named.Value as string; break;
                    case nameof(CobolRepositoryAttribute.CompilerVersion): compiler = named.Value as string; break;
                    case nameof(CobolRepositoryAttribute.Edition) when named.Value is int e: edition = e; break;
                    case nameof(CobolRepositoryAttribute.HasSignatures) when named.Value is bool b: signatures = b; break;
                    case nameof(CobolRepositoryAttribute.Units): unitRows = named.Value as ImmutableArray<CustomAttributeTypedArgument<string>>?; break;
                }
            }

            var units = new List<RepositoryUnitRow>(unitRows?.Length ?? 0);
            if (unitRows is { } rows)
            {
                if (rows.Length > MaxUnits) return Fail(image, RepositoryReadFailure.TooManyUnits, $"the index lists {rows.Length} units");
                foreach (CustomAttributeTypedArgument<string> row in rows)
                    if (row.Value is string text && TryDecodeUnit(text, out RepositoryUnitRow unit)) units.Add(unit);
                    else return Fail(image, RepositoryReadFailure.Malformed, $"the unit index row '{row.Value}' is not '<kind>:<name>:<word>'");
            }

            var references = new List<string>(md.AssemblyReferences.Count);
            foreach (AssemblyReferenceHandle h in md.AssemblyReferences)
                references.Add(md.GetString(md.GetAssemblyReference(h).Name));

            return new RepositoryIndexOutcome(new RepositoryIndex(image, md)
            {
                Mvid = md.GetGuid(md.GetModuleDefinition().Mvid), SchemaVersion = schema, RuntimeVersion = runtimeVersion, CallAbi = callAbi, Registrar = registrar,
                CompilerVersion = compiler, Edition = edition, HasSignatures = signatures, Units = units, AssemblyReferences = references,
            }, RepositoryReadFailure.None, null);
        }
        catch (Exception e) when (e is BadImageFormatException or InvalidOperationException or ArgumentException or IOException)
        {
            image?.Dispose();
            return new RepositoryIndexOutcome(null, e is BadImageFormatException && image is null ? RepositoryReadFailure.NotManagedAssembly
                : RepositoryReadFailure.Malformed, e.Message);
        }

        static RepositoryIndexOutcome Fail(PEReader image, RepositoryReadFailure failure, string? detail)
        {
            image.Dispose();
            return new RepositoryIndexOutcome(null, failure, detail);
        }
    }

    /// <summary>Release the prefetched image; <see cref="Metadata"/> is invalid afterwards.</summary>
    public void Dispose() => _image.Dispose();

    /// <summary>Write one <see cref="CobolRepositoryAttribute.Units"/> row: <c>&lt;kind&gt;:&lt;externalized name&gt;:&lt;word&gt;</c>,
    /// the kind in decimal and every <c>:</c> and <c>\</c> inside either name written <c>\:</c> and <c>\\</c>, so the three
    /// fields split unambiguously (DESIGN-external-repository §5.4 item 3).</summary>
    public static string EncodeUnit(RepositoryUnitKind kind, string externalizedName, string word) =>
        $"{(int)kind}:{Escape(externalizedName)}:{Escape(word)}";

    /// <summary>Read one row written by <see cref="EncodeUnit"/>; false for any other text, including a kind this schema does
    /// not define and a name with a dangling escape.</summary>
    public static bool TryDecodeUnit(string row, out RepositoryUnitRow unit)
    {
        unit = default;
        int colon = row.IndexOf(':');
        if (colon <= 0 || !int.TryParse(row.AsSpan(0, colon), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int kind)
            || !Enum.IsDefined((RepositoryUnitKind)kind)) return false;
        var fields = new List<string>(2);
        var current = new StringBuilder();
        for (int i = colon + 1; i < row.Length; i++)
        {
            char c = row[i];
            if (c == '\\')
            {
                if (++i >= row.Length || row[i] is not ('\\' or ':')) return false;   // a dangling or unknown escape
                current.Append(row[i]);
            }
            else if (c == ':')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else current.Append(c);
        }
        fields.Add(current.ToString());
        if (fields.Count != 2) return false;
        unit = new RepositoryUnitRow((RepositoryUnitKind)kind, fields[0], fields[1]);
        return true;
    }

    private static string Escape(string name) => name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace(":", "\\:", StringComparison.Ordinal);

    /// <summary>The simple type name of <paramref name="attribute"/> when it is an attribute of the repository schema (a type
    /// of this namespace, <c>WiseOwl.Cobol.Runtime.Repository</c>), else null: how the compiler's <c>RepositoryReader</c> and
    /// this index tell the schema's attributes from every other attribute of an entry, by name and without loading anything
    /// (DESIGN-external-repository §7.4, §15.1).</summary>
    /// <param name="md">The entry's metadata.</param>
    /// <param name="attribute">An attribute row of the entry.</param>
    public static string? SchemaAttributeName(MetadataReader md, CustomAttribute attribute)
    {
        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                {
                    MemberReference member = md.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    if (member.Parent.Kind != HandleKind.TypeReference) return null;
                    TypeReference type = md.GetTypeReference((TypeReferenceHandle)member.Parent);
                    return md.StringComparer.Equals(type.Namespace, SchemaNamespace) ? md.GetString(type.Name) : null;
                }
            case HandleKind.MethodDefinition:
                {
                    TypeDefinition type = md.GetTypeDefinition(md.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType());
                    return md.StringComparer.Equals(type.Namespace, SchemaNamespace) ? md.GetString(type.Name) : null;
                }
            default:
                return null;
        }
    }

    /// <summary>Decode a schema attribute's arguments with the schema's one decoding provider (<see cref="DecodeTypes"/>):
    /// every enum as its <c>int</c> value, every array as its elements.</summary>
    /// <param name="attribute">An attribute <see cref="SchemaAttributeName"/> names.</param>
    /// <exception cref="BadImageFormatException">The attribute's blob is not a well-formed custom-attribute value.</exception>
    public static CustomAttributeValue<string> DecodeSchemaAttribute(CustomAttribute attribute) => attribute.DecodeValue(DecodeTypes.Instance);

    /// <summary>The decoding provider: primitives, strings, enums (always <c>int</c>-backed, DESIGN-external-repository §6.2
    /// rule 1) and one-dimensional arrays of those. A type is named, never loaded, and every enum is taken as <c>int</c>
    /// (the reader cannot learn an underlying type otherwise); the schema's attribute properties are read by their exact types,
    /// so an argument of any other type fails the read of the property that names it (§15.1).</summary>
    private sealed class DecodeTypes : ICustomAttributeTypeProvider<string>
    {
        public static readonly DecodeTypes Instance = new();

        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetSystemType() => "System.Type";
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            FullName(reader, reader.GetTypeDefinition(handle).Namespace, reader.GetTypeDefinition(handle).Name);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            FullName(reader, reader.GetTypeReference(handle).Namespace, reader.GetTypeReference(handle).Name);
        public string GetTypeFromSerializedName(string name) => name;
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
        public bool IsSystemType(string type) => type == "System.Type";

        private static string FullName(MetadataReader reader, StringHandle ns, StringHandle name) =>
            ns.IsNil ? reader.GetString(name) : reader.GetString(ns) + "." + reader.GetString(name);
    }
}
