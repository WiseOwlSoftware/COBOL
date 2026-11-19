// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace WiseOwl.Cobol.Listing.Building;

/// <summary>A method of the assembly that holds a statement's sequence point.</summary>
/// <param name="Token">Its metadata token.</param>
/// <param name="Type">The full name of its declaring type.</param>
/// <param name="Name">Its name.</param>
/// <param name="CodeSize">Its IL body's size in bytes.</param>
internal sealed record ObjectMethod(int Token, string Type, string Name, int CodeSize);

/// <summary>One statement's sequence point, as the PDB holds it, with the stretch of code it begins.</summary>
/// <param name="Method">The <see cref="ObjectMethod"/> holding it, by its index in <see cref="ObjectCodeFacts.Methods"/>.</param>
/// <param name="Start">Its IL offset.</param>
/// <param name="End">The IL offset of the method's next visible sequence point, or the end of its body.</param>
/// <param name="Document">The full path of the document it names.</param>
/// <param name="Line">Its 1-based start line.</param>
/// <param name="Column">Its 1-based start column (a character of the line is one column).</param>
/// <param name="EndLine">Its 1-based end line.</param>
/// <param name="EndColumn">Its 1-based end column (exclusive).</param>
/// <param name="Instructions">The stretch's IL instructions, when they were asked for.</param>
internal sealed record ObjectSequencePoint(int Method, int Start, int End, string Document, int Line, int Column, int EndLine,
    int EndColumn, IReadOnlyList<ListingInstruction>? Instructions);

/// <summary>What the assembly and its PDB say of the compiled statements: the methods holding them and every visible sequence point, in
/// metadata order and then IL order.</summary>
internal sealed record ObjectCodeFacts(IReadOnlyList<ObjectMethod> Methods, IReadOnlyList<ObjectSequencePoint> Points);

/// <summary>
/// ⛔ THE SOURCE OF TRUTH OF THE OBJECT-CODE MAP (DESIGN-listing.md section 6.9): the produced assembly and its portable PDB, read with
/// <c>System.Reflection.Metadata</c>, never a backend's internals or its generated source. Every backend writes a sequence point at each
/// COBOL statement's position (the Roslyn backend through line directives, a CIL backend natively), so this reader is the same for each.
/// </summary>
internal static class ObjectCodeMapReader
{
    /// <summary>Read <paramref name="assembly"/> and its PDB <paramref name="pdb"/>: each method with a visible sequence point, and each such
    /// point with the stretch of IL from it to the method's next visible point (a hidden point, the code a backend writes between two
    /// statements, does not end a stretch), decoded when <paramref name="instructions"/>.</summary>
    public static ObjectCodeFacts Read(string assembly, string pdb, bool instructions)
    {
        using var peStream = File.OpenRead(assembly);   // not a compilation input: the compilation's own output
        using var pe = new PEReader(peStream);
        var metadata = pe.GetMetadataReader();
        using var pdbStream = File.OpenRead(pdb);   // not a compilation input: the compilation's own output
        using var pdbProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var debug = pdbProvider.GetMetadataReader();

        var methods = new List<ObjectMethod>();
        var points = new List<ObjectSequencePoint>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            var definition = metadata.GetMethodDefinition(handle);
            if (definition.RelativeVirtualAddress == 0) continue;
            var visible = debug.GetMethodDebugInformation(handle.ToDebugInformationHandle()).GetSequencePoints()
                .Where(p => !p.IsHidden).OrderBy(p => p.Offset).ToList();
            if (visible.Count == 0) continue;
            var body = pe.GetMethodBody(definition.RelativeVirtualAddress);
            var il = body.GetILContent();
            int method = methods.Count;
            methods.Add(new ObjectMethod(MetadataTokens.GetToken(handle), MetadataNames.TypeName(metadata, definition.GetDeclaringType()),
                metadata.GetString(definition.Name), il.Length));
            for (int i = 0; i < visible.Count; i++)
            {
                var point = visible[i];
                int end = i + 1 < visible.Count ? visible[i + 1].Offset : il.Length;
                string document = Path.GetFullPath(debug.GetString(debug.GetDocument(point.Document).Name));
                points.Add(new ObjectSequencePoint(method, point.Offset, end, document, point.StartLine, point.StartColumn,
                    point.EndLine, point.EndColumn, instructions ? IlDisassembler.Decode(metadata, il, point.Offset, end) : null));
            }
        }
        return new ObjectCodeFacts(methods, points);
    }
}
