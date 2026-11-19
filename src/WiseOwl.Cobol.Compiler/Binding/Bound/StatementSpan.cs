// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>
/// ⛔ WHERE A STATEMENT IS WRITTEN IN THE FILE THE USER EDITS (DESIGN-listing.md section 6.9): the physical file, and the 1-based line
/// and column of its first character and of the position after its last, as a debugger and the source listing count them (a
/// character of the line is one column). Every backend places the statement's sequence point here, so a .NET debugger steps
/// through COBOL source and the listing's object-code map reads each statement's code back from the PDB at this position.
/// </summary>
/// <remarks>Backend-neutral: a COBOL source position, never a position in generated code. Stamped once, by
/// <c>StatementBinder.BindStatement</c> (the one funnel every statement passes through), from the statement's first and last
/// tokens through the one resultant-to-physical mapping (<c>EditionContext.LocationOf</c>). A statement whose last token lies in
/// another file than its first (a COPY statement ends it) ends where it begins: a sequence point spans one file.</remarks>
/// <param name="File">The path of the file holding the statement's first character, as the compilation read it.</param>
/// <param name="Line">The 1-based line of the first character.</param>
/// <param name="Column">The 1-based column of the first character.</param>
/// <param name="EndLine">The 1-based line of the last character.</param>
/// <param name="EndColumn">The 1-based column after the last character (exclusive).</param>
/// <param name="VerbEndColumn">The 1-based column after the last character of the statement's first word, its verb, on
/// <paramref name="Line"/> (exclusive). A backend places a statement's later sequence points there (the code it compiles after a
/// statement nested in it: a PERFORM's step and test), so a reader of the PDB tells the statement's first sequence point, which spans
/// the statement, from its later ones, which span the verb.</param>
public sealed record StatementSpan(string File, int Line, int Column, int EndLine, int EndColumn, int VerbEndColumn);
