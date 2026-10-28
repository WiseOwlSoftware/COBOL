// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Runtime.Repository;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ WHAT A MODULE PUBLISHES DOES NOT DEPEND ON THE BACKEND THAT WROTE IT (DESIGN-external-repository §17.4; kb/Work PB2100).
/// The repository schema is metadata (ISO §8.13's information, as custom attributes, §6), so a direct-CIL backend must be able
/// to publish what the Roslyn backend publishes. The renderer states every encoding rule of §6.2 once, as values
/// (<see cref="AttributeApplication"/>); this fixture is the second backend: it writes those applications into a module with
/// <see cref="MetadataBuilder"/> and the ECMA-335 custom-attribute encoders, on the same CLR shapes a module carries (a
/// program's formals on a method's parameters and its returning item on the return value; a class's methods on its type's
/// methods and its factory's on the factory type the record names), and the module <see cref="RepositoryReader"/> reads back is
/// EQUAL to the one the Roslyn backend's C# text compiles to — both equal to the records written — and its <c>Units</c> index
/// is the record set.
/// </summary>
public sealed class RepositoryBackendIndependenceTests
{
    [Fact]
    public void ARecordWrittenByRoslynAndByMetadataBuilder_DecodesEqual()
    {
        var codec = RepositoryCodecTests.Bind(RepositoryCodecTests.Source, 2023);
        var oo = RepositoryCodecTests.Bind(RepositoryCodecTests.OoSource, 2023);
        List<RepositoryRecord> records = [.. RepositoryReaderTests.Fixtures.Values,
            .. HostEntryEmitter.EntryUnits(codec.Units).Select(RepositoryCodec.Of),
            .. RepositoryCodecTests.OoRecordsOf(oo).Select(r => r.Record),
            .. HostEntryEmitter.EntryUnits(oo.Units).Select(RepositoryCodec.Of)];
        Assert.Contains(records, r => r.Class is { Methods.Count: > 0 });
        Assert.Contains(records, r => r.ReferencedPrototypes.Count > 0 && r.TypeDeclarations.Count > 0);

        var viaRoslyn = RepositoryReaderTests.ReadLibrary(records);
        using RepositoryIndex index = RepositoryIndex.Open(new MemoryStream(Write(records))).Index!;
        var outcome = RepositoryReader.Read(index);
        Assert.True(outcome.Failure == RepositoryReadFailure.None, $"{outcome.Failure}: {outcome.Detail}");
        Assert.Equal(records, outcome.Records);
        Assert.Equal(viaRoslyn, outcome.Records);
        Assert.Equal(records.Select(r => new RepositoryUnitRow(r.Kind, r.ExternalizedName, r.Name)), index.Units);
    }

    // ── the second backend: MetadataBuilder + the ECMA-335 custom-attribute encoders ──────────────────────────────────

    /// <summary>A module holding <paramref name="records"/>, each on the CLR shape its kind takes in an emitted module.</summary>
    private static byte[] Write(IReadOnlyList<RepositoryRecord> records)
    {
        var w = new Writer();
        w.Attribute(EntityHandle.AssemblyDefinition, RepositoryAttributeRenderer.ModuleAttribute("R",
            [.. records.Select(r => new RepositoryUnitRow(r.Kind, r.ExternalizedName, r.Name))], 2023, hasSignatures: true));
        for (int n = 0; n < records.Count; n++)
        {
            var r = records[n];
            List<AttributeApplication> typeAttributes = [RepositoryAttributeRenderer.UnitAttribute(r.Kind, r.Name, r.ExternalizedName, r),
                .. RepositoryAttributeRenderer.TableAttributes(r)];
            if (r.Class is { } c)
            {
                w.Type($"U{n}", [.. typeAttributes, .. RepositoryAttributeRenderer.PropertyAttributes(c, factory: false)],
                    c.Methods.Where(m => !m.IsFactory).Select(Method));
                if (c.FactoryType is { } factoryType)
                    w.Type(factoryType, [.. RepositoryAttributeRenderer.PropertyAttributes(c, factory: true)], c.Methods.Where(m => m.IsFactory).Select(Method));
                continue;
            }
            w.Type($"U{n}", typeAttributes, [new Writer.Method("Call", [], r.Formals.Select(RepositoryAttributeRenderer.FormalAttributes).ToList(),
                r.Returning is { } returning ? [.. RepositoryAttributeRenderer.ReturningAttributes(returning)] : null)]);
        }
        return w.Serialize();

        static Writer.Method Method(MethodRecord m) => new(m.ClrName, [RepositoryAttributeRenderer.MethodAttribute(m)],
            m.Formals.Select(RepositoryAttributeRenderer.FormalAttributes).ToList(),
            m.Returning is { } returning ? [.. RepositoryAttributeRenderer.ReturningAttributes(returning)] : null);
    }

    /// <summary>The metadata writer: one module, a reference to the runtime assembly for the attribute types, and static
    /// methods with trivial bodies whose parameters and return values carry the attributes.</summary>
    private sealed class Writer
    {
        /// <summary>A method: its name, its own attributes, one attribute list per parameter (each parameter typed
        /// <see cref="object"/>), and the return value's (an <see cref="object"/> return when present, else <c>void</c>).</summary>
        public sealed record Method(string Name, IReadOnlyList<AttributeApplication> Attributes,
            IReadOnlyList<IEnumerable<AttributeApplication>> Parameters, IReadOnlyList<AttributeApplication>? Returning);

        private readonly MetadataBuilder _md = new();
        private readonly BlobBuilder _il = new();
        private readonly MethodBodyStreamEncoder _bodies;
        private readonly AssemblyReferenceHandle _runtime;
        private readonly TypeReferenceHandle _object;
        private readonly Dictionary<Type, EntityHandle> _types = [];
        private readonly Dictionary<Type, MemberReferenceHandle> _constructors = [];

        public Writer()
        {
            _bodies = new MethodBodyStreamEncoder(_il);
            _md.AddModule(0, _md.GetOrAddString("fixture.dll"), _md.GetOrAddGuid(new Guid("8a1f4d1e-9b0c-4c51-8a37-5d0c2a7b3e11")), default, default);
            _md.AddAssembly(_md.GetOrAddString("fixture"), new Version(1, 0, 0, 0), default, default, 0, AssemblyHashAlgorithm.None);
            var systemRuntime = _md.AddAssemblyReference(_md.GetOrAddString("System.Runtime"), new Version(10, 0, 0, 0), default, default, 0, default);
            _runtime = _md.AddAssemblyReference(_md.GetOrAddString(typeof(CobolUnitAttribute).Assembly.GetName().Name!),
                typeof(CobolUnitAttribute).Assembly.GetName().Version!, default, default, 0, default);
            _object = _md.AddTypeReference(systemRuntime, _md.GetOrAddString("System"), _md.GetOrAddString("Object"));
            _md.AddTypeDefinition(default, default, _md.GetOrAddString("<Module>"), default,
                MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        }

        public void Type(string name, IEnumerable<AttributeApplication> attributes, IEnumerable<Method> methods)
        {
            var type = _md.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class,
                default, _md.GetOrAddString(name), _object, MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(_md.GetRowCount(TableIndex.MethodDef) + 1));
            foreach (var a in attributes) Attribute(type, a);
            foreach (var m in methods) Add(m);
        }

        private void Add(Method m)
        {
            var signature = new BlobBuilder();
            new BlobEncoder(signature).MethodSignature().Parameters(m.Parameters.Count,
                r => { if (m.Returning is null) r.Void(); else r.Type().Object(); },
                p => { for (int i = 0; i < m.Parameters.Count; i++) p.AddParameter().Type().Object(); });
            var body = new InstructionEncoder(new BlobBuilder());
            if (m.Returning is not null) body.OpCode(ILOpCode.Ldnull);
            body.OpCode(ILOpCode.Ret);
            var firstParameter = MetadataTokens.ParameterHandle(_md.GetRowCount(TableIndex.Param) + 1);
            var method = _md.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                MethodImplAttributes.IL, _md.GetOrAddString(m.Name), _md.GetOrAddBlob(signature), _bodies.AddMethodBody(body), firstParameter);
            foreach (var a in m.Attributes) Attribute(method, a);
            if (m.Returning is not null)
            {
                var returnValue = _md.AddParameter(ParameterAttributes.None, default, 0);
                foreach (var a in m.Returning) Attribute(returnValue, a);
            }
            for (int i = 0; i < m.Parameters.Count; i++)
            {
                var parameter = _md.AddParameter(ParameterAttributes.None, _md.GetOrAddString($"p{i}"), i + 1);
                foreach (var a in m.Parameters[i]) Attribute(parameter, a);
            }
        }

        public void Attribute(EntityHandle parent, AttributeApplication application)
        {
            var value = new BlobBuilder();
            new BlobEncoder(value).CustomAttributeSignature(out var fixedArguments, out var namedArguments);
            foreach (object argument in application.Arguments) Literal(fixedArguments.AddArgument(), argument);
            var named = namedArguments.Count(application.Named.Count);
            foreach (var (name, argument) in application.Named)
            {
                named.AddArgument(isField: false, out var type, out var nameEncoder, out var literal);
                ArgumentType(type, argument);
                nameEncoder.Name(name);
                Literal(literal, argument);
            }
            _md.AddCustomAttribute(parent, Constructor(application.Type), _md.GetOrAddBlob(value));
        }

        public byte[] Serialize()
        {
            var pe = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(_md), _il,
                deterministicIdProvider: _ => new BlobContentId(Guid.Empty, 1));
            var image = new BlobBuilder();
            pe.Serialize(image);
            return image.ToArray();
        }

        /// <summary>The attribute's one constructor, as a reference into the runtime assembly with its declared parameter types.</summary>
        private MemberReferenceHandle Constructor(Type attribute)
        {
            if (_constructors.TryGetValue(attribute, out var handle)) return handle;
            var parameters = attribute.GetConstructors().Single().GetParameters();
            var signature = new BlobBuilder();
            new BlobEncoder(signature).MethodSignature(isInstanceMethod: true).Parameters(parameters.Length, r => r.Void(), p =>
            {
                foreach (var parameter in parameters)
                {
                    var type = p.AddParameter().Type();
                    if (parameter.ParameterType == typeof(int)) type.Int32();
                    else if (parameter.ParameterType == typeof(string)) type.String();
                    else if (parameter.ParameterType.IsEnum) type.Type(TypeOf(parameter.ParameterType), isValueType: true);
                    else throw new InvalidOperationException($"{attribute.Name}: no encoding for a {parameter.ParameterType.Name} parameter");
                }
            });
            handle = _md.AddMemberReference(TypeOf(attribute), _md.GetOrAddString(".ctor"), _md.GetOrAddBlob(signature));
            _constructors.Add(attribute, handle);
            return handle;
        }

        private EntityHandle TypeOf(Type type)
        {
            if (!_types.TryGetValue(type, out var handle))
                _types.Add(type, handle = _md.AddTypeReference(_runtime, _md.GetOrAddString(type.Namespace!), _md.GetOrAddString(type.Name)));
            return handle;
        }

        private static void ArgumentType(NamedArgumentTypeEncoder type, object value)
        {
            if (value is Array array) ElementType(type.SZArray().ElementType(), array.GetType().GetElementType()!);
            else ElementType(type.ScalarType(), value.GetType());
        }

        private static void ElementType(CustomAttributeElementTypeEncoder type, Type element)
        {
            if (element.IsEnum) type.Enum($"{element.FullName}, {element.Assembly.GetName().Name}");
            else if (element == typeof(int)) type.Int32();
            else if (element == typeof(bool)) type.Boolean();
            else if (element == typeof(char)) type.Char();
            else if (element == typeof(string)) type.String();
            else throw new InvalidOperationException($"no custom-attribute encoding for a {element.Name}");
        }

        private static void Literal(LiteralEncoder literal, object value)
        {
            if (value is Array array)
            {
                var elements = literal.Vector().Count(array.Length);
                foreach (object element in array) Literal(elements.AddLiteral(), element);
                return;
            }
            literal.Scalar().Constant(value is Enum member ? Convert.ToInt32(member, System.Globalization.CultureInfo.InvariantCulture) : value);
        }
    }
}
