// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Listing;

/// <summary>The COBOL to object-code map (DESIGN-listing.md section 6.9): each COBOL statement's object code, read from the assembly the
/// compilation wrote and its portable PDB, the same for every backend. Each statement's code is the IL from the sequence point the
/// backend placed at the statement's position to the next statement's sequence point in the same method.</summary>
/// <remarks>Step 8 of the design's section 11. It names no generated source: a method is named by its metadata, an instruction by
/// its IL.</remarks>
/// <param name="Assembly">The path of the assembly the map was read from, as the compilation wrote it.</param>
/// <param name="Methods">Every method of the assembly that holds the code of a statement of the listing, in metadata order.</param>
/// <param name="Ranges">Every stretch of a statement's object code, in listing order (by line, then column, then method and IL
/// offset). A statement compiled more than once (a statement the compiler emits in two places) has a range for each.</param>
public sealed record ListingObjectCodeMap(string Assembly, IReadOnlyList<ListingMethod> Methods, IReadOnlyList<ListingObjectRange> Ranges);

/// <summary>A method of the assembly that holds the code of statements of the listing.</summary>
/// <param name="Id">1-based, in metadata order.</param>
/// <param name="Token">The method's metadata token (ECMA-335 II.22.26, table 0x06).</param>
/// <param name="Type">The full name of the type that declares the method, a nested type after its enclosing type and a '/'.</param>
/// <param name="Name">The method's name in the metadata.</param>
/// <param name="CodeSize">The size of the method's IL body in bytes.</param>
public sealed record ListingMethod(int Id, int Token, string Type, string Name, int CodeSize);

/// <summary>One stretch of a COBOL statement's object code.</summary>
/// <param name="Line">The listing line the statement begins on.</param>
/// <param name="Column">The 1-based character position of the statement's first character in that line's text.</param>
/// <param name="Method">The <see cref="ListingMethod.Id"/> of the method holding the code.</param>
/// <param name="Start">The IL offset of the statement's sequence point.</param>
/// <param name="End">The IL offset after the stretch: the next statement's sequence point in the method, or the end of its body.</param>
/// <param name="Instructions">The IL instructions of the stretch, in order, when the object-code section was asked for; null
/// otherwise.</param>
public sealed record ListingObjectRange(int Line, int Column, int Method, int Start, int End,
    IReadOnlyList<ListingInstruction>? Instructions);

/// <summary>One IL instruction (ECMA-335 partition III), decoded from the method body.</summary>
/// <param name="Offset">The instruction's IL offset in its method.</param>
/// <param name="OpCode">The instruction's name (<c>ldarg.0</c>, <c>call</c>, <c>br.s</c>).</param>
/// <param name="Operand">The operand as text, or null for an instruction that takes none: a number, a quoted string, a branch
/// target as <c>IL_xxxx</c>, a switch's targets, or the type, field or method a token names (<c>Type::Member</c>).</param>
public sealed record ListingInstruction(int Offset, string OpCode, string? Operand);
