// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using CobolNet.Runtime;

namespace CobolNet.Binding.Bound;

/// <summary>
/// The bound-tree STORE analysis: does a statement write through a Place rooted at a given
/// <see cref="DataItem"/>? First client: the object-property reference desugar (ISO §8.4.3.9.4 GR1–GR3,
/// deep-dive D-P2) — a reference is a RECEIVING occurrence exactly when its synthesized temp is stored by
/// the statement it desugared out of (GR2/GR3), so the SET accessor is invoked only then; a misclassified
/// "sending" would skip a required SET (silently lost store), a misclassified "receiving" would run a
/// side-effecting SET the spec says is not invoked — hence the walk is a TOTAL explicit taxonomy over every
/// <c>BoundStatement</c> (the 2026-07-04 15-agent survey, scratchpad <c>bound_stores_classification.md</c>:
/// 119 nodes, 46 store-bearing; every store field verified against the emitter). It is the exhaustive
/// generated <see cref="IBoundStatementVisitor{T}"/> (PHASE-07 Step 6c), so a new bound-statement leaf is a
/// COMPILE error here; the nine leaves outside the classified taxonomy return <see langword="null"/> so the
/// caller stages LOUD instead of guessing (§1.4 — never silent).
/// </summary>
/// <remarks>
/// Scope facts that keep this walk small and honest:
/// - Only PLACE stores can hit a property temp. Non-Place stores (generated index/ALTER fields, external
///   switches, FILE STATUS via <c>FileModel</c>, report-engine counters, run-unit exception state,
///   <c>BoundTableSort.ArrayPath</c>) can never target the temp — a temp is a synthesized simple local
///   (and a property subject cannot carry OCCURS, COBOLNET0842), so those arms are <c>None</c> here.
/// - <see cref="Place.Item"/> is identity-compared: every Place wrapper (RefMod/alias/view) forwards
///   <c>Item</c> to the underlying item, so a windowed write through <c>RefModPlace</c> still classifies.
/// - The child-statement recursion (<c>Kids</c>) is DEFENSIVELY total over conditional-phrase bodies (SIZE
///   ERROR / AT END / INVALID KEY / ON EXCEPTION / ON OVERFLOW / AT EOP), IF/EVALUATE arms, inline-PERFORM
///   bodies, SEARCH arms, <c>BoundEcChecked.Inner</c> and <c>BoundSequence.Steps</c>. In practice a property
///   temp is statement-local: every nested statement is bound through its OWN <c>BindStatement</c> and so
///   drains its own property ops (<c>StatementBinder.BindStatement</c>), so the temp lives in the carrying
///   statement's DIRECT operands/condition, never a separately-wrapped nested body — which is why arms that
///   omit the recursion (e.g. <c>BoundKeyedDelete</c>) are equivalent, not buggy. Out-of-line PERFORM bodies
///   are pc ranges (not nested statements), so they are never walked.
/// </remarks>
/// <summary>How a statement touches a given item through its STORE positions (the polarity that selects the
/// §8.4.3.9.4 general rule): <see cref="None"/> → the occurrence is purely SENDING (GR1, get only);
/// <see cref="Write"/> → a WRITE-only receiving position (GR2, set only — the get method is NOT invoked);
/// <see cref="ReadWrite"/> → an in-place read-modify-write position (GR3, get before + set after).</summary>
public enum StoreKind { None, Write, ReadWrite }

public static class BoundStores
{
    /// <summary>The store polarity of <paramref name="item"/> in <paramref name="s"/> —
    /// <see cref="StoreKind.None"/> = provably never stored (a pure sending occurrence);
    /// <see cref="StoreKind.Write"/> / <see cref="StoreKind.ReadWrite"/> per the emitter-verified
    /// classification; <see langword="null"/> = a statement type outside the classified taxonomy (stage
    /// loud, never guess). A property temp occurs at exactly ONE Place in the tree, so the first store
    /// found is total.</summary>
    public static StoreKind? StoreKindOf(BoundStatement s, DataItem item) => s.Accept(new StoreKindVisitor(item));

    /// <summary>The per-node store classification (the former <c>StoreKindOf</c> switch, one arm per leaf) —
    /// the exhaustive <see cref="IBoundStatementVisitor{T}"/> over the bound statements, carrying the target
    /// <paramref name="item"/> so recursion is <c>child.Accept(this)</c>.</summary>
    private sealed class StoreKindVisitor(DataItem item) : IBoundStatementVisitor<StoreKind?>
    {
        private bool Hit(Place? p) => p is not null && ReferenceEquals(p.Item, item);
        private bool TargetHit(BoundSetTarget? t) => t is SetPlaceTarget sp && Hit(sp.Place);
        private bool ReceiversHit(IReadOnlyList<Receiver> rs) => rs.Any(r => Hit(r.Place));

        // Aggregate child-statement lists: a found store dominates (the temp occurs exactly once);
        // otherwise an unknown poisons the result.
        private StoreKind? Kids(params IEnumerable<BoundStatement>?[] lists)
        {
            bool sawUnknown = false;
            foreach (var list in lists)
                foreach (var child in list ?? [])
                    switch (child.Accept(this))
                    {
                        case StoreKind.Write: return StoreKind.Write;
                        case StoreKind.ReadWrite: return StoreKind.ReadWrite;
                        case null: sawUnknown = true; break;
                    }
            return sawUnknown ? null : StoreKind.None;
        }

        private StoreKind? StoreOrKids(bool stored, StoreKind kind, params IEnumerable<BoundStatement>?[] lists)
            => stored ? kind : Kids(lists);

        private bool InitStores(IReadOnlyList<InitializeAction> actions)
        {
            foreach (var a in actions)
                switch (a)
                {
                    case InitializeStore st when st.Target.Item == item: return true;
                    case InitializeLoop lp when InitStores(lp.Body): return true;
                }
            return false;
        }

        // ── Pure control / read-only / model-routed statements (survey "pure" list) ─────────────────────
        public StoreKind? Visit(BoundUnsupported n) => StoreKind.None;
        public StoreKind? Visit(BoundStop n) => StoreKind.None;
        public StoreKind? Visit(BoundStopLiteral n) => StoreKind.None;
        public StoreKind? Visit(BoundDisplay n) => StoreKind.None;
        public StoreKind? Visit(BoundGoTo n) => StoreKind.None;
        public StoreKind? Visit(BoundGoToDepending n) => StoreKind.None;
        public StoreKind? Visit(BoundExitParagraph n) => StoreKind.None;
        public StoreKind? Visit(BoundExitPerform n) => StoreKind.None;
        public StoreKind? Visit(BoundNop n) => StoreKind.None;
        public StoreKind? Visit(BoundNextSentence n) => StoreKind.None;
        public StoreKind? Visit(BoundOpen n) => StoreKind.None;
        public StoreKind? Visit(BoundClose n) => StoreKind.None;
        public StoreKind? Visit(BoundInitiate n) => StoreKind.None;
        public StoreKind? Visit(BoundGenerate n) => StoreKind.None;
        public StoreKind? Visit(BoundTerminate n) => StoreKind.None;
        public StoreKind? Visit(BoundRaise n) => StoreKind.None;
        public StoreKind? Visit(BoundResume n) => StoreKind.None;
        public StoreKind? Visit(BoundSetLastException n) => StoreKind.None;
        public StoreKind? Visit(BoundGoToAlterable n) => StoreKind.None;
        public StoreKind? Visit(BoundCancel n) => StoreKind.None;
        public StoreKind? Visit(BoundExitProgram n) => StoreKind.None;
        public StoreKind? Visit(BoundGoback n) => StoreKind.None;
        public StoreKind? Visit(BoundMethodReturn n) => StoreKind.None;
        public StoreKind? Visit(BoundAlter n) => StoreKind.None;
        public StoreKind? Visit(BoundSetSwitches n) => StoreKind.None;
        public StoreKind? Visit(BoundKeyedDelete n) => StoreKind.None;   // no data receiver; its INVALID KEY body is separately wrapped
        public StoreKind? Visit(BoundSort n) => StoreKind.None;
        public StoreKind? Visit(BoundMerge n) => StoreKind.None;
        public StoreKind? Visit(BoundTableSort n) => StoreKind.None;
        public StoreKind? Visit(BoundUnlock n) => StoreKind.None;

        // ── Wrappers / containers ───────────────────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundSequence n) => Kids(n.Steps);
        public StoreKind? Visit(BoundEcChecked n) => n.Inner.Accept(this);
        public StoreKind? Visit(BoundIf n) => Kids(n.Then, n.Else);
        public StoreKind? Visit(BoundEvaluate n) => Kids([.. n.Whens.SelectMany(w => w.Statements)], n.Other);
        public StoreKind? Visit(BoundInlinePerform n) => StoreOrKids(
            n.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var)),
            StoreKind.ReadWrite, n.Body);   // induction var: init + augment (GR12/GR13)
        public StoreKind? Visit(BoundOutOfLinePerform n) =>
            n.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var))
                ? StoreKind.ReadWrite : StoreKind.None;

        // ── Data movement / arithmetic (polarity per the survey: in-place vs WRITE-only) ────────────────
        public StoreKind? Visit(BoundMove n) => n.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundAddTo n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundAddGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundSubtractFrom n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundSubtractGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundMultiplyBy n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundMultiplyGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundDivideInto n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundDivideGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundDivideRemainder n) => StoreOrKids(Hit(n.Quotient.Place) || Hit(n.Remainder), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundCompute n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundComputeBoolean n) => n.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None;   // §14.9.8 F2 — no size-error phrase
        public StoreKind? Visit(BoundCorresponding n) => StoreOrKids(n.Pairs.Any(p => Hit(p.Target)),
            n.Verb == CorrVerb.Move ? StoreKind.Write : StoreKind.ReadWrite,
            n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind? Visit(BoundInitialize n) => InitStores(n.Actions) ? StoreKind.Write : StoreKind.None;

        // ── SET family ──────────────────────────────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundSetConditions n) => n.Sets.Any(x => Hit(x.Parent)) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundSetTo n) => n.Targets.Any(TargetHit) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundSetUpDown n) => n.Targets.Any(TargetHit) ? StoreKind.ReadWrite : StoreKind.None;

        // ── SEARCH ──────────────────────────────────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundSearch n) => StoreOrKids(TargetHit(n.AlsoVaried), StoreKind.ReadWrite,
            n.AtEnd, [.. n.Whens.SelectMany(w => w.Statements)]);

        // ── ACCEPT / STRING / UNSTRING / INSPECT ────────────────────────────────────────────────────────
        public StoreKind? Visit(BoundAccept n) => Hit(n.Target) ? StoreKind.Write : StoreKind.None;
        public StoreKind? Visit(BoundStringStmt n) => StoreOrKids(Hit(n.Into) || Hit(n.Pointer),
            StoreKind.ReadWrite,   // Into: GR7 read-modify-write; Pointer: GR4 read + GR8 writeback
            n.OnOverflow, n.NotOnOverflow);
        public StoreKind? Visit(BoundUnstringStmt n) =>
            Hit(n.Pointer) || Hit(n.Tallying)
                ? StoreKind.ReadWrite                                   // GR11a/GR13 + GR14 read-then-add
                : StoreOrKids(n.Receivers.Any(r => Hit(r.Target) || Hit(r.DelimiterIn) || Hit(r.CountIn)),
                    StoreKind.Write, n.OnOverflow, n.NotOnOverflow);    // GR11c/d/e pure stores
        public StoreKind? Visit(BoundInspect n) =>
            (n.Replacing.Count > 0 || n.Converting is not null) && Hit(n.Target)
                ? StoreKind.ReadWrite                                   // image read, modified, stored
                : n.Tallying.Any(tl => Hit(tl.Counter))
                    ? StoreKind.ReadWrite                               // GR11 counter accumulate
                    : StoreKind.None;

        // ── File I/O (FILE STATUS / record-area stores route through FileModel — never the temp) ─────────
        public StoreKind? Visit(BoundWrite n) => StoreOrKids(n.From is not null && Hit(n.Record),
            StoreKind.ReadWrite, n.AtEop, n.NotAtEop);                 // FROM-move then read as the image
        public StoreKind? Visit(BoundRead n) => StoreOrKids(Hit(n.Into), StoreKind.Write, n.AtEnd, n.NotAtEnd);
        public StoreKind? Visit(BoundRewrite n) => n.From is not null && Hit(n.Record) ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind? Visit(BoundKeyedRead n) => StoreOrKids(Hit(n.Into), StoreKind.Write,
            n.AtEnd, n.NotAtEnd, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundKeyedWrite n) => StoreOrKids(n.From is not null && Hit(n.Record), StoreKind.ReadWrite,
            n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundKeyedRewrite n) => StoreOrKids(n.From is not null && Hit(n.Record), StoreKind.ReadWrite,
            n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundKeyedDeleteFile n) => Kids(n.OnException, n.NotOnException);
        public StoreKind? Visit(BoundKeyedStart n) => Kids(n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind? Visit(BoundRelease n) => n.From is not null && Hit(n.Record) ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind? Visit(BoundReturn n) => StoreOrKids(Hit(n.RecordArea) || Hit(n.Into) || Hit(n.Varying?.Depending),
            Hit(n.RecordArea) ? StoreKind.ReadWrite : StoreKind.Write,  // area: stored then INTO-source
            n.AtEnd, n.NotAtEnd);

        // ── CALL / INVOKE (BY REFERENCE crossings: copy-in + writeback = ReadWrite) ──────────────────────
        public StoreKind? Visit(BoundCallProgram n) =>
            n.Args.Any(a => a.Mode == CobolPassMode.Reference && Hit(a.Place))
                ? StoreKind.ReadWrite
                : StoreOrKids(Hit(n.Returning), StoreKind.Write, n.OnException, n.NotOnException);
        public StoreKind? Visit(BoundInvoke n) =>
            (n.Args?.Any(a => a.WriteBack && Hit(a.Source)) ?? false) ? StoreKind.ReadWrite
            : Hit(n.Returning) ? StoreKind.Write
            : StoreKind.None;

        // ── Outside the classified taxonomy — return null so the caller stages LOUD (never guess). These
        //    were the former `_ => null` catch-all; now explicit so a NEW leaf cannot silently join them. ─
        public StoreKind? Visit(BoundAllocate n) => null;
        public StoreKind? Visit(BoundFree n) => null;
        public StoreKind? Visit(BoundInvokeUniversal n) => null;
        public StoreKind? Visit(BoundRaiseObject n) => null;
        public StoreKind? Visit(BoundSetAddressOfBased n) => null;
        public StoreKind? Visit(BoundSetCapacity n) => null;
        public StoreKind? Visit(BoundSetObjectRef n) => null;
        public StoreKind? Visit(BoundSetPointer n) => null;
        public StoreKind? Visit(BoundSetPointerUpDown n) => null;
    }
}
