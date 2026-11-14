// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Frontend.Parsing;

/// <summary>Which written shape a <c>negatedAbbreviatedRelation</c> (<c>NOT</c> before the relational operator of an
/// abbreviated relation whose next word is not GREATER, &gt;, LESS, &lt;, EQUAL or =) has. The editions read the two
/// shapes differently, so the edition gate and the binder both ask THIS classifier (kb/Work PB5123, review R1-1).</summary>
public enum NegatedAbbreviationForm
{
    /// <summary><c>NOT</c> before an IS-led simple relational operator with no NOT of its own (<c>NOT IS &gt; C</c>,
    /// <c>NOT IS EQUAL TO C</c>). At 85 the NOT is the logical operator (§1985:VI.6.3.3 GRL2.2). ISO 2002 prints
    /// <c>[NOT] simple-relational-operator</c> in the abbreviated tail (§2002:8.8.4.2.4.1), and GR2 b) of
    /// §2002:8.8.4.2.4.3 makes that NOT "part of the simple relational operator", so at 2002 it reads
    /// <c>A NOT &gt; C</c> and carries <c>NOT &gt;</c>. The 2014 general format (§2014:8.8.4.2.4.1) drops the
    /// bracketed NOT: from 2014 the shape is in no format.</summary>
    IsLedSimpleOperator,

    /// <summary>Every other shape: <c>NOT</c> before an extended relational operator (<c>NOT &gt;= C</c>, <c>NOT IS
    /// LESS THAN OR EQUAL TO C</c>, <c>NOT IS &lt;&gt; C</c>) or before an IS-led operator carrying its own NOT
    /// (<c>NOT IS NOT &gt; C</c>). The logical NOT of 85 (§1985:VI.6.3.3 GRL2.2); ISO 2002 prohibits the first
    /// (§2002:F.1 item 22) and the second (§2002:8.8.4.2.4.2 SR3), and no later format admits either.</summary>
    LogicalNotOnly,
}

/// <summary>⛔ THE ONE READING OF A NEGATED ABBREVIATED RELATION'S SHAPE (kb/Work PB5123, review R1-1).</summary>
public static class NegatedAbbreviationForms
{
    /// <summary>The shape of <paramref name="nar"/>. §2002:8.7.5.1 Format 1 prints the simple relational operators
    /// as <c>IS [NOT] GREATER THAN</c>, <c>IS [NOT] &gt;</c>, <c>IS [NOT] LESS THAN</c>, <c>IS [NOT] &lt;</c>,
    /// <c>IS [NOT] EQUAL TO</c> and <c>IS [NOT] =</c>; the grammar's predicate already excluded the IS-less ones, so an
    /// IS-led one with no NOT, no OR (the extended word forms) and none of the extended symbols is the first shape.
    /// Single-child accessors only: this runs once in the edition pass and once at bind.</summary>
    public static NegatedAbbreviationForm Of(CobolParserCore.NegatedAbbreviatedRelationContext nar)
    {
        var op = nar.abbreviatedRelation().comparisonOperator();
        return op.IS() is not null && op.NOT() is null && op.OR() is null
               && op.GTEQUAL() is null && op.LTEQUAL() is null && op.NOTEQUAL() is null
            ? NegatedAbbreviationForm.IsLedSimpleOperator
            : NegatedAbbreviationForm.LogicalNotOnly;
    }
}
