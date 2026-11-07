// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Cst;
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;


namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>The enclosing occurrence a SEARCH's WHEN phrase names: the subscripts of the tables enclosing the
/// searched table, outermost first, and the pre-ops that must run before those positions can be read (see
/// <see cref="CaptureEnclosingSubscripts"/>), and the object-property references among them whose GETs those
/// positions read.</summary>
internal sealed record EnclosingOccurrence(IReadOnlyList<Position> Subscripts, IReadOnlyList<Bound.BoundStatement> PreOps,
    IReadOnlyList<OoPendingPropertyOp> PropertyOps);

/// <summary>Which statement's <b>table</b> operand a subscript list is read for (<see cref="ReadSubscripts"/>) —
/// the operand names a table, not one occurrence of it, so its own level takes no subscript and what is written
/// is the ENCLOSING tables' occurrences. <see cref="Sort"/>: a Format-2 SORT's data-name-2 (§2023:14.9.40.3 SR13).
/// <see cref="Search"/>: SEARCH's identifier-1 (§2023:14.9.37.3 SR2), read only for a dynamic-capacity table nested
/// under other tables, whose scan bound is one instance's current capacity.</summary>
internal enum TableSubject { None, Sort, Search }

/// <summary>ONE written position — a subscript of a subscript list, or a reference modifier's leftmost position or
/// length — as the PARSE gives it (kb/Work PB2151). <paramref name="Tokens"/> are its terminals, for the readers
/// whose syntax rules are about the written text (§2023:14.9.37.3 SR8/SR9's index-name subscript, §2023:13.10.3 SR3's
/// literal subscripts). <paramref name="Operand"/> is the node the position binds from: the subscript item's or
/// the modifier's <c>functionArgument</c>; null for <c>ALL</c> and for a piece of a written item the tree cannot
/// give a subscript reading (<see cref="SegmentsOf"/>), which the binder refuses by name.
/// <paramref name="Cut"/> is the reference inside <paramref name="Operand"/> whose subscript list the
/// declarations gave to the NEXT subscript (kb/Work PB136): it is read without that list.
/// <paramref name="FromCutList"/> marks that next subscript — the list's single item, written in its own
/// parentheses.</summary>
internal sealed record PositionSegment(
    List<IToken> Tokens, Core.FunctionArgumentContext? Operand,
    Core.DataReferenceContext? Cut = null, bool FromCutList = false);

/// <summary>One index-name written as a subscript and the declaration it resolved to — §2023:8.4.2.3.3 SR4's
/// collector entry (<see cref="ScreenIndexNameAssociation"/>).</summary>
internal readonly record struct IndexUse(IToken Token, IndexDeclaration Decl);

/// <summary>
/// ⛔ <b>SUBSCRIPTS: THE WRITTEN POSITIONS OF A TABLE ELEMENT REFERENCE, BOUND TO TYPED POSITIONS</b> (ISO §2023:8.4.2.3,
/// DESIGN-architecture-review §8.3 (4), kb/Work PB2374). "Subscripts are used when reference is made to an individual element
/// within a table of like elements" (§2023:8.4.2.3.1). This type reads a written <c>subscriptPart</c> into one
/// <see cref="Position"/> per subscript (a subscript is an arithmetic expression, an index-name with an optional relative
/// integer, or ALL — §2023:8.4.2.3.2), screens the rules a subscript list is held to (SR2 and SR3's count, SR4's index-name
/// association, SR5's "each table element reference shall be subscripted", SR6's ALL), and binds the leftmost-position and
/// length of a reference modifier through the SAME position binder, because §2023:8.4.3.3.3 SR4 gives both the arithmetic-expression
/// operand form (<see cref="BindPosition"/>, <see cref="SegmentPosition"/>).
/// <para>A position the direct walk cannot read — a function activation, a division — is MATERIALIZED through the statement
/// binder's hook (<see cref="MaterializeSegment"/>, the D18 hook of kb/Work PB17) into the one <c>ExpressionBinder.BindExpr</c>,
/// so no position reaches code generation as C# text (kb/Work PB2151).</para>
/// </summary>
internal sealed class SubscriptBinder(ReferenceResolver refs, DataBinder data)
{
    /// <summary>The D18 hook that MATERIALIZES a subscript / ref-mod position the direct walk cannot read
    /// (fix-queue PB17): given the position's PARSED arithmetic expression (null when the written position is not
    /// one — the hook refuses it by name, COBOL2363), its written text for that message and its line, it binds the
    /// expression through the ONE <c>ExpressionBinder.BindExpr</c>, synthesizes the §2023:15.4 temporary via
    /// <c>CompilerTemps.Create</c>, registers the store as a statement-scoped pending PRE-op on
    /// <see cref="ProcedureBindState.PendingPreOps"/>, and returns the temp — which this resolver then reads as an ordinary
    /// position (<see cref="PositionRead"/>).
    /// <para>⛔ IT IS A HOOK, NOT A COLLABORATOR REFERENCE, because the binder dependency is ONE-WAY:
    /// <c>StatementBinder(DataBinder, ReferenceResolver)</c>. StatementBinder installs it in its constructor (the
    /// <c>ConditionRenderer.Calls</c> property-wire precedent). Null on the DATA-division resolution paths
    /// (<c>DataBinder.Constants</c>/<c>Ptr</c> build a throwaway resolver with no procedure binder), where the
    /// old loud posture stands — a VALUE/ADDRESS OF subscript cannot carry a function activation anyway.</para>
    /// <para>⚠ Binding happens HERE, at resolve time, NOT at the drain. That is load-bearing for nesting: the UDF
    /// precedent's own words are "a nested call registers while its consumer's arguments bind, so it precedes the
    /// consumer in the sequence". Deferring the bind to the drain would append an INNER segment's temp AFTER its
    /// consumer's — <c>W-E(FUNCTION INTEGER(W-F(FUNCTION INTEGER(2))))</c> would store in the wrong order.</para></summary>
    /// <remarks>⛔ THE POSITION RIDES THE HOOK (kb/Work PB170/PB172). It used to be <c>Func&lt;string, int,
    /// DataItem?&gt;</c>, so ONE hook served BOTH <see cref="SegmentPosition.Subscript"/> and
    /// <see cref="SegmentPosition.RefMod"/> and the binder hard-coded the index-name window context for both —
    /// but §2023:13.18.38.3 r7's five contexts list "as a subscript" and do NOT list a reference-modification
    /// position, so an index-name in a ref-mod bound was wrongly admitted. One hook, two positions, one context
    /// was the defect; the position is now part of the hook's question.</remarks>
    internal Func<Core.ArithmeticExpressionContext, SegmentPosition, int, DataItem?>? MaterializeSegment { get; set; }

    /// <summary>⛔ THE WRITTEN SUBSCRIPT LIST OF <paramref name="dref"/> AGAINST <paramref name="item"/>, read ONCE for
    /// both commit entries (<see cref="Resolve"/> and <see cref="ResolveForItem"/> each carried a copy before kb/Work
    /// PB1030): §2023:8.4.2.3.2's empty-parentheses screen, the segment renderer, §2023:8.4.2.3.3 SR4's index-name
    /// association and the SR2/SR3/SR5 arity screen. Null when the list is acceptable (<paramref name="indexExprs"/>
    /// then holds the rendered subscripts); otherwise the non-place answer.</summary>
    /// <param name="subject">Which statement's TABLE operand the reference is (<see cref="TableSubject"/>), or
    /// <see cref="TableSubject.None"/> for an ordinary table-element reference. A Format-2 table SORT's subject
    /// (kb/Work PB1055) sorts the table's OWN level, which takes no subscript, so the reference writes the subscripts
    /// of the ENCLOSING tables only — <see cref="ScreenTableSubjectArity"/> — and §2023:8.4.2.3.3 SR6's rightmost ALL,
    /// "equivalent to omitting the rightmost or only subscript in this context", is admitted and dropped. SEARCH's
    /// identifier-1 is the same shape (§2023:14.9.37.3 SR2: not subscripted at the searched level) but its statement
    /// owns the arity screen (<c>StatementValidation.ResolveSearchTable</c>, where SR3's reading lets the
    /// superordinate subscripts be left to the WHEN phrases) and SR6's ALL is SORT's alone.</param>
    internal RefResolution? ReadSubscripts(Core.DataReferenceContext dref, DataItem item,
        Core.SubscriptPartContext? subCtx, out List<Position> indexExprs, TableSubject subject = TableSubject.None)
    {
        indexExprs = [];
        if (subCtx is null)
            return (subject is TableSubject.Sort ? ScreenTableSubjectArity(dref, item, 0)   // §2023:8.4.2.3.3 SR5 — none written (kb/Work PB681)
                    : subject is TableSubject.None && ScreenSubscriptArity(dref, item, 0))
                ? RefResolution.Refused(DataBinder.WrittenText(dref)) : null;
        if (ScreenEmptyParentheses(dref, subCtx))   // §2023:8.4.2.3.2 / §2023:8.4.3.3.2 (kb/Work PB969)
            return RefResolution.Refused(DataBinder.WrittenText(dref));
        List<IndexUse> ixNames = [];
        int preOpMark = data.Procedure.PendingPreOps.Mark();   // the subscripts' own pre-ops (D18 temp stores, activations) start here
        int propertyMark = data.Procedure.PropertyOps.Mark();   // ... and their object-property references (the other pending list)
        var e = InterpretSubscripts(subCtx, ixNames, subject is TableSubject.Sort);
        if (e is null) return refs.SegmentFailure(DataBinder.WrittenText(dref));   // a segment the materializer refused or deferred
        ScreenIndexNameAssociation(item, ixNames);   // §2023:8.4.2.3.3 SR4 (kb/Work PB459)
        if (subject switch
            {
                TableSubject.Sort => ScreenTableSubjectArity(dref, item, e.Count),
                TableSubject.Search => false,
                _ => ScreenSubscriptArity(dref, item, e.Count),   // §2023:8.4.2.3.3 SR2/SR3 (kb/Work PB877)
            })
            return RefResolution.Refused(DataBinder.WrittenText(dref));
        indexExprs = e;
        if (!refs.Probing) _enclosingCapture?.Offer(item, e, data.Procedure.PendingPreOps.Since(preOpMark), data.Procedure.PropertyOps.Since(propertyMark));
        return null;
    }

    /// <summary>⛔ THE ENCLOSING OCCURRENCE A SEARCH'S WHEN PHRASES NAME (kb/Work PB3365; ISO §2023:14.9.37.4 GR1: "The
    /// subscript that is used to determine the occurrence of each superordinate table to search is specified by the user
    /// in the WHEN phrases"). Runs <paramref name="bindWhile"/> — the binding of one WHEN condition — and returns the
    /// subscripts of the enclosing tables that its FIRST committed reference to <paramref name="table"/> (or to an item
    /// within it) wrote, outermost first, or null when it references none. The positions are taken from the one
    /// committing subscript reading (<see cref="ReadSubscripts"/>), never read a second time: a subscript that
    /// activates a function would activate twice, and a probe (<c>Probing</c>) offers nothing for the same reason.
    /// <para>Only a dynamic-capacity table needs this: its scan bound is one occurrence's current capacity
    /// (§2023:8.5.1.9.1), where a fixed or occurs-depending table has one bound for every occurrence of the enclosing
    /// tables. When a condition references the table at two occurrences, the first reference in source order names it.
    /// Save/restored like <c>Probing</c>: a nested resolution can re-enter.</para>
    /// <para>⛔ A SUBSCRIPT THAT BEARS A FUNCTION CARRIES ITS PRE-OPS WITH IT. Such a subscript is lowered to a D18
    /// temp whose store (and the activation feeding it) the WHEN condition's per-evaluation wrapper runs INSIDE the
    /// condition, but the scan bound is read BEFORE the first condition evaluation — so the positions alone name a
    /// temp nobody has stored yet (a NullReferenceException on the first probe; on a repeated statement the previous
    /// execution's value, silently the wrong occurrence). The occurrence's pre-ops are therefore captured too
    /// (<see cref="EnclosingOccurrence.PreOps"/>) and the SEARCH runs them once ahead of the scan to determine the
    /// occurrence it bounds (§2023:14.9.37.4 GR1); the WHEN condition still evaluates its own copy each time it is tried.
    /// An OBJECT-PROPERTY subscript is the same shape on the OTHER pending list (<c>ProcedureBindState.PropertyOps</c>,
    /// which the per-evaluation wrapper drains alongside the pre-ops), so its pending GET is captured as well
    /// (<see cref="EnclosingOccurrence.PropertyOps"/>; kb/Work PB3365 review R2-1).</para></summary>
    internal EnclosingOccurrence? CaptureEnclosingSubscripts(DataItem table, Action bindWhile)
    {
        var saved = _enclosingCapture;
        var mine = _enclosingCapture = new EnclosingSubscriptCapture(table);
        try { bindWhile(); }
        finally { _enclosingCapture = saved; }
        return mine.Enclosing;
    }

    private EnclosingSubscriptCapture? _enclosingCapture;

    private sealed class EnclosingSubscriptCapture(DataItem table)
    {
        public EnclosingOccurrence? Enclosing { get; private set; }

        /// <summary>A committed subscript reading of <paramref name="item"/>: the first one that is <c>table</c> or lies
        /// within it, fully subscripted (a reference the arity screen refused never reaches here), keeps the leading
        /// positions that belong to the tables ENCLOSING <c>table</c>.</summary>
        public void Offer(DataItem item, IReadOnlyList<Position> indexExprs, ReadOnlySpan<Bound.BoundStatement> preOps,
            ReadOnlySpan<OoPendingPropertyOp> propertyOps)
        {
            if (Enclosing is not null) return;
            for (DataItem? n = item; n is not null; n = n.Parent)
            {
                if (!ReferenceEquals(n, table)) continue;
                Enclosing = new([.. indexExprs.Take(table.SubscriptArity - 1)], [.. preOps], [.. propertyOps]);
                return;
            }
        }
    }

    /// <summary>⛔ §2023:8.4.2.3.3 SR3 / SR5 e) / SR6 FOR THE SUBJECT OF A FORMAT-2 TABLE SORT (kb/Work PB1055) — the
    /// written subscripts against the table's ENCLOSING levels. SR5 e) lets "the subject of a SORT statement that
    /// references a table" omit its subscripting, and SR6 spells the omission "ALL … as the rightmost or only
    /// subscript of a table in the table format of a SORT statement. This is equivalent to omitting the rightmost
    /// or only subscript in this context": what is omitted is the TABLE'S OWN level (the one the statement sorts),
    /// so the reference still writes one subscript for each enclosing table (SR3 — "the number of subscripts shall
    /// equal the number of OCCURS clauses", less the one SR5 e) lets go). Returns <see langword="true"/> when the
    /// reference is rejected. Before this screen the subscripts of the subject were never read at all, so
    /// <c>SORT E(2)</c> over a table inside <c>ROW OCCURS 2</c> bound to a deferral and an unsubscripted
    /// <c>SORT E</c> drew the same one: the legal and the illegal spelling were indistinguishable.</summary>
    private bool ScreenTableSubjectArity(Core.DataReferenceContext dref, DataItem table, int written)
    {
        int enclosing = table.SubscriptArity - 1;
        if (written == enclosing) return false;
        if (refs.Probing) return true;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        if (!refs.Diagnosed.Add(dref)) return true;
        string subject = table.CobolName ?? table.ClrName;
        data.Edition.Error(DiagnosticCatalog.SubscriptCountMismatch,
            $"'{DataBinder.WrittenText(dref)}': the table '{subject}' sits inside {enclosing} enclosing OCCURS "
            + $"clause{(enclosing == 1 ? "" : "s")}, so the SORT subject writes {enclosing} subscript"
            + $"{(enclosing == 1 ? "" : "s")}, outermost first, and none for '{subject}' itself — the level the "
            + $"statement sorts; ALL may stand as the rightmost one; {written} written (ISO §2023:8.4.2.3.3 SR3, SR5 e), "
            + "SR6; §2023:14.9.40.3 SR13).");
        return true;
    }

    /// <summary>The subscripts a table operand (<paramref name="subject"/>: a Format-2 table SORT's data-name-2 or a
    /// SEARCH's identifier-1) writes for its ENCLOSING tables, one rendered index expression per level, outermost
    /// first (empty for a table that lies in no other table) — or the refusal. ISO §2023:14.9.40.3 SR13: "Subscripting
    /// shall be specified in accordance with 8.4.2.3"; the rule that shape takes for a SORT subject is
    /// <see cref="ScreenTableSubjectArity"/>; a SEARCH's count is screened by its own statement rule.</summary>
    internal RefResolution? ReadTableSubjectSubscripts(Core.DataReferenceContext dref, DataItem table,
        TableSubject subject, out List<Position> outerIndexExprs) =>
        ReadSubscripts(dref, table, SubscriptGroupOf(dref), out outerIndexExprs, subject);

    /// <summary>⛔ A DATA REFERENCE FOLLOWED BY EMPTY PARENTHESES — <c>WS-X()</c> or <c>WS-X( )</c> — is neither
    /// form a parenthesis after a data-name can take (kb/Work PB969): §2023:8.4.2.3.2 writes a subscript list as
    /// <c>( subscript … )</c>, at least one subscript, and §2023:8.4.3.3.2 a reference modifier as
    /// <c>( leftmost-position : [ length ] )</c>, a required leftmost position. Returns <see langword="true"/> when
    /// the reference is rejected, so the caller returns null.
    /// <para>The GRAMMAR admits the empty group because the same parenthesis carries a keyword-omitted
    /// function-identifier's argument list, where §2023:8.4.3.2.2 brackets the arguments inside the parentheses and
    /// <c>F()</c> is the zero-argument spelling — only the resolved symbol tells a function from a data item, and
    /// a name that reached THIS resolver resolved to a data item. Unscreened, <c>DISPLAY WS-X( )</c> compiled CLEAN
    /// and aborted at RUN time on NotImplementedCobolFeatureException while the receiving side drew COBOL0899's
    /// "not yet implemented". It is asked of the written GROUP, before the subscript interpreter.</para></summary>
    /// <summary>THE test for an empty written parenthesis group — no subscript list inside it (kb/Work PB969). One
    /// definition for every reader that must refuse <c>X()</c> on a data reference.</summary>
    internal static bool IsEmptyGroup(Core.SubscriptPartContext group) => group.subscriptList() is null;

    /// <summary>The COBOL2309 message body (the quoted reference is the caller's).</summary>
    internal const string EmptyParenthesesMessage = "parentheses with nothing inside them follow a data reference. A "
        + "subscript list writes at least one subscript (ISO §2023:8.4.2.3.2 — \"( subscript … )\") and a reference "
        + "modifier a leftmost position (ISO §2023:8.4.3.3.2); empty parentheses belong only to a function-identifier "
        + "with no arguments (ISO §2023:8.4.3.2.2). Remove the parentheses, or write the subscript.";

    private bool ScreenEmptyParentheses(Core.DataReferenceContext dref, Core.SubscriptPartContext group)
    {
        if (!IsEmptyGroup(group)) return false;
        // R30 purity: a probe never diagnoses (kb/Work PB157); one report per written reference (Diagnosed).
        if (refs.Probing) return true;
        if (!refs.Diagnosed.Add(dref)) return true;
        data.Edition.Error(DiagnosticCatalog.EmptyParenthesesOnDataReference,
            $"'{DataBinder.WrittenText(dref)}': " + EmptyParenthesesMessage);
        return true;
    }

    /// <summary>⛔ §2023:8.4.2.3.3 SR2 AND SR3 — THE WRITTEN SUBSCRIPT LIST AGAINST THE ITEM'S DIMENSIONS, asked ONCE
    /// per source reference and therefore on BOTH sides of every statement (kb/Work PB877). Returns
    /// <see langword="true"/> when the reference is rejected, so the caller returns null.
    /// <para><b>SR2</b> — "If a subscript is specified, the data description entry describing qualified-data-name-1
    /// or the conditional variable associated with qualified-condition-name-1 shall contain an OCCURS clause or
    /// shall be subordinate to a data description entry that contains an OCCURS clause" — is
    /// <see cref="DataItem.IsTableElement"/>. <b>SR3</b> — "the number of subscripts shall equal the number of
    /// OCCURS clauses in the description of the table element being referenced" — is
    /// <see cref="DataItem.SubscriptArity"/>.</para>
    /// <para>⛔ BOTH WERE DECIDABLE HERE AND NEITHER WAS DECIDED. The resolver returned a bare null on an arity
    /// mismatch and what the programmer saw depended on WHICH SIDE OF THE STATEMENT the reference stood on:
    /// <c>MOVE 1 TO PLAIN (1)</c> drew the receiving chokepoint's catch-all, COBOL0899 "a reference shape
    /// WiseOwl COBOL does not yet implement as a receiver" — a promise, about source no edition of the standard will
    /// ever admit (the PB489 shape) — while <c>DISPLAY PLAIN (1)</c> COMPILED CLEAN and aborted at run time on
    /// <c>NotImplementedCobolFeatureException</c>, where §2023:4.2.2 requires a compile-time mechanism.</para>
    /// <para><b>SR5</b> — "Each table element reference shall be subscripted except when such reference appears"
    /// as a SEARCH subject, in a REDEFINES clause, in an OCCURS KEY IS phrase, in a SORT statement's KEY phrase or
    /// table subject, in a screen description entry's FROM/TO/USING phrase, or as a SUM clause addend. ⛔ NONE OF
    /// THOSE SEVEN CONTEXTS REACHES THIS SCREEN, which is why an OMITTED list is screened here too (kb/Work PB681):
    /// the SEARCH subject resolves through <see cref="ResolveTableOperand"/>, the SORT table subject and keys
    /// through the SORT binder's own data-description walk, and REDEFINES / OCCURS KEY / SUM through the data
    /// binder — never through <see cref="Resolve"/> or <see cref="ResolveForItem"/>, the procedure-division callers
    /// of this screen. (The third caller is the data division's constant entry, <c>CONSTANT AS LENGTH OF
    /// data-name-2</c> — ConstantEntryBinder.BindConstantLength, kb/Work PB1016 — which is not an SR5 context either.)
    /// Before PB681 the omitted case was left to <see cref="PlaceForItem"/>, whose null NOBODY reported:
    /// <c>MOVE E TO B</c> over a table element compiled clean and aborted the run unit at the MOVE.</para></summary>
    internal bool ScreenSubscriptArity(Core.DataReferenceContext dref, DataItem item, int written)
    {
        // ⛔ THE SR5 BOUNDARY IS A PROPERTY OF THE ENTRY POINTS, and it is written down where they are: the seven
        // contexts that may omit a table element's subscripts resolve through their own entries (see the summary),
        // so every reference that reaches THIS screen is outside them and SR5 applies with no exception.
        int arity = item.SubscriptArity;
        if (written == arity) return false;
        // R30 purity: a probe never diagnoses (kb/Work PB157); one report per written reference (Diagnosed).
        if (refs.Probing) return true;
        if (!refs.Diagnosed.Add(dref)) return true;
        string text = DataBinder.WrittenText(dref);
        string subject = item.CobolName ?? item.ClrName;
        if (written == 0)
            data.Edition.Error(DiagnosticCatalog.TableElementNotSubscripted,
                $"'{text}': '{subject}' is a table element — its description contains, or is subordinate to, "
                + $"{arity} OCCURS clause{(arity == 1 ? "" : "s")} — and ISO §2023:8.4.2.3.3 SR5 requires \"Each table "
                + "element reference shall be subscripted\" outside seven contexts (a SEARCH subject, a REDEFINES "
                + "clause, an OCCURS KEY IS phrase, a SORT key or table subject, a screen FROM/TO/USING phrase, a "
                + $"SUM addend), none of which this is. Write {arity} subscript{(arity == 1 ? "" : "s")}, "
                + "outermost first.");
        else if (arity == 0)
            data.Edition.Error(DiagnosticCatalog.SubscriptOnNonTableItem,
                $"'{text}': a subscript is written on '{subject}', whose data description entry neither "
                + "contains an OCCURS clause nor is subordinate to one, so no subscript may be written on it "
                + "(ISO §2023:8.4.2.3.3 SR2). For a character span write the reference-modification colon form "
                + $"instead — '{subject} (1:n)' (§2023:8.4.3.3).");
        else
            data.Edition.Error(DiagnosticCatalog.SubscriptCountMismatch,
                $"'{text}': {written} subscript{(written == 1 ? "" : "s")} written, but the description of "
                + $"'{subject}' contains {arity} OCCURS clause{(arity == 1 ? "" : "s")}; ISO §2023:8.4.2.3.3 SR3 "
                + "requires one subscript per OCCURS clause, written in the order of successively less inclusive "
                + "dimensions of the table.");
        return true;
    }

    /// <summary>The FIRST subscript group of <paramref name="dref"/> — the <c>subscriptPart</c> that carries the
    /// reference's subscript list, taken from the base word's own suffix or from a qualification's suffix tail (<c>K OF E (IX)</c> hangs it off the qualification). Shared by
    /// <see cref="ResolveForItem"/>, which renders it into index expressions, and by
    /// <see cref="SubscriptSegments"/>, which keeps it as written.</summary>
    internal static Core.SubscriptPartContext? SubscriptGroupOf(Core.DataReferenceContext dref) =>
        ReferenceResolver.ReadWritten(dref).SubscriptGroup;

    /// <summary>The reference's subscript list AS WRITTEN — one token segment per subscript position, outermost
    /// first, read by the ONE <see cref="SegmentsOf"/> with the same declaration-informed '(' rule
    /// <see cref="InterpretSubscripts"/> uses (kb/Work PB136), or <see langword="null"/> when the reference carries
    /// no subscript group at all.
    /// <para>⛔ The RENDERED form cannot answer the question this exists for. ISO §2023:14.9.37.3 SR8 and SR9 are rules
    /// about the SOURCE TEXT of a SEARCH ALL subscript — "shall be subscripted by the first index-name associated
    /// with identifier-1 … The index-name subscript shall not be followed by a '+' or a '–'" — and the C# index
    /// expression <see cref="InterpretSubscripts"/> produces has already erased both facts: an index-name and an
    /// integer data item of the same value render identically, and <c>IX + 1</c> folds into the arithmetic.</para></summary>
    internal List<PositionSegment>? SubscriptSegments(Core.DataReferenceContext dref) =>
        SubscriptSegmentsOf(ReferenceResolver.ReadWritten(dref).SubscriptGroup);

    /// <summary>True when any subscript of <paramref name="dref"/>, AS WRITTEN, names the index-name
    /// <paramref name="index"/> — the ONE reader for the two SEARCH rules that prohibit it: §2023:14.9.37.3 SR5's second
    /// sentence over the Format-1 VARYING identifier-2 ("shall not be subscripted by the first or only index-name
    /// specified in the INDEXED phrase … for identifier-1", kb/Work PB211) and SR10's over a Format-2 WHEN operand.
    /// Asked of the source text for the reason <see cref="SubscriptSegments"/> gives: the rendered subscript has
    /// already erased which name was written. Index-names are user-defined words, compared case-insensitively
    /// (§2023:8.1.3.2 GR3 a)).</summary>
    /// <para>⛔ BY DECLARATION, NOT SPELLING (kb/Work PB919): with two tables each <c>INDEXED BY IX</c>, a
    /// subscript <c>IX OF OTHER</c> names the other table's index and is legal here, so each written occurrence of
    /// the spelling is resolved — with the OF/IN qualifiers that follow it — and compared by identity.</para>
    internal bool SubscriptNamesIndex(Core.DataReferenceContext dref, IndexDeclaration index)
    {
        if (SubscriptSegments(dref) is not { } segs) return false;
        foreach (var seg in segs.Select(s => s.Tokens))
            for (int i = 0; i < seg.Count; i++)
            {
                var t = seg[i];
                if (!IsNameToken(t) || !CobolNames.Same(t.Text, index.Name)) continue;
                var quals = QualifiersAfter(seg, i, out _);
                if (refs.Names.ResolveIndexName(t.Text, quals, t) is { Outcome: IndexRefOutcome.Resolved } ix
                    && ReferenceEquals(ix.Decl, index)) return true;
            }
        return false;
    }

    /// <summary>The <see cref="SubscriptSegments"/> split over an ALREADY-READ subscript group (the caller has
    /// the <see cref="WrittenReference"/> in hand and need not walk the suffix tail a second time).</summary>
    internal List<PositionSegment>? SubscriptSegmentsOf(Core.SubscriptPartContext? group) =>
        group is null ? null : SegmentsOf(group, CannotBeSubscripted);

    /// <summary>⛔ THE ONE DECLARATION-INFORMED <c>'('</c> PREDICATE (kb/Work PB136, corrected by PB877) — the
    /// segmenter's question "does this name own the <c>'('</c> that follows it, or does that paren open a new
    /// subscript?", answered by §2023:8.4.2.3.3 SR2: a name that may carry a subscript owns its paren; a name that may
    /// not cannot, so the paren can only begin a parenthesized-expression subscript.
    /// <para>⛔ IT IS A NAMED MEMBER, NOT A LAMBDA AT EACH CALL SITE, AND THAT IS THE POINT. Both callers —
    /// <see cref="SubscriptSegments"/> (the §2023:14.9.37.3 SR8/SR9 source-text reader) and
    /// <see cref="InterpretSubscripts"/> (the rendering path) — spelled the lambda out for themselves, and
    /// <see cref="SubscriptSegments"/>'s own doc-comment CLAIMED they used "the same declaration-informed '(' rule".
    /// They did, by coincidence of two identical copies; PB877 corrected one of them and the claim would have
    /// become false in silence. One rule, one place, and the claim is now structural.</para>
    /// <para>Answers true ONLY for a name that RESOLVES to a data item on which no subscript may be written.
    /// An UNRESOLVED name answers false — it may be a function reference (<c>FUNCTION INTEGER (X)</c>), whose
    /// argument list must not be split from it — and so does a table element, which owns its paren.</para>
    /// <para>⛔ ASKED OF THE WHOLE QUALIFIED NAME (kb/Work PB1455's sibling sweep). §2023:8.4.2.3.2 hangs the subscript list
    /// off <c>qualified-data-name-1</c>, so in <c>X (E OF T (1))</c> the paren belongs to <c>E OF T</c>; asking the
    /// LAST word alone asked about <c>T</c> — a group that carries no OCCURS — split <c>(1)</c> off as a second
    /// subscript of X, and refused the legal reference COBOL2270 ("E OF T … shall be subscripted").</para></summary>
    internal bool CannotBeSubscripted(string name, List<string> qualifiers) =>
        (qualifiers.Count > 0 ? refs.Names.ResolveQualified(name, qualifiers) : refs.Names.ResolveUnqualified(name)) is { IsTableElement: false };

    // ── Intrinsic-argument entries (ISO §2023:15.3; consumed by IntrinsicBinder) ─────────────────────────────────
    // The table(ALL) argument resolves its name and renders its non-ALL subscripts itself (it builds an enumeration,
    // not one place) — these thin internal entries expose the SAME private resolution (ResolveUnqualified /
    // ResolveQualified → PlaceForItem → BindPosition) so argument references see identical view/qualification/
    // subscript semantics as every verb operand (singular-pattern rule).

    /// <summary>Bind one written subscript to its typed <see cref="Position"/> (the private
    /// <see cref="BindPosition"/>), or null when it cannot be bound (caller fails loud).
    /// <para>⛔ <paramref name="indexNames"/> IS NOT OPTIONAL (kb/Work PB1472): a caller that renders a subscript
    /// owes §2023:8.4.2.3.3 SR4's association screen (<see cref="ScreenIndexNameAssociation"/>) for the index-names it
    /// wrote, and the collector is how the screen sees them. The only caller outside the resolver, the table(ALL)
    /// intrinsic argument, passed none, so `FUNCTION SUM(E2(IXA, ALL))` with IXA declared on another table compiled
    /// clean while `E2(IXA, 1)` was refused — the second arm of one subscript reading.</para></summary>
    internal Position? BindIndexSegment(PositionSegment segment, List<IndexUse> indexNames) =>
        BindPosition(segment, SegmentPosition.Subscript, indexNames);

    /// <summary>True when one subscript segment is the bare word <c>ALL</c> (§2023:8.4.2.3.3 SR6) — the ONE test, asked by
    /// the reference reader and the table(ALL) intrinsic argument alike.</summary>
    internal static bool IsAllSegment(PositionSegment segment) => segment.Tokens is [{ Type: Core.ALL }];

    /// <param name="indexNames">§2023:8.4.2.3.3 SR4's collector — the index-names used as subscripts, for
    /// <see cref="ScreenIndexNameAssociation"/> at the caller, which knows the table being referenced.</param>
    /// <param name="admitRightmostAll">True only for a Format-2 SORT table subject (§2023:8.4.2.3.3 SR6): a rightmost
    /// <c>ALL</c> is "equivalent to omitting the rightmost or only subscript", so the segment is dropped from the
    /// result rather than bound (<see cref="ReadSubscripts"/>).</param>
    /// <summary>Bind each subscript of the written list to its typed <see cref="Position"/>, outermost first; a
    /// segment that cannot be bound yields null (→ the caller fails loud).</summary>
    private List<Position>? InterpretSubscripts(
        Core.SubscriptPartContext ctx, List<IndexUse>? indexNames = null, bool admitRightmostAll = false)
    {
        var positions = new List<Position>();
        // kb/Work PB136 — declaration-informed '(' splitting, through the ONE predicate (kb/Work PB877): an
        // inline `IsTable` lambda stood here and answered only §2023:8.4.2.3.3 SR2's FIRST half, so a name SUBORDINATE
        // to an OCCURS — legally subscripted, and a legal arithmetic-expression-1 subscript under §2023:8.4.2.3.2 +
        // §2023:8.8.1.1 + §2023:8.4.3.1.2 Format 2 — had its own '(' split off as an EXTRA subscript.
        var segments = SegmentsOf(ctx, CannotBeSubscripted);
        if (admitRightmostAll && segments.Count > 0 && IsAllSegment(segments[^1])) segments.RemoveAt(segments.Count - 1);
        foreach (var seg in segments)
        {
            if (BindPosition(seg, SegmentPosition.Subscript, indexNames) is not { } p) return null;
            positions.Add(p);
        }
        return positions;
    }

    /// <summary>The parse tree's terminals under <paramref name="node"/>, in source order.</summary>
    internal static void CollectLeafTokens(IParseTree node, List<IToken> tokens)
    {
        if (node is ITerminalNode term) { tokens.Add(term.Symbol); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectLeafTokens(node.GetChild(i), tokens);
    }

    internal static List<IToken> LeafTokens(IParseTree node)
    {
        var tokens = new List<IToken>();
        CollectLeafTokens(node, tokens);
        return tokens;
    }

    /// <summary>A name as the subscript reader reads one: a word that can name a data item, an index-name or a
    /// constant (the lexer's own data-name word set, <c>CobolLexer.SubscriptTriggerTokens</c> — IDENTIFIER plus every
    /// context-sensitive word that can be user-defined). Whether it names one HERE is the resolver's question.</summary>
    internal static bool IsNameToken(IToken t) => CobolLexer.SubscriptTriggerTokens.Contains(t.Type);

    /// <summary>The OF/IN qualifiers written after the name at <paramref name="at"/> in <paramref name="tokens"/>
    /// (ISO §2023:8.4.2.2.2), in written order; <paramref name="last"/> is the index of the last token they cover.</summary>
    private static List<string> QualifiersAfter(List<IToken> tokens, int at, out int last)
    {
        var qualifiers = new List<string>();
        last = at;
        while (last + 2 < tokens.Count && tokens[last + 1].Type is Core.OF or Core.IN && IsNameToken(tokens[last + 2]))
        {
            qualifiers.Add(tokens[last + 2].Text);
            last += 2;
        }
        return qualifiers;
    }

    /// <summary>⛔ THE WRITTEN SUBSCRIPT LIST AS SEGMENTS, ONE PER SUBSCRIPT, read off the PARSE (kb/Work PB2113).
    /// The grammar already split the list — each <c>subscriptItem</c> is one operand, separated by §2023:8.3.5's space or
    /// comma/semicolon-plus-space — so a segment is an item. The one decision the grammar cannot make is kept, and it
    /// is asked of the tree:
    /// <para>⛔ kb/Work PB136 — the '(' after a name, at an item's own level, is the name's subscript list when the
    /// name can carry one and the start of a NEW subscript when it cannot (§2023:8.4.2.3.3 SR2): Annex D.3.5.3's
    /// <c>DOG (XCOUNTER (- YCOUNTER))</c> is two subscripts, <c>DOG (BAKER (I) 3)</c> two with BAKER(I) the first. The
    /// parser always reads the first way (the lexer typed the paren REF_LPAREN after the name), so a name that
    /// <paramref name="parenSplitsAfterName"/> says cannot be subscripted gives its list to a new subscript — the cut
    /// the declarations decide, which §2023:8.4.2.3.2 leaves to them. With no predicate (none is known) nothing is cut.</para>
    /// <para>⚠ A cut has a subscript reading only when its list CLOSES the item: the item without the list is the
    /// first subscript and the list's single item the second. A list followed by more of the item
    /// (<c>T (A + X (- Y) + 1)</c>, where the tree joins <c>+ 1</c> to <c>X (- Y)</c>), or a second cut in one item,
    /// has none the tree can give; its pieces carry no operand and the binder refuses them by name (COBOL2363)
    /// rather than re-cut the expression out of its tokens (DESIGN-binder-bound-tree.md §3.9.2).</para></summary>
    internal static List<PositionSegment> SegmentsOf(Core.SubscriptPartContext group,
        Func<string, List<string>, bool>? parenSplitsAfterName)
    {
        var segments = new List<PositionSegment>();
        if (group.subscriptList() is not { } list) return segments;
        foreach (var item in list.subscriptItem())
        {
            var tokens = LeafTokens(item);
            var cuts = new List<(Core.DataReferenceContext Ref, Core.SubscriptPartContext List)>();
            if (parenSplitsAfterName is not null) CollectCuts(item, parenSplitsAfterName, cuts);
            if (cuts.Count == 0) { segments.Add(new PositionSegment(tokens, item.functionArgument())); continue; }
            int at = tokens.IndexOf(cuts[0].List.Start);
            var head = tokens.GetRange(0, at);
            var tail = tokens.GetRange(at, tokens.Count - at);
            var cutList = cuts[0].List;
            bool closes = cuts.Count == 1 && ReferenceEquals(tokens[^1], cutList.Stop);
            if (!closes)
            {
                segments.Add(new PositionSegment(head, null));
                segments.Add(new PositionSegment(tail, null));
                continue;
            }
            segments.Add(new PositionSegment(head, item.functionArgument(), Cut: cuts[0].Ref));
            var only = cutList.subscriptList()?.subscriptItem() is [{ } single] ? single.functionArgument() : null;
            segments.Add(new PositionSegment(tail, only, FromCutList: true));
        }
        return segments;
    }

    /// <summary>The cut points of one subscript item (see <see cref="SegmentsOf"/>): every subscript list hung on a
    /// name that cannot be subscripted, at the item's OWN level — inside a nested paren (another reference's
    /// subscripts or reference modifier, a function's arguments, a parenthesized expression) the split question
    /// belongs to that paren's owner, so the walk does not descend there.</summary>
    private static void CollectCuts(IParseTree node, Func<string, List<string>, bool> cannotBeSubscripted,
        List<(Core.DataReferenceContext Ref, Core.SubscriptPartContext List)> cuts)
    {
        switch (node)
        {
            case Core.DataReferenceContext dref:
                if (dref.cobolWord() is not { } head) return;
                var qualifiers = new List<string>();
                foreach (var suffix in dref.dataReferenceSuffix())
                {
                    Core.SubscriptPartContext? sp = suffix.subscriptPart();
                    if (suffix.qualification() is { } q)
                    {
                        qualifiers.Add(q.cobolWord().GetText());
                        if (q.subscriptPart() is [var qsp, ..]) sp = qsp;
                    }
                    if (sp is null) continue;
                    // §2023:8.4.2.3.2 hangs the list off the whole qualified name (kb/Work PB1455's sweep)
                    if (cannotBeSubscripted(head.GetText(), qualifiers)) cuts.Add((dref, sp));
                    return;   // the first list decides; what follows it is that list's own reading
                }
                return;
            case Core.SubscriptPartContext or Core.RefModPartContext or Core.FunctionCallContext:
                return;
            case Core.PrimaryExpressionContext p when p.LPAREN() is not null:
                return;   // GROUPING-PAREN-ONLY: a parenthesized expression's inside is its own level (FNARG_ / REF_ parens belong to functionCall / subscriptPart, handled above)
        }
        for (int i = 0; i < node.ChildCount; i++) CollectCuts(node.GetChild(i), cannotBeSubscripted, cuts);
    }

    /// <summary>Bind one subscript / ref-mod position to its typed <see cref="Position"/> (kb/Work PB2151), or
    /// <see langword="null"/> if neither the direct walk nor the D18 materialization route can bind it (so the
    /// caller fails loud). The direct walk reads the PARSE NODE through the arithmetic grammar and keeps the shapes
    /// that map to an ordinal term by term — integer literals, the figurative ZERO (the integer 0, kb/Work PB3960), data-name / index-name references, <c>+ - *</c>,
    /// unary minus and written parentheses, i.e. the relative-subscript and simple-index forms. <b>EVERYTHING ELSE
    /// routes through <see cref="MaterializePosition"/></b>, which binds the same node through the ONE expression
    /// binder; the walk is an optimization over that route, never the arbiter of what is legal in the position
    /// (fix-queue PB42).
    /// <para>⚠ THAT INVARIANT WAS A CLAIM, NOT A FACT, until kb/Work PB170. A name the walk CAN read never reached
    /// the binder, so nothing ever applied §2023:8.8.1.1 to it and <c>T(XE)</c> with <c>XE PIC X(4)</c> compiled clean
    /// under STRICT — the walk WAS deciding the position's legality, by omission. It holds now because
    /// <see cref="ScreenPositionOperandClass"/> asks the same classifier the binder would have asked, so the direct
    /// walk and the D18 route reach the same verdict rather than two different ones.</para>
    /// <para>⛔ THE WALK'S SCREENS ARE DEFERRED TO ITS COMMIT POINT (kb/Work PB220). Its exits to D18 are
    /// ORDER-DEPENDENT — a later operand with no direct form (<c>**</c>), an unresolvable name, or a scaled operand in
    /// a compound abandons the walk AFTER an earlier name was already screened, and the D18 route then screens the
    /// same operand again through the expression binder. Measured: <c>MOVE E(XE ** 2) TO R</c> with <c>XE PIC X(4)</c>
    /// emitted COBOL0844 TWICE. So the screens are QUEUED and flushed only when the walk returns a position; every
    /// D18 reroute discards the queue, which makes the deduplication a property of the control flow.</para></summary>
    /// <param name="indexNames">§2023:8.4.2.3.3 SR4's collector — see
    /// <see cref="ResolveSubscriptName(string,List{string},SegmentPosition,ref List{PendingScreen},out bool,ValueTuple{IToken,List{IndexUse}})"/>.</param>
    internal Position? BindPosition(PositionSegment seg, SegmentPosition position, List<IndexUse>? indexNames = null)
    {
        var tokens = seg.Tokens;
        // A cut list's position is written in its OWN parentheses, the reference's: it is never a direct term.
        if (seg.FromCutList || seg.Operand?.arithmeticExpression() is not { } expr)
            return MaterializePosition(seg, position);
        // kb/Work PB136: a QUOTIENT-bearing position routes to D18 UNCONDITIONALLY — the direct form would be C#
        // integer division over long reads, truncating where §2023:8.4.2.3.4 GR1b evaluates the exact result of the whole
        // expression and requires EC-BOUND-SUBSCRIPT on a non-integer (`E((W-A + W-B) / 2)` with the sum 7 silently
        // selected occurrence 3).
        if (tokens.Any(t => t.Type is Core.SLASH)) return MaterializePosition(seg, position);
        // ⛔ A COMPOUND position (one carrying an arithmetic operator) whose operands include a SCALED item cannot be
        // read operand by operand, because §2023:8.4.2.3.4 GR1b tests the integrality of THE RESULT of the whole
        // expression, not of each operand: `W-E(W-P + W-Q)` with W-P = W-Q = 1.5 has the integral result 3.0 and is
        // a legal subscript, while de-scaling each operand first yields 1 + 1 = 2 AND raises the condition twice on
        // source that never violated it. Such a position routes to D18, which evaluates the expression at full
        // precision into the §2023:15.4 temp and applies the integrality rule exactly once — to the result. A SINGLE
        // scaled operand needs no such detour: it IS the result, so the direct read is equivalent and cheaper.
        bool compound = tokens.Any(t => t.Type is Core.PLUS or Core.MINUS or Core.STAR or Core.SLASH or Core.POWER);
        List<PendingScreen>? pending = null;
        bool functionChecked = false;
        if (Additive(expr.additiveExpression()) is not { } bound) return MaterializePosition(seg, position);
        // ⛔ AN EXPRESSION THAT CAN PASS THE HOST'S long IS NOT A DIRECT SHAPE (kb/Work PB2616): §2023:8.4.2.3.4 1) b) takes the
        // subscript from "the evaluation of arithmetic-expression-1", and the typed walk renders + - * as C# long
        // arithmetic, so `A * B - C * D` over PIC 9(21) operands wrapped and selected another element. Decided at
        // compile time from the operands' digit bounds (Position.ExceedsLongArithmetic); the narrow common case
        // keeps the fast path, the rest is bound through the ONE expression binder, like every other shape the walk
        // cannot render.
        if (bound.ExceedsLongArithmetic) return MaterializePosition(seg, position);
        if (pending is not null && !refs.Probing)   // R30 purity: a probe never diagnoses (kb/Work PB157)
            foreach (var ps in pending)
                if (ps.AllZero)
                    FigurativeAssociation.GateAllZeroAsArithmeticOperand(data.Edition.Edition, data.Edition.Sink, ps.Name, where: "");
                else if (ps.Item is { } it) ScreenPositionOperandClass(it, ps.Name, position);
                else IndexNameInPositionError(ps.Name, position);
        return bound;

        Position? Additive(Core.AdditiveExpressionContext a)
        {
            var mults = a.multiplicativeExpression();
            var ops = a.addOp();
            if (Multiplicative(mults[0]) is not { } acc) return null;
            for (int i = 0; i < ops.Length; i++)
            {
                if (Multiplicative(mults[i + 1]) is not { } right) return null;
                acc = new PositionBinary(acc, ops[i].MINUS() is null ? PositionOperator.Add : PositionOperator.Subtract, right);
            }
            return acc;
        }

        Position? Multiplicative(Core.MultiplicativeExpressionContext m)
        {
            var powers = m.powerExpression();
            if (Power(powers[0]) is not { } acc) return null;
            for (int i = 1; i < powers.Length; i++)
            {
                if (Power(powers[i]) is not { } right) return null;   // `/` never reaches here (above)
                acc = new PositionBinary(acc, PositionOperator.Multiply, right);
            }
            return acc;
        }

        // `**` has no direct form (fix-queue PB42): the first operand is read, then the operator abandons the walk.
        Position? Power(Core.PowerExpressionContext p) =>
            Unary(p.unaryExpression(0)) is { } first && p.POWER().Length == 0 ? first : null;

        Position? Unary(Core.UnaryExpressionContext u)
        {
            if (u.addOp() is { } sign)
                return Unary(u.unaryExpression()) is { } operand
                    ? sign.MINUS() is null ? operand : new PositionNegate(operand) : null;
            return Primary(u.primaryExpression());
        }

        Position? Primary(Core.PrimaryExpressionContext p)
        {
            // GROUPING-PAREN-ONLY: primaryExpression's LPAREN is a written grouping parenthesis; a subscript list's
            // REF_ parens and a function's FNARG_ parens belong to dataReference / functionCall, which route to D18.
            if (p.LPAREN() is not null)
                return Additive(p.arithmeticExpression().additiveExpression()) is { } inner ? new PositionGroup(inner) : null;
            if (p.numericLiteral() is { } lit) return IntegerLiteral(lit);
            // ⛔ The figurative ZERO beside a ref-mod colon, an operator or a lone grouping pair (ZeroTokenRewriter) is the
            // INTEGER 0 (§2023:8.8.1.1 "the figurative constant ZERO"; §2023:8.3.3.6.4 GR4 — the zero format is the value 0 in a
            // numeric context), so it is a literal like `0` and meets every literal-only screen as one (kb/Work PB3960):
            // routed through D18 it became a §2023:15.4 temp read, and `T(ZERO:2)` over PIC X(4) escaped the §2023:8.4.3.3.4 5) b)
            // range screen that `T(0:2)` meets. ALL before it is §2023:8.3.3.6.2 Format 1's optional word, whose
            // §2023:8.3.3.6.3 SR1 a) edition edge is queued with the other screens (an abandoned walk must not report twice).
            if (p.ZERO_ARITH() is { } zero)
            {
                if (p.ALL() is not null)
                    (pending ??= []).Add(new PendingScreen(null, "ALL " + WiseOwl.Cobol.Frontend.Parsing.ZeroTokenRewriter.WrittenWord(zero.Symbol), AllZero: true));
                return new PositionConstant(0);
            }
            if (p.dataReference() is not { } dref || !IsNameToken(dref.Start)) return null;
            // A FUNCTION-BEARING position cannot be read term by term (the head word is a function name, not a
            // data-name), so the WHOLE position routes to D18. Asked once, at the first name, over the whole position.
            if (!functionChecked)
            {
                functionChecked = true;
                if (IsFunctionBearing(tokens)) return null;
            }
            // A reference carrying its own subscripts, reference modifier or object qualifier is not a bare name —
            // unless its list is the one the declarations gave to the next subscript (kb/Work PB136).
            var written = ReferenceResolver.ReadWritten(dref);
            if (!ReferenceEquals(dref, seg.Cut) && (written.SubscriptGroup is not null || written.IsReferenceModified))
                return null;
            if (written.PropertyObject is not null) return null;
            // ⛔ AN UNRESOLVABLE NAME ROUTES TO D18 — IT IS NOT A VERDICT (fix-queue PB50): the expression binder
            // reports a genuinely undefined name at the operand itself.
            if (ResolveSubscriptName(dref.cobolWord().GetText(), written.Qualifiers, position, ref pending,
                    out bool scaled, (dref.Start, indexNames)) is not { } read) return null;
            // A scaled operand inside a compound position — evaluate the whole expression instead (above).
            return scaled && compound ? null : read;
        }
    }

    /// <summary>An integer literal (a sign before it is a unary operator of the expression, ISO §2023:8.3.3.3.2) as a
    /// <see cref="PositionConstant"/> — or, past the host's <c>long</c>, a <see cref="PositionWideConstant"/> that keeps
    /// the value (kb/Work PB2938); null for any other numeric literal (a decimal or floating-point literal — the D18
    /// route applies §2023:8.4.2.3.4 GR1 b)'s integrality rule to it).</summary>
    private static Position? IntegerLiteral(Core.NumericLiteralContext lit)
    {
        var signed = lit.signedNumericLiteral();
        var core = signed.numericLiteralCore();
        if (core.ChildCount != 1 || core.Start.Type is not (Core.INTEGERLIT or Core.SIGNED_INTEGERLIT)) return null;
        if (HostPosition(core.Start.Text) is not { } constant) return null;
        return signed.MINUS() is null ? constant
            : constant is PositionWideConstant w ? new PositionWideConstant(-w.Value)
            : new PositionNegate(constant);
    }

    /// <summary>⛔ THE ONE integer-literal reader of a position (kb/Work PB1058 / PB1579) at the position intakes' long
    /// width: a <see cref="PositionConstant"/>, or — for a literal past <c>long</c>, which saturates there and stays out
    /// of every range — a <see cref="PositionWideConstant"/> holding the literal's own value so a diagnostic can name it
    /// (kb/Work PB2938), saturated by sign at <see cref="Int128.MaxValue"/> past 38 digits (the standard's numeric literal
    /// maximum, and the 38-digit position temporary's capacity). Null when <paramref name="text"/> is not an integer.</summary>
    private static Position? HostPosition(string text) =>
        !WiseOwl.Cobol.Validation.IntegerOperandRules.TryHostPosition(text, out long value, out bool saturated) ? null
        : saturated ? new PositionWideConstant(Int128.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out Int128 wide)
            ? wide : text[0] == '-' ? -Int128.MaxValue : Int128.MaxValue)
        : new PositionConstant(value);

    /// <summary>A screen the fast path OWES once it commits to rendering the segment — either the §2023:8.8.1.1 class
    /// screen over a resolved item (<see cref="ScreenPositionOperandClass"/>) or the §2023:13.18.38.3 r7 index-name
    /// screen (<see cref="IndexNameInPositionError"/>, <c>Item</c> null). Queued rather than emitted so an exit
    /// to D18 later in the token loop cannot leave a diagnostic behind for the D18 route to duplicate — see
    /// <see cref="BindPosition"/>. The third kind (<paramref name="AllZero"/>, <c>Name</c> the operand as written) is the
    /// §2023:8.3.3.6.3 SR1 a) edition edge of a position written <c>ALL ZERO</c> (kb/Work PB3960).</summary>
    private readonly record struct PendingScreen(DataItem? Item, string Name, bool AllZero = false);

    /// <summary>⛔ §2023:8.4.2.3.3 SR4 — THE ONE CONSUMER OF THE INDEX→TABLE ASSOCIATION on the reference side:
    /// "Index-name-1 shall correspond to a data description entry in the hierarchy of the table being referenced
    /// that contains an INDEXED BY phrase specifying that index-name."
    /// <para>kb/Work PB459 measured the hole: §2023:14.9.39.4 GR1 builds the association ("Index-names are associated
    /// with a given table by being specified in the INDEXED BY phrase of the OCCURS clause for that table") and
    /// <c>DataItem.IndexNames</c> holds it, but outside SEARCH nothing read it — so
    /// <c>MOVE 77 TO E2(IX1)</c>, with IX1 the index of an unrelated table, compiled clean and wrote through that
    /// other table's occurrence number, with no diagnostic at any stage.</para>
    /// <para>The rule is applied EXACTLY as written: the index-name must appear in SOME entry of the referenced
    /// item's own hierarchy — SR4 says "in the hierarchy of the table being referenced", not "on the OCCURS level
    /// this subscript position selects", so a multi-dimensional reference that names its own table's indexes out
    /// of dimension order is NOT refused here. Reading the extra restriction in would reject source the standard
    /// admits.</para>
    /// <para>A REDEFINES / RENAMES view resolves to its own entry, and an index-name declared on the redefining
    /// entry is found through that entry's own hierarchy — no special case.</para></summary>
    internal void ScreenIndexNameAssociation(DataItem item, List<IndexUse> indexNames)
    {
        if (indexNames.Count == 0 || refs.Probing) return;   // R30 purity: a probe never diagnoses (kb/Work PB157)
        foreach (var (t, decl) in indexNames)
        {
            if (DeclaredInHierarchy(item, decl)) continue;
            data.Edition.Error(DiagnosticCatalog.IndexNameNotInTable,
                $"'{item.CobolName}' subscripted by the index-name '{t.Text}', which is not in the INDEXED BY "
                + "phrase of any OCCURS clause in this item's hierarchy (ISO §2023:8.4.2.3.3 SR4)");
        }
    }

    /// <summary>True when the RESOLVED declaration <paramref name="decl"/> belongs to an entry of
    /// <paramref name="item"/>'s own hierarchy — the item itself or any containing entry (§2023:8.4.2.3.3 SR4's
    /// "hierarchy of the table being referenced"). ⛔ BY DECLARATION IDENTITY, NOT SPELLING (kb/Work PB919): with
    /// two tables each <c>INDEXED BY IX</c>, <c>EB(IX OF EA)</c> names the index of the OTHER table, and a
    /// spelling test would pass it because EB's hierarchy also declares an IX.</summary>
    private static bool DeclaredInHierarchy(DataItem item, IndexDeclaration decl)
    {
        for (DataItem? e = item; e is not null; e = e.Parent)
            if (ReferenceEquals(e, decl.Table)) return true;
        return false;
    }

    /// <summary>True when this segment contains a FUNCTION-IDENTIFIER (ISO §2023:8.4.3.1.2 Format 1) and therefore
    /// belongs to the D18 materialization route rather than the direct position walk: either the explicit
    /// <c>FUNCTION</c> keyword, or the §2023:8.4.3.2.3 SR2 keyword-omitted form — a REPOSITORY-declared intrinsic or a
    /// user-function name, immediately followed by its parenthesis, that is NOT shadowed by a declared data item (a declared item always wins, exactly as in
    /// <c>IntrinsicBinder.KeywordOmittedFunction</c>; the two must not drift apart, which is why both ask the
    /// question the same way).</summary>
    private bool IsFunctionBearing(List<IToken> tokens)
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type == Core.FUNCTION) return Activation(tokens[i]);
            if (!IsNameToken(tokens[i])) continue;
            string w = tokens[i].Text;
            // This arm detects the KEYWORD-OMITTED form `name(args)`, which by definition has no FUNCTION token
            // before the name — so its '(' is never retyped FNARG_LPAREN (the lexer's mark keys on exactly that
            // token): it is the reference paren REF_LPAREN the lexer gives every '(' after a name (kb/Work PB2113).
            // The explicit-keyword form is caught by the FUNCTION test above and never reaches here.
            if (i + 1 >= tokens.Count || tokens[i + 1].Type != Core.REF_LPAREN) continue;
            if (data.Symbols.TryResolve(w, data.ActiveScope, out _)) continue;   // a declared item wins
            // The REPOSITORY half is the ONE membership the declaration screen and KeywordOmittedFunction ask
            // (DataBinder.IsRepositoryIntrinsic — >>COBOL-WORDS and the edition window included; kb/Work PB1083).
            // An IDENTIFIER keeps its WRITTEN word (the lexer retypes only keyword-token words), so the directive is
            // applied to it once, here.
            if (data.State.Configuration.UserFunctionNames.Contains(w)
                || data.IsRepositoryIntrinsic(FunctionWord.OfWrittenWord(w, data.CobolWords)))
                return Activation(tokens[i]);
        }
        return false;

        // The function-identifier's head token IS an activation site (ISO §2023:7.3.14.4 GR4 b reads it back from
        // DataBindState.ActivationSites — the subscript has no FunctionCall parse node of its own).
        bool Activation(IToken head)
        {
            data.State.ActivationSites.Add(head);
            return true;
        }
    }

    /// <summary>The D18 route (fix-queue PB17, widened by PB42): materialize ANY position the direct walk cannot
    /// read into the §2023:15.4 temporary, and read the position as that temp's ordinary position read. The position's
    /// PARSE NODE — its <c>arithmeticExpression</c> — is handed to the <see cref="MaterializeSegment"/> hook, which
    /// binds it through the ONE expression binder (kb/Work PB2151: the node is bound where the grammar parsed it; the
    /// verbatim-text re-parse through an isolated fragment rule is gone).
    /// <para>⛔ IT IS DELIBERATELY NOT GATED ON A TOKEN LIST. The ARITHMETIC-EXPRESSION GRAMMAR decides
    /// admissibility: a position that did not parse as an arithmetic expression — the word ALL outside §2023:8.4.2.3.3
    /// SR6's two contexts, a nonnumeric literal, a piece of an item the declarations cut that has no subscript
    /// reading (<see cref="SegmentsOf"/>) — is refused here by name (COBOL2363, kb/Work PB1030) and never reaches
    /// the hook. That is what lets a function-identifier, <c>**</c>, a decimal literal and every
    /// future arithmetic form share ONE route with no per-token edit.</para>
    /// <para>Null when the hook is absent (a data-division resolver builds a throwaway resolver with no procedure
    /// binder) or the hook refuses or defers the position — in every such case the caller's loud posture stands.</para></summary>
    private Position? MaterializePosition(PositionSegment seg, SegmentPosition position)
    {
        // R30 PURITY (kb/Work PB157): a probe must not BIND the position — the hook registers a §2023:15.4 pre-op and a
        // function-bearing position would activate TWICE (once per probe+commit). The probe's Place is discarded
        // after its Item is read, so the dummy occurrence is never emitted.
        if (refs.Probing) return new PositionConstant(1);
        var tokens = seg.Tokens;
        if (MaterializeSegment is null || tokens.Count == 0) return null;
        // A reference the declarations cut (kb/Work PB136) is read without its list only by the direct walk; inside an
        // expression the walk cannot read, the tree has no reading of the cut, so the position is refused by name.
        if ((seg.Cut is null ? seg.Operand?.arithmeticExpression() : null) is not { } expr)
        {
            // ⛔ A POSITION THAT IS NOT AN ARITHMETIC EXPRESSION IS NOT A SUBSCRIPT (kb/Work PB1030). §2023:8.4.2.3.2 writes a
            // subscript as ALL, arithmetic-expression-1, or index-name-1 [{+|-} integer-1] (the last is read directly
            // and never arrives here), and §2023:8.4.3.3.3 SR4 makes both reference-modifier bounds arithmetic expressions.
            // What did not parse as one is the word ALL outside the two places §2023:8.4.2.3.3 SR6 admits it (an
            // intrinsic-function argument or a SORT table's rightmost subscript, neither of which resolves through
            // here), or not a subscript at all (`E("A")`). It used to be returned unreported, and the caller bound a
            // run-time NotImplemented that aborted the run unit.
            string text = tokens[0].InputStream is { } stream
                ? stream.GetText(new Antlr4.Runtime.Misc.Interval(tokens[0].StartIndex, tokens[^1].StopIndex))
                : string.Join(" ", tokens.Select(t => t.Text));
            data.Edition.Error(DiagnosticCatalog.NotASubscript, IllegalPositionMessage(text.Trim(), position));
            return null;
        }
        return MaterializeSegment(expr, position, tokens[0].Line) is { } temp ? PositionRead(temp, position) : null;
    }

    /// <summary>The COBOL2363 text for a position that is not an arithmetic expression (kb/Work PB1030) — the
    /// word ALL in a subscript names §2023:8.4.2.3.3 SR6's two contexts; anything else names the position's own rule.</summary>
    private static string IllegalPositionMessage(string segment, SegmentPosition position) =>
        position == SegmentPosition.RefMod
            ? $"'{segment}' is written as a reference-modification bound, but a leftmost-position and a length shall "
              + "be arithmetic expressions (ISO §2023:8.4.3.3.3 SR4)."
        : CobolNames.Same(segment, "ALL")
            ? "the subscript ALL is written where it is not permitted: ISO §2023:8.4.2.3.3 SR6 — \"The subscript ALL may "
              + "be used only\" when the subscripted identifier is an intrinsic function argument, or as the rightmost "
              + "or only subscript of a table in the table format of a SORT statement. Write the occurrence you mean."
        : $"'{segment}' is written as a subscript, but a subscript is ALL, an arithmetic expression, or an index-name "
          + "optionally followed by + or - and an integer (ISO §2023:8.4.2.3.2).";

    /// <summary>A subscript data-name → its C# read expression: an INDEXED BY index-name (a <c>long</c> field), or
    /// a (possibly OF/IN-qualified, ISO §2023:8.4.2.3.2) numeric data item read; <see langword="null"/> if unresolvable.
    /// A data-item read is wrapped in <c>CobolTable.Occ(…)</c> — overload resolution converts a STRING-stored
    /// occurrence number (a leaf the post-bind whole-group analysis flags <see cref="DataItem.StoreAsImage"/>, a
    /// decision NOT yet made when this bind-time text is produced) exactly as a native <c>long</c>.</summary>
    /// <param name="scaled">Set when the resolved operand carries a nonzero PICTURE scale, so the caller can send a
    /// COMPOUND segment to the D18 materializer instead (the §2023:8.4.2.3.4 GR1b result-vs-operand distinction).</param>
    /// <param name="nameToken">The name's own token, and the collector it is recorded into when it resolves to an
    /// index-name IN A SUBSCRIPT position: the caller is the only frame that knows WHICH table is being
    /// referenced, so §2023:8.4.2.3.3 SR4 is applied there (<see cref="ScreenIndexNameAssociation"/>). Threaded rather
    /// than kept on the resolver because the D18 materializer can re-enter this resolver for a nested
    /// reference (kb/Work PB459).</param>
    private Position? ResolveSubscriptName(string name, List<string> qualifiers, SegmentPosition position,
        ref List<PendingScreen>? pending, out bool scaled, (IToken Token, List<IndexUse>? Into) nameToken)
    {
        scaled = false;
        // An index-name is an occurrence number by construction (§2023:13.18.38) and a constant-name substitutes an
        // INTEGER literal — neither can be scaled, so both keep the fast path.
        // ⛔ BUT ONLY IN THE POSITION r7 LISTS (kb/Work PB170). §2023:13.18.38.3 r7's five contexts include "as a
        // subscript" and do NOT include a reference-modification position, and this line returned the index
        // field regardless of `position` — so `W(IX:2)` compiled clean. The R16 screen for exactly this rule
        // already existed (ExpressionBinder.ScreenIndexNameOperand); it simply was not applied here.
        // The name may be QUALIFIED (§2023:8.4.2.2.2 Format 3 / §2023:8.4.2.2.3 SR6 — `E(IX OF T)`), and a duplicated one
        // must be (SR1): the ONE resolution decides both (kb/Work PB919).
        if (refs.Names.ResolveIndexName(name, qualifiers, nameToken.Token) is { Outcome: not IndexRefOutcome.NotAnIndexName } ix)
        {
            if (position == SegmentPosition.Subscript)
            {
                // §2023:8.4.2.3.3 SR4 — screened by the caller, which has the table
                if (ix.Decl is { } decl) nameToken.Into?.Add(new IndexUse(nameToken.Token, decl));
                return IndexCellOf(ix);
            }
            (pending ??= []).Add(new PendingScreen(null, name));
            return IndexCellOf(ix);   // keep reading: a null here would re-route to D18 and screen the operand twice
        }
        // An INTEGER constant-name in a subscript position substitutes its integer literal (ISO §2023:13.10.3 SR2 /
        // §2023:13.10.4 GR1/GR3 — a subscript is a literal position, §2023:8.4.2.3.2) — its VALUE is the position, read by THE ONE
        // integer-literal reader (kb/Work PB1579).
        if (qualifiers.Count == 0
            && data.Constants.FindConstant(name) is { Category: PicCategory.Numeric, IntegerText: { } integer })
            return HostPosition(integer);
        // A report VARYING counter in scope (§2023:13.18.64.4 GR4's NOTE: "data-name-1 [may] be used … as a subscript to a source
        // data item") is the compose-local integer the placement declares; it is no data item and has no scale.
        if (qualifiers.Count == 0 && refs.VaryingScope is { } counters && counters.TryGetValue(name, out var counter))
            return new PositionLocal(counter.Local, AsLong: true);
        DataItem? item = qualifiers.Count == 0 ? refs.Names.ResolveUnqualified(name) : refs.Names.ResolveQualified(name, qualifiers);
        if (item is null) return null;
        if (!IntrinsicArgumentRules.IsArithmeticOperandClass(item)) (pending ??= []).Add(new PendingScreen(item, name));
        scaled = item.Pic?.Scale > 0;
        return PositionRead(item, position);
    }

    /// <summary>⛔ §2023:13.18.38.3 r7 AT THE TOKEN RENDERER'S REF-MOD BOUND — <b>ONE RULE, ONE LANE POSTURE, DECIDED
    /// BY THE SLOT KIND</b> (kb/Work PB219). r7 is enforced at four sites and they were not all on the same
    /// disposition; the axis that settles each one is what the slot IS, never which route reached it:
    /// <list type="bullet">
    ///   <item><b>An ARITHMETIC-expression position</b> — <c>ExpressionBinder.IndexNameExpr</c> (R29) and THIS
    ///   site. §2023:8.4.3.3.3 SR4 makes a reference-modification leftmost-position/length an arithmetic expression,
    ///   so the ref-mod bound is R29's family, not R16's. Posture: strict REJECTS with the r7 citation;
    ///   <c>--permissive</c> WARNS and computes the occurrence number (the documented GnuCOBOL coercion). The
    ///   emit floor is identical either way — this method's caller renders <c>field</c> in both lanes.</item>
    ///   <item><b>An IDENTIFIER slot</b> — <c>ExpressionBinder.ScreenIndexNameOperand</c> (R16: DISPLAY, MOVE,
    ///   STRING, the STOP RUN/GOBACK status operand) and <c>InspectBinder</c>. Posture: an unconditional Error in
    ///   BOTH lanes, and the written reason is that THERE IS NO COERCION TO OFFER — the slot needs an identifier,
    ///   and an occurrence number is not one. That is a leniency this compiler declines to invent, not an
    ///   oversight; <c>dialect_two_axes</c> constrains the leniencies you implement, it does not require one.</item>
    /// </list>
    /// Before this, the ref-mod fast path carried the R16 posture while its OWN D18 route carried R29's, so
    /// under <c>--permissive</c> <c>W(IX:2)</c> was a hard error and <c>W(IX / 1:2)</c> — the same rule, the same
    /// position — warned and compiled, keyed on nothing but whether the direct walk could read the
    /// bound.</summary>
    private void IndexNameInPositionError(string name, SegmentPosition position)
    {
        string what = $"the index-name '{name}' is not an identifier (ISO §2023:8.4.3.1.2) and a "
            + "reference-modification leftmost-position/length is not one of §2023:13.18.38.3 r7's five contexts (a "
            + "subscript, PERFORM/SEARCH VARYING, SET, a relation condition) — §2023:8.4.3.3.3 SR4 makes both bounds "
            + "arithmetic expressions";
        if (data.Edition.Permissive)
            data.Edition.Warning(DiagnosticCatalog.IndexNameContext,
                $"{what}; accepted under --permissive, computing the occurrence number");
        else
            data.Edition.Error(DiagnosticCatalog.IndexNameContext,
                $"{what}. SET a data item to the index first (SET data-item TO {name})");
    }

    /// <summary>⛔ THE §2023:8.8.1.1 CLASS SCREEN FOR THE TOKEN RENDERER'S FAST PATH (kb/Work PB170) — the funnel entry
    /// that never reached the funnel.
    /// <para><b>The chain, every link cite.py-checked.</b> §2023:8.4.2.3.2's subscript general format is exactly three
    /// alternatives — <c>ALL | arithmetic-expression-1 | index-name-1 [{+|-} integer-1]</c> — so a bare data-name
    /// subscript is admitted ONLY as arithmetic-expression-1; §2023:8.8.1.1 then admits "an identifier referencing a
    /// NUMERIC data item, a numeric literal, the figurative constant ZERO"; §2023:8.5.2.1 Table 2 puts category
    /// alphanumeric and alphanumeric-edited in class ALPHANUMERIC. §2023:8.4.3.3.3 SR4 ("leftmost-position and length
    /// shall be arithmetic expressions") carries the identical rule to the ref-mod bounds. So <c>T(XE)</c> and
    /// <c>W(XE:2)</c> with <c>XE PIC X(4)</c> are illegal, and <c>T(IDX)</c> with an index DATA item is illegal
    /// twice over (§2023:13.18.60.3 SR10's closed reference list has no subscript entry).</para>
    /// <para><b>Why the screen has to be HERE.</b> <see cref="ResolveSubscriptName"/> renders the position read
    /// straight from the token — <c>ExpressionBinder.OperandRef</c> never runs, because the operand never enters
    /// the expression binder at all. Measured on 02c72682: <c>E(XE)</c> compiled clean and digit-decoded "0002"
    /// to occurrence 2, and so did the COMPOUND <c>E(XE + 1)</c> — the renderer routes to the screened D18 path
    /// only for a slash, a function, an unresolvable name, a SCALED operand inside a compound, or a token with no
    /// case arm, and plain <c>+</c> over an unscaled alphanumeric name is none of those.</para>
    /// <para>⚠ THE VERDICT IS <see cref="IntrinsicArgumentRules"/>'s, deliberately: a category switch written here
    /// would have been the fifth place answering "is this operand class numeric". Routing the segment to D18
    /// instead — which would reuse OperandRef's screen with no new code — was REJECTED: it materializes a §2023:15.4
    /// temp and a PendingPreOp for every permissive alphanumeric subscript, changing permissive-lane emitted text
    /// and adding an integrality check where <c>CobolTable.Occ(string)</c> has none. Screening in place keeps the
    /// emit floor byte-identical.</para>
    /// <para>⚠ AND THE RENDERER'S OWN INVARIANT IS RESTORED, not abandoned: <see cref="BindPosition"/> documents
    /// itself as "an optimization over that route, never the arbiter of what is legal in the position", and this
    /// defect falsified it. Asking the ONE classifier the question the binder would have asked is what makes the
    /// sentence true again — the fast path now reaches the same verdict, not a different one.</para>
    /// <para>⚠ PRECONDITION: the class question is asked in <see cref="ResolveSubscriptName"/>
    /// (<c>IntrinsicArgumentRules.IsArithmeticOperandClass</c>) and only a REJECTED item is queued, so this
    /// method composes the message and nothing else. The <c>Probing</c> purity guard lives at the queue's flush
    /// in <see cref="BindPosition"/> — ONE place, since that is also where the ordering fix lives
    /// (kb/Work PB220).</para></summary>
    private void ScreenPositionOperandClass(DataItem item, string name, SegmentPosition position)
    {
        // ⛔ NO GROUP / POINTER / OBJECT-REFERENCE ARM, AND THAT IS A REACHABILITY FACT, NOT AN OMISSION
        // (kb/Work PB201). This method runs only when the fast path COMMITS to rendering the segment, and
        // <see cref="HasPositionOverload"/> lets it commit only for a carrier the position read declares a
        // parameter for — <c>long</c> or <c>string</c> for a non-numeric operand (numeric ones add the wide tiers). A group's
        // carrier is its per-program <c>record struct</c> and a pointer's is <c>ManagedPointer</c>, so
        // both now route to D18 and are screened by <c>ExpressionBinder.OperandRef</c> instead — the same
        // COBOL0844 over the same §2023:8.8.1.1, minus this method's position phrase.
        // ⚠ THE INTERSECTION IS NARROWER THAN THAT LIST, and it is the second precondition that narrows it: only
        // an item <c>IntrinsicArgumentRules.IsArithmeticOperandClass</c> REJECTED is ever queued, and the
        // numeric-only carriers belong to class NUMERIC items, which it accepts. So what reaches HERE is
        // exactly: an index DATA item (an <c>IndexCell</c> is a <c>long</c>) and the string-carrier categories
        // — alphanumeric, national, boolean, numeric-edited and their edited forms.
        string what = item.Pic is { Usage: Usage.Index }
                ? $"item '{name}', an index data item (class index, ISO §2023:8.5.2.1 Table 2; §2023:13.18.60.3 SR10 admits "
                  + "an index data item only in SEARCH/SET, a relation condition, or an intrinsic argument)"
            // The class SPELLED by the one Table-2 spelling (kb/Work PB1400): `ToString().ToLowerInvariant()` of the
            // enum printed a numeric-edited subscript as "class numericediteddeediting".
            : $"item '{name}' of class "
              + $"{IntrinsicArgumentRules.ClassNameOf(item)} "
              + "(ISO §2023:8.5.2.1 Table 2)";
        string where = position == SegmentPosition.Subscript
            ? "a subscript is arithmetic-expression-1 (ISO §2023:8.4.2.3.2)"
            : "a reference-modification leftmost-position/length is an arithmetic expression (ISO §2023:8.4.3.3.3 SR4)";
        // The SAME dialect gate the expression-binder screen carries (dialect_two_axes — every leniency is
        // dialect-gated): --permissive keeps the CobolTable.Occ(string) digit decode, which is the leniency
        // already implemented by that overload, and the emitted text is unchanged either way.
        if (data.Edition.Permissive)
            data.Edition.Warning(DiagnosticCatalog.ArithmeticOperandNotNumeric, $"{what} is not a numeric operand — {where}, and ISO §2023:8.8.1.1 "
                + "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the "
                + "figurative constant ZERO; accepted under --permissive, decoding its digit characters as an "
                + "unsigned integer");
        else
            data.Edition.Error(DiagnosticCatalog.ArithmeticOperandNotNumeric, $"{what} is not a numeric operand: {where}, and ISO §2023:8.8.1.1 "
                + "admits only an identifier referencing a NUMERIC data item, a numeric literal, or the "
                + "figurative constant ZERO. --permissive accepts it as a digit-decoding extension");
    }

    /// <summary>⛔ THE ONE ORDINAL-POSITION READ (fix-queue PB41): an already-resolved numeric item → the C#
    /// <c>long</c> expression for the POSITION it denotes, in either position kind.
    /// <para>A WiseOwl COBOL numeric item stores UNSCALED — <c>PIC 9V9 VALUE 2.0</c> is the field <c>20L</c> at scale
    /// 1 — so the item's VALUE and its STORAGE are different numbers whenever the PICTURE has a <c>V</c>. Both
    /// position clauses are about the VALUE: §2023:8.4.2.3.4 GR1b makes the subscript "the result of the evaluation of
    /// arithmetic-expression-1", and §2023:8.4.3.3.4 rule 5)c) says the same for a leftmost-position/length. Reading the
    /// storage instead is what made <c>W-E(W-S)</c> with <c>W-S = 2.0</c> index occurrence 20 and return the
    /// out-of-range scratch.</para>
    /// <para>⛔ A NUMERIC item reads through its OWN PROFILE, at every scale and in both positions:
    /// <c>CobolTable.Occ(path, _P_n)</c> / <c>CobolString.RefModPosition(path, _P_n)</c>. The profile carries the
    /// scale (GR1b's integrality, and a trailing-P item's multiplier), the sign, and the byte form §2023:14.6.13.2 rule
    /// 2's check needs when the post-bind whole-group analysis stores the item as its character image — a position
    /// is ITEM IDENTIFICATION, which rule 1 names as checked even inside a class condition (kb/Work PB1117). The
    /// profile-less form this replaced decoded that image through a tolerant digit scan: EC-DATA-INCOMPATIBLE was
    /// unreachable and a signed image's sign was dropped. Only a NON-numeric operand — the two <c>--permissive</c>
    /// carriers COBOL0844 admits — keeps the bare <c>CobolTable.Occ(path)</c> digit decode.</para>
    /// <para>The read is a typed <see cref="PositionItemRead"/> (kb/Work PB2151): the item, its structural path
    /// and the overload form; <c>CodeGen.PositionRenderer</c> renders it through <c>RuntimeApi</c>.</para></summary>
    private PositionItemRead? PositionRead(DataItem item, SegmentPosition position)
    {
        bool numeric = item.Pic is { Category: PicCategory.Numeric };
        // ⛔ THE BET ON OVERLOAD RESOLUTION IS ONLY GOOD FOR THE CARRIERS THAT HAVE AN OVERLOAD (kb/Work PB201).
        if (!HasPositionOverload(item, numeric)) return null;
        // An unsubscripted read: an item inside a table has no path without its subscripts (§2023:8.4.2.3.3 SR3's count).
        if (PlaceBuilder.BuildAccessPath(item, []) is not { } path) return null;
        return new PositionItemRead(path, item,
            !numeric ? PositionReadForm.Digits
            : position == SegmentPosition.Subscript ? PositionReadForm.Occurrence
            : PositionReadForm.RefModPosition);
    }

    /// <summary>An index-name reference as a position: its declaration's cell, or — for a failed reference whose
    /// error already fails the compile — an inert occurrence number, so the bind continues without a cascade.</summary>
    private static Position IndexCellOf(IndexRef ix) =>
        ix.Decl is { } decl ? new PositionIndexCell(decl.Cell) : new PositionConstant(1);

    /// <summary>⛔ THE FAST PATH'S ADMISSION TEST (kb/Work PB201): can the C# text <see cref="PositionRead"/> is
    /// about to emit actually BIND against the operand's generated field? The bind-time renderer names the field
    /// and lets C# overload resolution supply the conversion — the deliberate design that lets ONE text serve a
    /// carrier the post-bind whole-group analysis has not chosen yet — but that bet is good ONLY for the carrier
    /// types the emitted helper declares a parameter for.
    /// <para><b>The overload sets are the whole rule.</b> A NUMERIC operand renders the profile arity —
    /// <c>CobolTable.Occ(x, in NumProfile)</c> / <c>CobolString.RefModPosition(x, in NumProfile)</c> — which takes
    /// <c>long | string | Int128 | ulong | UInt128</c> at every scale (the profile carries the scale); a
    /// NON-numeric <c>--permissive</c> operand renders <c>CobolTable.Occ(x)</c>, which takes <c>long | string</c>.
    /// The remaining carriers <see cref="DataItem.ElementType"/> can produce — <c>double</c>/<c>float</c> (a COMP-1/COMP-2
    /// leaf), <c>ManagedPointer</c>/<c>ProgramPointer</c>, an object reference, and a GROUP's <c>record struct</c>
    /// name — have none, and the emitted text was therefore not C# that compiles.
    /// ⚠ The FLOAT exclusion is a RULE, not a missing overload: a <c>double</c> operand can be
    /// fractional and §2023:8.4.2.3.4 GR1b sets EC-BOUND-SUBSCRIPT when the expression "does not result in an
    /// integer", a test the scale-less overload does not perform — so a float belongs on the D18
    /// route, where the §2023:15.4 temp applies the rule once, to the result.
    /// MEASURED, six shapes, before this fix: <c>TE(FD1)</c> with <c>FD1 USAGE COMP-2</c>,
    /// <c>TE(BIG)</c> with <c>BIG PIC 9(20) COMP</c> and <c>TE(W-U)</c> with
    /// <c>W-U USAGE BINARY-DOUBLE UNSIGNED</c> each failed the BACKEND with
    /// <c>CS1503 cannot convert from 'double'/'System.Int128'/'ulong' to 'long'</c> — in the DEFAULT lane, on source
    /// §2023:8.8.1.1 and §2023:8.4.2.3.4 GR1b make perfectly legal — <c>W(FD1:2)</c> the same at a ref-mod bound, and a
    /// GROUP subscript under <c>--permissive</c> emitted the record struct (<c>CS1503 '_T_0' to 'long'</c>) or, for
    /// a class-tier BASED group, a name with no C# field at all (<c>CS0103</c>).</para>
    /// <para><b>Why a route and not only wider overloads.</b> The three INTEGER carriers above genuinely were
    /// missing overloads and got them (they are also needed by the second emitter, <c>PlaceRenderer.CountRead</c>,
    /// which renders the OCCURS DEPENDING current count at CODEGEN time and has no route to fall back to). But
    /// overloads alone can never close this: a group's carrier can never have a runtime overload, being a
    /// per-program generated type, and a pointer has no numeric value to convert at all. The
    /// route already exists and is the documented posture: <see cref="BindPosition"/> is "an optimization over
    /// that route, never the arbiter of what is legal in the position", so an operand it cannot render is
    /// <see cref="MaterializePosition"/>'s, where <c>ExpressionBinder</c> screens it under §2023:8.8.1.1 and
    /// <c>NumericRenderer.FieldNum</c> — THE ONE numeric read — supplies the carrier-correct decode (the float
    /// sending check, the wide tier, and a group's §2023:8.5.2.1 alphanumeric IMAGE, which is exactly the digit decode
    /// the <c>--permissive</c> message promises and the fast path never performed).</para>
    /// <para>⚠ Returning FALSE is always sound and never a verdict: the segment reroutes to D18, whose loud
    /// posture on failure is the caller's pre-existing one. The pre-promotion reading of <c>ElementType</c> is
    /// likewise safe in the one direction it can be wrong — promotion only ever moves a leaf TO
    /// <c>CharImage</c>/<c>string</c>, which is in the set, so a leaf admitted here stays admitted.</para></summary>
    private static bool HasPositionOverload(DataItem item, bool numeric)
    {
        // A Tier-B class member — an EXTERNAL or BASED item, an ADDRESS-OF-taken record, a REDEFINES view — is a
        // window over the class backing and owns no field of its own name, so the fast path's bare name was
        // `CS0103` (`E (EXT-I)` with `EXT-I EXTERNAL`: found by kb/Work PB1250's golden repair). The D18 route
        // reads it through its place.
        if (item.Class is { Tier: RedefinesTier.StringCanonical }) return false;
        string carrier = item.ElementType;
        foreach (string admitted in numeric ? NumericPositionCarriers : NonNumericPositionCarriers)
            if (admitted == carrier) return true;
        return false;
    }

    /// <summary>The carrier types the NUMERIC position read — <c>CobolTable.Occ(<i>x</i>, in NumProfile)</c> and
    /// <c>CobolString.RefModPosition(<i>x</i>, in NumProfile)</c> — declares a parameter for: the ONE list
    /// <see cref="HasPositionOverload"/> reads for a numeric operand, exposed so
    /// <c>PositionCarrierOverloadDriftTests</c> can compare it to both runtime methods' ACTUAL overloads by
    /// reflection. Adding an overload without widening this list leaves the fast path routing a carrier it could
    /// now render; widening this list without the overload puts the CS1503 back. The test fails on either.
    /// <c>Int128</c> must be here for a second reason: the D18 segment temp is a 19-digit integer position item —
    /// the wide tier — and <see cref="MaterializePosition"/> reads it back through <see cref="PositionRead"/>.</summary>
    internal static readonly string[] NumericPositionCarriers =
        ["long", "string", "Int128", "ulong", "UInt128"];

    /// <summary>The carrier types <c>CobolTable.Occ(<i>x</i>)</c> declares a parameter for — a NON-numeric
    /// position operand, which only <c>--permissive</c> admits (COBOL0844): an index data item's <c>long</c>
    /// and a character item's <c>string</c>. Held to the runtime overloads by the same drift test.</summary>
    internal static readonly string[] NonNumericPositionCarriers = ["long", "string"];
}

/// <summary>Which ORDINAL-POSITION rule a rendered segment is subject to. The two positions share one token
/// renderer and one integrality rule, and differ only in the Table 13 condition a non-integer value sets:
/// §2023:8.4.2.3.4 GR1b names EC-BOUND-SUBSCRIPT for a subscript, §2023:8.4.3.3.4 rule 5)c) names EC-BOUND-REF-MOD for a
/// leftmost-position/length (fix-queue PB41). Carried by parameter rather than a field — the renderer is
/// re-entrant through <see cref="RefModBinder.ReadRefMod(CobolParserCore.RefModPartContext)"/>, and ambient
/// state goes stale across a re-entrant descent (the ExpressionBinder OperandContext discipline).</summary>
internal enum SegmentPosition
{
    /// <summary>A subscript (ISO §2023:8.4.2.3.2 <c>arithmetic-expression-1</c>) — EC-BOUND-SUBSCRIPT.</summary>
    Subscript,

    /// <summary>A reference-modifier leftmost-position or length (§2023:8.4.3.3.3 SR4) — EC-BOUND-REF-MOD.</summary>
    RefMod,
}
