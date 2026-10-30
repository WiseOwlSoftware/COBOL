// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ A WORD §8.9 LEAVES FREE IS AN UNDECLARED USER WORD, NOT THE PREDEFINED IDENTIFIER IT SPELLS (kb/Work PB655's
/// determination; PB1673). SELF, SUPER, NULL and ADDRESS are reservation-gated: a program that DECLARES one at an
/// edition where §8.9 leaves it free has every occurrence retyped to a user-defined word before the parse, so a keyword
/// token that still reaches a binder there names nothing the program declared — the §8.4.2.1 undefined reference
/// (COBOL1639), never a rule about a predefined identifier the edition does not have (COBOL-85 has no SELF, NULL or
/// address-identifier). ONE statement of that verdict, for the procedure division's receiving operands
/// (<c>ExpressionBinder.RefuseNonReceivable</c>) and for every position the predefined NULL's own rule reports
/// (<see cref="PredefinedNullRule"/>).
/// </summary>
internal static class UnreservedKeywordReference
{
    /// <summary>True when §8.9 leaves <paramref name="word"/> free at the edition being compiled — it is then a
    /// user-defined word, and a token of it the parse kept as the keyword names nothing declared.</summary>
    public static bool IsFreeAt(string word, EditionContext edition) =>
        !Editions.ReservedWordSet.Default.RejectsAt(CobolNames.UpperFold(word), edition.Edition.Year);

    /// <summary>Report <paramref name="word"/> as the undeclared user-defined word it is at this edition.</summary>
    public static void Report(EditionContext edition, string word)
    {
        string head = CobolNames.UpperFold(word);
        edition.Error(DiagnosticCatalog.UndefinedReference, $"'{head}' is not defined — no declaration in this "
            + $"source element gives the name '{head}', and §8.9 does not reserve the word at this edition, so it is "
            + "no predefined identifier here (ISO §8.4.2.1: \"a statement shall contain a reference that uniquely "
            + "identifies that resource\"). Check the spelling, or declare the item.");
    }
}
