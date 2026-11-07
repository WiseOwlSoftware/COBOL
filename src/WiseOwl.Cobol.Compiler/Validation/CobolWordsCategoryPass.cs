// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;

namespace WiseOwl.Cobol.Validation;

/// <summary>
/// THE <c>&gt;&gt;COBOL-WORDS</c> CATEGORY RULES (ISO §2023:7.3.10.3 SR3 and SR4; kb/Work PB1373, PB4239, PB4586): the EXISTING
/// word of a directive (literal-1, -3 and -4) shall be a reserved word, a context-sensitive word or an intrinsic function
/// name, and the NEW word (literal-2, -5 and -6) shall be none of those. The frontend's directive stage enforces the
/// text-stage rules SR1, SR2 and SR5 (<c>CobolWordsDirectiveProcessor</c>); these two need the three word registries,
/// which only the compiler reaches (§2023:8.11's list is <see cref="IntrinsicCatalog"/>), so they are judged HERE.
/// <para>⛔ <b>They need no parse tree, so they run beside the parse, never inside a pass that needs one</b>
/// (<c>CompilerDriver.Compile</c>, before the frontend gate). They used to run inside <see cref="VersionConformancePass"/>,
/// the terminal bind pass, and a directive that violated SR3 or SR4 in a program that also failed to parse reported only the
/// parse error (kb/Work PB4586: <c>&gt;&gt;COBOL-WORDS UNDEFINE "AUTHOR"</c> plus <c>MOVE TO.</c>). The directive's verdict
/// does not depend on the program it heads.</para>
/// </summary>
internal static class CobolWordsCategoryPass
{
    /// <summary>Judge every operation of <paramref name="map"/>, reporting COBOL1623 to <paramref name="sink"/> at the
    /// directive's own line and column.</summary>
    public static void Run(CobolWordsMap map, IDiagnosticSink sink)
    {
        if (map.IsEmpty) return;
        foreach (var op in map.Ops)
        {
            using var _ = sink.At(op.At);   // the directive's own line and column (kb/Work PB1925)
            // The existing word is a KEYWORD, written in basic letters: one written with an extended letter (DİSPLAY,
            // U+0130) is no reserved word, and the fold never makes it one (§2023:8.1.3.2 3) a), 4) b); kb/Work PB4240, PB4420).
            if (op.Existing is { } e && !IsReservedContextOrIntrinsic(e))
                Invalid(sink, $"the existing word '{e}' is not a reserved word, "
                    + "context-sensitive word, or intrinsic-function name (ISO §2023:7.3.10.3 SR3)");
            if (op.New is { } n && IsReservedContextOrIntrinsic(n))
                Invalid(sink, NewWordRefusal(n));
        }
    }

    /// <summary>SR4's sentence for <paramref name="word"/>, which names the cause: a word this implementation reserves for a
    /// nonstandard extension (§2023:4.2.10 3), docs/CONFORMANCE.md D-RW1) is reserved though no list of the standard holds it, and
    /// the user who wrote it as a new word must be told which reservation refuses it (kb/Work PB4239).</summary>
    private static string NewWordRefusal(string word) =>
        !IsOnTheStandardsLists(word) && NonstandardExtensionRegister.ReservesWord(word)
            ? $"the new word '{word}' is a word this implementation reserves for a nonstandard extension "
              + "(ISO §2023:4.2.10; docs/CONFORMANCE.md D-RW1) and cannot be a user-defined word (ISO §2023:7.3.10.3 SR4)"
            : $"the new word '{word}' is a reserved / context-sensitive / "
              + "intrinsic-function word and cannot be a user-defined word (ISO §2023:7.3.10.3 SR4)";

    /// <summary>⛔ THE POPULATION SR3 AND SR4 SPEAK OF, in one place (kb/Work PB1373, PB4239): "a reserved word, a
    /// context-sensitive word, or an intrinsic function name" — §2023:8.9, §2023:8.10 and §2023:8.11 as the COBOL-2023 text lists them,
    /// because the directive is a COBOL-2023 introduction (<see cref="CobolWordsMap.DirectiveEdition"/>) and no other
    /// edition ever asks — AND the reserved words this implementation adds as nonstandard extensions (§2023:4.2.10 3), the
    /// register's <see cref="NonstandardExtension.ReservedWords"/>). A word the compiler reserves is a reserved word
    /// (§2023:8.3.2.1 1)), whoever's list holds it: §2023:7.3.10.1 and D.12.1 give the directive to a programmer so a program written
    /// to an implementor's extension, and D.12.3 so a change of compiler that adds reserved words conflicting with existing
    /// code, may free or respell them (owner decision of 2026-10-31, kb/Work PB4239).
    /// <para>It used to be two guesses that disagreed with the standard in opposite directions: the SR3 test accepted
    /// anything the LEXER tokenizes or ANY edition's §2023:8.9 row carries (so <c>UNDEFINE "AUTHOR"</c>, a word reserved in 1985
    /// and gone from §2023:8.9, compiled), and the SR4 test rejected anything the lexer tokenizes (so <c>RESERVE "AUTHOR"</c>, a
    /// fresh word, was refused). The lexer's vocabulary is what this compiler TOKENIZES, which is not a population of the
    /// standard; the §2023:8.9 flag is the 2023 list itself (<c>ReservedWordsDriftTests</c>), the §2023:8.10 table is the 2023 list
    /// (every row, whatever edition introduced it: <c>ContextSensitiveWordEntry.Introduced</c> serves the diagnostics'
    /// edition-aware naming, not this test), an intrinsic function is a name of the edition inside its window
    /// (<see cref="IntrinsicSig.IsDefinedAt"/>), and the extension words are the register's rows, so the next
    /// <c>extensionReserved</c> word reaches SR3 and SR4 with no edit here.</para></summary>
    internal static bool IsReservedContextOrIntrinsic(string w) =>
        IsOnTheStandardsLists(w) || NonstandardExtensionRegister.ReservesWord(w);

    /// <summary>§2023:8.9 at 2023, §2023:8.10, and §2023:8.11 inside the directive's own edition: the three lists the standard prints.</summary>
    private static bool IsOnTheStandardsLists(string w) =>
        ReservedWords.Find(w) is { R2023: true } || ContextSensitiveWords.Contains(w)
        || (IntrinsicCatalog.TryGet(w, out var function) && function.IsDefinedAt(CobolWordsMap.DirectiveEdition));

    private static void Invalid(IDiagnosticSink sink, string message) =>
        sink.Report(new EditionDiagnostic(DiagnosticCatalog.CobolWordsDirectiveInvalid.Code, EditionSeverity.Error,
            "cobol-words-directive-invalid", message, "", "ISO §2023:7.3.10.3"));
}
