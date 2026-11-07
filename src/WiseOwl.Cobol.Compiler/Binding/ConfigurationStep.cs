// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Core = WiseOwl.Cobol.Frontend.Generated.CobolParserCore;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The configuration and OPTIONS a CLASS DEFINITION establishes, which its OBJECT and FACTORY halves inherit (ISO §2023:12.3.4
/// GR1: "The entries explicitly or implicitly specified in the configuration section of a source unit that contains other
/// source units apply to each directly or indirectly contained source unit"; §2023:11.9.4 GR1 for the OPTIONS paragraph; a half
/// is a source unit the class definition contains, §2023:3.165). Produced once per class by <see cref="ConfigurationStep"/>'s
/// <c>Own</c> arm and handed to both halves' <c>ClassHalf</c> arms; the halves hold the VALUE, never the class's binder.
/// </summary>
/// <param name="Slice">The class definition's frozen configuration slice.</param>
internal sealed record ClassConfiguration(ConfigurationSlice Slice);

/// <summary>
/// Where a unit's configuration slice comes from — a closed union with one arm per kind of unit the standard gives a
/// configuration (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296 M5). The arms are nested
/// sealed records of an abstract record whose only constructor is private, so no other arm can exist;
/// <see cref="ConfigurationStep.Produce"/> switches over them.
/// </summary>
internal abstract record ConfigurationSource
{
    private ConfigurationSource() { }

    /// <summary>An outermost program, function, class definition or interface: it binds its own CONFIGURATION SECTION and
    /// inherits nothing. <paramref name="SkeletonOptions"/> is the OPTIONS paragraph of an OO definition's skeleton
    /// (§2023:10.6.1; null for a program or function, whose paragraph is in its identification division).</summary>
    internal sealed record Own(Core.OptionsParagraphContext? SkeletonOptions = null) : ConfigurationSource;

    /// <summary>A contained program: "The configuration section shall not be specified in a program that is contained
    /// within another program" (§2023:12.3.3 SR1), so it takes its container's slice whole (§2023:12.3.4 GR1), and its OPTIONS start
    /// from the container's model (§2023:11.9.4 GR1).</summary>
    internal sealed record Contained(ConfigurationSlice Container) : ConfigurationSource;

    /// <summary>An OBJECT or FACTORY half: its class definition's configuration (§2023:12.3.4 GR1 — the class contains the half)
    /// overlaid clause by clause by the half's own SPECIAL-NAMES, the one configuration paragraph §2023:12.3.3 SR3 leaves it
    /// ("The SOURCE-COMPUTER, OBJECT-COMPUTER, and REPOSITORY paragraphs shall not be specified in a factory definition
    /// or an instance definition"). <paramref name="HalfOptions"/> is the half's own OPTIONS paragraph.</summary>
    internal sealed record ClassHalf(ClassConfiguration Class, Core.OptionsParagraphContext? HalfOptions) : ConfigurationSource;
}

/// <summary>
/// ⛔ THE ONE PRODUCER OF A UNIT'S CONFIGURATION SLICE (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work
/// PB2296 M5). Every unit, outermost or contained or a class half, gets its slice here, before its first data phase (the
/// unit's first literal and PICTURE already read it), from the arm its kind names (<see cref="ConfigurationSource"/>): the
/// container's whole configuration-derived state and OPTIONS baseline, the OPTIONS paragraph an OO definition carries on its
/// skeleton, the class definition's configuration for a half. What the unit's OWN CONFIGURATION SECTION and OPTIONS
/// paragraph add is bound over the seeded slice by <c>DataBinder.BindDeclarations</c>.
/// <para>The REPOSITORY class- and interface-specifiers (<see cref="Compiler.Oo.OoRepositoryScope"/>) are not in the slice:
/// the class table resolves them in pass 1, before any binder exists (<c>OoClassTable.RepositoryScopeFor</c>), so that scope
/// stays the pre-bind product of the source elements' parse trees.</para>
/// </summary>
internal static class ConfigurationStep
{
    /// <summary>Seed <paramref name="slice"/>, the unit's own, from <paramref name="source"/>. Called once per unit, before
    /// the unit binds.</summary>
    public static void Produce(ConfigurationSource source, ConfigurationSlice slice)
    {
        switch (source)
        {
            case ConfigurationSource.Own own:
                slice.OptionsParagraph = own.SkeletonOptions;
                break;
            case ConfigurationSource.Contained contained:
                Inherit(contained.Container, slice);
                break;
            case ConfigurationSource.ClassHalf half:
                Inherit(half.Class.Slice, slice);
                slice.OptionsParagraph = half.HalfOptions;
                break;
            default:
                throw new InvalidOperationException($"unknown configuration source {source.GetType().Name}");
        }
    }

    /// <summary>The class definition's product for its halves, taken after the class's own section and OPTIONS paragraph
    /// have bound into <paramref name="slice"/>.</summary>
    public static ClassConfiguration ForClass(ConfigurationSlice slice) => new(slice);

    /// <summary>
    /// ⛔ ISO §2023:12.3.4 GR1 — "The entries explicitly or implicitly specified in the configuration section of a source unit
    /// that contains other source units apply to each directly or indirectly contained source unit" — and §2023:12.3.3 SR1
    /// forbids the containee a configuration section of its own, so a contained program CANNOT restate DECIMAL-POINT IS
    /// COMMA, CURRENCY SIGN, a CLASS, an ALPHABET, the PROGRAM COLLATING SEQUENCE or a switch and is entitled to inherit every
    /// one of them. The container itself inherited from ITS container first (units bind container-first), so one level carries
    /// the whole ancestry. ONE method, the WHOLE configuration-derived state (feedback_model_the_rule_shape_not_one_case):
    /// before kb/Work PB60 only the REPOSITORY sets were inherited, and inside a contained program of a DECIMAL-POINT IS COMMA
    /// unit NUMVAL("123,45") valued 0 while NUMVAL("123.45") valued 123.45 — the exact inversion of §2023:15.67.3 r5, with no
    /// diagnostic (AR-15.67.3-5). The SYMBOLIC CHARACTERS figurative constants (§2023:12.3.7.4 GR11 a; §2023:8.4.6.1 lists
    /// symbolic-character among the names a configuration section declares for "any source element contained within that
    /// source element") carry their containment distance (<see cref="ConfigurationNames{T}.InheritedDepths"/>), so a nearer
    /// declaration of the spelling hides them (<c>InheritStep.DropShadowed</c>; kb/Work PB3154 review R1-1).
    /// §2023:11.9.4 GR1's OPTIONS inheritance rides the same call: the container's model is the baseline the unit's own OPTIONS
    /// paragraph overrides clause by clause.
    /// </summary>
    private static void Inherit(ConfigurationSlice from, ConfigurationSlice into)
    {
        // SPECIAL-NAMES (§2023:12.3.7)
        into.DecimalPointIsComma = from.DecimalPointIsComma;
        foreach (var (k, v) in from.DeclaredCurrencySigns) into.DeclaredCurrencySigns.TryAdd(k, v);
        into.ExplicitCurrencyStrings.UnionWith(from.ExplicitCurrencyStrings);
        into.DollarSpecified = from.DollarSpecified;
        // The names it declares (§2023:8.4.6.1's configuration-section list), each a ConfigurationNames table whose every
        // resolution records a §2023:8.3.2.2 use in the referring element's census (kb/Work PB3153).
        into.SwitchConditions.InheritFrom(from.SwitchConditions);
        into.UserClasses.InheritFrom(from.UserClasses);
        into.Alphabets.InheritFrom(from.Alphabets);
        into.NationalAlphabets.InheritFrom(from.NationalAlphabets);
        into.SymbolicCharacters.InheritFrom(from.SymbolicCharacters);
        into.OrderTables.InheritFrom(from.OrderTables);
        into.DynamicLengthStructures.InheritFrom(from.DynamicLengthStructures);
        into.Locales.InheritFrom(from.Locales);   // §2023:12.3.7.4 GR1 — locale-names reach contained units
        // The mnemonic-names (§2023:12.3.7.2): read after the unit's own entries, so the nearest declaration wins.
        into.InheritMnemonicsFrom(from);
        // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE (§2023:12.3.6 GR9–GR11) and CHARACTER CLASSIFICATION (GR1 — every
        // OBJECT-COMPUTER clause applies to the contained units)
        into.Collating = from.Collating;
        into.Classification = from.Classification;
        into.NationalCollating = from.NationalCollating;
        // SOURCE-COMPUTER … WITH DEBUGGING MODE (the '85 compile-time switch; §2023:12.3.5.4 GR1)
        into.DebuggingModeDeclared = from.DebuggingModeDeclared;
        // REPOSITORY (§2023:12.3.8). The containee's OWN specifier wins on a name clash — TryAdd, the sibling posture, and the one
        // §2023:12.3.8.3 SR15 shape that matters here (the containee may re-specify a name the container also declares).
        // §2023:12.3.8.2's program-specifiers reach contained units on the same rule (kb/Work PB237): §2023:12.3.8.4 GR10 scopes
        // program-prototype-name-1 "throughout the scope of the containing environment division".
        into.UserFunctionNames.UnionWith(from.UserFunctionNames);
        foreach (var (k, v) in from.ProgramSpecifiers) into.ProgramSpecifiers.TryAdd(k, v);
        foreach (var (k, v) in from.RefusedProgramSpecifiers) into.RefusedProgramSpecifiers.TryAdd(k, v);
        into.RepositoryIntrinsics.UnionWith(from.RepositoryIntrinsics);
        if (from.RepositoryAllIntrinsic) into.RepositoryAllIntrinsic = true;
        foreach (var (k, v) in from.OoRepositoryProperties) into.OoRepositoryProperties.TryAdd(k, v);
        foreach (var (k, v) in from.FunctionSpecifiers) into.FunctionSpecifiers.TryAdd(k, v);
        into.RefusedFunctionSpecifiers.UnionWith(from.RefusedFunctionSpecifiers);
        // OPTIONS (§2023:11.9.4 GR1) — the unit's own OPTIONS paragraph overrides clause by clause when it binds.
        into.InheritedOptions = from.Options;
    }
}
