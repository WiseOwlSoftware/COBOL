// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.CompilerServices;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>⛔ THE DECLARATIONS GAVE THIS PARENTHESIS TO THE NEXT ITEM OF ITS LIST (kb/Work PB136, PB5781). Inside a list that
/// separates its items by a space (a subscript list, an argument list: §2023:8.3.5 SR1), the lexer types the '(' after any
/// word that could name a data item as that reference's own paren, and the parse hangs it on the name. Whether the name
/// owns it is a question of its DESCRIPTION, which no parse can answer: a name on which no subscript may be written
/// (§2023:8.4.2.3.3 SR2) cannot own it, so the paren begins the NEXT item, a parenthesized expression — Annex D.3.5.3's
/// <c>DOG (XCOUNTER (- YCOUNTER))</c> is two subscripts, and <c>FUNCTION MIN (A (- B))</c> two arguments, the reading
/// §1985:VI.6.2.3 SR4 requires of a unary-led expression after an identifier.
/// <para>Marked by the binder's ONE list reader (<c>SubscriptBinder.GiveAwayClosingList</c>) when it reads the list the item
/// stands in, before any operand of it binds, and read by every reader of a written reference
/// (<c>ReferenceResolver.ReadWritten</c>) and by the edition gate (<c>VersionConformancePass</c>): a paren given away is not
/// that reference's subscript list. The mark is a weak annotation of the parse node — it lives exactly as long as the
/// tree — rather than a member of the generated context type, which stays the generator's alone.</para></summary>
public static class SubscriptPartOwnership
{
    private static readonly ConditionalWeakTable<CobolParserCore.SubscriptPartContext, object> GivenAway = new();
    private static readonly object Given = new();

    /// <summary>Mark <paramref name="list"/> as the first item of the next list item. Idempotent.</summary>
    public static void GiveToNextItem(this CobolParserCore.SubscriptPartContext list) => GivenAway.AddOrUpdate(list, Given);

    /// <summary>Did the declarations give <paramref name="list"/> to the next item of its list?</summary>
    public static bool IsGivenToNextItem(this CobolParserCore.SubscriptPartContext list) => GivenAway.TryGetValue(list, out _);
}
