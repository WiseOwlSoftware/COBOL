// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Repository;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ EVERY COMPILE-TIME INPUT THAT CHANGES WHAT A SKELETON'S SOURCE MEANS (ISO §9.3.12: an expansion "is treated in all
/// respects the same as if it were a class that is not a parameterized class"; DESIGN-external-repository §6.1, §10.5; kb/Work
/// PB2104). A parameterized definition published to the external repository is expanded by a compilation that was not the one
/// it was written for, so the expansion is made under THIS state, not the expanding compilation's: the importer re-lexes the
/// recorded source at <see cref="Edition"/> and <see cref="Permissive"/> with the recorded <see cref="CobolWordsMap"/>, and the
/// expansion's own assembly is compiled with the directive lines the three text members state. The text members are DIRECTIVE
/// LINES (free-form source), so the one directive processor that read them the first time reads them again; a state with none is
/// the empty string. These records live in the model layer because the emitter writes them and may read only the model.
/// <para><b>Exhaustive by a drift test</b> (<c>CompileStateDriftTests</c>): every member of <c>CompilerDriver.Options</c> and
/// every member of <c>BindSession</c> is a field here, or is named in that test's list of what changes no meaning, with its
/// reason. A new option that the test does not know is red before it can be silently dropped from a skeleton.</para>
/// </summary>
/// <param name="Edition"><c>Options.DialectLevel</c>.</param>
/// <param name="Permissive"><c>Options.Permissive</c>.</param>
/// <param name="SignEncoding"><c>Options.SignEncoding</c> (the over-punch convention is baked into every emitted profile).</param>
/// <param name="FlagExtensions"><c>Options.FlagExtensions</c> (it only adds warnings, but a skeleton states every option the
/// expanding compile could read, so the key and the separate compile agree on it).</param>
/// <param name="NumericDisplay"><c>Options.NumericDisplay</c> (PB3505: the form DISPLAY and ACCEPT give a numeric operand).</param>
/// <param name="CobolWordsMap"><c>BindSession.CobolWords</c> as the <c>&gt;&gt;COBOL-WORDS</c> directive lines that state it.</param>
/// <param name="DirectiveFolds"><c>BindSession.Turn</c>, <c>Propagate</c>, <c>RefModZeroLength</c> and <c>LeapSecond</c> folded at
/// the definition's first line, as the directive lines that state that fold.</param>
/// <param name="CompilationVariables"><c>BindSession.CompilationVariables</c> as of the definition, as <c>&gt;&gt;DEFINE</c>
/// lines (a constant's FROM reads one).</param>
internal sealed record CompileState(int Edition, bool Permissive, SignEncoding SignEncoding, bool FlagExtensions,
    RepositoryNumericDisplay NumericDisplay, string CobolWordsMap, string DirectiveFolds, string CompilationVariables)
{
    /// <summary>Does <paramref name="other"/> bind a source the way this state does? Every member but <see cref="FlagExtensions"/>, which
    /// only adds warnings. The ONE answer for both questions that need it: whether an expansion of a skeleton read from the repository may
    /// be compiled in a group's own assembly (<c>ExpansionPlacementPass</c>), and whether the group's screens of that text say what the
    /// skeleton's own compilation said (<c>BinderDriver.Bind</c>, kb/Work PB4483).</summary>
    public bool BindsAs(CompileState other) => this with { FlagExtensions = false } == other with { FlagExtensions = false };
}

/// <summary>One class-specifier or interface-specifier of a skeleton's OWN REPOSITORY paragraph that is not a formal: the word
/// it declares and the identity it stands for (ISO §12.3.8.4 GR2: the literal, else the word). The expansion binds the skeleton's
/// own words to these identities wherever it is made, and its key holds the record of what each names (§10.5).</summary>
internal sealed record ResolutionRecord(string Word, bool IsInterface, string Identity);

/// <summary>What a parameterized class or interface publishes (§5.2 "ParameterizedClass / ParameterizedInterface"; §6.1): its
/// formals with the kind each is declared as, its source after text manipulation, its specifier resolutions and the
/// <see cref="CompileState"/> it was compiled under.</summary>
internal sealed record SkeletonRecord(RecordList<string> Formals, RecordList<bool> FormalIsInterface, string Source,
    RecordList<ResolutionRecord> Resolutions, CompileState State);

/// <summary>What an expansion publishes beside its class record (§5.2 "Expansion"; §10.5): the definition it expands and the
/// actuals, all by externalized name, the <see cref="Key"/> that says what it MEANS, and whether it is the only unit of an
/// assembly of its own (placement (a)) or lives in the expanding group's assembly (placement (b)). <see cref="Parts"/> are the labeled
/// digests of the components the key is made of (<c>ExpansionKey.Of</c>), so a compilation that finds this expansion stale can say
/// which component changed. <see cref="Homes"/> are the simple names of the assemblies the classes it names lived in when it was made (each
/// resolution in turn, then each actual; empty for a class the standard supplies): the key hashes a class's record, not its home, so
/// they say which classes the expansion's code was compiled against (kb/Work PB4650).</summary>
internal sealed record ExpansionIdentity(string DefinitionIdentity, RecordList<string> ActualIdentities, string Key, RecordList<string> Parts,
    RecordList<string> Homes, bool OwnAssembly);
