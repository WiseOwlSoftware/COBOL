// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

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
    /// paragraph (and so every clause in it — ARITHMETIC, DEFAULT ROUNDED, INTERMEDIATE ROUNDING, …) was
    /// introduced by ISO/IEC 1989:2014 (§11.9): targeting 85/2002 REJECTS it with an edition diagnostic, and the
    /// implied defaults (native arithmetic, nearest-away-from-zero bare ROUNDED) apply — exactly the pre-2014
    /// semantics.</summary>
    public static OptionsModel Bind(Core.ProgramUnitContext program, EditionContext? edition = null)
    {
        var paragraphs = program.identificationDivision()?.identificationBody()?.identificationParagraph();
        if (paragraphs is null) return OptionsModel.Default;

        var options = paragraphs.Select(p => p.optionsParagraph()).FirstOrDefault(o => o is not null);
        if (options is null) return OptionsModel.Default;

        if (edition is { DialectLevel: < 2014 })
        {
            edition.Error("COBOLNET0804", "the OPTIONS paragraph (ARITHMETIC / DEFAULT ROUNDED / INTERMEDIATE "
                + $"ROUNDING …) was introduced by ISO/IEC 1989:2014 (§11.9) — it requires --std 2014 or later "
                + $"(targeting COBOL-{edition.DialectLevel})");
            return OptionsModel.Default;
        }

        var model = OptionsModel.Default;
        foreach (var clause in options.optionsClause())
            model = Apply(model, clause);
        return model;
    }

    private static OptionsModel Apply(OptionsModel m, Core.OptionsClauseContext c)
    {
        if (c.arithmeticClause()?.arithmeticMethod() is { } am)
            return m with { Arithmetic = ArithmeticOf(am) };
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
            return m with { Initialize = InitializeOf(init) };
        return m;
    }

    private static ArithmeticMode ArithmeticOf(Core.ArithmeticMethodContext m) =>
        m.STANDARD_BINARY() is not null ? ArithmeticMode.StandardBinary
        : m.STANDARD_DECIMAL() is not null ? ArithmeticMode.StandardDecimal
        : m.STANDARD() is not null ? ArithmeticMode.Standard
        : ArithmeticMode.Native;

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

    private static OptionsInitialize InitializeOf(Core.OptionsInitializeClauseContext init)
    {
        var target = init.optionsInitializeTarget();
        OptionsSections sections = target.ALL() is not null
            ? OptionsSections.All
            : target.optionsInitializeSection().Aggregate(OptionsSections.None, (acc, s) => acc | SectionOf(s));

        var fill = init.optionsInitializeFill();
        if (fill.literal() is { } lit) return new OptionsInitialize(sections, OptionsFill.Literal, lit.GetText());
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
