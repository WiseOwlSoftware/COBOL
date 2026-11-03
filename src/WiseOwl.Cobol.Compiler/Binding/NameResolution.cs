// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

using WiseOwl.Cobol.Compiler.Oo;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>What a written name resolved to as an INDEX-NAME (kb/Work PB919).</summary>
internal enum IndexRefOutcome
{
    /// <summary>The spelling names no index-name visible here — resolve it as a data-name.</summary>
    NotAnIndexName,
    /// <summary>Exactly one declaration survives the qualification — <see cref="IndexRef.Decl"/>.</summary>
    Resolved,
    /// <summary>An index-name reference that identifies no single declaration: already REPORTED (or, in a
    /// probe, deliberately not). The caller must not re-resolve it as a data-name, which would report a
    /// second, false "not defined".</summary>
    Failed,
}

/// <summary>The outcome of <see cref="ResolveIndexName"/>.</summary>
internal readonly record struct IndexRef(IndexRefOutcome Outcome, IndexDeclaration? Decl)
{
    /// <summary>The C# read of the reference: the declaration's cell, or — for a failed reference whose error
    /// already fails the compile — an inert occurrence number, so the bind continues without a cascade.</summary>
    public string Cell => Decl?.Cell ?? "1";
}

/// <summary>
/// ⛔ <b>QUALIFICATION: THE NAME A DATA REFERENCE WRITES, RESOLVED OVER THE SYMBOLS</b> (ISO §8.4.2.2, DESIGN-architecture-review
/// §8.3 (4), kb/Work PB2372). "Qualification is the specification of superordinate names from the hierarchy to which a
/// user-defined name belongs" (§8.4.2.2.1); a reference shall "uniquely identify" its resource, and §8.4.2.2.3 SR1 makes the
/// written qualifiers establish that. This type asks the candidates of a spelling from <see cref="DataBinder"/>'s symbol tables
/// (<c>QualifiedCandidates</c>, <c>Symbols</c>), decides uniqueness, and says why when it cannot: the unqualified and qualified
/// data-name lookups (<see cref="ResolveUnqualified"/>, <see cref="ResolveQualified"/>), the index-name lookup
/// (§8.4.2.2.3 SR6, <see cref="ResolveIndexName"/>), the one COBOL1639 report (<see cref="ReportUnidentified"/>, with the
/// misqualified- and hidden-condition-name wordings) and the §13.7.3 SR4 LINKAGE screens every identifier entry asks.
/// <para>It holds no place and no subscript: <see cref="ReferenceResolver"/>'s identification step calls it first and the place
/// half builds from the <see cref="DataItem"/> it answers. The per-resolution state it shares with the other parts
/// (<c>Probing</c>, <c>Diagnosed</c>) is the funnel's.</para>
/// </summary>
internal sealed class NameResolution(ReferenceResolver refs, DataBinder data)
{
    /// <summary>The COBOL1639 report for a reference no declaration identifies (kb/Work R30): "not defined"
    /// when the bare name exists nowhere; "does not uniquely identify" when it exists but the qualifiers or an
    /// ambiguity defeat resolution (§8.4.2.2 — qualification shall establish uniqueness).</summary>
    internal void ReportUnidentified(Core.DataReferenceContext dref, string name, List<string> qualifiers)
    {
        // ⛔ A NAME DECLARED AS SOMETHING OTHER THAN A DATA ITEM IS NOT UNDEFINED (kb/Work PB1906): the one query of
        // what it IS. "Not defined" would send the user hunting a declaration that is right there.
        var nonData = data.NonDataDeclarationOf(name, qualifiers, data.ActiveScope);
        // kb/Work R32 — a SCREEN SECTION name. Since kb/Work PB260 the section itself is REFUSED (COBOL1560), so the
        // reference is to a REFUSED declaration (COBOL2364). Before kb/Work PB1030 this arm reported NOTHING, and the
        // statement bound a run-time NotImplemented announced as a WiseOwl COBOL gap.
        if (nonData is { Kind: NonDataDeclarationKind.ScreenEntry })
        {
            ReportRefusedDeclaration(dref, name, "a SCREEN SECTION entry — COBOL1560");
            return;
        }
        // A constant-name whose entry was refused (a cycle, a bad operand; reported there): `DISPLAY K` is that fault's
        // consequence, named as such (COBOL2364) rather than as a constant-name in a data-item position. A statement
        // still answers for its refusal with an error of its own (the COBOL2362 ledger), so it is never silent.
        if (nonData is { Kind: NonDataDeclarationKind.ConstantName, DeclarationRefused: true })
        {
            ReportRefusedDeclaration(dref, name, "a constant entry that yielded no value", "a constant-name");
            return;
        }
        if (!refs.Diagnosed.Add(dref)) return;
        string text = DataBinder.WrittenText(dref);
        // ⛔ A DECLARED INDEX-NAME (kb/Work PB1266 — the resolver-level twin of SetBinder's PB388 screen): `ADD 1 TO IX`
        // and `CALL "P" USING IX` put an index-name in an identifier position. §13.18.38.3 SR7 closes the list of
        // contexts an index-name may appear in, and this position is not one, so the reference is refused for the
        // CATEGORY — the same COBOL1637 DISPLAY / MOVE / COMPUTE already draw.
        if (nonData is { Kind: NonDataDeclarationKind.IndexName })
        {
            data.Edition.Error(DiagnosticCatalog.IndexNameContext,
                $"'{text}' is an index-name, which is not an identifier (ISO §8.4.3.1.2): §13.18.38.3 SR7 admits an "
                + "index-name only as a subscript, in PERFORM/SEARCH VARYING, in SET, or in a relation condition, and "
                + $"this position needs an identifier. SET a data item to the index first (SET data-item TO {text}) "
                + "and reference the data item");
            return;
        }
        // ⛔ A DECLARED REPORT SECTION ENTRY IS NOT UNDEFINED EITHER (kb/Work PB1046). A printable item, a group entry or a
        // report group of this source element's reports — or of a container's GLOBAL report — is in no name table the
        // lookup above reads, so `MOVE PV TO W` and `DISPLAY DL` were told "no declaration gives the name" about names
        // the REPORT SECTION declares. The reference is refused for its position (§13.8.6.2.3, §13.15.3 SR7); the
        // admitted ones — a sum counter, resolved before name lookup, and a DETAIL group named by GENERATE — never get here.
        // ⛔ AN AMBIGUOUS ORDINARY REFERENCE IS TOLD ITS AMBIGUITY FIRST (kb/Work PB3767): when the written qualifiers reach
        // SEVERAL data items, §8.4.2.2.3 SR1 ("uniqueness shall be established through a sequence of qualifiers that precludes
        // any ambiguity of reference") is the rule the reference breaks whether or not a report entry spells the same, and the data-name candidates are the ones the programmer
        // can write more qualifiers to single out. The report entry is the verdict only when no ambiguity among data items
        // stands in its way: the candidates (QualifiedCandidates) are the ones that rank with the report entries, because
        // the lookup leaves none that a report entry of a NEARER source element hides (§8.4.6.2.1 3) a), kb/Work PB4196,
        // ReportScope.NearerEntryHides), so the refusal still stands when the data items are ambiguous GLOBAL ones of
        // a farther element (review R1-1) and when there is only one.
        var dataItems = data.QualifiedCandidates(name, qualifiers, data.ActiveScope);
        if (data.ReportSection.ReportEntryReferenceRefused(dataItems, $"'{text}'", name, qualifiers)) return;
        // A report entry of the data items' own source element is a data-name §8.4.2.2.3 SR1 counts with them, so the
        // ambiguity's text counts it too (review R1-2).
        int reportEntries = dataItems.Count > 1 ? data.ReportSection.ReportEntriesInTierOf(dataItems, name, qualifiers) : 0;
        static string ReportEntriesWording(int count) => count == 0
            ? ""
            : $" (counting {(count == 1 ? "a REPORT SECTION entry" : $"{count} REPORT SECTION entries")} of that spelling)";
        string msg;
        // ⛔ A DECLARED CONDITION-NAME (kb/Work PB567): one qualified by a data-name it is NOT subordinate to (`IS-A OF
        // H1`, H1 an unrelated record; `IS-A OF G1 OF S`, real ancestors in reversed order) is worded by the one
        // misqualified-condition text. Every other kind — an alphabet-name (kb/Work R38: GnuCOBOL's INSPECT CONVERTING
        // alphabet is an extension no edition admits), a constant-name (kb/Work PB3287: `ADDRESS OF C`), a type-name or
        // a type declaration's member (kb/Work PB1906, §13.18.58.4 GR1/GR2) — identifies no data item (§8.4.2.1).
        if (nonData is { Kind: NonDataDeclarationKind.ConditionName })
            msg = MisqualifiedConditionText(text, name, qualifiers);
        else if (nonData is { } declared)
            msg = $"'{text}' {declared.DeclaredAs(text, name)}, so this reference identifies no data item (ISO "
                + "§8.4.2.1: \"a statement shall contain a reference that uniquely identifies that resource\").";
        else if (!data.Symbols.TryResolve(name, data.ActiveScope, out var candidates))
            msg = $"'{text}' is not defined — no declaration in this source element gives the name '{name}', so "
                + "the statement's reference identifies no resource (ISO §8.4.2.1: \"a statement shall contain a "
                + "reference that uniquely identifies that resource\"). Check the spelling, or declare the item.";
        else if (qualifiers.Count == 0)
            msg = $"'{text}' does not uniquely identify a data item — {candidates.Count + reportEntries} declarations share the "
                + $"name{ReportEntriesWording(reportEntries)} and no qualification distinguishes them (ISO §8.4.2.2 — "
                + "qualification shall establish uniqueness).";
        else
        {
            int matches = 0;
            foreach (var c in candidates) if (data.QualifierChainMatches(c, qualifiers)) matches++;
            matches += reportEntries;
            msg = matches > 1
                ? $"'{text}' does not uniquely identify a data item — {matches} declarations of '{name}'"
                  + $"{ReportEntriesWording(reportEntries)} match "
                  + "the written qualifiers (ISO §8.4.2.2 — qualification shall establish uniqueness; write "
                  + "further qualifiers to single one out)."
                : $"'{text}' does not uniquely identify a data item — '{name}' is declared, but not under the "
                  + $"given qualifier{(qualifiers.Count > 1 ? "s" : "")} ({string.Join(" OF ", qualifiers)}) "
                  + "(ISO §8.4.2.2 — qualification shall establish uniqueness).";
        }
        data.Edition.Error(DiagnosticCatalog.UndefinedReference, msg);
    }

    /// <summary>⛔ THE ONE WORDING of a reference whose word is a DECLARED level-88 condition-name that the written
    /// qualifiers do not reach (kb/Work PB567) — every site that finds no condition-name under the qualifiers says
    /// this, and never "not defined". ISO §13.16.3 SR23: "Each condition-name is subordinate to the data-name with
    /// which it is associated", and §8.4.2.2.3 SR5: "The qualification of a condition-name may include the
    /// conditional variable with which the condition-name is associated, as well as by any name by which that
    /// conditional variable may be qualified" — in SR4's order of successively more inclusive levels. So a
    /// qualifier the conditional variable is not subordinate to, or ancestors written in reversed order, identify
    /// NO condition-name. Unqualified, the word is a condition-name written where a data item is required.</summary>
    internal static string MisqualifiedConditionText(string text, string name, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count == 0
            ? $"'{text}' is a condition-name (a level-88 entry), which identifies no data item, and this "
              + "reference requires one (ISO §8.4.2.1: \"a statement shall contain a reference that uniquely "
              + "identifies that resource\")."
            : $"'{text}' does not identify a condition-name — '{name}' is declared as a condition-name, but none "
              + $"is subordinate to the given qualifier{(qualifiers.Count > 1 ? "s" : "")} "
              + $"({string.Join(" OF ", qualifiers)}): a condition-name is qualified only by its conditional "
              + "variable and that variable's containing groups, innermost first (ISO §13.16.3 SR23; "
              + "§8.4.2.2.3 SR4, SR5).";

    /// <summary>What a named CAPACITY register is, as the noun phrase <see cref="HiddenConditionText"/> says (the register
    /// is no data item of the name table, so <see cref="NonDataDeclaration.ClassPhrase"/> does not cover it).</summary>
    internal const string CapacityRegisterClassPhrase =
        "the CAPACITY register of an OCCURS DYNAMIC table (a CAPACITY IN phrase, ISO §13.18.38.3 SR30)";

    /// <summary>The wording of a reference in a CONDITION-ONLY position whose word names a level-88 of a containing program
    /// that a nearer declaration of the spelling hides (<see cref="DataBinder.ConditionNameHiddenByNearerDataItem"/>, kb/Work
    /// PB4219 review R1-1; <see cref="DataBinder.ConditionNameHiddenByNearerNonDataName"/>, kb/Work PB4432 review R1-1):
    /// the reference names that item, so it is neither a misqualified condition-name nor an undeclared word, and saying
    /// either states the opposite of the facts. <paramref name="nearer"/> is what the nearer declaration is ("a data
    /// item", <see cref="NonDataDeclaration.ClassPhrase"/>, <see cref="CapacityRegisterClassPhrase"/>).</summary>
    internal static string HiddenConditionText(string text, string name, string nearer) =>
        $"'{text}' names {nearer}, not a condition-name, and this position requires a condition-name: '{name}' is "
        + "declared in a source element nearer than the containing program whose GLOBAL condition-name "
        + $"'{name}' it hides (ISO §8.4.6.2.1 3) a): \"If the name is declared in source element B, the item in source "
        + "element B is the referenced item\").";

    /// <summary>⛔ ISO §13.7.3 SR4 FOR A DATA-NAME REFERENCE (kb/Work PB1249): the procedure division may reference a
    /// linkage item "if, and only if" it is a header operand, under one, a redefinition or renaming of one, or BASED
    /// (<see cref="DataBinder.UnreferenceableLinkageRecord"/> is the decision). The reference still resolves, so the
    /// statement binds without a cascade; the compile fails here, once per written reference. The condition-name and
    /// index-name legs (e) ask <see cref="ReportUnreferenceableLinkage"/> from their own resolutions.</summary>
    internal void ScreenLinkageReference(Core.DataReferenceContext dref, DataItem item)
    {
        if (data.UnreferenceableLinkageRecord(item) is not { } record || !refs.Diagnosed.Add(dref)) return;
        ReportUnreferenceableLinkage(DataBinder.WrittenText(dref), record);
    }

    /// <summary>The ONE wording of COBOL2746, whichever kind of name (data-name, condition-name, index-name) the
    /// written reference is.</summary>
    internal void ReportUnreferenceableLinkage(string written, DataItem record) =>
        data.Edition.Error(DiagnosticCatalog.LinkageItemNotReferenceable,
            $"'{written}' belongs to LINKAGE SECTION record '{record.CobolName}', which the procedure division may not "
            + "reference (ISO §13.7.3 SR4): the record is not an operand of the USING or RETURNING phrase of the "
            + "procedure division header, not a redefinition or renaming of one, and not BASED. Name it in the "
            + "header, or describe it BASED (§13.18.5).");

    /// <summary>COBOL2364 — a reference to a name whose declaration the compiler REFUSED (a Tier-D REDEFINES
    /// view; a SCREEN SECTION name, the section being declined as COBOL1560; a constant-name whose entry yielded no
    /// value, kb/Work PB1906). The compile has already failed at the declaration; this names the consequence at the
    /// statement, instead of "not defined" (the name IS declared) or "not implemented" (it is not a gap in WiseOwl
    /// COBOL). <paramref name="declared"/> is what the declaration declares the name as.</summary>
    internal void ReportRefusedDeclaration(Core.DataReferenceContext dref, string name, string? why,
                                          string declared = "a data item")
    {
        if (refs.Probing || !refs.Diagnosed.Add(dref)) return;   // R30 purity; one report per written reference
        string text = DataBinder.WrittenText(dref);
        data.Edition.Error(DiagnosticCatalog.ReferenceToRefusedDeclaration,
            (text == name ? $"'{text}' names {declared}" : $"'{text}' names '{name}'") + ", whose declaration was refused"
            + (why is null ? "" : $" ({why})") + "; see the error at the declaration. The reference cannot be bound.");
    }

    /// <summary>Resolve a (possibly OF/IN-qualified) data-name to its <see cref="DataItem"/>, or null. Used by
    /// the table(ALL) expansion (§15.3) to read the OCCURS counts before building per-occurrence places.</summary>
    internal DataItem? FindItem(string name, IReadOnlyList<string> qualifiers) =>
        qualifiers.Count == 0 ? ResolveUnqualified(name)
            : ResolveQualified(name, qualifiers as List<string> ?? [.. qualifiers]);

    /// <summary>⛔ THE LOOKUP OF A WRITTEN NAME THAT NAMES A TABLE, REPORTED (kb/Work PB3287): <see cref="FindItem"/>, then
    /// the one §8.4.2.2 report when no item is identified (<see cref="ReportUnidentified"/>) and the §13.7.3 SR4 screen
    /// when one is (<see cref="ScreenLinkageReference"/>) — what <see cref="Identify"/> asks for every identifier, for the
    /// two references that name a TABLE and not a place (the SEARCH / SORT table operand and the table(ALL) intrinsic
    /// argument), which read their own subscripts. Both used to call <see cref="FindItem"/> and stop, so `SEARCH L-TABLE`
    /// and `FUNCTION SUM (L-TABLE (ALL))` over a LINKAGE table the procedure division may not reference compiled clean
    /// while `MOVE L-TABLE (1) TO X` drew COBOL2746. Null having reported.</summary>
    internal DataItem? FindReportedItem(Core.DataReferenceContext dref, string name, List<string> qualifiers)
    {
        if (FindItem(name, qualifiers) is not { } item)
        {
            ReportUnidentified(dref, name, qualifiers);
            return null;
        }
        ScreenLinkageReference(dref, item);
        return item;
    }

    /// <summary>The item an unqualified name resolves to (first match; COBOL requires qualification to
    /// disambiguate) — through the ONE scope-aware <see cref="Model.SymbolTable"/> (P7 Step 10a, the DEVLOG-773
    /// pickup): the §11.7 GR5 method-overlay-first precedence (a method-local name shadows object data; sibling
    /// methods' names are invisible) lives in <c>TryResolve</c>, no longer duplicated here.</summary>
    internal DataItem? ResolveUnqualified(string name)
    {
        // §8.4.6.2.1 3) — a contained program's own declaration hides a container's global one, and a nearer
        // container's hides a farther one (kb/Work PB1047 / PB1243): the unit map holds every tier, and the ONE
        // nearest-tier rule narrows it before ambiguity is asked.
        if (!data.Symbols.TryResolveUnqualified(name, data.ActiveScope, out var list)) return null;
        if (list.Count > 1)
        {
            // §8.4.2.2.1 (kb/Work R33): "Qualification of a user-defined name is required unless … 1) No
            // other name has the identical spelling." TryResolve returns ONE namespace — the §8.4.6.2.1
            // rule-3a method overlay OR the unit map, never a mix — and NearestDeclaring has narrowed the unit map
            // to one source element, so a plural list is genuine same-element ambiguity, not legal scope
            // shadowing. Measured before enforcing: ZERO of 762 corpus+NIST
            // programs hit this (the note's blast-radius sweep). Strict: null → ReportUnidentified's
            // N-declarations arm (dead until now — R30 built it, this makes it reachable).
            // --permissive: the traditional first-declared match, warned (the DA6/R29 disposition shape).
            if (!data.Edition.Permissive) return null;
            if (refs.Probing) return list[0];   // R30 purity: the committing resolution warns (kb/Work PB157)
            data.Edition.Warning(DiagnosticCatalog.UndefinedReference,
                $"'{name}' is declared {list.Count} times and referenced without qualification — "
                + "ISO §8.4.2.2.1 requires qualification when spellings collide; --permissive resolves to "
                + "the first declaration");
        }
        return list[0];
    }

    /// <summary>⛔ THE DECLARATION AN UNQUALIFIED DATA-NAME NAMES, IN THE REFERENCE'S OWN SCOPE (kb/Work PB467) —
    /// for the verbs whose operand is a <i>data-name</i> and whose rule is a predicate over the DECLARATION
    /// rather than over a storage place (SET ADDRESS OF / ALLOCATE's §14.9.39.3 SR18 and §14.9.3.3 SR1 based-item
    /// screen). It is <see cref="ResolveUnqualified"/> plus this resolver's own §8.4.2.1 report, so such a verb
    /// inherits EVERY scoping rule the resolver knows — now and later — instead of restating one.
    /// <para>⛔ THE BYPASS THIS REPLACED ANSWERED A SCOPED QUESTION FROM THE FLAT MAP. <c>PtrResolveBased</c> read
    /// <c>ctx.Data.ByName</c> directly, which has no notion of a reference's scope, and the based-item verdict
    /// went wrong in BOTH directions inside a method: a legal method-local <c>01 MB BASED</c> was refused as
    /// not-BASED (the method's own declaration was invisible), and a method-local NON-based item that legally
    /// shadows an object-level BASED one was ACCEPTED for rebasing (the object declaration was visible when it
    /// must not be). Moving the same declaration between the object's data division and the method's flipped the
    /// verdict — the proof that scope, not the declaration, was deciding. §11.7.4 GR5 is the rule: "If a given
    /// user-defined word is defined in the data division of this method definition and in the data division of
    /// the containing object definition, the use of that word in this method refers to the declaration in this
    /// method. The declaration in the containing object definition is inaccessible to this method" — which
    /// <see cref="Model.SymbolTable.TryResolve"/> already implements, one file away.</para>
    /// <para>Returns null having REPORTED (COBOL1639) when no declaration in scope carries the name or when
    /// several do — §8.4.2.2.1's ambiguity is the ordinary qualification question and gets the ordinary
    /// diagnostic, never the silent first-wins the bypass took. A caller distinguishes "reported" from "resolved
    /// to the wrong kind of item" with <see cref="WasDiagnosed"/>.</para></summary>
    internal DataItem? DeclarationOf(Core.DataReferenceContext dref, string name)
    {
        if (ResolveUnqualified(name) is { } item) return item;
        if (!refs.Probing) ReportUnidentified(dref, name, []);
        return null;
    }

    /// <summary>
    /// Resolve a qualified reference <c>name OF q[0] OF q[1] …</c> (ISO §8.4.2.2) by CANDIDATE-SET matching:
    /// every in-scope declaration of <paramref name="name"/> whose ancestor chain carries each written
    /// qualifier in order (inner → outer, §8.4.2.2.2 Format 1 — qualifiers need not be consecutive levels),
    /// the OUTERMOST qualifier optionally the owning FILE's name (§8.4.2.2 — the FD/SD is the highest
    /// permissible qualifier; SQ207M's <c>WRITE PRINT-REC IN PRINT-FILE</c>). Exactly ONE survivor resolves;
    /// zero or several is a resolution failure the caller reports (kb/Work R30).
    /// <para>⛔ THE UNIQUENESS DEMANDED IS OF THE MATCH, NEVER OF A QUALIFIER NAME IN ISOLATION (kb/Work
    /// R31): §8.4.2.2.1 — "Identical user-defined names may be specified in a source unit; however,
    /// uniqueness shall be established through qualification". The prior walk resolved the outermost
    /// qualifier as a unique unqualified name FIRST, so legal <c>Z IN X</c> — two Xs, exactly one holding a
    /// Z — was rejected (the differential's syn_definition:931 flip, GnuCOBOL's own "Unique reference with
    /// ambiguous qualifiers" case). Level-66 RENAMES aliases are registered names with the owning record as
    /// <see cref="DataItem.Parent"/>, so <c>HARRY OF A-GLOB</c> (NC209A) matches through the same chain.</para>
    /// </summary>
    /// <para>⛔ THE MATCHING ITSELF LIVES ON <see cref="DataBinder.QualifiedCandidates"/> — ONE §8.4.2.2 resolver
    /// for the procedure division AND the file-description / file-control clauses, which used to carry a weaker
    /// private copy of it (kb/Work PB489). What stays here is only this format's decision about the survivors:
    /// exactly one resolves.</para>
    internal DataItem? ResolveQualified(string name, List<string> qualifiers)
    {
        return data.QualifiedCandidates(name, qualifiers, data.ActiveScope).Single;
    }

    /// <summary>Written index-name references already reported, keyed by the written node (a
    /// <see cref="Core.DataReferenceContext"/> or the subscript's name <see cref="IToken"/>): several binders ask
    /// about the same reference (SET classifies its operand, then binds it).</summary>
    private readonly HashSet<object> _indexRefsReported = new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ THE ONE RESOLUTION OF A WRITTEN INDEX-NAME REFERENCE — <c>index-name-1 [OF|IN qualifier] …</c>
    /// (ISO §8.4.2.2.2 Format 3) — for EVERY position that admits one: a subscript (the position walk) and the
    /// identifier positions of SET, PERFORM / SEARCH VARYING and the relation condition (through
    /// <c>ExpressionBinder.IndexFieldOf</c>). kb/Work PB919: both used to look the bare spelling up in a
    /// <c>name → cell</c> map, so a qualified reference was "not defined" and a duplicated one silently read the
    /// cell the two tables shared.
    /// <para>The candidate set is <see cref="Model.SymbolTable.IndexCandidates"/>'s; the count is §8.4.2.2.3 SR1's
    /// ("For each non unique user-defined name that is explicitly referenced, uniqueness shall be established
    /// through a sequence of qualifiers that precludes any ambiguity of reference"), judged by the ONE verdict
    /// <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/> — the data-name's own diagnostic (COBOL1639) and
    /// its <c>--permissive</c> disposition. A spelling that IS an index-name but whose qualifiers name no table
    /// declaring it is the same diagnostic's "not defined" arm, citing SR6.</para></summary>
    internal IndexRef ResolveIndexName(string name, IReadOnlyList<string> qualifiers, object writtenAt)
    {
        if (data.Symbols.IndexCandidates(name, qualifiers, data.ActiveScope) is not { } cands)
            return new IndexRef(IndexRefOutcome.NotAnIndexName, null);
        // ISO/IEC 1989:2002 Annex F.2 item 108: "Index-names may be qualified in the same manner as data and
        // condition-names" — a COBOL-2002 relaxation (qualified-index-name-2002; kb/Work PB4305). Once per written
        // reference, never from a probe (R30).
        if (qualifiers.Count > 0 && !refs.Probing && _qualifiedIndexGated.Add(writtenAt))
            ConstructRegistry.Check(data.Edition.Edition, data.Edition.Sink, Constructs.QualifiedIndexName2002,
                $"the qualified index-name '{DataBinder.WrittenQualified(name, qualifiers)}'");
        if (cands.Single is { } one)
        {
            ScreenLinkageIndex(one, name, qualifiers, writtenAt);
            return new IndexRef(IndexRefOutcome.Resolved, one);
        }
        // R30 purity (kb/Work PB157): a probe never diagnoses, and of the several asks one written reference gets
        // (SET classifies its operand, then binds it) only the first committing one reports. The OUTCOME is the
        // same at every ask — only the report is once (wave 1073 review R1-1: a second ask that answered Failed
        // handed the backend the inert "1" as a SET receiver, CS0131).
        bool report = !refs.Probing && _indexRefsReported.Add(writtenAt);
        string written = DataBinder.WrittenQualified(name, qualifiers);
        if (cands.Count == 0)
        {
            if (report)
                data.Edition.Error(DiagnosticCatalog.UndefinedReference, $"'{written}' is not defined — '{name}' is "
                    + "an index-name, but no table that declares it is named by the written qualifiers (ISO §8.4.2.2.3 "
                    + "SR6: \"The qualification of an index-name may include the name of the table with which the "
                    + "index-name is associated, as well as any name by which that table may be qualified.\")");
            return new IndexRef(IndexRefOutcome.Failed, null);
        }
        if (data.UniqueOrReportAmbiguous(cands, "the index-name reference", written, out _, report) is not { } first)
            return new IndexRef(IndexRefOutcome.Failed, null);
        // --permissive: warned, and resolved to the first declaration, which is then screened like a unique one —
        // ambiguity is a fault of the WRITTEN reference and does not excuse the table it resolves to (§13.7.3 SR4 e):
        // an index of a linkage record outside the USING phrase is refused as it would be for a unique reference).
        ScreenLinkageIndex(first, name, qualifiers, writtenAt);
        return new IndexRef(IndexRefOutcome.Resolved, first);
    }

    /// <summary>The written qualified index-name references already asked the qualified-index-name-2002 gate.</summary>
    private readonly HashSet<object> _qualifiedIndexGated = new(ReferenceEqualityComparer.Instance);

    /// <summary>The written index-name references already reported as §13.7.3 SR4 e) violations.</summary>
    private readonly HashSet<object> _linkageIndexReported = new(ReferenceEqualityComparer.Instance);

    /// <summary>§13.7.3 SR4 e): an index-name of a linkage table is referenceable exactly when the table is — the
    /// decision <see cref="ScreenLinkageReference"/> asks of a data-name, asked once per written reference (a probe
    /// never reports, R30).</summary>
    private void ScreenLinkageIndex(IndexDeclaration decl, string name, IReadOnlyList<string> qualifiers, object writtenAt)
    {
        if (refs.Probing || data.UnreferenceableLinkageRecord(decl.Table) is not { } record
            || !_linkageIndexReported.Add(writtenAt)) return;
        ReportUnreferenceableLinkage(DataBinder.WrittenQualified(name, qualifiers), record);
    }
}
