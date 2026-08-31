// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Builds the <see cref="OptionsModel"/> from a source unit's OPTIONS paragraph (ISO/IEC 1989:2023 §11.9). The
/// paragraph is now structurally parsed (<c>optionsClause+</c>), so each clause maps to a model field with a typed
/// accessor — no token scanning. An absent paragraph (or clause) leaves the ISO-implied default.
/// </summary>
internal static class OptionsBinder
{
    /// <summary>Read the program unit's OPTIONS paragraph into an <see cref="OptionsModel"/>. The OPTIONS
    /// paragraph was introduced by ISO/IEC 1989:2002 (§11.9 — with the ARITHMETIC clause; Annex E.2 item 21
    /// back-derives the container from obsolete-in-2014 Standard Arithmetic): targeting 85 REJECTS it with an
    /// edition diagnostic and the implied defaults (native arithmetic, nearest-away-from-zero bare ROUNDED)
    /// apply. The 2014-only clauses (DEFAULT ROUNDED, INTERMEDIATE ROUNDING, ENTRY-CONVENTION, FLOAT-BINARY/
    /// -DECIMAL, INITIALIZE; the STANDARD-BINARY/-DECIMAL keywords) carry per-clause 2014 rows in the pass
    /// (P10 Step 12) — a strict 2002 compile fails on those diagnostics before the bound model matters.</summary>
    /// <param name="baseline">The model the unit's clauses override — a contained program's CONTAINER model
    /// (ISO §11.9.4 GR1: the container's clauses apply "unless overridden by a clause in an OPTIONS paragraph in
    /// a contained source element"), null for an outermost unit (the all-defaults model).</param>
    public static OptionsModel Bind(Core.ProgramUnitContext program, EditionContext? edition = null, OptionsModel? baseline = null)
    {
        var start = baseline ?? OptionsModel.Default;
        var paragraphs = program.identificationDivision()?.identificationBody()?.identificationParagraph();
        if (paragraphs is null) return start;

        var options = paragraphs.Select(p => p.optionsParagraph()).FirstOrDefault(o => o is not null);
        return BindParagraph(options, edition, start);
    }

    /// <summary>The paragraph-level core (kb/Work PB135): the OO units carry their OPTIONS paragraph on the
    /// SKELETON (§10.6.1), not inside an identification body, so OoDriver / OoBindMethodData hand the
    /// context in directly — with the container's model as the §11.9.4 GR1 baseline.</summary>
    public static OptionsModel BindParagraph(Core.OptionsParagraphContext? options, EditionContext? edition, OptionsModel? baseline)
    {
        var start = baseline ?? OptionsModel.Default;
        if (options is null) return start;

        // options-paragraph-2002: the pass owns the edition gate (Exec Step E); below 2002 the paragraph is
        // routed INERT (the baseline) — the strict compile fails on the pass's diagnostic before the model matters.
        if (edition is { DialectLevel: < 2002 })
            return start;

        var model = start;
        foreach (var clause in options.optionsClause())
            model = Apply(model, clause, edition);
        return model;
    }

    private static OptionsModel Apply(OptionsModel m, Core.OptionsClauseContext c, EditionContext? edition)
    {
        if (c.arithmeticClause()?.arithmeticMethod() is { } am)
            return m with { Arithmetic = ArithmeticOf(am, edition) };
        if (c.defaultRoundedClause()?.roundingModeName() is { } dr)
            return m with { DefaultRounding = RoundingModes.Map(dr) };
        if (c.entryConventionClause()?.cobolWord() is { } ec)
            return m with { EntryConvention = ec.GetText() };
        if (c.floatBinaryClause()?.endiannessPhrase() is { } fb)
            return m with { FloatBinaryEndianness = EndiannessOf(fb) };
        if (c.floatDecimalClause()?.floatDecimalEncoding() is { } fd)
            return ApplyFloatDecimal(m, fd);
        if (c.intermediateRoundingClause()?.intermediateRoundingMode() is { } ir)
            return m with { IntermediateRounding = RoundingModes.MapIntermediate(ir) };
        if (c.optionsInitializeClause() is { } init)
            return m with { Initialize = InitializeOf(init, edition) };
        return m;
    }

    /// <summary>The ARITHMETIC clause's mode — AND the one place COBOL.NET declines STANDARD-BINARY.
    ///
    /// <para>⛔ THE SCREEN LIVES HERE BECAUSE THIS IS THE SINGLE CONSTRUCTION POINT. It used to live in
    /// <c>DataBinder</c>, which binds programs, functions and the class/factory/object skeletons but NOT a
    /// method's or an interface's OPTIONS paragraph — so <c>METHOD-ID … OPTIONS. ARITHMETIC IS STANDARD-BINARY.</c>
    /// compiled with NO diagnostic at all at <c>--std 2014</c> (measured 2026-08-31, kb/Work PB197), and the
    /// INTERFACE-ID arm was hollow the same way. Six grammar productions carry <c>optionsParagraph?</c>
    /// (identificationParagraph · classDefinition · factoryParagraph · objectParagraph · interfaceDefinition ·
    /// methodDefinition) and every one of them reaches THIS method, so screening the clause where it is READ
    /// makes the next options-bearing production automatic instead of adding a seventh place to remember.
    /// <c>ArithmeticModeScreenDriftTests</c> enumerates the productions out of the grammar and fails when one
    /// is added without a covering fixture.</para>
    ///
    /// <para>The decline is a §4.2.6 processor-dependence choice, not an obsolescence one: A.3 item 2 makes the
    /// clause processor-dependent ("The ARITHMETIC IS STANDARD-BINARY clause in the OPTIONS paragraph is
    /// dependent on the capabilities of the processor"), and §4.2.6 grants the discretion to decline plus the
    /// obligation to warn and to document. Obsolescence grants nothing — F.2 says a conforming implementation
    /// "shall support obsolete language elements EXCEPT for elements that are also optional or
    /// processor-dependent", so it is the processor-dependence that carries this, and F.2 item 3's
    /// reevaluation note is why the decline is recorded as REVISITABLE. docs/CONFORMANCE.md §2 row 2.</para>
    ///
    /// <para>Screening the CLAUSE rather than the resulting model also keeps §11.9.4 GR1 inheritance quiet: a
    /// contained unit that merely inherits the container's mode has written nothing, and is not diagnosed twice.</para>
    /// </summary>
    private static ArithmeticMode ArithmeticOf(Core.ArithmeticMethodContext m, EditionContext? edition)
    {
        if (m.STANDARD_BINARY() is not null)
        {
            edition?.Error("COBOLNET0806", "ARITHMETIC IS STANDARD-BINARY is a processor-dependent language "
                + "element (ISO Annex A.3 item 2) for which support is not claimed (§4.2.6); it is also obsolete "
                + "(§8.8.1.4.1 NOTE 1 / Annex F.2 item 3). Use NATIVE or STANDARD-DECIMAL");
            return ArithmeticMode.StandardBinary;
        }
        return m.STANDARD_DECIMAL() is not null ? ArithmeticMode.StandardDecimal
            : m.STANDARD() is not null ? ArithmeticMode.Standard
            : ArithmeticMode.Native;
    }

    private static FloatEndianness EndiannessOf(Core.EndiannessPhraseContext e) =>
        e.HIGH_ORDER_LEFT() is not null ? FloatEndianness.HighOrderLeft : FloatEndianness.HighOrderRight;

    private static OptionsModel ApplyFloatDecimal(OptionsModel m, Core.FloatDecimalEncodingContext fd)
    {
        FloatEncoding encoding = fd.encodingPhrase() is { } e
            ? (e.BINARY_ENCODING() is not null ? FloatEncoding.BinaryEncoding : FloatEncoding.DecimalEncoding)
            : m.FloatDecimalEncoding;
        FloatEndianness endianness = fd.endiannessPhrase() is { } ep ? EndiannessOf(ep) : m.FloatDecimalEndianness;
        return m with { FloatDecimalEncoding = encoding, FloatDecimalEndianness = endianness };
    }

    private static OptionsInitialize InitializeOf(Core.OptionsInitializeClauseContext init, EditionContext? edition)
    {
        var target = init.optionsInitializeTarget();
        OptionsSections sections = target.ALL() is not null
            ? OptionsSections.All
            : target.optionsInitializeSection().Aggregate(OptionsSections.None, (acc, s) => acc | SectionOf(s));

        var fill = init.optionsInitializeFill();
        // §8.8.3.3 GR3: a concatenation-expression fill literal folds to its equivalent literal's raw text
        // (GetText would glue the operand tokens). OPTIONS precedes SPECIAL-NAMES, so no PCS table applies.
        if (fill.literal() is { } lit)
            return new OptionsInitialize(sections, OptionsFill.Literal,
                lit.nonNumericLiteral()?.concatenationExpression() is { } ce && edition is not null
                    ? ConcatFolder.Fold(ce, edition, null).RawText
                    : lit.GetText());
        OptionsFill kind =
            fill.BINARY() is not null ? OptionsFill.BinaryZeroes
            : fill.HIGH_VALUE() is not null ? OptionsFill.HighValues
            : fill.LOW_VALUE() is not null ? OptionsFill.LowValues
            : OptionsFill.Spaces;
        return new OptionsInitialize(sections, kind, null);
    }

    private static OptionsSections SectionOf(Core.OptionsInitializeSectionContext s) =>
        s.LOCAL_STORAGE() is not null ? OptionsSections.LocalStorage
        : s.SCREEN() is not null ? OptionsSections.Screen
        : OptionsSections.WorkingStorage;
}
