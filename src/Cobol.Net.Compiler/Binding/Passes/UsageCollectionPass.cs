// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using CobolNet.Binding.Bound;

namespace CobolNet.Binding.Passes;

/// <summary>
/// PHASE-05 Step 5 (DESIGN-data-model §2.5 step 9): the single owner of the "which groups are referenced as a WHOLE
/// character-image operand" fact — the input to the numeric-DISPLAY-leaf image promotion (§14.9 MOVE GR4). It is
/// collected here, AFTER binding, by an explicit TYPED walk of the bound tree, replacing <see cref="ReferenceResolver"/>'s
/// mid-resolve mutation of <see cref="DataBinder.WholeGroupReferenced"/>.
/// <para><b>Correctness (owner-directed redesign, 2026-07-10):</b> the bound tree is the CORRECT oracle — a group has
/// a <see cref="Place"/> in the tree EXACTLY where it is used as a whole operand (MOVE/DISPLAY/compare/ACCEPT/record
/// I-O/whole-group CORRESPONDING pair/boundary formal). The legacy resolver over-collected: it added ANY resolved
/// group, including CORRESPONDING OPERANDS and SEARCH TABLES that are decomposed to member/element access and are
/// never whole-image operands (verified output-neutral: corrA vs corrB emit string vs long fields with byte-identical
/// runtime output). So this pass deliberately produces a SMALLER, correct set; the divergence from legacy is
/// legacy-wrong / this-right, validated by OUTPUT (greenfield DifferentialGolden + conformance byte-exact runtime),
/// not by legacy set-equality.</para>
/// <para>A group can only surface as a whole operand in a <see cref="Place"/> field, a <see cref="BoundFieldOperand"/>,
/// a relational/88/class <see cref="BoundCondition"/>, a whole-group <see cref="CorrespondingPair"/>, a
/// <see cref="SetPlaceTarget"/>, or an INITIALIZE store — NEVER in a numeric <see cref="BoundExpr"/> or a
/// <see cref="BoundBoolExpr"/> (those carry only numeric/boolean leaves). The FILE record areas and boundary-copied
/// formals are whole-image-filled by I-O / a CALL crossing (§9.1.2 / §14.2.3 GR8) WITHOUT surfacing as a procedure
/// Place, so they are added structurally.</para>
/// </summary>
internal static class UsageCollectionPass
{
    /// <summary>Fill <paramref name="data"/>'s <see cref="DataBinder.WholeGroupReferenced"/> from the whole-group
    /// operands of the bound <paramref name="programs"/> + the boundary-copied program/OO formals
    /// (<paramref name="extraFormalGroups"/> is the OO method USING/RETURNING group items). The FILE record areas are
    /// added earlier, at bind time, by <c>DataBinder.MarkFileRecordImageLeaves</c> (the SORT binder needs their image
    /// flags DURING binding), so they are NOT re-added here. Runs once per binder, after procedure binding, before
    /// <c>MarkStoreAsImage</c> reads the set.</summary>
    public static void Collect(DataBinder data, IEnumerable<BoundProgram?> programs,
        IEnumerable<DataItem>? extraFormalGroups = null)
    {
        var set = data.WholeGroupReferenced;

        // Boundary-copied program formals — a group formal crosses as its character image (§14.2.3 GR8). Formerly
        // registered by the pre-emit LinkageFormals resolve (through ReferenceResolver, now deleted).
        foreach (var lf in data.LinkageFormals)
            if (!lf.CarrierResident && lf.Item.IsGroup) set.Add(lf.Item);

        // The program/function PROCEDURE DIVISION RETURNING group item crosses the activation boundary as its image
        // too (its GOBACK-RETURNING / caller copy-out is a whole-group MOVE) — formerly registered by the pre-emit
        // returning resolve (through ReferenceResolver, now deleted).
        if (data.LinkageReturning is { IsGroup: true } ret) set.Add(ret);

        // OO method USING/RETURNING group items (supplied by the caller — not on data as LinkageFormals).
        if (extraFormalGroups is not null)
            foreach (var g in extraFormalGroups)
                if (g.IsGroup) set.Add(g);

        var v = new Visitor(set);
        foreach (var prog in programs) v.Program(prog);
    }

    /// <summary>The typed bound-tree walk. Every container that can hold a whole-group operand OR a nested statement is
    /// descended (cross-checked against <c>VersionConformancePass.Recurse</c> for the nested-statement coverage); a
    /// group <see cref="Place.Item"/> at any whole-operand position is collected.</summary>
    private sealed class Visitor(HashSet<DataItem> set)
    {
        public void Program(BoundProgram? prog)
        {
            if (prog is null) return;
            foreach (var para in prog.Paragraphs)
                foreach (var s in para.Statements) Stmt(s);
        }

        private void List(IReadOnlyList<BoundStatement>? list)
        {
            if (list is null) return;
            foreach (var s in list) Stmt(s);
        }

        private void Stmt(BoundStatement s)
        {
            switch (s)
            {
                // ── whole-group operand positions ──
                case BoundMove x: Op(x.Source); foreach (var t in x.Targets) P(t); break;
                case BoundDisplay x: foreach (var o in x.Operands) Op(o); break;
                case BoundAccept x: P(x.Target); break;
                case BoundInspect x:
                    P(x.Target);
                    foreach (var t in x.Tallying) { P(t.Counter); Op(t.Pattern); Op(t.Before); Op(t.After); }
                    foreach (var r in x.Replacing) { Op(r.Pattern); Op(r.Replacement); Op(r.Before); Op(r.After); }
                    if (x.Converting is { } c) { Op(c.From); Op(c.To); Op(c.Before); Op(c.After); }
                    break;
                case BoundStringStmt x:
                    P(x.Into); P(x.Pointer);
                    foreach (var snd in x.Sendings) { Op(snd.Value); Op(snd.Delimiter); }
                    List(x.OnOverflow); List(x.NotOnOverflow); break;
                case BoundUnstringStmt x:
                    P(x.Source); P(x.Pointer); P(x.Tallying);
                    foreach (var r in x.Receivers) { P(r.Target); P(r.DelimiterIn); P(r.CountIn); }
                    List(x.OnOverflow); List(x.NotOnOverflow); break;
                case BoundWrite x: P(x.Record); Op(x.From); List(x.AtEop); List(x.NotAtEop); break;
                case BoundRewrite x: P(x.Record); Op(x.From); break;
                case BoundRead x: P(x.Into); List(x.AtEnd); List(x.NotAtEnd); break;
                case BoundKeyedRead x:
                    P(x.Into); List(x.AtEnd); List(x.NotAtEnd);
                    List(x.InvalidKey?.Invalid); List(x.InvalidKey?.NotInvalid); break;
                case BoundKeyedWrite x: P(x.Record); Op(x.From); List(x.InvalidKey?.Invalid); List(x.InvalidKey?.NotInvalid); break;
                case BoundKeyedRewrite x: P(x.Record); Op(x.From); List(x.InvalidKey?.Invalid); List(x.InvalidKey?.NotInvalid); break;
                case BoundKeyedDelete x: List(x.InvalidKey?.Invalid); List(x.InvalidKey?.NotInvalid); break;
                case BoundKeyedStart x: P(x.Operand); List(x.InvalidKey?.Invalid); List(x.InvalidKey?.NotInvalid); break;
                case BoundKeyedDeleteFile x: List(x.OnException); List(x.NotOnException); break;
                case BoundReturn x: P(x.RecordArea); P(x.Into); List(x.AtEnd); List(x.NotAtEnd); break;
                case BoundRelease x: P(x.Record); Op(x.From); break;   // RELEASE record FROM x ≡ MOVE x TO record (§14.9.32 GR4)
                case BoundInitialize x: foreach (var a in x.Actions) InitAct(a); break;
                case BoundCorresponding x:
                    // ONLY the whole-moved group-side pairs (a group corresponding to a namesake) are whole-group
                    // operands; the two statement OPERANDS are decomposed, never whole-image (correctly omitted —
                    // BoundCorresponding retains no Place to them).
                    foreach (var pr in x.Pairs) { P(pr.Source); P(pr.Target); }
                    Size(x.SizeError); break;
                case BoundSetConditions x: foreach (var (p, _) in x.Sets) P(p); break;
                case BoundCallProgram x:
                    Op(x.DynamicName);
                    foreach (var a in x.Args) { P(a.Place); Op(a.Value); }
                    P(x.Returning); List(x.OnException); List(x.NotOnException); break;
                case BoundCancel x: foreach (var (_, dn) in x.Targets) Op(dn); break;
                case BoundInvoke x:
                    P(x.Receiver); P(x.Returning);
                    if (x.Args is { } args) foreach (var a in args) P(a.Source); break;
                case BoundInvokeUniversal x:
                    P(x.Receiver); P(x.MethodSource); P(x.Returning);
                    foreach (var a in x.Args) P(a.Source); break;
                case BoundGoToDepending x: Op(x.Selector); break;
                case BoundGoback x: P(x.ReturningSource); break;   // GOBACK RETURNING group ≡ a whole-group MOVE source
                case BoundSetTo x: foreach (var t in x.Targets) SetT(t); break;
                case BoundSetUpDown x: foreach (var t in x.Targets) SetT(t); break;

                // ── conditions + nested statement containers ──
                case BoundIf x: Cond(x.Condition); List(x.Then); List(x.Else); break;
                case BoundEvaluate x: foreach (var w in x.Whens) { Cond(w.Match); List(w.Statements); } List(x.Other); break;
                case BoundInlinePerform x: Perf(x.Control); List(x.Body); break;
                case BoundOutOfLinePerform x: Perf(x.Control); break;
                case BoundSearch x:
                    // The searched table is scanned by INDEX (element access), never a whole image — correctly NOT
                    // retained as a Place and NOT collected.
                    if (x.AlsoVaried is { } av) SetT(av);
                    List(x.AtEnd);
                    foreach (var w in x.Whens) { Cond(w.Condition); List(w.Statements); } break;
                case BoundSequence x: List(x.Steps); break;
                case BoundEcChecked x: Stmt(x.Inner); break;

                // ── arithmetic: receivers are numeric leaves, operands numeric exprs — no group operands; only the
                //    ON SIZE ERROR nested statements can carry one. ──
                case BoundAddTo x: Size(x.SizeError); break;
                case BoundAddGiving x: Size(x.SizeError); break;
                case BoundSubtractFrom x: Size(x.SizeError); break;
                case BoundSubtractGiving x: Size(x.SizeError); break;
                case BoundMultiplyBy x: Size(x.SizeError); break;
                case BoundMultiplyGiving x: Size(x.SizeError); break;
                case BoundDivideInto x: Size(x.SizeError); break;
                case BoundDivideGiving x: Size(x.SizeError); break;
                case BoundDivideRemainder x: Size(x.SizeError); break;
                case BoundCompute x: Size(x.SizeError); break;

                default: break;   // a leaf statement with no group operand + no nested statements
            }
        }

        private void InitAct(InitializeAction a)
        {
            switch (a)
            {
                case InitializeStore x: P(x.Target); Op(x.Source); break;
                case InitializeLoop x: foreach (var b in x.Body) InitAct(b); break;
                case InitializeDynLoop x: foreach (var b in x.Body) InitAct(b); break;
                default: break;
            }
        }

        private void Cond(BoundCondition? c)
        {
            switch (c)
            {
                case BoundRelational x: Op(x.Left); Op(x.Right); break;
                case BoundLogical x: foreach (var o in x.Operands) Cond(o); break;
                case BoundNot x: Cond(x.Operand); break;
                case BoundCondition88 x: P(x.Parent); break;
                case BoundClassCondition x: Op(x.Operand); break;
                case BoundUserClassCondition x: Op(x.Operand); break;
                default: break;   // sign/boolean condition — numeric/boolean operand, no group
            }
        }

        private void Perf(BoundPerformControl ctl)
        {
            switch (ctl)
            {
                case PerformTimes x: Op(x.Count); break;
                case PerformUntil x: Cond(x.Until); break;
                case PerformVarying x: foreach (var lvl in x.Levels) { SetT(lvl.Var); Cond(lvl.Until); } break;
                default: break;
            }
        }

        private void Op(BoundOperand? o)
        {
            if (o is BoundFieldOperand f) P(f.Place);
        }

        private void SetT(BoundSetTarget t)
        {
            if (t is SetPlaceTarget p) P(p.Place);
        }

        private void Size(SizeErrorPhrase? se)
        {
            if (se is null) return;
            List(se.OnError); List(se.NotOnError);
        }

        private void P(Place? place)
        {
            // An OCCURS DYNAMIC element access (data-model D9) stores the group as a typed element of a
            // CobolDynTable<T> — a whole-group MOVE into it round-trips through the TABLE codec (FromImage/AsImage on
            // the element struct), never the item-level character-image mechanism. So its numeric-DISPLAY leaves stay
            // NATIVE and the element group is NOT whole-image-referenced (matching the legacy dynamic-resolution path,
            // which never mutated WholeGroupReferenced for a dynamic element).
            if (place is DynTablePlace) return;
            if (place?.Item is { IsGroup: true } g) set.Add(g);
        }
    }
}
