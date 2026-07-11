// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding.Bound;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Passes;

/// <summary>
/// PHASE-05 Step 5 (DESIGN-data-model §2.5 step 9): the single owner of the "which groups are referenced as a WHOLE
/// character-image operand" fact — the input to the numeric-DISPLAY-leaf image promotion (§14.9 MOVE GR4). It is
/// collected here, AFTER binding, by an explicit walk of the bound tree, replacing <see cref="ReferenceResolver"/>'s
/// mid-resolve mutation of <see cref="DataBinder.WholeGroupReferenced"/>.
/// <para><b>Correctness (owner-directed redesign, 2026-07-10):</b> the bound tree is the CORRECT oracle — a group has
/// a <see cref="Place"/> in the tree EXACTLY where it is used as a whole operand (MOVE/DISPLAY/compare/ACCEPT/record
/// I-O/whole-group CORRESPONDING pair/boundary formal). The legacy resolver over-collected: it added ANY resolved
/// group, including CORRESPONDING OPERANDS and SEARCH TABLES that are decomposed to member/element access and are
/// never whole-image operands (verified output-neutral). So this pass deliberately produces a SMALLER, correct set;
/// the divergence from legacy is legacy-wrong / this-right, validated by OUTPUT (greenfield DifferentialGolden +
/// conformance byte-exact runtime), not by legacy set-equality.</para>
/// <para><b>Traversal (PHASE-07 Step 6h):</b> the per-statement whole-operand extraction is the generated exhaustive
/// <see cref="IBoundStatementVisitor{T}"/> — a new statement leaf is a COMPILE error here, never a silent
/// <c>default: break</c> — and the nested-statement RECURSION rides the generated
/// <see cref="BoundStatementTree.StatementChildren"/> (the ONE drift-proof container source), so the former hand-listed
/// recursion "cross-checked against VersionConformancePass" by prose is gone. Both changes are output-neutral: the old
/// recursion set already equalled <c>StatementChildren</c>'s containers.</para>
/// <para>A group can only surface as a whole operand in a <see cref="Place"/> field, a <see cref="BoundFieldOperand"/>,
/// a relational/88/class <see cref="BoundCondition"/>, a whole-group <see cref="CorrespondingPair"/>, a
/// <see cref="SetPlaceTarget"/>, or an INITIALIZE store — NEVER in a numeric <see cref="BoundExpr"/> or a
/// <see cref="BoundBoolExpr"/> (those carry only numeric/boolean leaves). FILE record areas and boundary-copied
/// formals are whole-image-filled by I-O / a CALL crossing WITHOUT surfacing as a procedure Place, so they are added
/// structurally.</para>
/// </summary>
internal static class UsageCollectionPass
{
    /// <summary>The GROUP pass body (P6 Step 3 — <c>BindPipeline.GroupTail</c>, Requires <c>ProcedureBound</c>,
    /// Produces <c>UsageCollected</c>): collect the whole-group set for every class forest (OBJECT + FACTORY halves,
    /// with the method USING/RETURNING formals) and every program unit, in the fused pipeline's order.</summary>
    public static void Run(GroupBindContext ctx)
    {
        foreach (var cls in ctx.Classes)
        {
            Collect(cls.Data, [cls.Bound], OoFormalGroups(cls.Symbol.Methods));
            Collect(cls.FactoryData, [cls.FactoryBound], OoFormalGroups(cls.Symbol.FactoryMethods));
        }
        foreach (var unit in ctx.Units) Collect(unit.Data, [unit.Bound]);

        static IEnumerable<DataItem> OoFormalGroups(IEnumerable<OoMethodSymbol> methods) =>
            methods.SelectMany(m => m.Formals.Select(f => f.Item).Concat(m.Returning is { } r ? [r] : Array.Empty<DataItem>()));
    }

    /// <summary>Fill <paramref name="data"/>'s <see cref="DataBinder.WholeGroupReferenced"/> from the whole-group
    /// operands of the bound <paramref name="programs"/> + the boundary-copied program/OO formals
    /// (<paramref name="extraFormalGroups"/> is the OO method USING/RETURNING group items). FILE record areas are
    /// added earlier, at bind time, by <c>DataBinder.MarkFileRecordImageLeaves</c>. Runs once per binder, after
    /// procedure binding, before <c>MarkStoreAsImage</c> reads the set.</summary>
    public static void Collect(DataBinder data, IEnumerable<BoundProgram?> programs,
        IEnumerable<DataItem>? extraFormalGroups = null)
    {
        var set = data.WholeGroupReferenced;

        // Boundary-copied program formals — a group formal crosses as its character image (§14.2.3 GR8).
        foreach (var lf in data.LinkageFormals)
            if (!lf.CarrierResident && lf.Item.IsGroup) set.Add(lf.Item);

        // The program/function PROCEDURE DIVISION RETURNING group item crosses the activation boundary as its image.
        if (data.LinkageReturning is { IsGroup: true } ret) set.Add(ret);

        // OO method USING/RETURNING group items (supplied by the caller — not on data as LinkageFormals).
        if (extraFormalGroups is not null)
            foreach (var g in extraFormalGroups)
                if (g.IsGroup) set.Add(g);

        var v = new Visitor(set);
        foreach (var prog in programs)
            if (prog is not null)
                foreach (var para in prog.Paragraphs)
                    foreach (var s in para.Statements)
                        v.Walk(s);
    }

    /// <summary>The bound-tree walk. <see cref="Walk"/> collects a statement's OWN whole-group operand positions
    /// through the exhaustive statement visitor, then recurses every nested statement via
    /// <see cref="BoundStatementTree.StatementChildren"/>. The per-leaf <c>Visit</c> collects ONLY the node's direct
    /// operands/conditions/init-actions — nested statements are the recursion's job, so containers whose only
    /// group-bearing content is nested statements have an empty <c>Visit</c>.</summary>
    private sealed class Visitor(HashSet<DataItem> set) : IBoundStatementVisitor<bool>
    {
        public void Walk(BoundStatement s)
        {
            s.Accept(this);
            foreach (var child in s.StatementChildren())
                Walk(child);
        }

        // ── whole-group operand positions ──
        public bool Visit(BoundMove n) { Op(n.Source); foreach (var t in n.Targets) P(t); return false; }
        public bool Visit(BoundDisplay n) { foreach (var o in n.Operands) Op(o); return false; }
        public bool Visit(BoundAccept n) { P(n.Target); return false; }
        public bool Visit(BoundInspect n)
        {
            P(n.Target);
            foreach (var t in n.Tallying) { P(t.Counter); Op(t.Pattern); Op(t.Before); Op(t.After); }
            foreach (var r in n.Replacing) { Op(r.Pattern); Op(r.Replacement); Op(r.Before); Op(r.After); }
            if (n.Converting is { } c) { Op(c.From); Op(c.To); Op(c.Before); Op(c.After); }
            return false;
        }
        public bool Visit(BoundStringStmt n)
        {
            P(n.Into); P(n.Pointer);
            foreach (var snd in n.Sendings) { Op(snd.Value); Op(snd.Delimiter); }
            return false;
        }
        public bool Visit(BoundUnstringStmt n)
        {
            P(n.Source); P(n.Pointer); P(n.Tallying);
            foreach (var r in n.Receivers) { P(r.Target); P(r.DelimiterIn); P(r.CountIn); }
            return false;
        }
        public bool Visit(BoundWrite n) { P(n.Record); Op(n.From); return false; }
        public bool Visit(BoundRewrite n) { P(n.Record); Op(n.From); return false; }
        public bool Visit(BoundRead n) { P(n.Into); return false; }
        public bool Visit(BoundKeyedRead n) { P(n.Into); return false; }
        public bool Visit(BoundKeyedWrite n) { P(n.Record); Op(n.From); return false; }
        public bool Visit(BoundKeyedRewrite n) { P(n.Record); Op(n.From); return false; }
        public bool Visit(BoundKeyedDelete n) => false;
        public bool Visit(BoundKeyedStart n) { P(n.Operand); return false; }
        public bool Visit(BoundKeyedDeleteFile n) => false;
        public bool Visit(BoundReturn n) { P(n.RecordArea); P(n.Into); return false; }
        public bool Visit(BoundRelease n) { P(n.Record); Op(n.From); return false; }   // RELEASE record FROM x ≡ MOVE x TO record (§14.9.32 GR4)
        public bool Visit(BoundInitialize n) { foreach (var a in n.Actions) InitAct(a); return false; }
        public bool Visit(BoundCorresponding n)
        {
            // ONLY the whole-moved group-side pairs are whole-group operands; the two statement OPERANDS are
            // decomposed, never whole-image (correctly omitted — BoundCorresponding retains no Place to them).
            foreach (var pr in n.Pairs) { P(pr.Source); P(pr.Target); }
            return false;
        }
        public bool Visit(BoundSetConditions n) { foreach (var (p, _) in n.Sets) P(p); return false; }
        public bool Visit(BoundCallProgram n)
        {
            Op(n.DynamicName);
            foreach (var a in n.Args) { P(a.Place); Op(a.Value); }
            P(n.Returning);
            return false;
        }
        public bool Visit(BoundCancel n) { foreach (var (_, dn) in n.Targets) Op(dn); return false; }
        public bool Visit(BoundInvoke n)
        {
            P(n.Receiver); P(n.Returning);
            if (n.Args is { } args) foreach (var a in args) P(a.Source);
            return false;
        }
        public bool Visit(BoundInvokeUniversal n)
        {
            P(n.Receiver); P(n.MethodSource); P(n.Returning);
            foreach (var a in n.Args) P(a.Source);
            return false;
        }
        public bool Visit(BoundGoToDepending n) { Op(n.Selector); return false; }
        public bool Visit(BoundGoback n) { P(n.ReturningSource); return false; }   // GOBACK RETURNING group ≡ a whole-group MOVE source
        public bool Visit(BoundSetTo n) { foreach (var t in n.Targets) SetT(t); return false; }
        public bool Visit(BoundSetUpDown n) { foreach (var t in n.Targets) SetT(t); return false; }

        // ── conditions + loop control (the nested STATEMENTS are recursed by StatementChildren) ──
        public bool Visit(BoundIf n) { Cond(n.Condition); return false; }
        public bool Visit(BoundEvaluate n) { foreach (var w in n.Whens) Cond(w.Match); return false; }
        public bool Visit(BoundInlinePerform n) { Perf(n.Control); return false; }
        public bool Visit(BoundOutOfLinePerform n) { Perf(n.Control); return false; }
        public bool Visit(BoundSearch n)
        {
            // The searched table is scanned by INDEX (element access), never a whole image — correctly NOT collected.
            if (n.AlsoVaried is { } av) SetT(av);
            foreach (var w in n.Whens) Cond(w.Condition);
            return false;
        }

        // ── leaves with no whole-group operand and no non-statement group-bearing part (nested statements, if any,
        //    are recursed by StatementChildren). Explicit so a NEW leaf is a compile error, never a silent miss. ──
        public bool Visit(BoundSequence n) => false;
        public bool Visit(BoundEcChecked n) => false;
        public bool Visit(BoundAddTo n) => false;
        public bool Visit(BoundAddGiving n) => false;
        public bool Visit(BoundSubtractFrom n) => false;
        public bool Visit(BoundSubtractGiving n) => false;
        public bool Visit(BoundMultiplyBy n) => false;
        public bool Visit(BoundMultiplyGiving n) => false;
        public bool Visit(BoundDivideInto n) => false;
        public bool Visit(BoundDivideGiving n) => false;
        public bool Visit(BoundDivideRemainder n) => false;
        public bool Visit(BoundCompute n) => false;
        public bool Visit(BoundComputeBoolean n) => false;
        public bool Visit(BoundUnsupported n) => false;
        public bool Visit(BoundStop n) => false;
        public bool Visit(BoundStopLiteral n) => false;
        public bool Visit(BoundGoTo n) => false;
        public bool Visit(BoundGoToAlterable n) => false;
        public bool Visit(BoundExitParagraph n) => false;
        public bool Visit(BoundExitPerform n) => false;
        public bool Visit(BoundExitProgram n) => false;
        public bool Visit(BoundNop n) => false;
        public bool Visit(BoundNextSentence n) => false;
        public bool Visit(BoundOpen n) => false;
        public bool Visit(BoundClose n) => false;
        public bool Visit(BoundUnlock n) => false;
        public bool Visit(BoundInitiate n) => false;
        public bool Visit(BoundGenerate n) => false;
        public bool Visit(BoundTerminate n) => false;
        public bool Visit(BoundAlter n) => false;
        public bool Visit(BoundSetSwitches n) => false;
        public bool Visit(BoundSort n) => false;
        public bool Visit(BoundMerge n) => false;
        public bool Visit(BoundTableSort n) => false;
        public bool Visit(BoundMethodReturn n) => false;
        public bool Visit(BoundRaise n) => false;
        public bool Visit(BoundRaiseObject n) => false;
        public bool Visit(BoundResume n) => false;
        public bool Visit(BoundSetLastException n) => false;
        public bool Visit(BoundSetObjectRef n) => false;
        public bool Visit(BoundSetPointer n) => false;
        public bool Visit(BoundSetAddressOfBased n) => false;
        public bool Visit(BoundSetPointerUpDown n) => false;
        public bool Visit(BoundSetCapacity n) => false;
        public bool Visit(BoundAllocate n) => false;
        public bool Visit(BoundFree n) => false;

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
                default: break;   // sign/boolean/switch condition — numeric/boolean operand, no group
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

        private void P(Place? place)
        {
            // An OCCURS DYNAMIC element access (data-model D9) stores the group as a typed element of a
            // CobolDynTable<T> — a whole-group MOVE into it round-trips through the TABLE codec, never the item-level
            // character-image mechanism. So the element group is NOT whole-image-referenced.
            if (place is DynTablePlace) return;
            if (place?.Item is { IsGroup: true } g) set.Add(g);
        }
    }
}
