// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;

namespace WiseOwl.Cobol.Binding.Procedure;

using Core = CobolParserCore;
using WiseOwl.Cobol.Runtime;

/// <summary>
/// The Report Writer half of the statement binder (ISO/IEC 1989:2023 §2023:14.9.21 INITIATE / §2023:14.9.16 GENERATE /
/// §2023:14.9.46 TERMINATE; REPORT_WRITER_DESIGN §5): the three verb binders over the bound
/// <see cref="ReportModel"/>s, the LINE-COUNTER / PAGE-COUNTER reference interception (§2023:8.4.3.15 — the
/// registers are RWCS state, never storage; the <c>BoundLinageCounterRef</c> precedent), and the report-section
/// PRESENT WHEN / VARYING expression binding (§2023:13.18.41/§2023:13.18.64 — parse contexts captured at data bind,
/// bound HERE through the host's ONE condition/expression binders). P7 Step 10f collaborator; the host is
/// consumed only by <see cref="BindReportGroupClauses"/> (the standard (ctx, host) collaborator shape). The
/// receiving-side counter guard (<c>ResolveReceiving</c>) did NOT ride along — it is the shared receiving
/// spine (5 host pipelines consume it) and hoisted to the core, final home <c>ExpressionBinder</c> at 10q.
/// </summary>
internal sealed class ReportWriterBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind every report-section PRESENT WHEN condition (ISO §2023:13.18.41 Format 1) and VARYING FROM/BY
    /// expression (§2023:13.18.64) captured on this unit's report models — once per unit bind, through the ONE
    /// <c>ConditionBinder</c> / expression binder. Each DISTINCT condition context binds exactly once (an
    /// entry's condition appears in every subordinate chain — the memo keeps diagnostics single-shot).
    /// <para>⛔ EVERY CLAUSE OPERAND IS A PER-EVALUATION WINDOW (kb/Work PB4517). These clauses bind BEFORE the first
    /// statement and outside every statement, yet a function-identifier, a function-bearing subscript or an inline
    /// invocation in them registers an operand-evaluation step (a user-function activation, a §2023:15.4 subscript temporary
    /// store) on the statement-scoped pending list. Undrained, the FIRST procedure statement hoisted those steps, so the
    /// function ran once, at that statement, and every presentation read its stale result: <c>SOURCE WS-T (FUNCTION MOD
    /// (WS-N, 2) + 1)</c> and <c>SOURCE FUNCTION user-fn (WS-N)</c> printed one value for every line, and a FUNCTION in
    /// PRESENT WHEN was refused outright (COBOL0899) to keep it from doing the same. The clauses are evaluated when a
    /// GENERATE or TERMINATE presents the group — condition-1 "before the processing of any LINE clauses for the report
    /// group" (§2023:13.18.41.4 GR2), the SOURCE COMPUTE "whenever the associated item is printed" (§2023:13.18.53.4 GR2) — and a
    /// function's value "is determined when the function is referenced at runtime" (§2023:8.4.3.2.4 GR1), so each operand
    /// drains its OWN steps (<see cref="PerEvaluation(Func{BoundCondition})"/> and its two twins) and the report emitter
    /// runs them at each evaluation.</para></summary>
    public void BindReportGroupClauses()
    {
        var memo = new Dictionary<Core.ConditionContext, BoundCondition>(ReferenceEqualityComparer.Instance);
        var entry = host.Udf.Mark;
        BoundCondition Bind(Core.ConditionContext c)
        {
            // A condition §2023:13.15.3 SR16 refused at data bind was reported there, under its rule; binding it would word
            // the same report-section reference again under §2023:13.8.6.2.3 (kb/Work PB4226).
            if (!memo.TryGetValue(c, out var b))
                memo[c] = b = ctx.Data.State.Reports.PresentWhenRefused(c)
                    ? host.Cond.Refused("a PRESENT WHEN condition refused by ISO §2023:13.15.3 SR16")
                    : PerEvaluation(() => host.Cond.BindCondition(c));
            return b;
        }
        try
        {
            var reports = ctx.Data.State.Reports.Declared;
            foreach (var r in reports)
            {
                // The operand-evaluation steps a report's clauses drain are counted per report, outside every statement,
                // so the GENERATE and TERMINATE statements that present its groups know they carry such steps
                // (OperandStepsOf below; the BoundActivationSite they then take is StatementBinder's).
                using var evaluations = ctx.Data.Procedure.OperandEvaluations.Enter(0);
                BindReportClauses(r, Bind);
                r.EvaluatesOperandSteps = ctx.Data.Procedure.OperandEvaluations.Current > 0;
            }
            BindRolledValues(reports);   // after EVERY report's operands are bound: SR4 g) lets a total roll up another report's entry
            // ⛔ NOTHING MAY BE LEFT FOR THE FIRST STATEMENT TO HOIST: a clause operand bound outside a window would leave
            // its steps on the statement-scoped lists, and that was the PB4517 wrong answer. Loud at compile time.
            if (host.Udf.Mark != entry)
                throw new InvalidOperationException("report clause binding left operand-evaluation steps or property operations "
                    + "outside a per-evaluation window (kb/Work PB4517)");
        }
        finally
        {
            ctx.Refs.VaryingScope = null;
            ctx.Refs.EnclosingReport = null;
        }
    }

    /// <summary>One report's clause expressions, bound with the scope the REPORT SECTION gives them: the RD in whose
    /// description they are written (§2023:8.4.2.2.3 SR9/SR10 — "In the report section, an unqualified reference to the
    /// PAGE-COUNTER is qualified implicitly by the name of the report in whose report description entry the reference
    /// is made"; §2023:13.18.53.3 SR4 — a sum counter "defined in the current report") and the VARYING counters of the
    /// entry being bound (§2023:13.18.64.3 SR2 — "only within the current entry or a subordinate entry").</summary>
    private void BindReportClauses(ReportModel r, Func<Core.ConditionContext, BoundCondition> Bind)
    {
        ctx.Refs.EnclosingReport = r;
        ctx.Refs.VaryingScope = null;
        // The RD CODE clause's identifier-1 (§2023:13.18.12.2): the place the engine reads at each body group (GR3), through
        // the same ONE sending-operand resolution a SOURCE identifier takes. Asked only of a base item the data phase
        // resolved (ReportSectionBinder.ResolveReportCode): an unresolved or ambiguous one was reported there, once (kb/Work PB4226).
        if (r.Code is { Reference: { } codeRef, Item: not null } code)
        {
            using var at = ctx.Edition.At(codeRef);
            var (operand, steps) = WithSteps(() => host.Expr.FieldOperand(codeRef));
            if (operand is BoundFieldOperand { Place: { } codePlace }) (code.Value, code.Steps) = (codePlace, steps);
        }
        foreach (var g in r.Groups)
            foreach (var ln in g.Lines)
            {
                foreach (var c in ln.PresentWhenCtxs) ln.PresentWhen.Add(Bind(c));
                foreach (var f in ln.Fields)
                {
                    foreach (var c in f.PresentWhenCtxs) f.PresentWhen.Add(Bind(c));
                    // §2023:13.18.64.2 writes FROM/BY as plain arithmetic-expression-1/-2, and the RW VARYING is on NEITHER
                    // index clause's list (§2023:13.18.38.3 r7 names only PERFORM's and SEARCH's VARYING; §2023:13.18.60.3 SR10
                    // names no VARYING) — so both screens apply (kb/Work PB215). A counter's expressions may name the
                    // counters of the entries above it, and its BY the counters of its own entry (§2023:13.18.64.3 SR3),
                    // so each binds with exactly those in scope — once, whichever field reaches the counter first.
                    foreach (var use in f.Varyings)
                    {
                        var v = use.Counter;
                        if (v.Bound) continue;
                        v.Bound = true;
                        using var at = ctx.Edition.At(v.Entry);
                        // (A FROM naming a counter of its own clause is §2023:13.18.64.3 SR3's prohibition, reported ONCE per
                        // written entry by ScreenReportVaryingClauses; it binds in the same scope so the refusal is
                        // not followed by a second, misleading "not defined".)
                        ctx.Refs.VaryingScope = ScopeOf(v.Enclosing.Concat(v.Group));
                        if (v.FromCtx is { } fc) v.From = PerEvaluation(() => host.Expr.BindExpr(fc));
                        if (v.ByCtx is { } bc) v.By = PerEvaluation(() => host.Expr.BindExpr(bc));
                    }
                    // The field's operands see every counter in scope at the field (SR2).
                    ctx.Refs.VaryingScope = ScopeOf(f.Varyings.Select(u => u.Counter));
                    BindSourceOperands(r, f.Sources, f.PrintItem);
                    ctx.Refs.VaryingScope = null;
                }
            }
        // The SOURCE operand of an entry with NO printable item has no field to be bound by, and a rolled total still adds
        // it (§2023:13.18.54.4 GR6; §2023:13.18.53.4 GR3 — "the SOURCE clause causes no action, except where the entry is referred
        // to by means of a SUM clause"), so every recorded occurrence binds its own (kb/Work PB1294). Each entry's
        // PRESENT WHEN chain binds here too: GR11 asks whether data-name-1 is "declared to be absent". (A SUM entry's
        // occurrence shares its counter's chain, bound below.)
        foreach (var o in r.ItemOccurrences)
        {
            if (o.Sum is not null) continue;
            foreach (var c in o.PresentWhenCtxs) o.PresentWhen.Add(Bind(c));
            ctx.Refs.VaryingScope = ScopeOf(o.Varyings.Select(u => u.Counter));
            if (o.Source is not null) BindSourceOperands(r, [o.Source]);
            ctx.Refs.VaryingScope = null;
        }
        foreach (var s in r.Sums)
        {
            foreach (var c in s.PresentWhenCtxs) s.PresentWhen.Add(Bind(c));
            // SUM addends (§2023:13.18.54.3 SR5 — kb/Work PB482). An addend written as identifier-1 is an
            // ORDINARY IDENTIFIER (§2023:8.4.3.1.2 Format 2, qualified-data-name-with-subscripts), so its VALUE
            // is bound HERE, through the same `BindExpr` a procedure-division reference takes — which is
            // what makes `SUM WS-CELL(2)`, `SUM WS-CELL(IX)` and `SUM WS-CELL(IX + 1)` resolve at all: a
            // subscript may be an arithmetic expression or an index-name, and neither has a value at data
            // bind. A REJECTED addend is skipped: its rule has already been named, and re-binding it would
            // report the same words twice under a second clause.
            foreach (var t in s.Terms)
                foreach (var a in t.Addends)
                    if (!a.Rejected && !a.Rolled)   // a rolled total's value is its entry's (RolledValues), not an expression of its own
                    {
                        using var at = ctx.Edition.At(a.Ctx);
                        a.Value = PerEvaluation(() => host.Expr.BindExpr(a.Ctx));
                    }
            // The SUM clause's own ROUNDED phrase (§2023:13.18.54.2's trailing rounded-phrase) — §2023:13.18.54.4 GR4
            // computes the counter's delivery to the printable item "according to the general rules for the
            // COMPUTE statement with the ROUNDED phrase". Same §2023:14.7.4 reader as SOURCE's (kb/Work PB852).
            s.Rounding = host.Expr.RoundingOf(s.Rounded);
        }
    }

    /// <summary>Bind the SOURCE operands of one report entry, each ONCE (an operand an earlier pass bound is left alone —
    /// a printable entry's field binds them first, a rolled total's unprintable entry here; kb/Work PB1294).
    /// <para>A SOURCE identifier-1 is §2023:13.18.53.4 GR1's implicit MOVE sender: bound through the ONE operand binder a
    /// MOVE's sending operand takes, which is what knows a subscript, a reference modification, a counter of this or
    /// another report, a sum counter of the current report, a VARYING counter and a constant-name (kb/Work PB1292 ×
    /// PB1306 × PB1316 × PB1456).</para>
    /// <para>A SOURCE operand written as arithmetic-expression-1, or as identifier-1 under the clause's ROUNDED phrase
    /// (§2023:13.18.53.3 SR5) — §2023:13.18.53.4 GR2's implicit COMPUTE — binds through the same <c>BindExpr</c> a
    /// procedure-division reference takes (the kb/Work PB482 argument: a subscript may be an index-name or an
    /// expression and has no value at data bind), and the ROUNDED phrase resolves through the ONE §2023:14.7.4
    /// rounding-mode reader (kb/Work PB852).</para>
    /// <para><paramref name="printable"/> is the entry's printable item — the implicit MOVE's receiving operand
    /// (§2023:13.18.53.4 GR1) — or null for an entry that defines none (no COLUMN clause: §2023:13.18.53.4 GR3, "the SOURCE
    /// clause causes no action", so SR2's "the printable item as the receiving operand" has no receiver).</para></summary>
    private void BindSourceOperands(ReportModel r, IEnumerable<ReportFieldSource> sources, DataItem? printable = null)
    {
        foreach (var source in sources)
            switch (source)
            {
                case FieldReferenceSource { Rejected: false, Value: null } rs:
                {
                    using var at = ctx.Edition.At(rs.Ref);
                    var (operand, steps) = WithSteps(() => SourceIdentifierOperand(rs.Ref));
                    rs.Steps = steps;
                    // identifier-1 is a data item, so an index-name is no sender (§2023:13.18.53.3 SR2 — a MOVE shall be
                    // valid, and §2023:13.18.38.3 r7 admits an index-name in no MOVE).
                    if (host.Expr.ScreenIndexNameOperand(operand, rs.Written, $"RD '{r.Name}': SOURCE")
                        || ScreenSourceIdentifier(r, rs, operand, printable)) rs.Rejected = true;
                    else rs.Value = operand;
                    break;
                }
                case FieldComputeSource { Rejected: false, Value: null } cs:
                {
                    using var at = ctx.Edition.At(cs.Ctx);
                    (cs.Value, cs.Steps) = WithSteps(() => host.Expr.BindExpr(cs.Ctx));
                    cs.Rounding = host.Expr.RoundingOf(cs.Rounded);
                    break;
                }
            }
    }

    /// <summary>A SOURCE identifier-1 as the implicit MOVE's sending operand (§2023:13.18.53.4 GR1): a data reference through
    /// the ONE operand binder a MOVE's sender takes, a function-identifier as a sending item of its function's category
    /// (§2023:15.2) and an inline method invocation, object-view or SELF / SUPER as the temporary it references
    /// (§2023:8.4.3.1.2) — the forms <c>ReportSectionBinder.SourceIdentifierOf</c> classifies as identifier-1 (kb/Work PB5240).</summary>
    private BoundOperand SourceIdentifierOperand(Antlr4.Runtime.ParserRuleContext identifier) => identifier switch
    {
        Core.DataReferenceContext dref => host.Expr.FieldOperand(dref),
        Core.FunctionCallContext fc => host.Intrinsic.IntrinsicOperand(fc),
        _ => host.Oo.OoIdentifierOperand(identifier),
    };

    /// <summary>Bind one report-clause CONDITION as its own per-evaluation window (see <see cref="BindReportGroupClauses"/>):
    /// the steps it registers ride the condition as a <see cref="BoundUdfEvaluated"/>, so they run each time the group's
    /// presence probe evaluates it, and a condition that registered none is returned unchanged.</summary>
    private BoundCondition PerEvaluation(Func<BoundCondition> bind)
    {
        var mark = host.Udf.Mark;
        return host.Udf.UdfAttachPerEvaluation(bind(), mark);
    }

    /// <summary>The EXPRESSION twin of <see cref="PerEvaluation(Func{BoundCondition})"/> — a SUM addend and a VARYING
    /// FROM / BY: the steps ride the expression as a <see cref="BoundUdfEvaluatedExpr"/>, which the report emitter
    /// runs at the addition or the counter setting that reads the value.</summary>
    private BoundExpr PerEvaluation(Func<BoundExpr> bind)
    {
        var mark = host.Udf.Mark;
        return host.Udf.UdfAttachPerEvaluation(bind(), mark);
    }

    /// <summary>The window of a SOURCE operand (either form) and a CODE identifier, whose steps are kept BESIDE the
    /// bound value: one printable item renders its SOURCE several times (the plain, the checked and the raising
    /// transfer), but the steps run once per placement, and a CODE place has no expression to carry them.</summary>
    private (T Value, IReadOnlyList<BoundStatement>? Steps) WithSteps<T>(Func<T> bind)
    {
        var mark = host.Udf.Mark;
        var value = bind();
        return (value, host.Udf.DrainPerEvaluation(mark));
    }

    /// <summary>⛔ THE SOURCE CLAUSE'S identifier-1 RULES THAT NEED ITS BOUND ITEM (kb/Work PB1293), reported under
    /// COBOL2770 and returned as "refused" so the operand is neither bound nor reported twice.
    /// <list type="bullet">
    /// <item>ISO §2023:13.18.53.3 SR8 — "Identifier-1 shall not reference a variable-length group", asked of every
    /// identifier-1, printable entry or not.</item>
    /// <item>ISO §2023:13.18.53.3 SR2 — "If identifier-1 is specified without the ROUNDED phrase, identifier-1 shall be
    /// described such that a MOVE statement is valid with identifier-1 as the sending operand and the printable item
    /// as the receiving operand" (the ROUNDED spelling is a <see cref="FieldComputeSource"/>, SR5). It is §2023:13.18.53.4
    /// GR1's implicit MOVE, so it is the ONE §2023:14.9.25.3 validity chain every other MOVE asker uses
    /// (<see cref="MoveTable16.Validity(BoundOperand, Table16Operand, DataItem)"/>: SR2, SR6-SR9 and Table 16), asked
    /// of the entry's printable item. An entry with no COLUMN clause has no printable item (<paramref name="printable"/>
    /// null), hence no receiving operand and nothing to ask — except §2023:14.9.25.3 SR1's sending half, a class rule
    /// about identifier-1 alone (<see cref="MoveTable16.SenderClassRefusal"/>), asked of every identifier-1.</item>
    /// </list></summary>
    private bool ScreenSourceIdentifier(ReportModel r, FieldReferenceSource rs, BoundOperand operand, DataItem? printable)
    {
        string where = $"RD '{r.Name}': SOURCE {rs.Written}";
        if (MoveTable16.OperandItem(operand) is { } item && VariableLengthCompatibility.IsVariableLength(item))
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportSourceOperandRule, $"{where}: identifier-1 references the "
                + $"variable-length group '{item.CobolName ?? item.ClrName}', which a SOURCE clause shall not name "
                + "(ISO §2023:13.18.53.3 SR8)");
            return true;
        }
        // §2023:14.9.25.3 SR1's sending half is part of "a MOVE statement is valid": an index, message-tag, object or
        // pointer identifier-1 is refused, printable entry or not — no edition's list of the contexts an index data item
        // may be referenced in names a SOURCE clause (§2023:13.18.60.3 SR10; §1985:VI.5.14.3 SR5; kb/Work PB5188).
        if (MoveTable16.SenderClassRefusal(operand) is { } classRefusal)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportSourceOperandRule, $"{where}: the implicit MOVE of §2023:13.18.53.4 GR1 "
                + $"from identifier-1 is invalid — {classRefusal} (ISO §2023:13.18.53.3 SR2)");
            return true;
        }
        if (printable is null || MoveTable16.Validity(operand, Table16Operand.Of(printable), printable) is not { } refusal)
            return false;
        ctx.Edition.Error(DiagnosticCatalog.ReportSourceOperandRule, $"{where}: the implicit MOVE of §2023:13.18.53.4 GR1 "
            + $"from identifier-1 to the printable item is invalid — {refusal.Reason} (ISO §2023:13.18.53.3 SR2)");
        return true;
    }

    /// <summary>⛔ THE VALUE EACH ROLLED ADDITION ADDS (ISO §2023:13.18.54.4 GR6; kb/Work PB1294) — bound once every
    /// report's own clauses are, because a rolled total may name an entry of ANOTHER report (§2023:13.18.54.3 SR4 g)).
    /// "If data-name-1 specifies an item whose entry contains a SUM clause, the value added is that of the
    /// corresponding sum counter. … If data-name-1 specifies an item whose entry has a SOURCE or VALUE clause, the
    /// value added is that of the operand of the SOURCE or VALUE clause" — as a number, because GR3 adds it
    /// ("consistent with the general rules of the ADD statement"). Only occurrences some addend actually names are
    /// asked: an entry whose SOURCE feeds an alphanumeric printable item is no error until a SUM wants to add it.</summary>
    private void BindRolledValues(IEnumerable<ReportModel> reports)
    {
        var asked = new HashSet<ReportItemOccurrence>(ReferenceEqualityComparer.Instance);
        foreach (var report in reports)
            foreach (var sum in report.Sums)
                foreach (var addend in sum.Terms.SelectMany(t => t.Addends).Where(a => a.Rolled))
                    foreach (var occurrence in addend.RolledFrom)
                        if (asked.Add(occurrence)) occurrence.Value = RolledValueOf(occurrence);
    }

    private BoundExpr? RolledValueOf(ReportItemOccurrence o)
    {
        var report = o.Family.Report;
        if (o.Sum is { } counter)
            return new BoundNumRef(new ReportSumCounterPlace(
                report.CsIndex, counter.Id, counter.Family.Register, ctx.Data.State.Reports.Depth(report)));
        BoundExpr? value = o.Source switch
        {
            FieldComputeSource { Value: { } expression } => expression,
            FieldReferenceSource { Value: { } operand } => NumberOf(operand),
            FieldValueSource { Raw: var raw } => RawNumber(raw),
            _ => null,   // rejected where it was bound
        };
        if (value is null && o.Source is not (FieldComputeSource { Rejected: true } or FieldReferenceSource { Rejected: true }))
            ctx.Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"RD '{report.Name}': the entry '{o.Family.Name}' is the data-name-1 of "
                + "a SUM clause, but the operand of its SOURCE or VALUE clause is not a number a sum counter can add "
                + "(ISO §2023:13.18.54.4 GR6 — the value added is that of the operand; GR3 — added by the ADD statement's rules)");
        // The SOURCE operand's own steps (kb/Work PB4517) run again at the rolled addition: GR6 adds "the value of the
        // operand" when the entry's group is processed, which is an evaluation of its own.
        return value is not null && o.Source?.Steps is { } steps ? new BoundUdfEvaluatedExpr(steps, value) : value;
    }

    /// <summary>A bound SOURCE operand as the number an ADD adds, or null when it is no number — a numeric item, a numeric
    /// literal, a computed value or the figurative ZERO (§2023:8.8.1.1's list of what an arithmetic operand may be).</summary>
    private static BoundExpr? NumberOf(BoundOperand operand) => operand switch
    {
        BoundFieldOperand { Place: var place } when IntrinsicArgumentRules.IsArithmeticOperandClass(operand) => new BoundNumRef(place),
        BoundNumericLiteral literal => new BoundNumLiteral(literal.Text),
        // A function-identifier written as identifier-1 (kb/Work PB5240) is a number only when its result is numeric.
        BoundComputedOperand computed when IntrinsicResultType.OperandCategory(computed) is PicCategory.Numeric => computed.Expr,
        BoundFigurative { Kind: 'Z' } => new BoundNumLiteral("0"),
        _ => null,
    };

    /// <summary>The number a VALUE clause operand's raw text writes — a numeric literal as written, or the figurative
    /// ZERO — else null (a numeric item's VALUE is numeric, §2023:13.18.63.3 SR4/SR5, so null means a refused operand).</summary>
    private static BoundExpr? RawNumber(string raw)
    {
        string text = raw.Trim();
        if (CobolNames.Same(text, "ZERO") || CobolNames.Same(text, "ZEROS")
            || CobolNames.Same(text, "ZEROES")) return new BoundNumLiteral("0");
        return text.Length > 0 && text.All(c => char.IsAsciiDigit(c) || c is '.' or '+' or '-') ? new BoundNumLiteral(text) : null;
    }

    /// <summary>The scope a report entry's clause expression binds in (<see cref="ReferenceResolver.VaryingScope"/>).</summary>
    private static Dictionary<string, ReportVaryingModel> ScopeOf(IEnumerable<ReportVaryingModel> counters)
    {
        var scope = new Dictionary<string, ReportVaryingModel>(CobolNames.Comparer);
        foreach (var c in counters) scope[c.Name] = c;
        return scope;
    }

    /// <summary>A bare reference to a VARYING counter in scope (ISO §2023:13.18.64.4 GR3/GR4 — "this allows data-name-1 to be
    /// used as a source data item, as a subscript to a source data item …"): the compose-local integer the placement
    /// declares. Null for any other reference, and always outside a report entry's clause (§2023:13.18.64.3 SR2 — a counter
    /// may be referenced only within its entry or a subordinate entry). Asked by the operand and expression binders
    /// right after the report counters, ahead of name resolution: a counter is "not defined elsewhere in the source
    /// element" (SR2), so no ordinary name can answer the word.</summary>
    public BoundExpr? VaryingExpr(Core.DataReferenceContext dref) =>
        ctx.Refs.VaryingScope is { } scope && dref.dataReferenceSuffix().Length == 0
            && dref.cobolWord() is { } w && scope.TryGetValue(w.GetText(), out var counter)
            ? new BoundReportVaryingRef(counter.Local) : null;

    /// <summary><c>INITIATE report-name…</c> (ISO §2023:14.9.21): each name shall be an RD entry (SR1); a multi-name
    /// statement IS a separate INITIATE statement per report-name in written order (§2023:14.9.21.4 GR5) — one
    /// <see cref="BoundInitiate"/> per name inside a <see cref="BoundImplicitSeries"/>, so GR5's second sentence
    /// ("processing resumes at the next implicit INITIATE statement, if any") has a boundary to land on.</summary>
    public BoundStatement BindInitiate(Core.InitiateStatementContext stmt)
    {
        if (RejectInBeforeReporting("INITIATE") is { } refused) return refused;
        var members = new List<BoundStatement>();
        foreach (var rn in stmt.reportName())
        {
            if (ctx.Data.State.Reports.Named(rn.GetText()) is not { } r)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"INITIATE '{rn.GetText()}': report-name-1 shall be defined by a report "
                    + "description entry in the report section (ISO §2023:14.9.21.3 SR1)");
            if (RejectContainedReportFile(r, $"INITIATE {r.Name}", "ISO §2023:14.9.21.3 SR2") is { } notGlobal) return notGlobal;
            members.Add(new BoundInitiate([r]));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary><c>GENERATE {data-name | report-name}</c> (ISO §2023:14.9.16): a detail report group (SR1 — detail
    /// reporting) or a report-name whose RD has a CONTROL clause (SR2 — summary reporting, GR2).</summary>
    public BoundStatement BindGenerate(Core.GenerateStatementContext stmt)
    {
        if (RejectInBeforeReporting("GENERATE") is { } refused) return refused;
        var (name, qualifier) = ReportGroupResolution.Parts(stmt.reportGroupReference());
        // The report-name form (SR2, summary reporting) has NO qualifier — §2023:8.4.2.2 gives a report-name no
        // qualifier at all — so a qualified operand can only be the data-name form and skips this arm.
        if (qualifier is null && ctx.Data.State.Reports.Named(name) is { } summary)
        {
            if (summary.Controls.Count == 0)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNeedsControl, $"GENERATE {name}: the report-name form requires a CONTROL "
                    + "clause in the report description entry (ISO §2023:14.9.16.3 SR2)");
            // §1985:XIII.3.20.3 SR6 — the report-name form needs a report description with no more than one detail report
            // group; COBOL-2002 and later keep only the CONTROL-clause condition above.
            if (ctx.Edition.DialectLevel < 2002 && summary.Groups.Count(g => g.Kind == ReportGroupKindModel.Detail) is > 1 and var details)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateDetailCount, $"GENERATE {name}: the report description entry "
                    + $"holds {details} detail report groups; when a GENERATE report-name statement is specified, the report "
                    + "description entry must include no more than one detail report group (ISO §1985:XIII.3.20.3 SR6)");
            if (RejectContainedReportFile(summary, $"GENERATE {name}", "ISO §2023:14.9.16.3 SR4") is { } notGlobal) return notGlobal;
            // Summary reporting (GR2). §1985:XIII.3.20.4 GR11: a report with exactly one detail report group processes the
            // statement "as though a GENERATE data-name statement were being executed" for it, so the subtotalling designated
            // for that detail (SUM ... UPON) is performed though the detail is not printed (kb/Work PB5249).
            var detailGroups = summary.Groups.Where(g => g.Kind == ReportGroupKindModel.Detail).ToList();
            return Presenting(new BoundGenerate(summary, null, detailGroups is [{ Name: not null } only] ? only : null));
        }
        // SR1's data-name form, through the ONE funnel: it owns the qualified spelling ("It may be qualified by
        // a report-name") AND the §2023:8.4.2.2.3 SR1 ambiguity this loop used to resolve by writing order (PB365).
        string where = $"GENERATE {name}{(qualifier is null ? "" : $" OF {qualifier}")}";
        if (ReportGroupResolution.Resolve(ctx.Edition, ctx.Data.State.Reports.Visible, name, qualifier, where,
                out var report, out var group, ctx.Data) != ReportGroupResolution.Match.None)
        {
            if (group!.Kind != ReportGroupKindModel.Detail)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNotDetail, $"{where}: the named report group is not a "
                    + "DETAIL group (ISO §2023:14.9.16.3 SR1)");
            // SR3's REPORT half ("the report description entry in which data-name-1 is specified ... shall contain a
            // GLOBAL clause") holds by construction — a container's non-GLOBAL report is not visible here at all,
            // so its groups resolve to nothing (§2023:13.18.27.4 GR2). The FILE half is asked here.
            if (RejectContainedReportFile(report!, where, "ISO §2023:14.9.16.3 SR3") is { } notGlobal) return notGlobal;
            return Presenting(new BoundGenerate(report!, group, null));
        }
        // Match.None is UNDIAGNOSED by contract ("leaves the 'this is not a report group' diagnostic to the
        // caller") — so this is the report, never a deferral (kb/Work PB909).
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"{where}: '{name}' names neither a detail report group (ISO §2023:14.9.16.3 SR1) "
            + "nor a report description entry (SR2)");
    }

    /// <summary><c>TERMINATE report-name…</c> (ISO §2023:14.9.46 SR1; §2023:14.9.46.4 GR4) — one <see cref="BoundTerminate"/> per
    /// report-name inside a <see cref="BoundImplicitSeries"/>, GR4's "as though a separate TERMINATE statement had
    /// been executed for each report-name-1" and its per-implicit-statement resumption point.</summary>
    public BoundStatement BindTerminate(Core.TerminateStatementContext stmt)
    {
        if (RejectInBeforeReporting("TERMINATE") is { } refused) return refused;
        var members = new List<BoundStatement>();
        foreach (var rn in stmt.reportName())
        {
            if (ctx.Data.State.Reports.Named(rn.GetText()) is not { } r)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"TERMINATE '{rn.GetText()}': report-name-1 shall be defined by a report "
                    + "description entry in the report section (ISO §2023:14.9.46.3 SR1)");
            if (RejectContainedReportFile(r, $"TERMINATE {r.Name}", "ISO §2023:14.9.46.3 SR2") is { } notGlobal) return notGlobal;
            members.Add(Presenting(new BoundTerminate([r])));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary>A GENERATE or TERMINATE — the two statements that present report groups — is the statement a report
    /// clause's operand-evaluation steps run inside (kb/Work PB4517; see <see cref="BindReportGroupClauses"/>), so it
    /// counts as having evaluated such steps when ANY report it can reach carries them (a GENERATE of one report runs
    /// the SUM UPON additions and the rolled totals of another, §2023:13.18.54.4 GR6/GR7). The count is what makes
    /// <c>StatementBinder.BindStatement</c> give the statement its <see cref="BoundActivationSite"/>: ISO §2023:14.9.33.4 GR2
    /// a) 1./2. makes the statement an activation or a §2023:15.4 subscript store was evaluated in the one a RESUME AT NEXT
    /// STATEMENT lands after.</summary>
    private T Presenting<T>(T statement) where T : BoundStatement
    {
        if (ctx.Data.State.Reports.Visible.Any(r => r.EvaluatesOperandSteps))
            ctx.Data.Procedure.OperandEvaluations.Advance(ctx.Data.Procedure.OperandEvaluations.Current + 1);
        return statement;
    }

    /// <summary><c>SUPPRESS PRINTING</c> (ISO §2023:14.9.45): inhibit the current instance's printing of the report
    /// group named by the USE BEFORE REPORTING procedure in which this SUPPRESS lexically appears. §2023:14.9.45.4 GR1
    /// makes that group a STATIC property of the enclosing declarative, so it is resolved HERE at bind time: the
    /// declarative whose pc range covers the current bind cursor (a Format-2 <c>ReportGroup</c> scope) fixes the
    /// group, and its owning report drives the emitted engine call. §2023:14.9.45.3 SR1 — a SUPPRESS outside any USE
    /// BEFORE REPORTING procedure has no group to inhibit and is rejected (the per-instance suppression itself is
    /// the runtime half, GR2 — the report engine's one-shot flag).</summary>
    public BoundStatement BindSuppress(Core.SuppressStatementContext stmt)
    {
        // The containing declarative comes from the ONE bind-position probe (kb/Work PB403) — SUPPRESS's
        // §2023:14.9.45.3 SR1 is a placement rule like §2023:14.9.14.3 SR2 and §2023:14.9.33.3 SR1/SR2, and each of them used to
        // walk ctx.Table.Declaratives itself. Ranges are disjoint, so "the declarative containing the cursor,
        // if it is a USE BEFORE REPORTING one" is the same set as the old "first declarative that has a report
        // group AND contains the cursor".
        if (ctx.Enclosing.Declarative?.ReportGroup is not { } group)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ReportSuppressContext,
                "SUPPRESS PRINTING may appear only in a USE BEFORE REPORTING procedure (ISO §2023:14.9.45.3 SR1)");
        }
        var report = ctx.Data.State.Reports.Visible.First(r => r.Groups.Contains(group));
        return new BoundSuppress(report, group);
    }

    /// <summary>⛔ THE ONE CHECK of the rule the three report-driving verbs each restate (kb/Work PB369): "If
    /// report-name-1 is defined in a containing program, the file description entry associated with report-name-1
    /// shall contain a GLOBAL clause" — §2023:14.9.21.3 SR2 (INITIATE), §2023:14.9.46.3 SR2 (TERMINATE), §2023:14.9.16.3 SR4
    /// (GENERATE report-name) and the file half of §2023:14.9.16.3 SR3 (GENERATE data-name). The report itself is
    /// visible only because it is GLOBAL (<see cref="ReportScope.Visible"/>); the statement drives output to
    /// its FILE, which a contained program may reference only when the FD is GLOBAL too (§2023:13.18.27.3 SR3). Null
    /// when the statement may proceed — the report is this unit's own, or its file is GLOBAL.
    /// <para>The rule begins at 2002 (kb/Work PB5080): §2002:14.8.20.2 SR2 (INITIATE), §2002:14.8.42.2 SR2 (TERMINATE),
    /// §2002:14.8.15.2 SR3/SR4 (GENERATE) carry it word for word. The 1985 statements have only "Report-name-1 must be
    /// defined by a report description entry" (§1985:XIII.4.4.3 SR1, §1985:XIII.4.7.3 SR1), and a global name may be
    /// referenced by every contained program without describing it again (§1985:X.4.6.4 GR2), so a 1985 compilation
    /// asks nothing here.</para></summary>
    private BoundStatement? RejectContainedReportFile(ReportModel report, string where, string rule) =>
        ctx.Edition.Edition.Has(2002) && ctx.Data.State.Reports.Depth(report) > 0 && report.File is { IsGlobal: false } file
            ? BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ContainedReportFileNotGlobal,
                $"{where}: report '{report.Name}' is defined in a containing program and its file description entry "
                + $"'{file.CobolName}' does not contain a GLOBAL clause; the file description entry associated with a "
                + $"containing program's report shall contain a GLOBAL clause ({rule})")
            : null;

    /// <summary>ISO §2023:14.9.49.3 SR10 (kb/Work PB363): "The GENERATE, INITIATE, or TERMINATE statements shall not
    /// appear in a paragraph within a USE BEFORE REPORTING procedure." ONE guard read by all three verbs, over the
    /// ONE bind-position probe SUPPRESS's converse rule (§2023:14.9.45.3 SR1) reads, so the three cannot drift apart.
    /// The rule is lexical ("appear in a paragraph"); the same question asked of a statement REACHED through a
    /// PERFORM at run time is §2023:14.9.49.4 GR10's EC-FLOW-REPORT (kb/Work PB326). Null when the statement may
    /// proceed.</summary>
    private BoundStatement? RejectInBeforeReporting(string verb) =>
        ctx.Enclosing.InBeforeReportingProcedure
            ? BoundRejected.Report(ctx.Edition, DiagnosticCatalog.UseBeforeReportingRestriction,
                $"{verb}: this statement is in USE BEFORE REPORTING declarative section "
                + $"'{ctx.Enclosing.Declarative!.SectionName}' — \"The GENERATE, INITIATE, or TERMINATE statements "
                + "shall not appear in a paragraph within a USE BEFORE REPORTING procedure\" (ISO §2023:14.9.49.3 SR10)")
            : null;

    /// <summary>ISO §2023:14.9.49.3 SR11 (kb/Work PB363): "A USE BEFORE REPORTING procedure shall not alter the value of
    /// any control data item." Asked of one BOUND statement, through the ONE store classification
    /// (<see cref="BoundStores.StoresInto"/>), by <c>StatementBinder.BindStatement</c> for every statement bound in a
    /// USE BEFORE REPORTING procedure — so every verb that can store is covered by the classification's
    /// totality, never by a per-verb list here.
    /// <para>"Alter the value" is read over the storage the control data item occupies: a store into the item, into
    /// a group that contains it, or into an item subordinate to it (a MOVE to the record that holds the control
    /// field alters the field). "Any control data item" is literal — the control data items of every report of
    /// the source element, not only the report whose group the procedure names.</para></summary>
    /// <returns>True HAVING REPORTED a violation.</returns>
    public bool CheckControlDataStores(BoundStatement core)
    {
        List<DataItem>? controls = null;
        foreach (var r in ctx.Data.State.Reports.Visible)
            foreach (var c in r.Controls)
                if (c.Item is { } item) (controls ??= []).Add(item);
        if (controls is null) return false;

        DataItem? hit = null;
        if (!BoundStores.StoresInto(core, stored =>
            {
                foreach (var c in controls)
                    if (SameStorageLine(stored, c)) { hit = c; return true; }
                return false;
            }))
            return false;
        ctx.Edition.Error(DiagnosticCatalog.UseBeforeReportingRestriction,
            $"this statement alters control data item '{hit!.CobolName}' in USE BEFORE REPORTING declarative section "
            + $"'{ctx.Enclosing.Declarative!.SectionName}' — \"A USE BEFORE REPORTING procedure shall not alter the "
            + "value of any control data item\" (ISO §2023:14.9.49.3 SR11)");
        return true;
    }

    /// <summary>§1985:XIII.3.20.3 SR5, its USE half (kb/Work PB5424): "In control footing, page heading, page footing, and
    /// report footing report groups, SOURCE clauses and associated USE statements must not reference any of the following"
    /// — a group containing a control data item, an item subordinate to one, a redefinition or renaming of part of one —
    /// and in page heading and page footing groups the control data-names themselves. Asked by
    /// <c>StatementBinder.BindStatement</c> of every statement bound in a USE BEFORE REPORTING procedure, over the references
    /// the statement WRITES itself (<see cref="BoundReferences.Of"/>: a nested statement is asked at its own position), through
    /// the one relation the SOURCE half asks (<see cref="ReportSectionBinder.ControlDataReferenceOf"/>). The procedure is
    /// the declarative section the USE statement heads; a procedure it PERFORMs is not asked, as the segment rule of a
    /// PERFORM range asks only the procedures it names (docs/CONFORMANCE.md section 3.8). COBOL-2002 and later carry no
    /// such rule.</summary>
    /// <returns>True HAVING REPORTED a violation (one per statement).</returns>
    public bool CheckControlDataReferences(BoundStatement core)
    {
        if (ctx.Edition.DialectLevel >= 2002 || ctx.Enclosing.Declarative is not { ReportGroup: { } group } use) return false;
        var report = ctx.Data.State.Reports.Visible.FirstOrDefault(r => r.Groups.Contains(group));
        if (report is null) return false;
        foreach (var place in BoundReferences.Of(core).Places)
            if (place.Item is { } item && ReportSectionBinder.ControlDataReferenceOf(report, group.Kind, item) is { } hit)
            {
                ctx.Edition.Error(DiagnosticCatalog.ReportSourceControlData, "this statement of USE BEFORE REPORTING declarative "
                    + $"section '{use.SectionName}', which names the "
                    + $"{ReportSectionBinder.ReportGroupTypeWords(group.Kind).ToLowerInvariant()} report group '{group.Name}' of "
                    + $"RD '{report.Name}', references '{item.CobolName}', which {hit.Relation} '{hit.Control.Display}'; "
                    + ReportSectionBinder.ControlDataRuleWords(group.Kind, "USE statements"));
                return true;
            }
        return false;
    }

    /// <summary>Is one of the two items the other or an ancestor of it — the hierarchy in which a store into one
    /// alters the other's value?</summary>
    private static bool SameStorageLine(DataItem a, DataItem b)
    {
        for (var x = b; x is not null; x = x.Parent) if (ReferenceEquals(x, a)) return true;
        for (var x = a.Parent; x is not null; x = x.Parent) if (ReferenceEquals(x, b)) return true;
        return false;
    }

    /// <summary>Intercept a LINE-COUNTER / PAGE-COUNTER data reference (ISO §2023:8.4.3.15) ahead of normal name
    /// resolution (the LINAGE-COUNTER idiom in <c>FieldOperand</c>/<c>RefExpr</c>). Returns null when the
    /// reference is NOT a counter; a <see cref="BoundReportCounterRef"/> when it resolves (the OF/IN
    /// <c>cobolWord</c> is the report-name qualifier, SR2/§2023:8.4.2.2 — unqualified resolves only against a sole
    /// report); a <see cref="BoundExprError"/> (with a bind diagnostic) for a counter that cannot resolve.</summary>
    public BoundExpr? CounterExpr(Core.DataReferenceContext dref)
    {
        bool isPage = dref.PAGE_COUNTER() is not null;
        if (!isPage && dref.LINE_COUNTER() is null) return null;
        string reg = isPage ? "PAGE-COUNTER" : "LINE-COUNTER";
        return ctx.Refs.Counters.ReportOf(dref, reg) is { } report
            ? new BoundReportCounterRef(report, isPage, ctx.Data.State.Reports.Depth(report))
            : BoundExprError.Refused(ctx.Edition, $"{reg} reference '{DataBinder.WrittenText(dref)}'");
    }

    /// <summary>The <see cref="ReportPageCounterPlace"/> for a PAGE-COUNTER RECEIVING reference (ISO §2023:8.4.3.15.3
    /// SR1 — "In the procedure division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where an
    /// integer data item may appear"; SR3 removes LINE-COUNTER, and only LINE-COUNTER, from the receiving side,
    /// which <c>ExpressionBinder.ResolveReceiving</c> screens before it reaches here). Null when the reference
    /// does not resolve to a report — the qualification diagnostic has then already been raised, exactly as on
    /// the sending side, because BOTH directions ask <see cref="CounterResolver.ReportOf"/> (kb/Work PB429).</summary>
    public Place? CounterPlace(Core.DataReferenceContext dref) =>
        ctx.Refs.Counters.ReportOf(dref, "PAGE-COUNTER") is { } report
            ? new ReportPageCounterPlace(report.CsIndex, report.PageCounterRegister, ctx.Data.State.Reports.Depth(report))
            : null;
}
