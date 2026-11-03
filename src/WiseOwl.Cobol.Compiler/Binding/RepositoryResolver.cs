// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ ONE SOURCE OF REPOSITORY ANSWERS (DESIGN-external-repository §8.0): the compilation group
/// (<see cref="GroupRepositoryProvider"/>) and the external repository (<see cref="MetadataRepositoryProvider"/>). Each
/// answers by EXTERNALIZED name (ISO §12.3.8.4 GR10 / GR11 a)–c)) and lists the functions a WORD names (determination
/// D-R3 steps 2 and 3, §8.4.6.7). A provider states only what it holds; the rules that span providers live once, in
/// <see cref="RepositoryResolver"/>.
/// </summary>
internal interface IRepositoryProvider
{
    /// <summary>The details of the program whose externalized name is <paramref name="externalizedName"/>, as a source
    /// element that starts at <paramref name="position"/> (its own start, or a class's)
    /// sees them: GR10 a) / b) in the group, c) in the external repository, which has no source order. Null when this
    /// provider holds none.</summary>
    ProgramAnswer? Program(string externalizedName, int position);

    /// <summary>GR11 a) / b) / c)'s twin of <see cref="Program"/> for a function.</summary>
    FunctionAnswer? Function(string externalizedName, int position);

    /// <summary>Every function this provider holds whose user-function-name is <paramref name="userFunctionName"/> and
    /// that a source element at <paramref name="position"/> may name by it (§8.4.6.7: a definition "that follows that
    /// function definition within the compilation group", or one in the external repository), in the order the
    /// provider prefers them — D-R3 step 2 takes the first, step 3 the first of another externalized name.</summary>
    IEnumerable<FunctionAnswer> FunctionsByWord(string userFunctionName, int position);
}

/// <summary>Where a repository answer's details come from (DESIGN-external-repository §8.0): a unit of the compilation
/// group (<see cref="GroupUnitOrigin"/>), or an entry of the external repository (<see cref="RepositoryEntryOrigin"/>).</summary>
internal abstract record RepositoryOrigin
{
    /// <summary>True when the element the answer names is a DEFINITION this compilation group compiles, so the program
    /// or function a CALL through it activates is the one whose formals the bind checked (§14.9.4.4 GR3 d) can find no
    /// difference at run time; §4.2's <c>CalleeCompiledHere</c>). False for a prototype definition (its program is
    /// compiled elsewhere, GR10 b) / GR11 b)) and for a repository entry (c)).</summary>
    public abstract bool IsDefinitionCompiledHere { get; }

    /// <summary>How a diagnostic names the answer's unit: <c>the function F AS "G"</c>, <c>a function prototype …</c>.</summary>
    public abstract string Describe(string kind);
}

/// <summary>A unit of the compilation group.</summary>
internal sealed record GroupUnitOrigin(BoundUnit Unit) : RepositoryOrigin
{
    public override bool IsDefinitionCompiledHere => !Unit.IsPrototype;

    public override string Describe(string kind) =>
        $"{(Unit.IsPrototype ? $"a {kind} prototype" : $"the {kind}")} {Unit.Name} AS \"{Unit.ExternalizedName}\" of this compilation group";
}

/// <summary>A program's details and where they come from.</summary>
internal sealed record ProgramAnswer(CalleeSignature Signature, RepositoryOrigin Origin);

/// <summary>A function's details (under the answering unit's own word) and where they come from.</summary>
internal sealed record FunctionAnswer(UserFunctionSignature Signature, RepositoryOrigin Origin);

/// <summary>What <see cref="RepositoryResolver.ResolveFunction"/> found: the <paramref name="Signature"/> the entry's word
/// resolves to (under that word), its <paramref name="Origin"/>, and — D-R3 step 3 — the <paramref name="Shadowed"/>
/// function that carries the same word under another externalized name, or null.</summary>
internal sealed record FunctionResolution(UserFunctionSignature Signature, RepositoryOrigin Origin, FunctionAnswer? Shadowed);

/// <summary>
/// ⛔ THE REPOSITORY SPECIFIER RESOLVER (DESIGN-external-repository §8.0, §8.2, §8.3): every program-specifier and
/// function-specifier of the compilation group resolves here, over the providers in their search order — the group
/// first, then the external repository (§12.3.8.4 GR10 / GR11 a) and b) before c)). Determination D-R3 is ONE rule
/// across every provider, so it is written once, in <see cref="ResolveFunction"/>, and no provider repeats it.
/// <para>A specifier no provider answers is REPO-1 (§8.2 step 4, §8.3), reported once per specifier by
/// <see cref="ReportUnanswered"/>, unless the external repository's own finding already refused it
/// (<see cref="MetadataRepositoryProvider.Refused"/>).</para>
/// </summary>
internal sealed class RepositoryResolver(GroupRepositoryProvider group, MetadataRepositoryProvider external)
{
    private readonly IReadOnlyList<IRepositoryProvider> _providers = [group, external];
    private readonly HashSet<object> _reported = new(ReferenceEqualityComparer.Instance);

    /// <summary>The group's provider, for what only the group holds (<see cref="GroupRepositoryProvider.FunctionsAt"/>).</summary>
    public GroupRepositoryProvider Group { get; } = group;

    /// <summary>The external repository's provider, for the entries §9.2's check compares against.</summary>
    public MetadataRepositoryProvider External { get; } = external;

    /// <summary>⛔ REPO-1 (ISO §12.3.8.3 SR14 for a program, SR10 for a function; DESIGN-external-repository §14) for the
    /// <paramref name="specifier"/> — the written <c>PROGRAM</c> / <c>FUNCTION <paramref name="word"/> [AS literal]</c> whose
    /// externalized name is <paramref name="externalizedName"/> — that no provider answered, at the edition's current position
    /// (the caller places it on the specifier): once per specifier, however many source elements inherit it, and not at all
    /// when the external repository refused it with a finding of its own.</summary>
    public void ReportUnanswered(object specifier, bool function, string word, string externalizedName, EditionContext edition)
    {
        var family = function ? RepositoryFamily.Function : RepositoryFamily.Program;
        if (!_reported.Add(specifier) || External.Refused(family, externalizedName)) return;
        string kind = function ? "function" : "program";
        string written = ExternalizedNames.Same(word, externalizedName) ? word : $"{word} AS \"{externalizedName}\"";
        string alternatives = function
            ? $"no function prototype of this compilation group and no function definition specified before this paragraph is "
              + $"externalized \"{externalizedName}\" or has the user-function-name {word}"
            : $"no program prototype of this compilation group and no program definition specified before this paragraph is "
              + $"externalized \"{externalizedName}\"";
        edition.Error(DiagnosticCatalog.RepositorySpecifierUnanswered,
            $"REPOSITORY {kind.ToUpperInvariant()} {written} names no {kind}: {alternatives}, and no information exists for it in the "
            + $"external repository ({External.Searched(family, externalizedName)}) — ISO §12.3.8.3 {(function ? "SR10" : "SR14")}");
    }

    /// <summary>GR10 for a program-prototype whose externalized name is <paramref name="externalizedName"/>, as a source
    /// element at <paramref name="position"/> sees it: the first provider's answer (a) / b) in the group, c) in the
    /// repository), or null when none holds the program.</summary>
    public ProgramAnswer? ResolveProgram(string externalizedName, int position)
    {
        foreach (var provider in _providers)
            if (provider.Program(externalizedName, position) is { } answer) return answer;
        return null;
    }

    /// <summary>⛔ GR11 with determination D-R3 (DESIGN-external-repository §8.3; owner decision 2026-10-05) for a
    /// REPOSITORY entry <c>FUNCTION <paramref name="word"/> [AS literal-5]</c> — <paramref name="externalizedName"/> is
    /// literal-5, or the word itself when no AS is written (GR11 NOTE 2) — as seen by a source element at
    /// <paramref name="position"/>:
    /// <list type="number">
    /// <item>GR11 a) / b) / c) keyed by externalized name, every provider in order — whichever hits names the function
    /// (GnuCOBOL 3.2.0's answer and the standard's text);</item>
    /// <item>when all miss, the function the WORD names (§8.4.6.7; SR10's "the name of a function definition specified
    /// previously" and "the name of a function prototype specified in this compilation group"), every provider in
    /// order — its externalized name is what is activated;</item>
    /// <item>when step 1 hit and a DIFFERENT function — another externalized name — carries the word and is visible to
    /// the element in any provider, <see cref="FunctionResolution.Shadowed"/> names it so the caller can warn.</item>
    /// </list>
    /// Null: no provider answers. The signature carries the entry's own word, which is how the element refers to the
    /// function, and the activated function's externalized name.</summary>
    public FunctionResolution? ResolveFunction(string word, string externalizedName, int position)
    {
        FunctionAnswer? byName = null;
        foreach (var provider in _providers)
            if ((byName = provider.Function(externalizedName, position)) is not null) break;
        var details = byName ?? _providers.SelectMany(p => p.FunctionsByWord(word, position)).FirstOrDefault();
        if (details is null) return null;
        // Step 3: another function — another externalized name — that the element can see under the same word; "can
        // see" is the one rule each provider's FunctionsByWord states, so the warning never names a function the
        // search itself ignores.
        var shadowed = byName is null
            ? null
            : _providers.SelectMany(p => p.FunctionsByWord(word, position))
                .FirstOrDefault(f => !ExternalizedNames.Same(f.Signature.Externalized, details.Signature.Externalized));
        return new FunctionResolution(details.Signature with { Name = word }, details.Origin, shadowed);
    }
}
