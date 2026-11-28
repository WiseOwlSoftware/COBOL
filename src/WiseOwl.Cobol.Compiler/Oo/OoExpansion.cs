// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Compiler.Binding.Repository;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Compiler.Oo;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ PARAMETERIZED CLASSES AND INTERFACES ARE EXPANDED, NOT GENERIC (kb/Work PB759; OO deep-dive D12).
///
/// <para><b>The rule.</b> ISO §2023:9.3.12: "A parameterized class is a generic or skeleton class that has formal
/// parameters that will be replaced by one or more object-class-names or interface-names. When it is expanded by
/// substituting specific object-class-names or interface-names as actual parameters, a class is created that
/// functions as a non-parameterized class", and "An expansion of a parameterized class is treated in all respects
/// the same as if it were a class that is not a parameterized class". §2023:12.3.8.4 GR5 says how: "The class
/// object-class-name-1 is created from the parameterized class object-class-name-2 by replacing each specification
/// of the formal parameter by the corresponding actual parameter" (GR8 is the interface twin; §2023:9.3.13 its
/// concept).</para>
///
/// <para><b>Why a re-parse and not a C# generic.</b> The binder resolves method names, conformance (§2023:9.3.8.2.4),
/// typed object-reference descriptions and INVOKE lookups STATICALLY, keyed on a class NAME; a formal parameter is
/// unconstrained, so none of those can be answered inside the definition, only inside an expansion. So each
/// expansion becomes an ORDINARY class definition: the parameterized definition's own tokens, re-parsed with each
/// formal replaced by its actual (GR5's own words) and the header renamed to the expansion's name. From
/// <see cref="OoClassTable.Build"/> onward nothing can tell an expansion from a written class — which is exactly
/// the "in all respects" of §2023:9.3.12, and makes every later OO rule apply to expansions with no new code.</para>
///
/// <para><b>Why token substitution is the rule and not an approximation.</b> §2023:11.3.4 GR6 (§2023:11.6.4 GR4 for an
/// interface): "Parameter-name-1 may be specified within this class definition only where an object-class-name
/// or an interface-name is permitted." Every occurrence of a formal's word inside its definition therefore IS a
/// class-name or interface-name position, so replacing every user-defined-word occurrence is GR5's "each
/// specification of the formal parameter" — the substitution cannot hit a data-name, because a formal cannot be
/// spelled as one.</para>
///
/// <para><b>Identity.</b> §2023:9.3.12: "two classes with the same externalized object-class-name that are created by
/// expanding the same parameterized class with the same actual parameters are the same class instance", and
/// expansions with different actuals "shall not have the same externalized object-class-name". The expansion
/// name is its identity: specifiers repeating one (name, definition, actuals) triple across source elements
/// create ONE class; a name reused with a different definition or actuals is COBOL2240.</para>
///
/// <para><b>The definition itself emits nothing.</b> It is a skeleton, and §2023:12.3.8.4 GR1 ("If object-class-name-1
/// is a class described with the USING phrase, object-class-name-1 may be specified only in the REPOSITORY
/// paragraph") leaves it no other use. It is kept out of the class table and recorded as parameterized, so
/// <see cref="SymbolTable.ClassNames.Resolve"/> can name GR1 when some other position writes it. It is still a class or
/// interface definition, so it BINDS as itself for its own rules — its data, environment and method headers, with each
/// formal resolving as a formal (kb/Work PB2051; <c>OoDriver.BindParameterized</c>).</para>
///
/// <para><b>It is published, and another group's is expanded.</b> A group built with <c>--repository-update on</c> publishes each
/// definition as a SKELETON (<see cref="SkeletonSource"/>; <c>OoEmitter.EmitSkeletonUnit</c>): its text after text manipulation, its
/// formals, what its REPOSITORY paragraph names and the compile state it was written under. An EXPANDS target this group does not define
/// is read from the external repository (<see cref="IOoImporter.Skeleton"/>) and lexed again from that recorded text
/// (<c>FragmentParse.ParseUnit</c>), then expanded exactly as a written skeleton is. Which assembly an expansion lives in, and whether
/// one that already exists is the class, is decided once the group is bound (<c>ExpansionPlacementPass</c>): an expansion's meaning is
/// the published surface of everything it names, which exists only then (DESIGN-external-repository §10.5; kb/Work PB2104).</para>
/// </summary>
internal static class OoExpansion
{
    /// <summary>The group's definitions after expansion: the written NON-parameterized definitions in source order,
    /// then one synthesized definition per distinct expansion (in first-specifier order), plus the names of the
    /// parameterized definitions (class and interface names share one namespace, §2023:8.3.2.2) and the parameterized
    /// definitions themselves — kept out of the class table, but still class and interface definitions that every
    /// rule of their own text governs (kb/Work PB1505, PB2051): <c>OoClassTable.Build</c> screens their headers and the
    /// binder binds each as itself (<c>OoDriver.BindParameterized</c>), emitting nothing — and the names of the
    /// definitions that some expansion re-binds (<see cref="SymbolTable.ExpansionNames.IsExpanded"/>, the names of the expansion that the
    /// group tier of the symbol table holds, <see cref="Result.Names"/>): those are checked through their
    /// expansions, never also as themselves, because one fact is one diagnostic and a report naming the definition
    /// would sit beside each expansion's report of the same line (train 1053 review).</summary>
    public sealed record Result(
        IReadOnlyList<Core.ClassDefinitionContext> Classes,
        IReadOnlyList<Core.InterfaceDefinitionContext> Interfaces,
        SymbolTable.ExpansionNames Names,
        IReadOnlyList<Core.ClassDefinitionContext> ParameterizedClasses,
        IReadOnlyList<Core.InterfaceDefinitionContext> ParameterizedInterfaces,
        IReadOnlyList<SkeletonSource> Skeletons,
        IReadOnlyDictionary<ParserRuleContext, ExpansionOrigin> Origins)
    {
        /// <summary>⛔ THE EXPANSIONS WHOSE TEXT THE WRITTEN TREE DOES NOT HOLD (kb/Work PB4483): each made from a skeleton read from the
        /// repository, in the order the expansions were made (classes, then interfaces), with what it was made from. An expansion of a
        /// skeleton written in this group is the skeleton's own text with the formals replaced, at the skeleton's own positions, so the
        /// screens of the written tree have asked it already; these are the texts they have not seen.</summary>
        public IEnumerable<(ParserRuleContext Text, ExpansionOrigin Origin)> RepositoryTexts() => Origins.Count == 0 ? [] :
            Classes.Cast<ParserRuleContext>().Concat(Interfaces)
                .Select(d => (Text: d, Origin: Origins.GetValueOrDefault(d)))
                .Where(e => e.Origin is { Region.IsRepositoryText: true })
                .Select(e => (e.Text, e.Origin!));
    }

    /// <summary>One parameterized definition: its context, its formals in USING order, and the kind each formal's
    /// REPOSITORY specifier declares it as (null when undeclared — already COBOL2239). <paramref name="Source"/> is what the
    /// group publishes for it, or — for a skeleton another group published — what that group published.</summary>
    internal sealed record Skeleton(ParserRuleContext Ctx, string Name, bool IsInterface,
        IReadOnlyList<Core.OoParameterNameContext> Formals, IReadOnlyList<bool?> FormalIsInterface, SkeletonSource? Source = null);

    /// <summary>One distinct expansion (§2023:9.3.12 identity): the expansion name, its skeleton and the actuals, and the REPOSITORY
    /// entry that asked for it.</summary>
    internal sealed record Expansion(string Name, Skeleton Of, IReadOnlyList<string> Actuals, Core.RepositoryEntryContext Entry);

    private static readonly IReadOnlyDictionary<ParserRuleContext, ExpansionOrigin> NoOrigins =
        new Dictionary<ParserRuleContext, ExpansionOrigin>(ReferenceEqualityComparer.Instance);

    /// <param name="importer">Where a skeleton another group published comes from (ISO §2023:12.3.8.3 SR4 / SR7: the parameterized
    /// definition "shall be defined in the same REPOSITORY paragraph", and §2023:12.3.8.3 SR6 b requires information in the external
    /// repository for a class a group does not define), or null when there is no repository.</param>
    /// <param name="inputs">The compile-time inputs of the group, from which the state a skeleton is published under is read.</param>
    public static Result Expand(Core.CompilationUnitContext tree,
        IReadOnlyList<Core.ClassDefinitionContext> classes, IReadOnlyList<Core.InterfaceDefinitionContext> interfaces,
        EditionContext edition, CobolWordsMap words, IOoImporter? importer, CompileInputs inputs)
    {
        var published = new List<SkeletonSource>();
        var origins = new Dictionary<ParserRuleContext, ExpansionOrigin>(ReferenceEqualityComparer.Instance);
        // The expansion's names are the compilation group tier's (SymbolTable.ExpansionNames; docs/rearchitecture/DESIGN-listing.md
        // section 6.7.1, "Step 6b, remainder slice 5").
        var names = new SymbolTable.ExpansionNames();
        var plainClasses = new List<Core.ClassDefinitionContext>();
        var plainInterfaces = new List<Core.InterfaceDefinitionContext>();
        foreach (var c in classes)
        {
            var id = c.classIdParagraph();
            if (id.ooParameterName().Length == 0) { plainClasses.Add(c); continue; }
            AddSkeleton(c, id.className(0).GetText(), false, id.ooParameterName(), c.environmentDivision(), id.externalizedNamePhrase());
        }
        foreach (var i in interfaces)
        {
            if (i.ooParameterName().Length == 0) { plainInterfaces.Add(i); continue; }
            AddSkeleton(i, i.interfaceName(0).GetText(), true, i.ooParameterName(), i.environmentDivision(), i.externalizedNamePhrase());
        }
        var entries = new List<Core.RepositoryEntryContext>();
        CollectExpandsEntries(tree, entries);
        if (!names.HasWrittenSkeletons && entries.Count == 0)
            return new Result(classes, interfaces, SymbolTable.ExpansionNames.None, [], [], [], NoOrigins);   // the overwhelmingly common path
        // §2023:12.3.8.3 SR3 — an EXPANDS phrase inside a parameterized definition is reported once, on the definition
        // (AddSkeleton), and is otherwise inert: it creates nothing.
        entries.RemoveAll(e => EnclosingSkeleton(e) is not null);

        // Every name an actual parameter may resolve to: the written plain definitions and every expansion NAME
        // (an actual may itself be an expansion, declared in the same paragraph — §2023:12.3.8.3 SR4).
        foreach (var c in plainClasses) names.DeclareDefinition(c.classIdParagraph().className(0).GetText(), isInterface: false);
        foreach (var i in plainInterfaces) names.DeclareDefinition(i.interfaceName(0).GetText(), isInterface: true);
        foreach (var sk in names.WrittenSkeletons)
            if (names.DeclaresDefinition(sk.Name, isInterface: false) || names.DeclaresDefinition(sk.Name, isInterface: true))
            {
                using var _ = edition.At(sk.Ctx);
                edition.Error(DiagnosticCatalog.OoDefinitionNameNotUnique,
                    $"duplicate {Kind(sk)} definition '{sk.Name}' — class and interface definitions within a compilation "
                    + "group shall have unique names, and a class and an interface cannot share one (ISO §2023:8.4.6.4 / §2023:8.3.2.2)");
            }
        foreach (var e in entries)
        {
            string en = DeclaredName(e)!;
            if (names.WrittenSkeleton(DeclaredWord(e)) is { } clash)
            {
                using var _ = edition.At(e);
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"REPOSITORY {(e.INTERFACE() is not null ? "INTERFACE" : "CLASS")} '{en}' EXPANDS: '{en}' is the "
                    + $"name of the parameterized {Kind(clash)} definition itself, so no expansion can be created "
                    + "under it (ISO §2023:12.3.8.4 GR1/GR5; §2023:8.3.2.2)");
                continue;
            }
            names.DeclareDefinition(en, isInterface: e.INTERFACE() is not null);
        }

        var order = new List<Expansion>();
        foreach (var e in entries)
            if (names.WrittenSkeleton(DeclaredWord(e)) is null && Validate(e) is { } x)
            {
                if (names.DeclareExpansion(x) is not { } prior) order.Add(x);
                else if (!ReferenceEquals(prior.Of, x.Of) || !SymbolTable.ExpansionNames.SameActuals(prior.Actuals, x.Actuals))
                {
                    using var _ = edition.At(e);
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"REPOSITORY {Kind(x.Of)} '{x.Name}' EXPANDS {x.Of.Name} USING {string.Join(" ", x.Actuals)}: "
                        + $"the name '{x.Name}' is already given to the expansion of {prior.Of.Name} USING "
                        + $"{string.Join(" ", prior.Actuals)} — expansions {(ReferenceEquals(prior.Of, x.Of) ? "with different actual parameters" : "of different parameterized definitions")} "
                        + $"are not the same {Kind(x.Of)} instance and shall not have the same externalized name "
                        + (x.Of.IsInterface ? "(ISO §2023:9.3.13)" : "(ISO §2023:9.3.12)"));
                }
            }

        var outClasses = new List<Core.ClassDefinitionContext>(plainClasses);
        var outInterfaces = new List<Core.InterfaceDefinitionContext>(plainInterfaces);
        foreach (var x in order)
        {
            // Who answers for the expansion's text (DiagnosticRegion; kb/Work PB4469, PB4483): a skeleton written in this group is the
            // user's text, reported where the user wrote it; one read from the repository is reported at the EXPANDS entry.
            var region = x.Of.Source?.From is null ? new DiagnosticRegion(x.Name)
                : new DiagnosticRegion(x.Name, new DiagnosticCursor(x.Entry.Start.Line, x.Entry.Start.Column));
            var tokens = SubstitutedTokens(x, region);
            if (x.Of.IsInterface)
            {
                if (FragmentParse.ParseTokens(tokens, edition.Edition, words, p => p.interfaceDefinition()) is { } ictx)
                {
                    outInterfaces.Add(ictx);
                    names.MarkExpanded(x.Of.Name);
                    origins[ictx] = new ExpansionOrigin(x.Of.Source!, x.Entry, x.Actuals, SkeletonText.OfTokens(tokens), region, ClientWrittenTexts(tokens));
                }
                else ReparseFailed(x);
            }
            else if (FragmentParse.ParseTokens(tokens, edition.Edition, words, p => p.classDefinition()) is { } cctx)
            {
                outClasses.Add(cctx);
                names.MarkExpanded(x.Of.Name);
                origins[cctx] = new ExpansionOrigin(x.Of.Source!, x.Entry, x.Actuals, SkeletonText.OfTokens(tokens), region, ClientWrittenTexts(tokens));
            }
            else ReparseFailed(x);
        }
        return new Result(outClasses, outInterfaces, names,
            [.. classes.Where(c => c.classIdParagraph().ooParameterName().Length > 0)],
            [.. interfaces.Where(i => i.ooParameterName().Length > 0)], published, origins);

        void AddSkeleton(ParserRuleContext ctx, string name, bool isInterface, Core.OoParameterNameContext[] formals,
            Core.EnvironmentDivisionContext? env, Core.ExternalizedNamePhraseContext? asPhrase)
        {
            using var _ = edition.At(ctx);
            string header = isInterface ? "INTERFACE-ID" : "CLASS-ID";
            var rules = HeaderRules.Of(edition.Edition, isInterface);
            // The definition's OWN REPOSITORY paragraph (§2023:11.3.3 SR8 / §2023:11.6.3 SR4 say "of this class
            // definition" / "of this interface definition" — never a factory's or object's, which §2023:12.3.3 SR3
            // forbids to carry one anyway).
            var repo = SymbolTable.RepositoryScope.SpecifierEntries(env).ToList();
            var kinds = new List<bool?>();
            var repeated = SymbolTable.ExpansionNames.RepeatedFormals(formals).ToList();
            // A repeated parameter-name is refused in the USING clause itself from COBOL-2014 (§2014:11.3.2 SR9 / §2014:11.6.2 SR7,
            // §2023:11.3.3 SR9 / §2023:11.6.3 SR7); COBOL-2002 has no such rule, and its references are asked below (kb/Work PB5753).
            bool usingOnce = DialectBehaviors.IsActive(DialectBehavior.ParameterNameAppearsOnceInUsingClause, edition.DialectLevel);
            foreach (var f in formals)
            {
                string fn = f.GetText();
                using var __ = edition.At(f);
                if (usingOnce && repeated.Contains(f))
                    edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                        $"{header} '{name}' USING: the parameter-name '{fn}' is written more than once — a given "
                        + $"parameter-name shall not appear more than once in a USING clause ({rules.Repeated})");
                bool? kind = SymbolTable.ExpansionNames.SpecifierDeclaring(repo, fn) is { } decl
                    ? decl.INTERFACE() is not null : null;
                if (kind is null)
                    edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                        $"{header} '{name}' USING: the parameter-name '{fn}' is not declared by a class-specifier or "
                        + $"interface-specifier in the REPOSITORY paragraph of this {(isInterface ? "interface" : "class")} "
                        + $"definition ({rules.Declared})");
                kinds.Add(kind);
            }
            // §2002:11.2.3 GR5 / §2002:11.5.3 GR3: "Parameter-name-1 is the name given to the formal parameter", so at COBOL-2002
            // `USING P P` names two formal parameters P. Every other occurrence of P in the definition is a reference to a formal,
            // and it identifies neither one (§2002:8.4.1; a parameter-name cannot be qualified): exactly the occurrences an
            // expansion replaces (IsUserWordOccurrence), whose "corresponding actual parameter" (§2002:12.2.7.3 GR5 / GR8) would be
            // two. The formal's own class-specifier or interface-specifier, which §2002:11.2.2 SR9 / §2002:11.5.2 SR4 require,
            // declares its kind and is no reference; with every reference refused, which actual replaces it changes nothing.
            if (!usingOnce && repeated.Count > 0)
                foreach (var t in UserWordOccurrences(ctx, formals).Where(t => FormalSpecifierName(t) is null
                             && SymbolTable.ExpansionNames.IsFormal(repeated, t.GetText())))
                {
                    using var __ = edition.At(t.Symbol);
                    int count = formals.Count(f => SymbolTable.ExpansionNames.IsFormal([f], t.GetText()));
                    edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                        $"{header} '{name}': the parameter-name '{t.GetText()}' is written {count} times in the USING clause, so it "
                        + $"names {count} formal parameters and this reference identifies none of them uniquely — "
                        + $"\"{edition.Edition.Quote(inOrderTo: true)}\" (ISO §2002:8.4.1)");
                }
            // §2023:12.3.8.3 SR12 / SR13 (kb/Work PB1744): a parameter-name is a user-defined word (§2023:8.3.2.2) in the scope
            // of this definition's REPOSITORY paragraph, so one its FUNCTION … INTRINSIC specifier identifies is
            // refused — through the one declaration funnel, in a scope of its own: the binder that binds the definition
            // as itself (OoDriver.BindParameterized, kb/Work PB2051) declares the formal's class-specifier as an
            // object-class-name, and one word is only one type of user-defined word in a scope (§2023:8.3.2.2).
            new DataBinder(edition) { CobolWords = words }
                .DeclareParameterNames(SymbolTable.RepositoryScope.RepositoryEntries(env), formals);
            // §2023:11.3.4 GR6 / §2023:11.6.4 GR4: a parameter-name may be specified "only where an object-class-name or an
            // interface-name is permitted". Every occurrence is substituted (that is GR5), so a formal written in
            // a DECLARATION slot would silently rename a data item, paragraph, method or file in every expansion;
            // one written in a data REFERENCE is caught downstream (the actual names no data item). The
            // declaration slots are exactly the name rules below — none of them admits a class-name.
            foreach (var w in DeclarationSlotWords(ctx).Where(w => SymbolTable.ExpansionNames.IsFormal(formals, w.GetText())))
            {
                using var __ = edition.At(w);
                edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                    $"{header} '{name}': the parameter-name '{w.GetText()}' is written as a {SlotKind(w)} — a "
                    + $"parameter-name may be specified within the definition only where {rules.ClassNameKind} or an "
                    + $"interface-name is permitted ({rules.Placement})");
            }
            foreach (var r in repo.Where(r => r.expandsPhrase() is not null))
            {
                using var __ = edition.At(r);
                edition.Error(DiagnosticCatalog.ParameterizedDefinitionUsing,
                    $"{header} '{name}' is parameterized (it has a USING clause), so its REPOSITORY paragraph shall "
                    + $"not specify the EXPANDS phrase of '{DeclaredName(r)}' ({rules.NoExpands})");
            }
            // The skeleton's own END CLASS / END INTERFACE name (§2023:10.7.3 SR4 / SR6) is asked with every other end
            // marker by Validation.EndMarkerPass, over the written tree, before any expansion renames it.
            // What this group publishes for the definition (DESIGN-external-repository §10.5): its text, the state it is compiled
            // under and the specifiers of its own REPOSITORY paragraph that are not its formals.
            var source = new SkeletonSource(name, SkeletonIdentity(asPhrase, name), isInterface, ctx, SkeletonText.Of(ctx), inputs.At(ctx.Start.Line),
                [.. formals.Select(f => f.GetText())], [.. kinds.Select(k => k ?? false)],
                [.. repo.Where(r => !SymbolTable.ExpansionNames.IsFormal(formals, DeclaredName(r)!))], null);
            if (names.DeclareSkeleton(new Skeleton(ctx, name, isInterface, formals, kinds, source))) published.Add(source);
            else
                edition.Error(DiagnosticCatalog.OoDefinitionNameNotUnique,
                    $"duplicate {(isInterface ? "interface" : "class")} definition '{name}' — class and interface "
                    + "definitions within a compilation group shall have unique names, and a class and an interface cannot "
                    + "share one (ISO §2023:8.4.6.4 / §2023:8.3.2.2)");
        }

        // The externalized name a definition or a specifier states: the AS literal, else the declared word (ISO §2023:12.3.8.4 GR2 for a
        // specifier, §2023:11.3.4 GR1 for a CLASS-ID; the literal is screened by §2023:11.3.3 SR1). Read by the pure peek the identity scope uses, so a rejected literal resolves by the word it falls back to.
        string SkeletonIdentity(Core.ExternalizedNamePhraseContext? asPhrase, string word) =>
            asPhrase is null ? word
            : ExternalizedName.Peek(asPhrase, k => SymbolTable.IdentifyConstantBeforeBind(k, inputs.Variables), edition.DialectLevel) ?? word;

        Expansion? Validate(Core.RepositoryEntryContext e)
        {
            using var _ = edition.At(e);
            var ph = e.expandsPhrase();
            bool isInterface = e.INTERFACE() is not null;
            string kind = isInterface ? "INTERFACE" : "CLASS";
            string name = DeclaredName(e)!;
            var targetWord = ph.expandsTarget();
            string target = targetWord.GetText();
            string targetWritten = WrittenSource.Of(targetWord);
            var actuals = ph.expandsActual().Select(a => a.GetText()).ToList();
            string where = $"REPOSITORY {kind} '{name}' EXPANDS {targetWritten}";

            // §2023:12.3.8.3 SR4 / SR7: the parameterized name and every actual "shall be defined in the same
            // REPOSITORY paragraph where object-class-name-1 [interface-name-2] is defined".
            string sr = isInterface ? "ISO §2023:12.3.8.3 SR7" : "ISO §2023:12.3.8.3 SR4";
            var paragraph = ((Core.RepositoryParagraphContext)e.Parent).repositoryEntry();
            bool ok = true;
            foreach (string n in actuals.Prepend(target))
                if (SymbolTable.ExpansionNames.SpecifierDeclaring(paragraph, n) is null)
                {
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"{where}: '{n}' is not defined by a class-specifier or interface-specifier in the same "
                        + $"REPOSITORY paragraph ({sr})");
                    ok = false;
                }

            string gr = isInterface ? "ISO §2023:12.3.8.4 GR8" : "ISO §2023:12.3.8.4 GR5";
            var sk = names.WrittenSkeleton(targetWord);
            var usable = sk is not null && sk.IsInterface == isInterface ? sk : ImportedSkeleton(targetWord, isInterface, paragraph);
            if (usable is null)
            {
                bool isPlain = names.DeclaresDefinition(target, isInterface);
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"{where}: '{targetWritten}' is not a parameterized {(isInterface ? "interface" : "class")} of this "
                    + "compilation group — " + (sk is not null
                        ? $"it is a parameterized {(sk.IsInterface ? "interface" : "class")}, and "
                          + $"{(isInterface ? "an interface-specifier expands a parameterized interface" : "a class-specifier expands a parameterized class")}"
                        : isPlain ? $"it has no USING clause, so there is no formal parameter to replace"
                        : "no definition of that name is in this compilation group")
                    + $" ({gr})");
                return null;
            }
            sk = usable;
            if (actuals.Count != sk.Formals.Count)
            {
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"{where}: {actuals.Count} actual parameter(s) are supplied but '{sk.Name}' declares "
                    + $"{sk.Formals.Count} in the USING clause of its {(isInterface ? "INTERFACE-ID" : "CLASS-ID")} "
                    + $"paragraph — the numbers shall be the same ({gr})");
                return null;
            }
            for (int i = 0; i < actuals.Count; i++)
            {
                string a = actuals[i];
                // A class or interface of this group, else the one its specifier names for the external repository to answer
                // (ISO §2023:12.3.8.3 SR6 b / SR9): whether the repository does is the specifier's own REPO-1, not this phrase's.
                bool? actualIsInterface = names.IsParameterized(a) ? null
                    : names.DeclaresDefinition(a, isInterface: false) ? false : names.DeclaresDefinition(a, isInterface: true) ? true
                    : importer is not null && SymbolTable.ExpansionNames.SpecifierDeclaring(paragraph, a, specifierOnly: true) is { } spec
                        ? spec.INTERFACE() is not null : null;
                if (actualIsInterface is null)
                {
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"{where}: the actual parameter '{a}' is " + (names.IsParameterized(a)
                            ? "itself a parameterized definition, which is a skeleton and not a class or interface "
                              + "(ISO §2023:9.3.12/§2023:9.3.13); expand it in the REPOSITORY and pass the expansion"
                            : "not a class or interface of this compilation group")
                        + $" ({gr})");
                    ok = false;
                }
                else if (sk.FormalIsInterface[i] is { } fk && fk != actualIsInterface)
                {
                    edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                        $"{where}: the actual parameter '{a}' is {(actualIsInterface.Value ? "an interface" : "a class")} "
                        + $"but the formal '{sk.Formals[i].GetText()}' is declared by {(fk ? "an interface-specifier" : "a class-specifier")} "
                        + $"in '{sk.Name}' — replacing the formal by the actual ({gr}) would write "
                        + $"{(fk ? "an interface-specifier" : "a class-specifier")} naming {(actualIsInterface.Value ? "an interface" : "a class")}");
                    ok = false;
                }
            }
            return ok ? new Expansion(name, sk, actuals, e) : null;
        }

        // A parameterized definition another group published (ISO §2023:12.3.8.3 SR4 / SR6 b): the class-specifier or interface-specifier
        // of the EXPANDS target names it by identity, and a definition of this group wins a tie as everywhere (§2023:8.4). Re-lexed from the
        // text it recorded, under the edition and words map it recorded, so the expansion is made from the definition that was
        // published and not from this group's reading of the same words.
        Skeleton? ImportedSkeleton(ParserRuleContext targetWord, bool isInterface, IReadOnlyList<Core.RepositoryEntryContext> paragraph)
        {
            string target = targetWord.GetText();
            if (names.ImportedSkeleton(targetWord) is { } known) return known.IsInterface == isInterface ? known : null;
            if (importer is null || names.DeclaresDefinition(target, isInterface)) return null;
            var specifier = SymbolTable.ExpansionNames.SpecifierDeclaring(paragraph, target, specifierOnly: true, isInterface: isInterface);
            if (specifier is null) return null;
            string identity = SkeletonIdentity(specifier.externalizedNamePhrase(), target);
            if (importer.Skeleton(identity, isInterface) is not { } found) return null;
            using var _ = edition.At(specifier);
            var state = found.Record.State;
            var wordsMap = CobolWordsMap.FromDirectiveText(state.CobolWordsMap);
            var unit = wordsMap is null ? null : FragmentParse.ParseUnit(found.Record.Source, EditionInfo.Of(state.Edition, state.Permissive), wordsMap);
            ParserRuleContext? definition = isInterface
                ? unit?.compilationGroup().SelectMany(g => g.interfaceDefinition()).FirstOrDefault(d => d.ooParameterName().Length > 0)
                : unit?.compilationGroup().SelectMany(g => g.classDefinition()).FirstOrDefault(d => d.classIdParagraph().ooParameterName().Length > 0);
            if (definition is null)
            {
                edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                    $"REPOSITORY {(isInterface ? "INTERFACE" : "CLASS")} '{WrittenSource.Of(targetWord)}': the parameterized {(isInterface ? "interface" : "class")} '{identity}' that "
                    + $"'{found.From.AssemblyFile}' publishes does not parse from the source it recorded, so it cannot be expanded: recompile that "
                    + "assembly (ISO §2023:12.3.8.4 GR5/GR8)");
                return null;
            }
            var formals = definition is Core.ClassDefinitionContext cd ? cd.classIdParagraph().ooParameterName() : ((Core.InterfaceDefinitionContext)definition).ooParameterName();
            var env = definition is Core.ClassDefinitionContext c2 ? c2.environmentDivision() : ((Core.InterfaceDefinitionContext)definition).environmentDivision();
            bool[] kinds = [.. found.Record.FormalIsInterface];
            var source = new SkeletonSource(target, identity, isInterface, definition, found.Record.Source, state, [.. formals.Select(f => f.GetText())],
                kinds, [.. SymbolTable.RepositoryScope.SpecifierEntries(env).Where(r => !SymbolTable.ExpansionNames.IsFormal(formals, DeclaredName(r)!))], found.From);
            var imported = new Skeleton(definition, target, isInterface, formals, [.. kinds.Select(k => (bool?)k)], source);
            names.DeclareImportedSkeleton(targetWord, imported);
            return imported;
        }

        void ReparseFailed(Expansion x)
        {
            using var _ = edition.At(x.Of.Ctx);
            edition.Error(DiagnosticCatalog.ExpandsPhraseInvalid,
                $"the expansion '{x.Name}' of {x.Of.Name} USING {string.Join(" ", x.Actuals)} does not parse once its "
                + "formal parameters are replaced (ISO §2023:12.3.8.4 GR5/GR8) — an actual parameter's spelling is not "
                + "usable in every position its formal occupies");
        }

        Skeleton? EnclosingSkeleton(RuleContext e)
        {
            for (var c = e.Parent; c is not null; c = c.Parent)
                if (c is Core.ClassDefinitionContext or Core.InterfaceDefinitionContext)
                    return names.WrittenSkeletons.FirstOrDefault(s => ReferenceEquals(s.Ctx, c));
            return null;
        }
    }

    private static string Kind(Skeleton s) => s.IsInterface ? "interface" : "class";

    /// <summary>The rules of a parameterized definition's header that <c>AddSkeleton</c> asks, each cited in the edition the program
    /// is compiled under (kb/Work PB4950, PB5753): the formal's REPOSITORY specifier (<see cref="Declared"/>), the repeated
    /// parameter-name (<see cref="Repeated"/>, a rule from COBOL-2014 only, cited through <c>CiteRuleFrom2014</c> and reported
    /// only where <c>ParameterNameAppearsOnceInUsingClause</c> is active), where a formal may stand (<see cref="Placement"/>,
    /// worded with <see cref="ClassNameKind"/>: COBOL-2002 and COBOL-2014 say "class-name", COBOL-2023 "object-class-name") and
    /// no EXPANDS phrase in the definition's own REPOSITORY paragraph (<see cref="NoExpands"/>). COBOL-85 has no object
    /// orientation (refused COBOL0900), so a rule of COBOL-2002 stands for it (<c>CiteRuleFrom2002</c>, kb/Work PB6175).</summary>
    private readonly record struct HeaderRules(string Declared, string Repeated, string Placement, string NoExpands, string ClassNameKind)
    {
        public static HeaderRules Of(EditionInfo e, bool isInterface) => new(
            isInterface ? e.CiteRuleFrom2002("§2002:11.5.2 SR4", "§2023:11.6.3 SR4", y2014: "§2014:11.6.2 SR4")
                        : e.CiteRuleFrom2002("§2002:11.2.2 SR9", "§2023:11.3.3 SR8", y2014: "§2014:11.3.2 SR8"),
            isInterface ? e.CiteRuleFrom2014("§2014:11.6.2 SR7", "§2023:11.6.3 SR7")
                        : e.CiteRuleFrom2014("§2014:11.3.2 SR9", "§2023:11.3.3 SR9"),
            isInterface ? e.CiteRuleFrom2002("§2002:11.5.3 GR4", "§2023:11.6.4 GR4", y2014: "§2014:11.6.3 GR4")
                        : e.CiteRuleFrom2002("§2002:11.2.3 GR6", "§2023:11.3.4 GR6", y2014: "§2014:11.3.3 GR6"),
            e.CiteRuleFrom2002("§2002:12.2.7.2 SR3", "§2023:12.3.8.3 SR3", y2014: "§2014:12.3.7.2 SR3"),
            e.CiteFrom2002("a class-name", "an object-class-name"));
    }

    /// <summary>Whether the terminal <paramref name="t"/> is a user-defined-word occurrence, the one kind of occurrence of a formal
    /// parameter-name an expansion replaces (§2023:12.3.8.4 GR5 / GR8 "each specification of the formal parameter";
    /// <c>SubstitutedTokens</c>) and so the one kind that is a reference to a formal (<c>AddSkeleton</c>, kb/Work PB5753).</summary>
    private static bool IsUserWordOccurrence(ITerminalNode t) => t.Parent is Core.CobolWordContext || t.Symbol.Type == CobolLexer.IDENTIFIER;

    /// <summary>The user-defined-word occurrences (<see cref="IsUserWordOccurrence"/>) in <paramref name="definition"/>, in order,
    /// outside its USING clause's own <paramref name="formals"/>.</summary>
    private static IEnumerable<ITerminalNode> UserWordOccurrences(ParserRuleContext definition, Core.OoParameterNameContext[] formals)
    {
        var stack = new Stack<IParseTree>([definition]);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n is ITerminalNode t)
            {
                if (IsUserWordOccurrence(t)) yield return t;
                continue;
            }
            if (n is Core.OoParameterNameContext f && Array.IndexOf(formals, f) >= 0) continue;
            for (int i = n.ChildCount - 1; i >= 0; i--) stack.Push(n.GetChild(i));
        }
    }

    /// <summary>The REPOSITORY entry whose declared class-name or interface-name is the terminal <paramref name="t"/>, or null.</summary>
    private static Core.RepositoryEntryContext? FormalSpecifierName(ITerminalNode t) =>
        t.Parent?.Parent is Core.ClassNameContext or Core.InterfaceNameContext && t.Parent.Parent.Parent is Core.RepositoryEntryContext entry
            && entry.expandsPhrase() is null ? entry : null;

    /// <summary>The terminal tokens of <paramref name="rule"/>, in order.</summary>
    private static IEnumerable<IToken> Terminals(IParseTree rule)
    {
        if (rule is ITerminalNode t) { yield return t.Symbol; yield break; }
        for (int i = 0; i < rule.ChildCount; i++)
            foreach (var token in Terminals(rule.GetChild(i))) yield return token;
    }

    /// <summary>The name a CLASS / INTERFACE specifier DECLARES (object-class-name-1 / interface-name-2); null for
    /// every other specifier.</summary>
    private static string? DeclaredName(Core.RepositoryEntryContext r) => DeclaredWord(r)?.GetText();

    /// <summary>The word a CLASS / INTERFACE specifier DECLARES, as the parser read it; null for every other specifier.</summary>
    private static ParserRuleContext? DeclaredWord(Core.RepositoryEntryContext r) => SymbolTable.ExpansionNames.DeclaredWord(r);

    /// <summary>Every word the definition writes in a DECLARATION slot — a data-name, condition-name,
    /// paragraph- or section-name, method-name, file-name, report-name, screen-name or property-name. None of
    /// those positions admits an object-class-name or interface-name, so a formal parameter found in one breaks
    /// §2023:11.3.4 GR6 / §2023:11.6.4 GR4.</summary>
    private static IEnumerable<Core.CobolWordContext> DeclarationSlotWords(ParserRuleContext root)
    {
        var stack = new Stack<IParseTree>([root]);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n is Core.CobolWordContext w)
            {
                if (SlotOf(w) is not null) yield return w;
                continue;
            }
            for (int i = n.ChildCount - 1; i >= 0; i--)
                if (n.GetChild(i) is ParserRuleContext c) stack.Push(c);
        }
    }

    private static string SlotKind(Core.CobolWordContext w) => SlotOf(w) ?? "name";

    /// <summary>The declaration slot a word stands in (walking the short name-rule chain above it), or null.</summary>
    private static string? SlotOf(Core.CobolWordContext w)
    {
        for (var p = w.Parent; p is not null && p is not Core.StatementContext; p = p.Parent)
            switch (p)
            {
                case Core.DataNameContext d: return d.Parent is Core.ScreenDescriptionEntryContext ? "screen-name" : "data-name";
                case Core.ConditionNameContext: return "condition-name";
                case Core.ParagraphNameContext: return "paragraph-name";
                case Core.SectionNameContext: return "section-name";
                case Core.MethodNameContext: return "method-name";
                case Core.FileNameContext: return "file-name";
                case Core.ReportNameContext: return "report-name";
                case Core.PropertyNameContext: return "property-name";
                case Core.DataReferenceContext or Core.ClassNameContext or Core.InterfaceNameContext
                    or Core.RepositoryEntryContext:
                    return null;
            }
        return null;
    }


    /// <summary>Every REPOSITORY entry in the group that carries an EXPANDS phrase. REPOSITORY paragraphs live in
    /// ENVIRONMENT DIVISIONs only, so the walk never descends a DATA or PROCEDURE DIVISION (the bulk of a
    /// tree).</summary>
    private static void CollectExpandsEntries(IParseTree node, List<Core.RepositoryEntryContext> into)
    {
        switch (node)
        {
            case Core.RepositoryEntryContext r:
                if (r.expandsPhrase() is not null) into.Add(r);
                return;
            case Core.DataDivisionContext or Core.ProcedureDivisionContext or ITerminalNode:
                return;
        }
        for (int i = 0; i < node.ChildCount; i++) CollectExpandsEntries(node.GetChild(i), into);
    }

    /// <summary>The expansion's token run: the skeleton's own on-channel tokens, copied (a fresh stream renumbers
    /// them, and the originals belong to the live tree), with — (a) the header's and the end marker's name
    /// replaced by the expansion's name; (b) the USING clause dropped, since the expansion is not parameterized
    /// ("a class is created that functions as a non-parameterized class", §2023:9.3.12); (c) the header's
    /// <c>AS literal-1</c> dropped: it externalizes the SKELETON, and §2023:9.3.12 forbids two expansions with
    /// different actuals to share an externalized name, so carrying it into every expansion would make each
    /// program with two expansions non-conforming by construction — the expansion's externalized name is its
    /// specifier's object-class-name-1 (§2023:12.3.8.4 GR5 "a class object-class-name-1 is created"); and (d) every
    /// user-defined-word occurrence of a formal replaced by its actual (§2023:12.3.8.4 GR5/GR8, sound by §2023:11.3.4 GR6 /
    /// §2023:11.6.4 GR4 — see the type summary).</summary>
    private static HashSet<string> ClientWrittenTexts(IEnumerable<IToken> tokens) =>
        [.. tokens.OfType<ExpansionToken>().Where(t => t.ClientWritten).Select(t => t.Text)];

    private static List<IToken> SubstitutedTokens(Expansion x, DiagnosticRegion region)
    {
        var skel = x.Of;
        var substitution = SymbolTable.ExpansionNames.Substitution(skel.Formals, x.Actuals);
        // The identity an actual stands for is its specifier's (`AS literal-1`, ISO §2023:12.3.8.4 GR2), not its word: the formal's own
        // specifier in the definition's REPOSITORY paragraph becomes the actual's, so the expansion names the class the actual
        // names wherever it is made (DESIGN-external-repository §10.5: "substitutes the ACTUALS' identities (not the importer's words)").
        var actualSpecifier = ((Core.RepositoryParagraphContext)x.Entry.Parent).repositoryEntry();

        var drop = new HashSet<IToken>();
        bool published = region.IsRepositoryText;   // lexed again from the text another group recorded: one line, a file this group never read
        int entryLine = x.Entry.Start.Line;
        ParserRuleContext headerWord, endWord;
        if (skel.Ctx is Core.ClassDefinitionContext c)
        {
            var id = c.classIdParagraph();
            headerWord = id.className(0);
            endWord = c.endClassHeader().className();
            DropTokens(id.USING(), id.ooParameterName(), id.externalizedNamePhrase());
        }
        else
        {
            var i = (Core.InterfaceDefinitionContext)skel.Ctx;
            headerWord = i.interfaceName(0);
            endWord = i.interfaceName()[^1];
            DropTokens(i.USING(), i.ooParameterName(), i.externalizedNamePhrase());
        }

        IToken headerName = headerWord.Start, endName = endWord.Start;
        // The end marker is renamed with the header only when it repeats the header's name (§2023:10.7.3 SR4, SR6), the table's one
        // name equality (SymbolTable.EndMarkerAgrees).
        bool endRepeatsHeader = SymbolTable.EndMarkerAgrees(endWord, headerWord);
        var result = new List<IToken>();
        Walk(skel.Ctx);
        return result;

        void DropTokens(ITerminalNode? usingKw, Core.OoParameterNameContext[] formals, ParserRuleContext? asPhrase)
        {
            if (usingKw is not null) drop.Add(usingKw.Symbol);
            foreach (var f in formals) AddRange(f);
            if (asPhrase is not null) AddRange(asPhrase);
        }

        void AddRange(ParserRuleContext r)
        {
            for (int i = 0; i < r.ChildCount; i++)
                switch (r.GetChild(i))
                {
                    case ITerminalNode t: drop.Add(t.Symbol); break;
                    case ParserRuleContext sub: AddRange(sub); break;
                }
        }

        void Walk(IParseTree n)
        {
            if (n is ITerminalNode t)
            {
                var tok = t.Symbol;
                if (tok.Type == TokenConstants.EOF || drop.Contains(tok)) return;
                string? text = null;
                if (ReferenceEquals(tok, headerName)
                    || (ReferenceEquals(tok, endName) && endRepeatsHeader))
                    text = x.Name;
                else if (IsUserWordOccurrence(t) && substitution.ActualFor(tok.Text) is { } actual)
                    text = actual;
                var copy = new ExpansionToken(tok, region) { ClientWritten = text is not null };   // the client wrote the substituted word (kb/Work PB4721)
                if (text is not null)
                {
                    copy.Text = text;
                    // An actual is an ordinary user-defined word wherever its formal stood (§2023:11.3.4 GR6).
                    copy.Type = CobolLexer.IDENTIFIER;
                }
                // The text is another group's, recorded as one line; its positions are places in a text this group never read as a file.
                // The LINE becomes the EXPANDS entry's, which is the line every directive fold of the group reads (the expansion is asked for
                // there); the COLUMN stays the token's own in the recorded text, so two places of the text stay two (kb/Work PB4483). Where
                // a diagnostic about it is printed is the region's (DiagnosticRegion.ReportedAt, the entry), as the expansion's own
                // compilation prints it (ExpansionAssemblies.AtTheEntry).
                if (published) copy.Line = entryLine;
                result.Add(copy);
                if (text is not null && FormalSpecifierName(t) is { } formalSpecifier && formalSpecifier.externalizedNamePhrase() is null
                    && SymbolTable.ExpansionNames.SpecifierDeclaring(actualSpecifier, text)?.externalizedNamePhrase() is { } actualAs)
                    foreach (var asToken in Terminals(actualAs)) result.Add(new ExpansionToken(asToken, region) { ClientWritten = true });
                // (e) the specifier's own `AS literal-1` externalizes the EXPANSION (§2023:12.3.8.4 GR2), right after the name it follows.
                if (ReferenceEquals(tok, headerName) && x.Entry.externalizedNamePhrase() is { } entryAs)
                    foreach (var asToken in Terminals(entryAs)) result.Add(new ExpansionToken(asToken, region) { ClientWritten = true });
                return;
            }
            for (int i = 0; i < n.ChildCount; i++) Walk(n.GetChild(i));
        }
    }
}
