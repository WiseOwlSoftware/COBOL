// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Runtime.Collation;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The configuration-section services the data binder keeps for itself: the REPOSITORY paragraph's specifier screens
/// (§12.3.8.3) and the constant-name readings a configuration literal asks (§13.10.3 SR2). The seed that makes a contained
/// unit's configuration its container's (§12.3.4 GR1) is <see cref="ConfigurationStep"/>'s; the SPECIAL-NAMES state and its
/// binding are <see cref="SpecialNamesBinder"/>'s.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The unit's SPECIAL-NAMES binder and the configuration state it declares (ISO §12.3.6, §12.3.7): the
    /// switches, classes, alphabets, symbolic characters, locales, ordering tables, collating sequences, decimal
    /// point and currency signs. One per binder, made with it; every reader of these facts asks it here.</summary>
    public SpecialNamesBinder SpecialNames => field ??= new(this, Edition);

    /// <summary>The unit's INPUT-OUTPUT SECTION binder (ISO §12.4): the FILE-CONTROL entries it captures into
    /// <see cref="FileSlice.Declared"/> and the I-O-CONTROL paragraph (the APPLY COMMIT clause, the SAME clauses) bound against them.
    /// One per binder, made with it.</summary>
    public FileControlBinder FileControl => field ??= new(this, Edition);

    // ── The §12.3.8.2 program-specifier (kb/Work PB237) ──────────────────────────────────────────────────────
    /// <summary>Bind ONE <c>PROGRAM program-prototype-name-1 [AS literal-3]</c> entry of the REPOSITORY paragraph
    /// (ISO §12.3.8.2's program-specifier, PDF page 334). Two declaration rules apply and both are checked here:
    /// §12.3.8.3 syntax rule 2 (literal-3's class), and syntax rule 1 — "If any … program-prototype-name-1 … is
    /// specified more than once in the REPOSITORY paragraph, all the specifications for that name shall be
    /// identical", which for a program-specifier means the same externalized name.
    /// <para>What the specifier RESOLVES to — §12.3.8.4 GR10's a)/b)/c) search of the compilation group, and
    /// §12.3.8.3 SR15's "this specifier is ignored" for the unit's own or a containing program's name — needs the
    /// unit's identity and its siblings, which this binder does not have. Those run in
    /// <c>BinderDriver.ProgramPrototypesOf</c>, exactly where the function twin's GR11 search runs
    /// (<c>BinderDriver.UserFunctionsOf</c>, through <c>RepositoryResolver</c>). Collecting the SYNTAX here and resolving
    /// it there is the same split.</para></summary>
    private void BindProgramSpecifier(Core.RepositoryEntryContext re, string name)
    {
        // A rejected literal-3 registers NO prototype (the PB237 posture — a CALL through it then draws the
        // undeclared-prototype diagnostic rather than silently activating the declared word).
        if (BindSpecifierExternalizedName(re, "PROGRAM", name, "literal-3") is { } externalized)
            State.Configuration.ProgramSpecifiers.TryAdd(name, new ProgramSpecifier(name, externalized, re));
    }

    /// <summary>⛔ THE ONE SCREEN OF A REPOSITORY SPECIFIER'S <c>[ AS literal-n ]</c> — class (literal-1), interface
    /// (literal-2), program (literal-3), property (literal-4) and user-defined function (literal-5), §12.3.8.2
    /// (kb/Work PB974; the program specifier had it alone, PB237). Returns the externalized name — the literal's
    /// value, or the declared name when there is no AS phrase (§12.3.8.4 GR10 NOTE 1 / GR11 NOTE 2) — or null when
    /// the literal was rejected (and reported).
    /// <para>§12.3.8.3 SR2: "Literal-1, literal-2, literal-3, literal-4, and literal-5 shall be alphanumeric literals
    /// or national literals and shall be neither figurative constants nor zero-length literals" — the SAME sentence
    /// the identification paragraphs restate, so the ONE <c>ExternalizedName.Screen</c> (kb/Work PB303). SR1 is asked
    /// of the WHOLE specification by <see cref="CheckRepositorySpecification"/> (kb/Work PB1017).</para></summary>
    private string? BindSpecifierExternalizedName(Core.RepositoryEntryContext re, string kind, string name, string tag)
    {
        using var _ = Edition.At(re);   // the sink stamps every report below with THIS entry's position (PB82)
        string externalized = name;
        if (re.externalizedNamePhrase() is { } asPhrase)
        {
            if (ExternalizedName.Screen(asPhrase, ReadConstant, Edition, DiagnosticCatalog.RepositoryProgramSpecifier,
                    $"REPOSITORY {kind} '{name}' AS {ExternalizedName.Written(asPhrase)}", tag, "ISO §12.3.8.3 SR2",
                    LiteralEnv) is not { } literal) return null;
            externalized = literal;
        }
        var spec = new RepositorySpecification(kind, re.externalizedNamePhrase() is null ? null : externalized,
            ExpandsText(re.expandsPhrase()));
        return CheckRepositorySpecification(name, spec, externalized) ?? externalized;
    }

    /// <summary>ONE REPOSITORY specification of a name, as §12.3.8.3 SR1 compares it: the specifier KIND (class,
    /// interface, program, property, user-defined function, or intrinsic function), the value of its
    /// <c>[ AS literal-n ]</c> (null when absent — the name is then externalized by the implementor's mapping of the
    /// user-defined word, §8.3.2.2, so two case spellings of one word are one specification), and the canonical
    /// text of its <c>[ EXPANDS … USING … ]</c> phrase (null when absent). Every part of the written specification
    /// that carries a meaning is a component, so "identical" is record equality.</summary>
    private sealed record RepositorySpecification(string Kind, string? As, string? Expands)
    {
        public override string ToString() =>
            $"{Kind}{(As is null ? "" : $" AS \"{As}\"")}{(Expands is null ? "" : " " + Expands)}";
    }

    /// <summary>The kind an intrinsic-function-name-1 of <c>FUNCTION { intrinsic-function-name-1 } … INTRINSIC</c>
    /// is specified with.</summary>
    private const string IntrinsicKind = "FUNCTION … INTRINSIC";

    /// <summary>⛔ THE ONE §12.3.8.3 SR1 CHECK, OVER THE WHOLE SPECIFICATION (kb/Work PB1017). "If any
    /// object-class-name-1, interface-name-2, program-prototype-name-1, function-prototype-name-1,
    /// intrinsic-function-name-1 or property-name-1 is specified more than once in the REPOSITORY paragraph, all
    /// the specifications for that name shall be identical." The rule is keyed on THE NAME — not on (kind, name) —
    /// and compares everything the specification says: its kind, its externalized name, and its EXPANDS phrase.
    /// It used to compare the externalized name alone, per kind, so `CLASS QC EXPANDS QP USING QA` then
    /// `CLASS QC EXPANDS QP USING QB` (two different classes created under one name, §12.3.8.4 GR5), a name
    /// specified as a CLASS and as an INTERFACE, and `FUNCTION F INTRINSIC` beside `FUNCTION F` were all accepted.
    /// The externalized name is compared ordinally — an operating-environment name, not a COBOL word — and the
    /// declared name case-insensitively (§8.3.2). Returns the FIRST specification's externalized name when this one
    /// repeats a name (after reporting a difference), null when the name is new.</summary>
    private string? CheckRepositorySpecification(string name, RepositorySpecification spec, string externalized)
    {
        string key = name;
        if (_repositorySpecified.TryGetValue(key, out var first))
        {
            var (prior, priorExternalized) = first;
            if (prior != spec)
                Edition.Error(DiagnosticCatalog.RepositoryProgramSpecifier,
                    $"REPOSITORY '{name}' is specified more than once with different specifications ('{prior}' then "
                    + $"'{spec}'); ISO §12.3.8.3 syntax rule 1 requires all the specifications for one name to be "
                    + "identical");
            return priorExternalized;
        }
        _repositorySpecified[key] = (spec, externalized);
        return null;
    }

    /// <summary>The canonical text of an EXPANDS phrase — <c>EXPANDS target USING actual …</c>, each word
    /// upper-cased (§8.3.2: a user-defined word is case-insensitive), so two spellings of one phrase compare equal.</summary>
    private static string? ExpandsText(Core.ExpandsPhraseContext? ep) => ep is null ? null
        : $"EXPANDS {ep.expandsTarget().GetText().ToUpperInvariant()} USING "
          + string.Join(" ", ep.expandsActual().Select(a => a.GetText().ToUpperInvariant()));

    /// <summary>Upper-cased declared name → its FIRST specification and the externalized name it gave: the
    /// §12.3.8.3 SR1 memory, for THIS unit's REPOSITORY.</summary>
    private readonly Dictionary<string, (RepositorySpecification Spec, string Externalized)> _repositorySpecified
        = new(CobolNames.Comparer);

    /// <summary>The literal environment of a position OUTSIDE the SPECIAL-NAMES paragraph (kb/Work PB1406).</summary>
    internal LiteralEnvironment LiteralEnv => LiteralEnvironment.Program(this);

    /// <summary>Does <paramref name="word"/> name a constant here — one of this unit's constant entries (bound, bindable
    /// or refused) or an inherited GLOBAL one? The question a position whose word alternative has a meaning of its own
    /// (an ALPHABET code-name, an external-locale-name) asks before taking that meaning.</summary>
    internal bool NamesConstant(string word) => Constants.IsDeclaredConstant(word) || Constants.FindConstant(word) is not null;

    /// <summary>⛔ THE ONE CONSTANT-NAME READING OF A WORD AT A SPECIAL-NAMES LITERAL POSITION (kb/Work PB1942): literal-1
    /// … literal-9 of §12.3.7.2. ISO §13.10.3 SR2 — "Except in a compiler directive, constant-name-1 may be used anywhere
    /// that a format specifies a literal of the class and category of constant-name-1" — and §13.10.4 GR1 — "the
    /// effect of specifying constant-name-1 in other than this entry is as if literal-1 … were written where
    /// constant-name-1 is written". The constant entry stands later, in the DATA DIVISION; it was declared before this
    /// paragraph binds (DeclareDataEntries), so <see cref="FindConstant"/> binds it now, on demand. The caller holds the
    /// returned literal-1 to its own operand's rules (class, form, length), exactly as it holds a written literal.
    /// Null, reported through the caller's <paramref name="report"/>, when the word names no constant entry; null
    /// silently when it names one whose own entry was refused (that entry was reported).</summary>
    /// <param name="report">The caller's clause diagnostic, for the no-constant report.</param>
    /// <param name="what">The clause as the caller names it, for the diagnostic.</param>
    /// <param name="operand">The standard's name for the operand ("literal-7").</param>
    internal ConstantDef? SpecialNamesConstant(Core.CobolWordContext word, Action<string> report, string what, string operand)
    {
        string name = word.GetText();
        if (Constants.FindConstant(name) is { } constant) return constant;
        if (!Constants.IsFailedConstant(name))
            report($"{what}: {ConstantReading.NotAConstantMessage(operand, name)}");
        return null;
    }

    /// <summary>What <paramref name="name"/> stands for at a literal position INSIDE the data bind (kb/Work PB2588) — the
    /// AS phrase of a REPOSITORY specifier or of an EXTERNAL clause, read by <see cref="ExternalizedName.Screen"/>. The
    /// same lookup as <see cref="SpecialNamesConstant"/> (<see cref="FindConstant"/> binds the entry on demand and sees
    /// a container's GLOBAL constant), answered as a <see cref="ConstantReading"/>, the shape the pre-bind reader
    /// (<see cref="PreBindConstants"/>) gives the AS phrases read before this bind.</summary>
    internal ConstantReading ReadConstant(string name) =>
        Constants.FindConstant(name) is { } k ? ConstantReading.Of(new ConcatFolder.Folded(k.Category, k.Text))
        : Constants.IsFailedConstant(name) ? ConstantReading.Refused
        : ConstantReading.NotAConstant;
}
