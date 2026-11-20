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
/// <param name="Regions">The body's exception-handling clauses (ECMA-335 II.25.4.6), in the order the body lists them, which puts a
/// clause nested in another before it (ECMA-335 I.12.4.2.7); empty for a body with none.</param>
public sealed record ListingMethod(int Id, int Token, string Type, string Name, int CodeSize, IReadOnlyList<ListingExceptionRegion> Regions);

/// <summary>One exception-handling clause of a method body (ECMA-335 II.25.4.6): a protected block (its <c>try</c> block) and the handler
/// that runs for it, as the body's data section holds them. Offsets are IL offsets in the method; an end is the offset after the block's
/// last instruction.</summary>
/// <param name="Kind">What kind of handler the clause has.</param>
/// <param name="TryStart">The IL offset of the protected block's first instruction.</param>
/// <param name="TryEnd">The IL offset after the protected block.</param>
/// <param name="HandlerStart">The IL offset of the handler's first instruction.</param>
/// <param name="HandlerEnd">The IL offset after the handler.</param>
/// <param name="FilterStart">For a <see cref="ListingRegionKind.Filter"/> clause, the IL offset of its filter block's first instruction
/// (the filter block runs to the handler's start); null for every other kind.</param>
/// <param name="CatchType">For a <see cref="ListingRegionKind.Catch"/> clause, the type it catches, named as an instruction's operand names
/// a type (<see cref="ListingInstruction.Operand"/>); null for every other kind.</param>
public sealed record ListingExceptionRegion(ListingRegionKind Kind, int TryStart, int TryEnd, int HandlerStart, int HandlerEnd,
    int? FilterStart, string? CatchType);

/// <summary>The kind of an exception-handling clause's handler (ECMA-335 II.19 and II.25.4.6).</summary>
public enum ListingRegionKind
{
    /// <summary>A typed handler: it runs when the exception is an instance of the clause's type (<c>catch</c>).</summary>
    Catch,

    /// <summary>A filtered handler: the filter block decides whether the handler runs (<c>filter</c>).</summary>
    Filter,

    /// <summary>A handler that runs whenever the protected block is left (<c>finally</c>).</summary>
    Finally,

    /// <summary>A handler that runs only when the protected block is left by an exception (<c>fault</c>).</summary>
    Fault,
}

/// <summary>Where a line of a method's exception-handling structure stands, as ILDASM prints the structure (ECMA-335 II.19): the start or
/// the end of a protected block, of a filter block or of a handler.</summary>
public enum ListingRegionBoundary
{
    /// <summary>A protected block begins: <c>.try {</c>. Several clauses protecting the same block share one.</summary>
    TryStart,

    /// <summary>A protected block ends: <c>} // end .try</c>.</summary>
    TryEnd,

    /// <summary>A filter clause's filter block begins: <c>filter {</c>.</summary>
    FilterStart,

    /// <summary>A filter clause's filter block ends: <c>} // end filter</c>.</summary>
    FilterEnd,

    /// <summary>A handler begins: <c>catch Type {</c>, <c>finally {</c>, <c>fault {</c>, or a filter clause's <c>{ // handler</c>.</summary>
    HandlerStart,

    /// <summary>A handler ends: <c>} // end handler</c>.</summary>
    HandlerEnd,
}

/// <summary>The one answer to which boundaries open a block (<see cref="ListingRegionBoundary"/>), read by the builder that nests the
/// lines and by the renderers that indent the instructions after them.</summary>
public static class ListingRegionBoundaries
{
    /// <summary>Whether <paramref name="boundary"/> opens a block, so the lines after it stand one level deeper.</summary>
    public static bool Opens(ListingRegionBoundary boundary) => boundary is ListingRegionBoundary.TryStart
        or ListingRegionBoundary.FilterStart or ListingRegionBoundary.HandlerStart;
}

/// <summary>One line of a method's exception-handling structure, placed among a stretch's instructions (DESIGN-listing.md section 6.9):
/// it prints before the instruction at <see cref="Offset"/>, or after the stretch's last instruction when no instruction of the stretch
/// is at or after it.</summary>
/// <param name="Offset">The IL offset of the boundary.</param>
/// <param name="Boundary">Which boundary the line is.</param>
/// <param name="Region">The clause the boundary belongs to, as its 1-based position in <see cref="ListingMethod.Regions"/>; for a protected
/// block several clauses share, the first of them.</param>
/// <param name="Depth">The nesting depth of the line: 0 outside every block of the method, one more inside each block that contains the
/// boundary.</param>
public sealed record ListingRegionMark(int Offset, ListingRegionBoundary Boundary, int Region, int Depth);

/// <summary>One stretch of a COBOL statement's object code.</summary>
/// <param name="Line">The listing line the statement begins on.</param>
/// <param name="Column">The 1-based character position of the statement's first character in that line's text.</param>
/// <param name="Method">The <see cref="ListingMethod.Id"/> of the method holding the code.</param>
/// <param name="Start">The IL offset of the statement's sequence point.</param>
/// <param name="End">The IL offset after the stretch: the next statement's sequence point in the method, or the end of its body.</param>
/// <param name="Instructions">The IL instructions of the stretch, in order, when the object-code section was asked for; null
/// otherwise.</param>
/// <param name="Regions">The lines of the method's exception-handling structure the stretch prints among its instructions, in the order
/// they print, when the object-code section was asked for; null otherwise. A line prints in the stretch whose code it bounds: a line that
/// opens a block in the stretch holding its offset, a line that closes one in the stretch holding the block's last instruction (so a block
/// inside one stretch opens and closes in it); one whose code no stretch of the map holds, in the method's next stretch, before its first
/// instruction, or, when there is none, after the last instruction of the method's last stretch.</param>
/// <param name="Depth">The nesting depth (<see cref="ListingRegionMark.Depth"/>) in effect at the stretch's first line, its first
/// instruction or its first region line: the depth the method's earlier stretches leave, which is the number of the method's blocks that
/// contain the stretch's start when the stretch before it ends there.</param>
public sealed record ListingObjectRange(int Line, int Column, int Method, int Start, int End,
    IReadOnlyList<ListingInstruction>? Instructions, IReadOnlyList<ListingRegionMark>? Regions, int Depth);

/// <summary>One IL instruction (ECMA-335 partition III), decoded from the method body.</summary>
/// <param name="Offset">The instruction's IL offset in its method.</param>
/// <param name="OpCode">The instruction's name (<c>ldarg.0</c>, <c>call</c>, <c>br.s</c>).</param>
/// <param name="Operand">The operand as text, or null for an instruction that takes none: a number, a quoted string, a branch
/// target as <c>IL_xxxx</c>, a switch's targets, or the type, field or method a token names (<c>Type::Member</c>).</param>
public sealed record ListingInstruction(int Offset, string OpCode, string? Operand);
