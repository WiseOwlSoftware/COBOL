// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>⛔ THE ONE STATEMENT OF HOW A CALL ARGUMENT LIST'S PHRASES GIVE EACH ARGUMENT ITS PASS MODE (ISO §2023:14.9.4.4 GR5
/// and GR9; kb/Work PB3227). Two elements ask it: <see cref="CallBinder.BindCall"/>, which binds each argument, and
/// <see cref="StorageClaimScan"/>, which decides before any statement binds which items an activation may pass BY REFERENCE and
/// so must live in a cell (§2023:14.2.3 GR8: the argument and its formal parameter are one storage area). Each used to write
/// the rule itself, and the scan's copy applied Format 1's GR5 to both formats: in a Format-2 CALL a keyword-less identifier
/// after a BY VALUE or BY CONTENT phrase, which GR9 a) 1. passes BY REFERENCE to a BY REFERENCE formal, was never claimed, so
/// a formal laid over it by REDEFINES held a private copy and a store through it never reached the other formal sharing the
/// argument.
/// <list type="bullet">
/// <item>Format 1, GR5: "Both the BY CONTENT and BY REFERENCE phrases are transitive across the parameters that follow them
/// until another BY CONTENT or BY REFERENCE phrase is encountered. If neither the BY CONTENT nor the BY REFERENCE phrase is
/// specified prior to the first parameter, the BY REFERENCE phrase is assumed."</item>
/// <item>Format 2, GR9: an argument written without BY REFERENCE, BY CONTENT or BY VALUE takes its mode from the
/// CORRESPONDING FORMAL: a) BY REFERENCE (1. when the argument meets Syntax rule 3, else 2. BY CONTENT), b) BY VALUE when the
/// formal is BY VALUE. A phrase written on an earlier argument does not reach it.</item>
/// </list></summary>
internal static class CallArgumentModes
{
    /// <summary>The phrase written on <paramref name="a"/>: BY REFERENCE, BY CONTENT or BY VALUE, or null for a keyword-less
    /// argument (including a keyword-less OMITTED or address-identifier).</summary>
    public static CobolPassMode? WrittenPhrase(Core.CallArgumentContext a) =>
        a.callByReference() is not null ? CobolPassMode.Reference
        : a.callByContent() is not null ? CobolPassMode.Content
        : a.callByValue() is not null ? CobolPassMode.Value
        : null;

    /// <summary>§2023:14.9.4.4 GR5's TRANSITIVE mode after argument <paramref name="a"/>: the phrase it writes, else the one carried
    /// in (<paramref name="carried"/>). Every element that threads the mode through an argument list reads it here.</summary>
    public static CobolPassMode CarriedAfter(Core.CallArgumentContext a, CobolPassMode carried) => WrittenPhrase(a) ?? carried;

    /// <summary>The mode of a keyword-less IDENTIFIER argument, before GR9 a)'s Syntax-rule-3 test. Format 1
    /// (<paramref name="formatTwo"/> false): GR5's carried mode. Format 2: GR9 b) BY VALUE when the corresponding formal is BY VALUE,
    /// a) 1. BY REFERENCE otherwise; a) 2.'s BY CONTENT for an argument that does not meet Syntax rule 3 is asked of the resolved
    /// item by <see cref="CallBinder"/>, the one element that resolves it. <paramref name="formalByValue"/> null: no formal is known
    /// (a prototype compiled elsewhere), and the carried mode stands.</summary>
    public static CobolPassMode KeywordLessIdentifier(bool formatTwo, CobolPassMode carried, bool? formalByValue) =>
        !formatTwo || formalByValue is not { } byValue ? carried
        : byValue ? CobolPassMode.Value
        : CobolPassMode.Reference;

    /// <summary>The mode of a Format-2 keyword-less argument that is no identifier of storage: literal-2 (or a constant-name, which
    /// substitutes one), a function's or invocation's temporary, or arithmetic-expression-1. None is "a data item defined in the
    /// file, working-storage, local-storage, or linkage section", so none meets Syntax rule 3: GR9 a) 2. gives BY CONTENT, and GR9
    /// b) BY VALUE when the corresponding formal is BY VALUE. With no formal known, BY CONTENT is the format's own default.</summary>
    public static CobolPassMode KeywordLessValue(bool? formalByValue) =>
        formalByValue is true ? CobolPassMode.Value : CobolPassMode.Content;

    /// <summary>Whether the formal at <paramref name="position"/> of <paramref name="formals"/> is BY VALUE: null when no formal list
    /// is known; false past its end (the count rule, §2023:14.8.2.1, reports the surplus argument).</summary>
    public static bool? FormalIsByValue(IReadOnlyList<LinkageFormal>? formals, int position) =>
        formals is null ? null : position < formals.Count && formals[position].ByValue;
}
