// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE FORM A DISPLAY STATEMENT GIVES A NUMERIC OPERAND — the compile option <c>--numeric-display</c> and this
/// implementation's answer to ISO/IEC 1989:2023 §14.9.11.4 GR1: "Any conversion of data required between literal-1 or
/// the data item referenced by identifier-1 and the device is defined by the implementor" (Annex A.1 item 56,
/// docs/CONFORMANCE.md DOC-A.1-56). kb/Work PB3505 implements the owner's decision of kb/Work PB3467 (2026-10-21,
/// "yes"): the default follows GnuCOBOL 3.2.0's default configuration, the rule-1 precedent for an implementor
/// latitude the standard leaves open, and today's digit image stays behind the option.
/// <para>The form is a property of the COMPILED PROGRAM, never of the process — the compiler chooses which of two
/// runtime conversions a DISPLAY operand is emitted against, exactly as <c>--sign-encoding</c> bakes its convention into
/// every emitted profile. It reaches only DISPLAY's GR1 device conversion: a MOVE of the same item to an alphanumeric
/// receiver is §14.9.25.4 GR6's, and a record image is the item's storage (§14.9.30.4 GR14), whatever this option says.</para>
/// </summary>
public enum NumericDisplayForm
{
    /// <summary>THE DEFAULT, at every <c>--std</c>. The value as a reader writes it: the item's digit positions with
    /// the decimal point at the scale's place (the program's decimal separator under DECIMAL-POINT IS COMMA), the
    /// implied zeros of a P scaling written, and a SIGNED item's sign as a leading <c>+</c> or <c>-</c> character —
    /// <c>PIC 9(5)V99</c> holding 12.5 shows <c>00012.50</c>, <c>PIC S9(5)V99</c> holding -12.5 shows <c>-00012.50</c>.
    /// GnuCOBOL 3.2.0's <c>default</c>, <c>cobol85</c>, <c>cobol2002</c>, <c>cobol2014</c> and <c>mf</c> dialects.</summary>
    Decimal = 0,

    /// <summary>The digit image: the item's digits as stored with no decimal point (the point is implied), an
    /// unseparated sign over-punched onto a digit per <c>--sign-encoding</c> (<c>0001250</c> for 12.5 in
    /// <c>PIC 9(5)V99</c>, <c>000125{</c> signed). What IBM Enterprise COBOL shows under its default <c>DISPSIGN(COMPAT)</c>,
    /// and GnuCOBOL's <c>ibm</c> dialect up to its sign placement.</summary>
    Digits = 1,
}

/// <summary>The one reader of the <c>--numeric-display</c> vocabulary: the CLI's parser and the conformance harness's
/// <c>wiseowl/options.json</c> both call it, so they cannot disagree about what <c>digits</c> means.</summary>
public static class NumericDisplayFormOption
{
    /// <summary>The accepted spellings, in <see cref="NumericDisplayForm"/> order — the enum's own member names lower-cased,
    /// so a form added to the enum brings its spelling, its help text and its acceptance with it.</summary>
    public static IReadOnlyList<string> OptionSpellings { get; } =
        [.. Enum.GetNames<NumericDisplayForm>().Select(name => name.ToLowerInvariant())];

    /// <summary>Parse an option value (case-insensitive). An absent value (null) is the default,
    /// <see cref="NumericDisplayForm.Decimal"/>, and returns false; so does an unrecognized one (a numeric spelling is
    /// refused: <c>Enum.TryParse</c> alone would accept <c>"1"</c>, an internal ordinal and no part of the contract).</summary>
    public static bool TryParse(string? value, out NumericDisplayForm form)
    {
        form = NumericDisplayForm.Decimal;
        if (value is null) return false;
        NumericDisplayForm[] forms = Enum.GetValues<NumericDisplayForm>();
        for (int i = 0; i < forms.Length; i++)
            if (OptionSpellings[i].Equals(value, StringComparison.OrdinalIgnoreCase))   // not a COBOL word: a command-line option value
            {
                form = forms[i];
                return true;
            }
        return false;
    }
}
