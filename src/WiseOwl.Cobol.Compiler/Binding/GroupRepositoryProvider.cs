// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// ⛔ THE ONE SEARCH OF THE COMPILATION GROUP THAT A REPOSITORY SPECIFIER RESOLVES THROUGH (kb/Work PB989; ISO
/// §2023:12.3.8.4 GR10 for a program-prototype-name, GR11 for a function-prototype-name — one sentence written twice,
/// which is why it is one function here, <see cref="Find"/>; DESIGN-external-repository §8.0, §8.2, §8.3). It is the
/// group's <see cref="IRepositoryProvider"/>: <see cref="RepositoryResolver"/> asks it first and applies the rules that
/// span providers (determination D-R3) once, over every provider.
/// <para>The rule, in full (<c>cite.py --check 12.3.8.4 "specified previously in the same compilation group"</c>):</para>
/// <list type="number">
/// <item>a) the externalized name of the prototype is the externalized name of a DEFINITION "specified PREVIOUSLY in
/// the same compilation group" — the details come from that definition;</item>
/// <item>b) otherwise, it is the externalized name of a PROTOTYPE definition "specified in the same compilation
/// group" (NOT "previously": §2023:10.6.2 SR1 puts every prototype ahead of every other unit anyway) — the details come
/// from the prototype definition;</item>
/// <item>c) otherwise the details come from the external repository — another provider's answer, never this one's.</item>
/// </list>
/// <para><b>"Previously" is a POSITION, not a membership.</b> Every definition in the group used to be visible to every
/// source element, so a REPOSITORY entry naming a definition that FOLLOWS the element took a)'s details from it
/// (the order-blind tables this class replaced). The search now takes the position of the referencing element
/// (<see cref="BoundUnit.SourcePosition"/>, or a class's own start) and reads only the definitions that start strictly
/// before it. A prototype and its definition must have the same signature (§2023:10.6.2 SR2/SR3, COBOL1513), so a) and
/// b) never disagree on the details; the observable difference is a definition WITHOUT a prototype that follows its
/// caller, which is c).</para>
/// <para><b>Matched by EXTERNALIZED name</b> for a) and b) (GR10 a) / GR11 a) say "the externalized name" three
/// times). The user-function table used to merge a prototype with a definition of the same WORD, which dropped the
/// prototype's <c>AS literal-1</c> (§2023:11.5.4 GR2: literal-1 "is the name of the function prototype that is
/// externalized to the operating environment") whenever a definition happened to share its word. The WORD has one
/// job, for functions only: the candidates <see cref="FunctionsByWord"/> lists for D-R3 steps 2 and 3 (§2023:8.4.6.7).</para>
/// </summary>
internal sealed class GroupRepositoryProvider : IRepositoryProvider
{
    private readonly List<BoundUnit> _functions = [];
    private readonly List<BoundUnit> _programs = [];
    private readonly ElementBinders _binders;   // each unit's signature is its binder's linkage facts (kb/Work PB2361)

    private static readonly IReadOnlyDictionary<string, UserFunctionSignature> NoFunctions =
        new Dictionary<string, UserFunctionSignature>(CobolNames.Comparer);

    /// <summary>The group's FUNCTION-ID units (definitions and prototypes — §2023:9.4 puts them in the function namespace)
    /// and its OUTERMOST PROGRAM-ID units (a contained program is part of its container's definition and is reachable
    /// only through §2023:14.9.4.3 SR15's AS NESTED, which has its own table), in source order.</summary>
    public GroupRepositoryProvider(IReadOnlyList<BoundUnit> units, ElementBinders binders)
    {
        _binders = binders;
        foreach (var u in units)
            if (u.IsFunction) _functions.Add(u);
            else if (u.Parent is null) _programs.Add(u);
    }

    /// <summary>⛔ GR10 / GR11 a) then b), as seen by a source element that starts at <paramref name="position"/>: the
    /// units <paramref name="matches"/> accepts that may supply a prototype's details, in the rule's own order — every
    /// definition that starts strictly before the element, then every prototype definition, each in source order. The
    /// first is the unit that supplies the details (<see cref="Find"/>); none is c). The predicate is the
    /// externalized-name test for GR10 / GR11 and the user-function-name test for D-R3 steps 2 and 3.</summary>
    private static IEnumerable<BoundUnit> Candidates(List<BoundUnit> units, Func<BoundUnit, bool> matches, int position)
    {
        foreach (var u in units)
            if (!u.IsPrototype && u.SourcePosition < position && matches(u)) yield return u;
        foreach (var u in units)
            if (u.IsPrototype && matches(u)) yield return u;
    }

    private static BoundUnit? Find(List<BoundUnit> units, Func<BoundUnit, bool> matches, int position) =>
        Candidates(units, matches, position).FirstOrDefault();

    private static Func<BoundUnit, bool> Externalized(string externalized) =>
        u => ExternalizedNames.Same(u.ExternalizedName, externalized);

    /// <inheritdoc/>
    public ProgramAnswer? Program(string externalizedName, int position) =>
        Find(_programs, Externalized(externalizedName), position) is { } u
            ? new ProgramAnswer(_binders.SignatureOf(u), new GroupUnitOrigin(u))
            : null;

    /// <inheritdoc/>
    public FunctionAnswer? Function(string externalizedName, int position) =>
        Find(_functions, Externalized(externalizedName), position) is { } u ? AnswerOf(u) : null;

    /// <inheritdoc/>
    public IEnumerable<FunctionAnswer> FunctionsByWord(string userFunctionName, int position, string? otherThan) =>
        Candidates(_functions, u => CobolNames.Same(u.Name, userFunctionName), position)
            .Where(u => otherThan is null || !ExternalizedNames.Same(u.ExternalizedName, otherThan))   // D-R3 step 3: a DIFFERENT function
            .Select(AnswerOf);

    /// <summary>The user-defined functions a source element at <paramref name="position"/> can see by WORD, without a
    /// REPOSITORY entry resolving them: each word's definition specified before the element, else its prototype (D-R3
    /// step 2's search, unspecified). The entries of the element's own specifiers replace these
    /// (<c>BinderDriver.UserFunctionsOf</c>); the rest is what lets a reference to a function the element did not
    /// declare say which function it could have meant. The group's alone: a later-compiled unit reaches a repository
    /// function only through a REPOSITORY specifier (§2023:8.4.6.7), so no other provider has a counterpart.</summary>
    public IReadOnlyDictionary<string, UserFunctionSignature> FunctionsAt(int position)
    {
        if (_functions.Count == 0) return NoFunctions;
        var table = new Dictionary<string, UserFunctionSignature>(CobolNames.Comparer);
        foreach (var u in _functions)
            if (!table.ContainsKey(u.Name) && FunctionsByWord(u.Name, position, otherThan: null).FirstOrDefault() is { } details)
                table[u.Name] = details.Signature;
        return table;
    }

    /// <summary>A function unit's answer: its signature under its own word (the resolver re-words it with the
    /// referencing entry's word) and its origin.</summary>
    private FunctionAnswer AnswerOf(BoundUnit u)
    {
        var origin = new GroupUnitOrigin(u);
        var signature = _binders.SignatureOf(u);
        return new(new UserFunctionSignature(u.Name, u.ExternalizedName, signature.Returning, signature.Formals,
            origin.IsDefinitionCompiledHere), origin);
    }
}
