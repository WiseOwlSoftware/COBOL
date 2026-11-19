// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The PROGRAM-PROTOTYPE-NAMES (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 6"; kb/Work PB5932): the
/// REPOSITORY paragraph's program-specifiers as written (ISO §2023:12.3.8.2), the program prototypes they resolve to through
/// §2023:12.3.8.4 GR10 with §2023:8.4.6.8's containing program definitions, and the §2023:12.3.8.3 SR1 memory of every name the
/// paragraph specifies. Each table is keyed by <see cref="CobolName"/> and private: a program-prototype-name a statement writes is
/// <c>Identify</c> of
/// <see cref="NameClass.ProgramPrototype"/>, and a prototype-name a SYMBOL carries as text (a restricted program-pointer's USAGE
/// TO phrase, a program-address-identifier's prototype) is resolved here from the symbol, so no caller turns a name into a
/// prototype.
/// </summary>
public sealed partial class SymbolTable
{
    /// <summary>The program-specifiers in scope, in the order written (the container's first: units bind container-first and a
    /// contained unit inherits before its own REPOSITORY binds, §2023:12.3.8.4 GR10's "scope of the containing environment
    /// division").</summary>
    private readonly Dictionary<CobolName, ProgramSpecifier> _programSpecifiers = [];

    /// <summary>The program-prototype-names a specifier DECLARES that register no prototype, and why (kb/Work PB4708).</summary>
    private readonly Dictionary<CobolName, ProgramSpecifierRefusal> _refusedProgramSpecifiers = [];

    /// <summary>The program prototypes this element may name (§2023:8.4.6.8), made by <see cref="ResolveProgramPrototypes"/> once
    /// the group's REPOSITORY can be searched; empty before.</summary>
    private Dictionary<CobolName, ProgramPrototype> _programPrototypes = [];

    /// <summary>The §2023:12.3.8.3 SR1 memory of THIS element's REPOSITORY paragraph: each specified name's first specification
    /// and the externalized name it gave.</summary>
    private readonly Dictionary<CobolName, (RepositorySpecification Spec, string Externalized)> _repositorySpecified = [];

    // ---- declaration -----------------------------------------------------------------------------------------------------

    /// <summary>Declare a program-specifier as written (<c>DataBinder.BindProgramSpecifier</c>); a repeat of a name keeps the first.</summary>
    internal void DeclareProgramSpecifier(ProgramSpecifier specifier) => _programSpecifiers.TryAdd(Key(specifier.Name), specifier);

    /// <summary>Record that the specifier of <paramref name="word"/> registers no prototype, and why; the first reason stays.</summary>
    internal void RefuseProgramSpecifier(string word, ProgramSpecifierRefusal why) => _refusedProgramSpecifiers.TryAdd(Key(word), why);

    /// <summary>A contained unit's, or a class half's, inheritance of its container's program-specifiers (§2023:12.3.8.4 GR10;
    /// §2023:12.3.4 GR1), before its own bind; the container's order is kept.</summary>
    private void InheritProgramSpecifiers(SymbolTable container)
    {
        foreach (var (key, specifier) in container._programSpecifiers) _programSpecifiers.TryAdd(key, specifier);
        foreach (var (key, refusal) in container._refusedProgramSpecifiers) _refusedProgramSpecifiers.TryAdd(key, refusal);
    }

    /// <summary>⛔ THE ONE §2023:12.3.8.3 SR1 CHECK, OVER THE WHOLE SPECIFICATION (kb/Work PB1017): "If any object-class-name-1,
    /// interface-name-2, program-prototype-name-1, function-prototype-name-1, intrinsic-function-name-1 or property-name-1 is
    /// specified more than once in the REPOSITORY paragraph, all the specifications for that name shall be identical." Keyed on
    /// THE NAME, not on (kind, name), and comparing everything <paramref name="specification"/> says. Reports a difference and
    /// returns the FIRST specification's externalized name when <paramref name="name"/> repeats; null when the name is new.</summary>
    internal string? RepeatedRepositorySpecification(string name, RepositorySpecification specification, string externalized)
    {
        var key = Key(name);
        if (_repositorySpecified.TryGetValue(key, out var first))
        {
            var (prior, priorExternalized) = first;
            if (prior != specification)
                _data.Edition.Error(DiagnosticCatalog.RepositoryProgramSpecifier,
                    $"REPOSITORY '{name}' is specified more than once with different specifications ('{prior}' then "
                    + $"'{specification}'); ISO §2023:12.3.8.3 syntax rule 1 requires all the specifications for one name to be "
                    + "identical");
            return priorExternalized;
        }
        _repositorySpecified[key] = (specification, externalized);
        return null;
    }

    // ---- the prototypes ------------------------------------------------------------------------------------------------------

    /// <summary>Make the program prototypes ONE source element may name (ISO §2023:8.4.6.8, Scope of program-prototype-names:
    /// "Program-prototype-names referenced within a source element shall be either the program-name of a containing program
    /// definition or a program-prototype-name declared in the REPOSITORY paragraph"). Both spellings register here, so
    /// §2023:14.9.4.3 SR16 and §2023:14.9.5.3 SR3 are ONE lookup:
    /// <list type="number">
    /// <item>every §2023:12.3.8.2 program-specifier visible to the element (its own and its container's; for a CLASS's OBJECT or
    /// FACTORY forest, the class-level REPOSITORY, which §2023:12.3.4 GR1 applies to every method, kb/Work PB1100), resolved
    /// through GR10 by <paramref name="repository"/> AS OF <paramref name="position"/>: a definition specified LATER in the group
    /// is not a) and takes the element down to b) / c) (kb/Work PB989), c) being the external repository's entry; a specifier
    /// none answers is REPO-1 and is not registered (kb/Work PB2103);</item>
    /// <item>for a program <paramref name="unit"/>, its own name and every containing program definition's name, which §2023:8.4.6.8
    /// admits with no specifier at all, and which is also exactly what §2023:12.3.8.3 SR15 means by "references to
    /// program-prototype-name-1 are to the named program definition and this program-specifier is ignored": a self- or
    /// container-named specifier is never resolved, and the definition, registered LAST, is the name's entry. A class forest
    /// (<paramref name="unit"/> null) has neither.</item>
    /// </list>
    /// The table is the same at every edition. COBOL-85 has no program-prototype-names, and the verbs that read a bare word
    /// against this table decide that (<c>CallBinder.ProgramPrototypeNamesExist</c>, kb/Work PB4509). Asked by
    /// <c>BinderDriver.ResolvePointerRestrictions</c> and again where the element's procedure division binds, with the same
    /// inputs; each call makes the table anew.</summary>
    internal void ResolveProgramPrototypes(BoundUnit? unit, RepositoryResolver repository, int position, ElementBinders binders)
    {
        var map = new Dictionary<CobolName, ProgramPrototype>();
        var containing = BinderDriver.ContainingDefinitions(unit).Where(u => !u.IsFunction).Select(u => Key(u.Name)).ToList();
        foreach (var (key, spec) in _programSpecifiers)
        {
            // §2023:12.3.8.3 SR15: a specifier naming the element's own or a containing program definition "is ignored" -- never
            // resolved (it would take c) and draw REPO-1: a definition does not precede itself); the self entry below is the
            // reference's target.
            if (containing.Contains(key)) continue;
            // GR10 a) / b): the in-group definition SPECIFIED PREVIOUSLY, else the in-group program PROTOTYPE definition
            // (kb/Work PB894), supplies the details; c) the external repository entry that publishes the externalized name. None
            // is REPO-1 (SR14), at the specifier, and registers no prototype: a CALL through it is COBOL1760.
            using var _ = _data.Edition.At(spec.At);
            if (repository.ResolveProgram(spec.ExternalizedName, position) is { } details)
                map[key] = new ProgramPrototype(spec.Name, spec.ExternalizedName, details);
            else
            {
                repository.ReportUnanswered(spec, function: false, spec.Name, spec.ExternalizedName, _data.Edition);
                // The refusal's route, for the CALL / CANCEL sentence (kb/Work PB4708): the external repository's finding
                // suppresses REPO-1, so no SR14 refusal exists to cite. The fact is position-independent.
                if (repository.External.Refused(RepositoryFamily.Program, spec.Name, spec.ExternalizedName))
                    _refusedProgramSpecifiers.TryAdd(key, ProgramSpecifierRefusal.RepositoryFinding);
            }
        }
        // §2023:8.4.6.8's other spelling, registered LAST (it also realizes SR15's "ignored", kb/Work PB3121). The prototype NAME
        // is the referable word, its EXTERNALIZED name what a CALL activates (kb/Work PB303).
        foreach (var u in BinderDriver.ContainingDefinitions(unit))
            if (!u.IsFunction)
                map[Key(u.Name)] = new ProgramPrototype(u.Name, u.ExternalizedName,
                    new ProgramAnswer(binders.SignatureOf(u), new GroupUnitOrigin(u)));
        _programPrototypes = map;
    }

    /// <summary>The program prototype one word names here, or null (<see cref="NameClass.ProgramPrototype"/>).</summary>
    private ProgramPrototype? ProgramPrototypeNamed(CobolName key) => _programPrototypes.GetValueOrDefault(key);

    /// <summary>The prototype a restricted PROGRAM-pointer's USAGE TO phrase names (ISO §2023:13.18.60.4 GR25), resolved in THIS
    /// element (<c>DataBinder.ResolveRestrictedPrototypes</c>, kb/Work PB2495); null for any other item and for a name this element's
    /// table does not hold.</summary>
    internal RestrictedPrototype? RestrictedProgramPrototypeOf(DataItem pointer) =>
        pointer.Pic is { Category: PicCategory.ProgramPointer, RestrictedPrototypeName: { } name } && ProgramPrototypeNamed(Key(name)) is { } program
            ? RestrictedPrototype.Of(program)
            : null;

    /// <summary>The signature of the program-prototype a restricted program-pointer <paramref name="pointer"/> names in its USAGE TO
    /// phrase, read in THIS element's table (§2023:8.4.6.8), the scope its declaration was screened against. THE ONE reader every
    /// signature comparison of a restricted program-pointer asks (the SET program-pointer pairs, the address-identifier argument, a
    /// restricted program-pointer's CALL, kb/Work PB609). Null for an unrestricted item and for a name the table does not hold,
    /// which was refused where it was declared; a comparison with such a side has no second fault to report
    /// (<see cref="StatementBinder.SignaturesDiffer"/>).</summary>
    internal CalleeSignature? ProgramSignatureOf(DataItem pointer) =>
        pointer.Pic?.RestrictedPrototypeName is { } name ? ProgramPrototypeNamed(Key(name))?.Signature : null;

    /// <summary>The signature of the program-prototype a program-address-identifier's program-prototype-name-1 arm names
    /// (§2023:8.4.3.13.4 GR3: the identifier "has the characteristics of a program-pointer restricted to" it); null for the
    /// identifier and literal arms and for a name the table does not hold.</summary>
    internal CalleeSignature? ProgramSignatureOf(BoundProgramAddress address) =>
        address.Prototype is { } name ? ProgramPrototypeNamed(Key(name))?.Signature : null;

    /// <summary>The two restrictions a program-address-identifier's program-prototype-name-1 arm is described with: its TO phrase by
    /// the prototype's externalized name (<see cref="PrototypeSignatures.NamedRestriction(string)"/>) and what it may contain, the
    /// identity of the prototype's signature (<see cref="PrototypeSignatures.Identity(RestrictedPrototype)"/>). Null for the
    /// identifier and literal arms. A name the table does not hold was refused where it was declared (REPO-1), and both its
    /// restrictions are its TO phrase as written (kb/Work PB4107), which no signature identity can equal.</summary>
    internal PrototypeSignatures.PrototypeRestriction? ProgramRestrictionOf(BoundProgramAddress? address) =>
        address?.Prototype is not { } name ? null
        : ProgramPrototypeNamed(Key(name)) is { } p
            ? new(PrototypeSignatures.NamedRestriction(p.ExternalizedName), PrototypeSignatures.Identity(RestrictedPrototype.Of(p)))
            : new(PrototypeSignatures.NamedRestriction(name), PrototypeSignatures.NamedRestriction(name));

    // ---- questions that answer no symbol -----------------------------------------------------------------------------------

    /// <summary>Whether a PROGRAM specifier of the REPOSITORY paragraph in scope declares <paramref name="name"/>, whatever became of
    /// its externalized name (kb/Work PB4708): the one question every reader of "is this word a declared program-prototype-name"
    /// asks (the spelling-(b) arm of <c>CallBinder</c>, <see cref="WhyNoPrototype"/>, the ADDRESS OF PROGRAM operand of
    /// <c>SetBinder</c> and the USAGE PROGRAM-POINTER TO screen of the data binder).</summary>
    internal bool DeclaresProgramSpecifier(string name) =>
        _programSpecifiers.ContainsKey(Key(name)) || _refusedProgramSpecifiers.ContainsKey(Key(name));

    /// <summary>Whether a reference to <paramref name="name"/> is the cascade of a PROGRAM specifier whose <c>AS literal-3</c> was
    /// refused (§2023:12.3.8.3 SR2) and reported where it is written (kb/Work PB4936): then the reference adds no diagnostic and
    /// <see cref="EditionContext.NoteCauseReportedEarlier"/> answers for the refusal it builds. The twin of
    /// <see cref="ConfigurationSlice.IsRefusedFunctionReference"/>.</summary>
    internal bool IsRefusedProgramReference(string name, EditionContext edition) =>
        _refusedProgramSpecifiers.TryGetValue(Key(name), out var refusal) && refusal == ProgramSpecifierRefusal.Literal3
        && edition.NoteCauseReportedEarlier(RefusedSpecifierKind.Program, name);

    /// <summary>WHY <paramref name="name"/> is no program prototype here, the one sentence <c>CallBinder.ResolvePrototype</c>
    /// (COBOL1760) and the ADDRESS OF PROGRAM operand of <c>SetBinder</c> (COBOL2157) add to their refusals (kb/Work PB4633,
    /// PB4708). A word a specifier DECLARES is never called "neither declared"; the two routes to a declared word with no prototype
    /// that this sentence names are different refusals, and only the first is REPO-1. The third route reports nothing here
    /// (<see cref="IsRefusedProgramReference"/>):
    /// <list type="number">
    /// <item>no provider answers the specifier (§2023:12.3.8.3 SR14) and COBOL5962 is reported where it is written;</item>
    /// <item>the same, but the external repository holds an entry whose own finding refuses it, which suppresses REPO-1
    /// (<c>RepositoryResolver.ReportUnanswered</c>): the sentence then names no REPO-1 and no code;</item>
    /// <item>the specifier's AS literal-3 was refused (§2023:12.3.8.3 SR2), so it registered no row at all, and its reference is
    /// the silent cascade of that refusal.</item>
    /// </list></summary>
    internal string WhyNoPrototype(string name)
    {
        const string declared = "it is declared by a PROGRAM specifier in the REPOSITORY paragraph (ISO §2023:12.3.8.2), but ";
        var key = Key(name);
        if (_programSpecifiers.ContainsKey(key))
            return _refusedProgramSpecifiers.TryGetValue(key, out var refusal) && refusal == ProgramSpecifierRefusal.RepositoryFinding
                ? declared + "the external repository's entry for it was refused by that repository's own finding, so it "
                  + "registers no program prototype"
                : declared + "no program prototype and no program definition specified previously in this compilation group "
                  + "and no information in the external repository answers that specifier (ISO §2023:12.3.8.3 SR14, COBOL5962), "
                  + "so it registers no program prototype";
        return "it is neither declared by a PROGRAM specifier in the REPOSITORY paragraph (ISO §2023:12.3.8.2) nor the "
               + "program-name of a containing program definition (§2023:8.4.6.8)";
    }

    /// <summary>The program-specifiers' externalized names, in the order they were written, for the backend's
    /// <see cref="ConfigurationFacts"/> (kb/Work PB2361).</summary>
    internal IReadOnlyList<string> ProgramSpecifierExternalizedNames() =>
        [.. _programSpecifiers.Values.Select(static s => s.ExternalizedName)];
}

/// <summary>ONE REPOSITORY specification of a name, as §2023:12.3.8.3 SR1 compares it: the specifier KIND (class,
/// interface, program, property, user-defined function, or intrinsic function), the value of its
/// <c>[ AS literal-n ]</c> (null when absent — the name is then externalized by the implementor's mapping of the
/// user-defined word, §2023:8.3.2.2, so two case spellings of one word are one specification), and the canonical
/// text of its <c>[ EXPANDS … USING … ]</c> phrase (null when absent). Every part of the written specification
/// that carries a meaning is a component, so "identical" is record equality.</summary>
internal sealed record RepositorySpecification(string Kind, string? As, string? Expands)
{
    public override string ToString() =>
        $"{Kind}{(As is null ? "" : $" AS \"{As}\"")}{(Expands is null ? "" : " " + Expands)}";
}
