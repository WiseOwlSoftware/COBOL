// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// The <b>Compiler-directive words</b> of each edition — §2002:8.12, §2014:8.12 and §2023:8.12 — the ONE representation of the
/// words "reserved in compiler directives" (kb/Work PB1366, PB5599). §2023:7.3.3 SR7 reserves each of them within the context
/// of a directive that specifies it, and §2023:7.3.11.3 SR1 / §2023:7.3.8.4.3 SR1 forbid any of them as a
/// compilation-variable-name; before PB1366 no stage consulted the list, because the list had no representation in
/// <c>src</c> at all, so <c>&gt;&gt;DEFINE OFF AS 1</c> and <c>&gt;&gt;IF LISTING IS DEFINED</c> were accepted.
///
/// <para><b>One table, one edition window per word</b> (kb/Work PB5599). The three printed tables are not one list:
/// §2014:8.12 adds <c>&lt;&gt;</c>, FLAG-02 and MOVE-TO-SAME-NAME to §2002:8.12's, and §2023:8.12 adds 27 more (PUSH, POP,
/// EQUATE, SUBSTITUTE, COBOL-WORDS, FLAG-14, DISPLAY, ...) and drops FLAG-85 and FLAG-NATIVE-ARITHMETIC. Every word
/// therefore carries the first edition whose table prints it and the last one that does, so a name that is a directive
/// word only in another edition (<c>&gt;&gt;DEFINE PUSH AS 1</c> at --std 2002, <c>&gt;&gt;DEFINE FLAG-85 AS 1</c> at --std
/// 2023) is neither wrongly refused nor wrongly accepted. COBOL-85 has no compiler directives (§2002:7.2 is the first
/// edition to hold the facility), so no word is reserved in a directive below 2002.</para>
///
/// <para>The words are data, transcribed once from the printed tables and held equal to each edition's by
/// <c>CompilerDirectiveWordsDriftTests</c>; that test also asserts every word of the directive catalog
/// (<see cref="WiseOwl.Cobol.Editions.CompilerDirectiveCatalog"/>) is reserved at every edition at which its directive
/// exists, so a new directive cannot be added without its word being reserved. "In addition to the above list, all of the
/// exception-names specified in 14.6.13.1, Exception conditions, are reserved in the context of compiler directives" —
/// those are the <see cref="ExceptionCatalog"/>'s, asked through it (at the edition that introduced each), never copied.</para>
/// </summary>
public static class CompilerDirectiveWords
{
    /// <summary>One word of the tables: the first edition whose §8.12 prints it and the last one that does.</summary>
    private readonly record struct Entry(string Word, int From, int Through);

    private const int Last = int.MaxValue;

    private static Entry[] Build()
    {
        var all = new List<Entry>();
        void Words(int from, int through, params string[] words)
        {
            foreach (string w in words) all.Add(new Entry(w, from, through));
        }

        // The words every table prints: §2002:8.12, §2014:8.12 and §2023:8.12 alike.
        Words(2002, Last,
            "ALL", "AND", "AS",
            "B-AND", "B-NOT", "B-OR", "B-XOR",
            "CALL-CONVENTION", "CHECKING", "COBOL", "CORRESPONDING",
            "DE-EDITING", "DEFINE", "DEFINED", "DIVIDE",
            "ELSE", "END-EVALUATE", "END-IF", "EQUAL", "EVALUATE",
            "FIXED", "FORMAT", "FREE", "FUNCTION-ARGUMENT",
            "GREATER",
            "IF", "IMP", "IS",
            "LEAP-SECOND", "LESS", "LISTING", "LOCATION",
            "MOVE",
            "NOT", "NUMVAL",
            "OFF", "ON", "OR", "OTHER", "OVERRIDE",
            "PAGE", "PARAMETER", "PROPAGATE",
            "SET", "SIZE", "SOURCE", "STANDARD-1", "STANDARD-2",
            "THAN", "THROUGH", "THRU", "TO", "TRUE", "TURN",
            "WHEN", "WITH",
            "ZERO-LENGTH",
            "+", "-", "*", "/", "<=", ">=", "<", ">", "=", "(", ")");
        // Printed by §2002:8.12 and §2014:8.12, dropped by §2023:8.12 (Annex E.2 item 21).
        Words(2002, 2014, "FLAG-85", "FLAG-NATIVE-ARITHMETIC");
        // First printed by §2014:8.12.
        Words(2014, Last, "<>", "FLAG-02", "MOVE-TO-SAME-NAME");
        // First printed by §2023:8.12.
        Words(2023, Last,
            "B-SHIFT-L", "B-SHIFT-R", "B-SHIFT-LC", "B-SHIFT-RC",
            "COBOL-WORDS", "COMPILE-TIME-ARITHMETIC-EXPRESSIONS",
            "DISPLAY", "EQUATE", "EXCLUSIVE-OR", "EXTERNAL-FILE-FILE-STATUS",
            "FLAG-14",
            "I-O-DECLARATIVE", "I-O-STATUS-04", "I-O-STATUS-07",
            "POP", "PUSH",
            "READ-PREVIOUS", "REF-MOD-ZERO-LENGTH", "RESERVE",
            "SUBSTITUTE",
            "UNDEFINE", "UPON",
            "VALUE-EDITING", "VALUE-FIG-CON-LENGTH", "VALUE-ZERO",
            "WRITE-END-OF-PAGE",
            "XOR");
        return [.. all];
    }

    private static readonly Entry[] Table = Build();

    /// <summary>The reading side: word to the entry's window.</summary>
    private static readonly Dictionary<string, Entry> ByWord = Table.ToDictionary(e => e.Word, CobolNames.Comparer);

    /// <summary>The words of the §8.12 table of <paramref name="edition"/> (2002, 2014 or 2023), in no printed order; empty
    /// below 2002, which has no compiler directives.</summary>
    public static IReadOnlyList<string> Of(int edition) =>
        [.. Table.Where(e => e.From <= edition && edition <= e.Through).Select(e => e.Word)];

    /// <summary>True when <paramref name="word"/> is reserved in compiler directives at <paramref name="edition"/> (the
    /// targeted edition's year): a word of that edition's §8.12 table, or an exception-name of the edition's
    /// exception-condition clause (§2023:14.6.13.1; the <c>EC-USER-</c> family and the <c>EC-IMP-suffix</c> FORM included:
    /// §2023:8.12 reserves the family whether or not an implementor defines a member). Case-insensitive, like every COBOL
    /// word (§2023:8.3.1).</summary>
    public static bool IsReserved(string word, int edition) =>
        (ByWord.TryGetValue(word, out var e) && e.From <= edition && edition <= e.Through)
        || (edition >= 2002
            && ((ExceptionCatalog.TryGet(word, out var ec) && ec.IntroducedIn <= edition)
                || ExceptionCatalog.IsImplementorSuffixForm(word)));
}
