// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Procedure;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

/// <summary>Where a procedure-name reference is written: the METHOD whose body holds it (null outside every method) and the SECTION
/// that contains it (null for a reference in no section). The two scope the §2023:8.4.2.2 resolution: a method's procedure-names are
/// its own (§2023:8.4.6.1 over the method's own source element), and the containing section is what §2023:8.4.2.2.1 rule 6 is
/// measured against. The caller states it because the debug subjects of USE FOR DEBUGGING are resolved after collection, when no
/// ambient cursor stands where the reference was written.</summary>
internal readonly record struct ProcedureSite(OoMethodScope? Method, SectionInfo? Section);

/// <summary>A paragraph a procedure-name identified: its pc, the paragraph's identity in the procedure table.</summary>
internal sealed record ParagraphDefinition(int Pc);

/// <summary>What a procedure-name identified when it identified MORE THAN ONE procedure (kb/Work PB466): the rule the reference
/// broke, the spelling it broke it with (the head, or the qualifier when the qualifier is what is duplicated), every candidate in
/// declaration order (paragraphs by pc, then sections), and, for §2023:8.4.2.2.3 SR7, the section that declares the paragraph twice.
/// It names candidates, not one symbol, so it is no identification: the procedure table describes the candidates for the message
/// (<c>ProcedureTableBuilder</c>) and listing step 7b records no occurrence for it.</summary>
internal sealed record ProcedureCandidates(
    ProcedureAmbiguityRule Rule, string Name, IReadOnlyList<int> Paragraphs, IReadOnlyList<SectionInfo> Sections, SectionInfo? Section);

public sealed partial class SymbolTable
{
    /// <summary>⛔ THE procedure-name declaration map: a name → definition map that REMEMBERS THAT A NAME WAS DECLARED MORE THAN ONCE
    /// (kb/Work PB466). A bare <c>Dictionary.TryAdd</c> keeps the first and discards the rest, so it cannot even represent the
    /// question §2023:8.4.2.2.1 makes the whole of resolution ("uniqueness shall be established through qualification for each
    /// user-defined name explicitly referenced", excused by rule 1, "No other name has the identical spelling", or for a paragraph
    /// by rule 6, "the section containing the reference also contains the named paragraph"). The first declaration lives in
    /// <see cref="_first"/>; a repeated spelling also lands in <see cref="_duplicated"/>, which is empty for every well-formed source
    /// unit, so the ordinary path costs one probe and <see cref="Definitions"/> allocates nothing until a program collides.</summary>
    /// <typeparam name="T">What a declaration denotes: a pc for a paragraph, a <see cref="SectionInfo"/> for a section.</typeparam>
    private sealed class ProcedureNames<T>
    {
        private readonly Dictionary<CobolName, T> _first = [];

        /// <summary>Every definition of a spelling declared more than once, in declaration order, the first included.</summary>
        private readonly Dictionary<CobolName, List<T>> _duplicated = [];

        /// <summary>Records one declaration. A repeat is KEPT: the first stays the one <see cref="TryResolve"/> answers.</summary>
        public void Declare(CobolName name, T definition)
        {
            if (_first.TryAdd(name, definition)) return;
            if (!_duplicated.TryGetValue(name, out var all)) _duplicated[name] = all = [_first[name]];
            all.Add(definition);
        }

        /// <summary>The FIRST declaration of <paramref name="name"/>; says nothing about uniqueness (ask <see cref="IsDuplicated"/>).</summary>
        public bool TryResolve(CobolName name, out T definition) => _first.TryGetValue(name, out definition!);

        /// <summary>Whether this scope declares <paramref name="name"/> more than once (§2023:8.4.2.2.1 rule 1 is then false for it).</summary>
        public bool IsDuplicated(CobolName name) => _duplicated.Count != 0 && _duplicated.ContainsKey(name);

        /// <summary>How many times this scope declares <paramref name="name"/>: rule 1's candidate count is a SUM over the scopes a
        /// reference reaches (paragraphs and sections).</summary>
        public int CountOf(CobolName name) =>
            _duplicated.TryGetValue(name, out var all) ? all.Count : _first.ContainsKey(name) ? 1 : 0;

        /// <summary>Every definition of <paramref name="name"/>, in declaration order.</summary>
        public IReadOnlyList<T> Definitions(CobolName name) =>
            _duplicated.TryGetValue(name, out var all) ? all
            : _first.TryGetValue(name, out var one) ? [one]
            : [];
    }

    /// <summary>One source element's procedure-names: the program's (or function's), or one method's.</summary>
    private sealed class ProcedureScope
    {
        public ProcedureNames<int> Paragraphs { get; } = new();
        public ProcedureNames<SectionInfo> Sections { get; } = new();
    }

    /// <summary>The procedure-names of the program (or function) outside every method.</summary>
    private readonly ProcedureScope _procedures = new();

    /// <summary>Each method's own procedure-names: a method definition is its own source element (§2023:11.7.1, §2023:3.164),
    /// and §2023:8.4.6.1 confines paragraph-names and section-names to the element that declares them, so sibling methods may reuse
    /// a name and a cross-method reference identifies nothing.</summary>
    private readonly Dictionary<OoMethodScope, ProcedureScope> _methodProcedures = new(ReferenceEqualityComparer.Instance);

    /// <summary>Each section's own paragraph-names, for a reference qualified by the section (§2023:8.4.2.2.2 format 4) and for
    /// rule 6's in-section answer.</summary>
    private readonly Dictionary<SectionInfo, ProcedureNames<int>> _sectionParagraphs = new(ReferenceEqualityComparer.Instance);

    private ProcedureScope ProceduresOf(OoMethodScope? method)
    {
        if (method is null) return _procedures;
        if (!_methodProcedures.TryGetValue(method, out var scope)) _methodProcedures[method] = scope = new();
        return scope;
    }

    private ProcedureNames<int>? ParagraphsIn(SectionInfo section) => _sectionParagraphs.GetValueOrDefault(section);

    /// <summary>Declares the paragraph-name <paramref name="name"/> at <paramref name="pc"/>: in its method's scope when it is in a
    /// method body (else the program's), and in its section's own map when it is in one. A repeated spelling is kept (kb/Work PB466).</summary>
    internal void DeclareParagraph(string name, int pc, SectionInfo? section, OoMethodScope? method)
    {
        CobolName key = Key(name);
        ProceduresOf(method).Paragraphs.Declare(key, pc);
        if (section is null) return;
        if (!_sectionParagraphs.TryGetValue(section, out var paragraphs)) _sectionParagraphs[section] = paragraphs = new();
        paragraphs.Declare(key, pc);
    }

    /// <summary>Declares the section-name of <paramref name="section"/>, in its method's scope when it is in a method body.</summary>
    internal void DeclareSection(SectionInfo section, OoMethodScope? method) =>
        ProceduresOf(method).Sections.Declare(Key(section.Name), section);

    /// <summary>The procedure-name <paramref name="written"/> denotes from <paramref name="site"/>: a <see cref="ParagraphDefinition"/>
    /// or a <see cref="SectionInfo"/>, the <see cref="ProcedureCandidates"/> when it denotes several, or null when it denotes none.
    /// ⛔ THE ONE §2023:8.4.2.2 procedure-name resolution, one body for every scope (kb/Work PB390, PB466): an explicit
    /// <c>IN/OF section-name</c> qualifier resolves against that section's own paragraphs (§2023:8.4.2.2.2 format 4), and the
    /// qualifier must itself identify one section, since a section-name takes no qualifier of its own. Unqualified: first a
    /// paragraph of the CONTAINING section (§2023:8.4.2.2.1 rule 6, valid while that section declares it once: §2023:8.4.2.2.3 SR7,
    /// "If explicitly referenced, a paragraph-name shall not be duplicated within a section", tested here at the reference because
    /// an unreferenced duplicate is legal), then the whole source element, where rule 1 decides over paragraphs and sections
    /// together, because §2023:14.4.1 makes a procedure-name "a word used to refer to a paragraph or section".</summary>
    private object? ProcedureNamed(ParserRuleContext written, ProcedureSite site)
    {
        if (ProcedureWords(written) is not var (head, qualifier)) return null;
        var scope = ProceduresOf(site.Method);
        CobolName name = Key(head);
        if (qualifier is not null)
        {
            CobolName q = Key(qualifier);
            if (!scope.Sections.TryResolve(q, out var section)) return null;
            if (scope.Sections.IsDuplicated(q))
                return new ProcedureCandidates(ProcedureAmbiguityRule.Qualification, qualifier, [], scope.Sections.Definitions(q), null);
            if (ParagraphsIn(section) is not { } inSection || !inSection.TryResolve(name, out int qpc)) return null;
            return inSection.IsDuplicated(name)
                ? new ProcedureCandidates(ProcedureAmbiguityRule.InSectionDuplicate, head, inSection.Definitions(name), [], section)
                : new ParagraphDefinition(qpc);
        }

        if (site.Section is { } containing && ParagraphsIn(containing) is { } local && local.TryResolve(name, out int localPc))
            return local.IsDuplicated(name)
                ? new ProcedureCandidates(ProcedureAmbiguityRule.InSectionDuplicate, head, local.Definitions(name), [], containing)
                : new ParagraphDefinition(localPc);

        int candidates = scope.Paragraphs.CountOf(name) + scope.Sections.CountOf(name);
        if (candidates == 0) return null;
        if (candidates > 1)
            return new ProcedureCandidates(ProcedureAmbiguityRule.Qualification, head,
                scope.Paragraphs.Definitions(name), scope.Sections.Definitions(name), null);
        if (scope.Paragraphs.TryResolve(name, out int pc)) return new ParagraphDefinition(pc);
        return scope.Sections.TryResolve(name, out var named) ? named : null;
    }

    /// <summary>The head and the qualifier of a written procedure-name, read from the context's CHILDREN, never its glued text
    /// (<c>PAR-1A OF SEC-1</c> would read as one unmatchable word). A <c>procedureName</c> is <c>word</c> or <c>word OF section</c>; a
    /// USE FOR DEBUGGING operand is spelled as a <c>dataReference</c> because the phrase also admits file-, cd- and data-names, so its
    /// procedure-name shapes are its word alone or its word with one qualification and nothing else (a subscript, a reference
    /// modifier or a property is no procedure-name, kb/Work PB4363); any other context names a procedure when it is one word.</summary>
    private static (string Head, string? Qualifier)? ProcedureWords(ParserRuleContext written) => written switch
    {
        Core.ProcedureNameContext pn => (ProcedureNameShape.Head(pn), ProcedureNameShape.Qualifier(pn)),
        Core.DataReferenceContext operand => operand.cobolWord() is not { } head ? null
            : operand.dataReferenceSuffix() switch
            {
                [] => (head.GetText(), null),
                [{ } only] when only.qualification() is { } q && q.subscriptPart().Length == 0 && q.refModPart().Length == 0
                    => (head.GetText(), q.cobolWord().GetText()),
                _ => null,
            },
        _ => WordOf(written) is { } word ? (word.Word, null) : null,
    };
}
