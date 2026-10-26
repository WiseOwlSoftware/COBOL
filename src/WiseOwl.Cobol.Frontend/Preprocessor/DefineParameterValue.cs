// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Frontend.Preprocessor;

/// <summary>What an operating-environment value is, as the value of a <c>&gt;&gt;DEFINE … PARAMETER</c> compilation
/// variable (<see cref="DefineParameterValue.Classify"/>).</summary>
internal enum ParameterValueForm
{
    /// <summary>The environment makes no value available (§7.3.11.4 GR4, second sentence): the name is not defined.</summary>
    NoValue,
    /// <summary>The text is the SPELLING of one fixed-point numeric literal: the variable is numeric, and the text is that
    /// literal (§7.3.11.4 GR8).</summary>
    NumericLiteral,
    /// <summary>The text is exactly ONE quoted nonnumeric literal: the variable is that literal (§7.3.11.4 GR8).</summary>
    QuotedLiteral,
    /// <summary>Any other text: the variable is alphanumeric, its value the text exactly as given.</summary>
    Alphanumeric,
}

/// <summary>
/// ⛔ THE ONE RULE FOR WHAT A <c>&gt;&gt;DEFINE … PARAMETER</c> VALUE IS (DOC-A.1-49, kb/Work PB1626). ISO §7.3.11.4 GR4 —
/// "If the PARAMETER phrase is specified, the value referenced by compilation-variable-name-1 is obtained from the
/// operating environment by an implementor-defined method when the DEFINE directive is processed. If no value is made
/// available from the operating environment, compilation-variable-name-1 is not defined" — leaves the FORM of that value to
/// the implementor, so GnuCOBOL 3.2 decides (CLAUDE.md rule 1; <c>cobc/ppparse.y</c>, the <c>PARAMETER</c> action and
/// <c>ppp_check_needs_quote</c>, read and not copied). Its classification is SYNTACTIC, on the text as the environment holds it:
/// <list type="bullet">
/// <item>An empty value, or one that begins with a space, is no value.</item>
/// <item>A value that begins with a quotation symbol is a quoted literal when a closing delimiter of that quotation symbol
/// ends it (§8.3.3.2.3 3): a doubled one is content); one that is not so closed is no value. GnuCOBOL tests only the first and
/// last characters; this compiler holds the value to ONE literal, so <c>"a" &amp; "b"</c> and <c>"a"b"</c> are no value.</item>
/// <item>A value that is the spelling of a fixed-point numeric literal (§8.3.3.3.2, <see cref="NumericLiteral.IsFixedPointLiteralForm"/>)
/// is that numeric literal, of ANY length: its digit capacity is the literal's own limit, enforced where the literal is
/// evaluated (<c>CompileTimeExpressionEvaluator</c>), exactly as for <c>&gt;&gt;DEFINE X AS 12…</c>. GnuCOBOL's test also
/// calls <c>5.</c> numeric; §8.3.3.3.2 3) forbids the decimal point as the rightmost character, so it is alphanumeric here.</item>
/// <item>Anything else — <c>1,000</c>, <c>1E5</c>, <c>7x</c>, <c>5 </c> — is alphanumeric, the text as given.</item>
/// </list>
/// The classification names a form; the VALUE of a numeric or quoted one is made by the same compile-time operand path as a
/// literal written in the directive, so the two can never disagree about a literal's value, capacity or screening.
/// </summary>
internal static class DefineParameterValue
{
    public static ParameterValueForm Classify(string environmentText)
    {
        if (environmentText.Length == 0 || CobolSpace.IsSeparator(environmentText[0])) return ParameterValueForm.NoValue;
        char first = environmentText[0];
        if (CobolSourceText.IsQuotationSymbol(first))
            return environmentText.Length >= 2 && CobolSourceText.ClosingDelimiterAt(environmentText, 1, first) == environmentText.Length - 1
                ? ParameterValueForm.QuotedLiteral
                : ParameterValueForm.NoValue;
        return NumericLiteral.IsFixedPointLiteralForm(environmentText) ? ParameterValueForm.NumericLiteral : ParameterValueForm.Alphanumeric;
    }
}
