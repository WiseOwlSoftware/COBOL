// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Compiler.Symbols;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>
/// The FUNCTION-PROTOTYPE-NAMES (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 7"; kb/Work PB5932): the
/// REPOSITORY paragraph's user-defined-function specifiers as written (ISO §2023:12.3.8.2), the ones whose <c>AS literal-5</c> was
/// refused, and the user-defined functions the element may name, resolved through §2023:12.3.8.4 GR11 with §2023:8.4.6.6's
/// containing function definition (<see cref="ResolveFunctionPrototypes"/>). Each table is keyed by <see cref="CobolName"/> and
/// private: a function-prototype-name a statement writes is <c>Identify</c> of <see cref="NameClass.FunctionPrototype"/>, keyed by
/// the word the &gt;&gt;COBOL-WORDS directive made of its token (<see cref="FunctionWord"/>, §2023:7.3.10.4 GR2 to GR4), and a
/// prototype-name a SYMBOL carries as text (a restricted function-pointer's USAGE TO phrase) is resolved here from the symbol.
/// </summary>
public sealed partial class SymbolTable
{
    /// <summary>The function-prototype-names the REPOSITORY paragraph in scope DECLARES (every user-defined-function specifier,
    /// a refused one included), the container's first.</summary>
    private readonly Dictionary<CobolName, SymbolId> _functionWords = [];

    /// <summary>The user-defined-function specifiers in scope, in the order written: the container's first, and this element's own
    /// specifier of a name replacing the container's in its place (§2023:12.3.8.3 SR15's shape for a contained element).</summary>
    private readonly Dictionary<CobolName, FunctionSpecifier> _functionSpecifiers = [];

    /// <summary>The function-prototype-names whose <c>AS literal-5</c> was refused where it is written (§2023:12.3.8.3 SR2): declared
    /// (<see cref="_functionWords"/>), but no <see cref="_functionSpecifiers"/> row (kb/Work PB4822).</summary>
    private readonly HashSet<CobolName> _refusedFunctionSpecifiers = [];

    /// <summary>The user-defined functions this element may name, by function-prototype-name, made by
    /// <see cref="ResolveFunctionPrototypes"/> once the group's REPOSITORY can be searched; empty before.</summary>
    private Dictionary<CobolName, UserFunctionSignature> _userFunctions = [];

    // ---- declaration -----------------------------------------------------------------------------------------------------

    /// <summary>Declare a user-defined-function specifier's word (<c>DataBinder</c>'s REPOSITORY loop). <paramref name="specifier"/>
    /// is the specifier, or null when its <c>AS literal-5</c> was refused (§2023:12.3.8.3 SR2): the word is still declared, but it
    /// registers no specifier, so a reference resolves to no prototype and is that refusal's cascade (kb/Work PB4773, PB4822).</summary>
    internal void DeclareFunctionSpecifier(ParserRuleContext word, FunctionSpecifier? specifier)
    {
        var key = Key(word.GetText());
        // The specifier's word is the declaration, refused literal or not (its entry, at the word, is what each reference answers).
        var id = Enter(NameClass.FunctionPrototype, word.GetText(), PositionOf(word.Start), word);
        _functionWords.TryAdd(key, id);
        if (specifier is not null) _functionSpecifiers[key] = specifier;
        else _refusedFunctionSpecifiers.Add(key);
    }

    /// <summary>A contained unit's, or a class half's, inheritance of its container's function specifiers (§2023:12.3.4 GR1), before
    /// its own bind; the container's order is kept.</summary>
    private void InheritFunctionSpecifiers(SymbolTable container)
    {
        foreach (var (key, id) in container._functionWords) _functionWords.TryAdd(key, id);
        foreach (var (key, specifier) in container._functionSpecifiers) _functionSpecifiers.TryAdd(key, specifier);
        _refusedFunctionSpecifiers.UnionWith(container._refusedFunctionSpecifiers);
    }

    // ---- the prototypes ------------------------------------------------------------------------------------------------------

    /// <summary>Make the user-defined functions ONE source element may reference, by the function-prototype-name it WRITES -- the
    /// function twin of <see cref="ResolveProgramPrototypes"/> (kb/Work PB974, PB989), resolved by
    /// <see cref="RepositoryResolver.ResolveFunction"/> as of <paramref name="position"/> (determination D-R3,
    /// DESIGN-external-repository §8.3): §2023:12.3.8.4 GR11 a) / b) keyed by the EXTERNALIZED name -- literal-5 when the specifier
    /// writes <c>AS literal-5</c>, else the name itself (NOTE 2) -- over the definitions specified BEFORE this element and then the
    /// prototypes, then c) the external repository's entry of that externalized name; when all miss, the function the WORD names
    /// (§2023:8.4.6.7), in the group and then in the repository. No hit is REPO-1 at the specifier (SR10) and leaves the name
    /// unmapped; a definition that FOLLOWS the element and has no prototype is such a name. A hit that is NOT the function the word
    /// names, while a different function does carry the word, warns (COBOL2969): the externalized name wins, as GnuCOBOL's does.
    /// <para>The table starts from the group's functions by word as of <paramref name="position"/>
    /// (<see cref="GroupRepositoryProvider.FunctionsVisibleAt"/>: each word's first answer), which lets a reference to a function the
    /// element did not declare say which function it could have meant. A specifier naming the function definition it is written in
    /// is IGNORED (§2023:12.3.8.3 SR11; the definition is <see cref="BinderDriver.ContainingDefinitions"/>' function member) and the
    /// name resolves to its own signature, registered LAST, as <see cref="ResolveProgramPrototypes"/> registers its self entry: a
    /// function definition's own name is ALWAYS referable inside it (§2023:8.4.6.6), and a definition does not start strictly before
    /// itself. A class forest (<paramref name="unit"/> null) has the class REPOSITORY's specifiers (§2023:12.3.4 GR1, kb/Work PB1100)
    /// and no self entry. Asked by <c>BinderDriver.ResolvePointerRestrictions</c> and again where the element's procedure division
    /// binds, with the same inputs; each call makes the table anew.</para></summary>
    internal void ResolveFunctionPrototypes(BoundUnit? unit, RepositoryResolver repository, int position, ElementBinders binders)
    {
        var map = new Dictionary<CobolName, UserFunctionSignature>();
        foreach (var answer in repository.Group.FunctionsVisibleAt(position))
            map.TryAdd(Key(answer.Signature.Name), answer.Signature);
        // §2023:8.4.6.6's no-specifier leg: the containing function definition, i.e. the element's own when it is one.
        var selves = BinderDriver.ContainingDefinitions(unit).Where(u => u.IsFunction).ToList();
        var selfKeys = selves.Select(u => Key(u.Name)).ToHashSet();
        foreach (var (key, specifier) in _functionSpecifiers)
        {
            // §2023:12.3.8.3 SR11: "If the specified function-prototype-name-1 is the name of the function definition in which this
            // REPOSITORY paragraph is specified, references to function-prototype-name-1 are to that function definition and this
            // function-specifier is ignored" -- whatever its AS literal says, so it is never resolved (a remap there bound the
            // recursive self-call against another function's signature, kb/Work PB1084); the self entry below is the target.
            if (selfKeys.Contains(key)) continue;
            string name = key.Word;
            using var _ = _data.Edition.At(specifier.At);
            if (repository.ResolveFunction(name, specifier.ExternalizedName, position) is not { } resolved)
            {
                // None of SR10's three alternatives, nor §2023:8.4.6.7's word, holds: REPO-1 at the specifier
                // (DESIGN-external-repository §8.3), and the name is no function of the element.
                map.Remove(key);
                repository.ReportUnanswered(specifier, function: true, name, specifier.ExternalizedName, _data.Edition);
                continue;
            }
            map[key] = resolved.Signature;
            if (resolved.Shadowed is { } shadowed)
                _data.Edition.Warning(DiagnosticCatalog.FunctionWordNamesAnotherFunction,
                    $"FUNCTION {name} activates the function externalized \"{resolved.Signature.Externalized}\" "
                    + $"(§2023:12.3.8.4 GR11 a) / b) take the details by externalized name), but {shadowed.Origin.Describe("function")} "
                    + $"also has the user-function-name {name}: it is reachable only through a REPOSITORY entry "
                    + $"FUNCTION {name} AS \"{shadowed.Signature.Externalized}\" (§2023:8.4.6.7)");
        }
        foreach (var u in selves)
        {
            var signature = binders.SignatureOf(u);
            map[Key(u.Name)] = new UserFunctionSignature(u.Name, u.ExternalizedName, signature.Returning, signature.Formals,
                DefinitionCompiledHere: true);   // §2023:8.4.6.6: the element's own definition
        }
        _userFunctions = map;
    }

    /// <summary>The function-prototype-name one word names here (<see cref="NameClass.FunctionPrototype"/>), or null: a word the
    /// REPOSITORY paragraph declares or the containing function definition's own name (§2023:8.4.6.6), with the function it
    /// resolved to (none when no definition, prototype or repository entry answers it, or before the table is resolved) and whether
    /// its specifier's <c>AS literal-5</c> was refused.</summary>
    private (object? Symbol, SymbolId Id) FunctionPrototypeNamed(CobolName key) =>
        DeclaresFunction(key)
            ? (new FunctionPrototypeReference(key.Word, _userFunctions.GetValueOrDefault(key), _refusedFunctionSpecifiers.Contains(key)),
                FunctionEntryOf(key))
            : default;

    /// <summary>Whether <paramref name="key"/> is a function-prototype-name in this element's scope (§2023:8.4.6.6): declared by a
    /// REPOSITORY function-specifier, or the user-function-name of the containing function definition.</summary>
    private bool DeclaresFunction(CobolName key) => _functionWords.ContainsKey(key) || (_containingAreFunctions && _containingDefinitions.ContainsKey(key));

    /// <summary>The entry of the function-prototype-name <paramref name="key"/>: the containing function definition's name, to which a
    /// reference goes even when a specifier writes the name (§2023:12.3.8.3 SR11), else the specifier's word.</summary>
    private SymbolId FunctionEntryOf(CobolName key) =>
        _containingAreFunctions && _containingDefinitions.TryGetValue(key, out var definition) ? _entries.IdOf(definition)
        : _functionWords.GetValueOrDefault(key);

    /// <summary>The key of a function-prototype-name written as <paramref name="written"/>: the word the &gt;&gt;COBOL-WORDS directive
    /// made of the token (<see cref="FunctionWord.Name"/>; kb/Work PB1372: resolved once, from the token, never from its text a second
    /// time), or of the text for a word inside a token.</summary>
    private CobolName FunctionKey(WrittenWord written) =>
        Key((written.Start == 0 && written.Length == written.Token.Text.Length
            ? FunctionWord.OfToken(written.Token, _data.CobolWords)
            : FunctionWord.OfWrittenWord(written.Text, _data.CobolWords)).Name);

    /// <summary>The function prototype a restricted FUNCTION-pointer's USAGE TO phrase names (ISO §2023:13.18.60.4 GR26), resolved in
    /// THIS element (<c>DataBinder.ResolveRestrictedPrototypes</c>, kb/Work PB2495); null for any other item and for a name this
    /// element's table does not hold.</summary>
    internal RestrictedPrototype? RestrictedFunctionPrototypeOf(DataItem pointer) =>
        pointer.Pic is { Category: PicCategory.FunctionPointer, RestrictedPrototypeName: { } name }
        && _userFunctions.GetValueOrDefault(Key(name)) is { } function
            ? RestrictedPrototype.Of(function)
            : null;

    /// <summary>The function-prototype-name a function-pointer <paramref name="pointer"/> is restricted to by its USAGE TO phrase
    /// (§2023:8.4.3.2.4 GR4: "the function prototype specified in the TO phrase of the USAGE clause in the definition of
    /// function-pointer-name-1 is used to determine the characteristics of the activated element"), read in THIS element's table;
    /// null for an unrestricted pointer. The TO phrase was screened against §2023:8.4.6.6 where it is declared, so the reference
    /// answers whether or not the word is still declared here, as the activation always asked.</summary>
    internal FunctionPrototypeReference? FunctionPrototypeOf(DataItem pointer) =>
        pointer.Pic?.RestrictedPrototypeName is { } name
            ? new FunctionPrototypeReference(name, _userFunctions.GetValueOrDefault(Key(name)), _refusedFunctionSpecifiers.Contains(Key(name)))
            : null;

    /// <summary>The signature of the function-prototype a restricted function-pointer <paramref name="pointer"/> names in its USAGE TO
    /// phrase, read in THIS element's table (§2023:8.4.6.6): THE ONE reader every signature comparison of a restricted
    /// function-pointer asks (the SET function-pointer pairs, §2023:14.9.39.3 SR20, and GR14's run-time granularity). Null for an
    /// unrestricted item and for a name the table does not hold, which was refused where it was declared; a comparison with such a
    /// side has no second fault to report (<see cref="StatementBinder.SignaturesDiffer"/>).</summary>
    internal CalleeSignature? FunctionSignatureOf(DataItem pointer) => FunctionPrototypeOf(pointer)?.CalleeSignature;

    // ---- questions that answer no symbol -----------------------------------------------------------------------------------

    /// <summary>Whether <paramref name="word"/> is a function-prototype-name in this element's scope (§2023:8.4.6.6), for the readers
    /// that classify a word no token carries (a SUBSCRIPT-mode capture, <c>SubscriptBinder</c>) or that screen a declaration
    /// (<c>PtrBindBasedAndAddressablesPass</c>'s ADDRESS OF FUNCTION screen). A written function-prototype-name is <c>Identify</c>d.</summary>
    internal bool DeclaresFunctionPrototype(string word) => DeclaresFunction(Key(word));

    /// <summary>Whether the table holds a function of the group under <paramref name="word"/> that the element did not declare: the
    /// hint a refused <c>FUNCTION word</c> adds ("the compilation group defines FUNCTION-ID ..., declare it", §2023:12.3.8.4 GR12).</summary>
    internal bool GroupDefinesFunction(string word) => _userFunctions.ContainsKey(Key(word));

    /// <summary>Whether a reference to <paramref name="reference"/> is the cascade of a FUNCTION specifier whose <c>AS literal-5</c> was
    /// refused (§2023:12.3.8.3 SR2) and reported where it is written (kb/Work PB4822, PB4936): then the reference adds no diagnostic and
    /// <see cref="EditionContext.NoteCauseReportedEarlier"/> answers for the refusal it builds. The twin of
    /// <see cref="IsRefusedProgramReference"/>. Asked BEFORE the prototype: a refused specifier must not let the bare word resolve to a
    /// same-named function of the group.</summary>
    internal static bool IsRefusedFunctionReference(FunctionPrototypeReference reference, EditionContext edition) =>
        reference.LiteralRefused && edition.NoteCauseReportedEarlier(RefusedSpecifierKind.Function, reference.Word);
}

/// <summary>What a function-prototype-name identifies (<see cref="NameClass.FunctionPrototype"/>): the word as the reference names it
/// (<see cref="FunctionWord.Name"/>), the user-defined function it resolved to (null when no definition, prototype or external
/// repository entry answers its specifier, §2023:12.3.8.4 GR11, which the activation reports as COBOL1505), and whether its specifier's
/// <c>AS literal-5</c> was refused (§2023:12.3.8.3 SR2), whose cascade the activation stays silent on (kb/Work PB4822).</summary>
internal sealed record FunctionPrototypeReference(string Word, UserFunctionSignature? Signature, bool LiteralRefused)
{
    /// <summary>The signature as a callee's (§2023:14.8.2, §2023:14.8.3), the shape every name-to-name signature screen compares.</summary>
    public CalleeSignature? CalleeSignature => Signature is { } s ? new CalleeSignature(s.Formals, s.Returning) : null;
}
