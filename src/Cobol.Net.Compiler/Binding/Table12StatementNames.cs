// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// ISO Table 12 statement names (§14.5.1: "Statement names are identified in Table 12, Procedural statements"),
/// resolved from the statement KIND — the parse rule — never from the source's leading tokens. §15.32.3 r3 makes
/// this the answer FUNCTION EXCEPTION-STATEMENT must give: "The names of the statements are given in Table 12,
/// Procedural statements, in the column labeled 'Statement name'."
///
/// <para>⛔ <b>THE SPELLED-TOKEN AXIS IS THE WRONG AXIS, AND NOT ONLY FOR THE ROW THAT EXPOSED IT</b> (kb/Work
/// R04: the first token gave <c>GO</c> where Table 12 requires <c>GO TO</c>). No token repair can work: TO is an
/// optional word (<c>goToStatement : GO TO? …</c>), so <c>GO PARA.</c> is a GO TO statement whose tokens never
/// contain TO — both the "first token is GO" special case and a longest-token-match against the name set return
/// GO for it. The parse rule IS the kind, and the mechanical camelCase→spaced-uppercase projection of the rule
/// name matches the Table 12 row for every regular rule (<c>goTo</c> → <c>GO TO</c> falls out of the general
/// mechanism), so a newly added statement rule gets a correct name automatically.</para>
///
/// <para>The exception map carries only rules whose NAME differs from their Table 12 row: finer-grained formats
/// (§14.9.10.1 — DELETE FILE is a format of the DELETE statement; §14.9.37 — SEARCH ALL is Format 2 of the
/// SEARCH statement) and the Wave-H facility arms, whose rule names carry disambiguating prefixes/suffixes the
/// statement name does not. Rules with no 2023 Table 12 row at all (ALTER · ENTRY · ENTER · USE · NEXT SENTENCE
/// · the 2023 inline method invocation) keep their projected names as this implementation's DOCUMENTED answer —
/// strictly-conforming source cannot observe them (&gt;&gt;TURN is 2002+ and those statements were removed in
/// 2002, never raise, or bind loud), and a name is strictly better than a wrong sibling's name if one ever
/// escapes. <c>Table12StatementNameDriftTests</c> re-derives ALL of this from the scraped spec table and the
/// grammar's own <c>statement</c> alternatives, both directions, so neither the map nor the projection can rot
/// silently.</para>
/// </summary>
internal static class Table12StatementNames
{
    /// <summary>The §15.32.3 r3 statement name for a parsed statement. Every <c>statement</c> alternative is a
    /// single rule reference, so the kind is child 0's rule — asserted loudly, because a grammar restructure
    /// that breaks the shape must fail here, never record a wrong name into the last-exception state.</summary>
    public static string NameOf(Core.StatementContext s) =>
        NameOfRule(Core.ruleNames[
            (s.GetChild(0) as ParserRuleContext ?? throw new InvalidOperationException(
                "statement's first child is not a rule reference — the `statement` grammar rule changed shape "
                + "and Table12StatementNames must follow it")).RuleIndex]);

    /// <summary>Rule name → Table 12 name (internal so the drift test feeds every grammar alternative through
    /// the SAME path the compiler uses).</summary>
    internal static string NameOfRule(string ruleName)
    {
        string stem = ruleName.EndsWith("Statement", StringComparison.Ordinal) ? ruleName[..^9] : ruleName;
        return Irregular.TryGetValue(stem, out string? name) ? name : Project(stem);
    }

    /// <summary>The rules whose NAME differs from their Table 12 row — never a rule whose projection already
    /// matches (the drift test rejects a redundant entry by construction: it feeds every alternative through
    /// <see cref="NameOfRule"/> and compares against the scraped table).</summary>
    private static readonly FrozenDictionary<string, string> Irregular = new Dictionary<string, string>
    {
        ["deleteFile"] = "DELETE",          // §14.9.10.1 — the DELETE FILE format of the DELETE statement
        ["searchAll"] = "SEARCH",           // §14.9.37 — SEARCH Format 2 (binary search) of the SEARCH statement
        ["mcsReceive"] = "RECEIVE",         // Wave-H facility arms: the mcs/facility affixes disambiguate the
        ["mcsSend"] = "SEND",               // GRAMMAR rules (§4.2.6 recognize-and-name), not the statement names
        ["validateFacility"] = "VALIDATE",
        ["commitFacility"] = "COMMIT",
        ["rollbackFacility"] = "ROLLBACK",
    }.ToFrozenDictionary();

    /// <summary>camelCase → spaced uppercase: <c>goTo</c> → <c>GO TO</c>, <c>nextSentence</c> →
    /// <c>NEXT SENTENCE</c>, <c>accept</c> → <c>ACCEPT</c>.</summary>
    private static string Project(string stem)
    {
        Span<char> buf = stackalloc char[stem.Length * 2];
        int n = 0;
        foreach (char c in stem)
        {
            if (char.IsUpper(c)) buf[n++] = ' ';
            buf[n++] = char.ToUpperInvariant(c);
        }
        return new string(buf[..n]);
    }
}
