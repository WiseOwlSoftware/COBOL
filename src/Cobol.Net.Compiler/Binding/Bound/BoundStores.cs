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
/// 119 nodes, 46 store-bearing; every store field verified against the emitter), and an UNRECOGNIZED node
/// returns <see langword="null"/> so the caller stages LOUD instead of guessing (§1.4 — never silent).
/// </summary>
/// <remarks>
/// Scope facts that keep this walk small and honest:
/// - Only PLACE stores can hit a property temp. Non-Place stores (generated index/ALTER fields, external
///   switches, FILE STATUS via <c>FileModel</c>, report-engine counters, run-unit exception state,
///   <c>BoundTableSort.ArrayPath</c>) can never target the temp — a temp is a synthesized simple local
///   (and a property subject cannot carry OCCURS, COBOLNET0842), so those arms are <c>false</c> here.
/// - <see cref="Place.Item"/> is identity-compared: every Place wrapper (RefMod/alias/view) forwards
///   <c>Item</c> to the underlying item, so a windowed write through <c>RefModPlace</c> still classifies.
/// - Child-statement recursion covers every conditional-phrase body (SIZE ERROR / AT END / INVALID KEY /
///   ON EXCEPTION / ON OVERFLOW / AT EOP), IF/EVALUATE arms, inline-PERFORM bodies, SEARCH arms,
///   <c>BoundEcChecked.Inner</c> and <c>BoundSequence.Steps</c>. Out-of-line PERFORM bodies are pc ranges
///   (not nested statements) — a property temp is statement-local by construction, so a store to it can
///   only occur in the statement that carries it.
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
    /// classification; <see langword="null"/> = the walk met a statement type outside the classified
    /// taxonomy (stage loud, never guess). A property temp occurs at exactly ONE Place in the tree, so the
    /// first store found is total.</summary>
    public static StoreKind? StoreKindOf(BoundStatement s, DataItem item)
    {
        bool Hit(Place? p) => p is not null && ReferenceEquals(p.Item, item);
        bool TargetHit(BoundSetTarget? t) => t is SetPlaceTarget sp && Hit(sp.Place);
        bool ReceiversHit(IReadOnlyList<Receiver> rs) => rs.Any(r => Hit(r.Place));

        // Aggregate child-statement lists: a found store dominates (the temp occurs exactly once);
        // otherwise an unknown poisons the result.
        StoreKind? Kids(params IEnumerable<BoundStatement>?[] lists)
        {
            bool sawUnknown = false;
            foreach (var list in lists)
                foreach (var child in list ?? [])
                    switch (StoreKindOf(child, item))
                    {
                        case StoreKind.Write: return StoreKind.Write;
                        case StoreKind.ReadWrite: return StoreKind.ReadWrite;
                        case null: sawUnknown = true; break;
                    }
            return sawUnknown ? null : StoreKind.None;
        }

        StoreKind? StoreOrKids(bool stored, StoreKind kind, params IEnumerable<BoundStatement>?[] lists)
            => stored ? kind : Kids(lists);

        static bool InitStores(IReadOnlyList<InitializeAction> actions, DataItem item)
        {
            foreach (var a in actions)
                switch (a)
                {
                    case InitializeStore st when st.Target.Item == item: return true;
                    case InitializeLoop lp when InitStores(lp.Body, item): return true;
                }
            return false;
        }

        return s switch
        {
            // ── Pure control / read-only / model-routed statements (survey "pure" list) ─────────────────
            BoundUnsupported or BoundStop or BoundStopLiteral or BoundDisplay or BoundGoTo
                or BoundGoToDepending or BoundExitParagraph or BoundExitPerform or BoundNop
                or BoundNextSentence or BoundOpen or BoundClose or BoundInitiate or BoundGenerate
                or BoundTerminate or BoundRaise or BoundResume or BoundSetLastException
                or BoundGoToAlterable or BoundCancel or BoundExitProgram or BoundGoback
                or BoundMethodReturn or BoundAlter or BoundSetSwitches or BoundKeyedDelete
                or BoundSort or BoundMerge or BoundTableSort or BoundUnlock
                => StoreKind.None,

            // ── Wrappers / containers ───────────────────────────────────────────────────────────────────
            BoundSequence seq => Kids(seq.Steps),
            BoundEcChecked ec => StoreKindOf(ec.Inner, item),
            BoundIf f => Kids(f.Then, f.Else),
            BoundEvaluate ev => Kids([.. ev.Whens.SelectMany(w => w.Statements)], ev.Other),
            BoundInlinePerform ip => StoreOrKids(
                ip.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var)),
                StoreKind.ReadWrite, ip.Body),   // induction var: init + augment (GR12/GR13)
            BoundOutOfLinePerform op =>
                op.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var))
                    ? StoreKind.ReadWrite : StoreKind.None,

            // ── Data movement / arithmetic (polarity per the survey: in-place vs WRITE-only) ────────────
            BoundMove mv => mv.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None,
            BoundAddTo a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.ReadWrite, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundAddGiving a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.Write, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundSubtractFrom a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.ReadWrite, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundSubtractGiving a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.Write, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundMultiplyBy a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.ReadWrite, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundMultiplyGiving a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.Write, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundDivideInto a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.ReadWrite, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundDivideGiving a => StoreOrKids(ReceiversHit(a.Targets), StoreKind.Write, a.SizeError?.OnError, a.SizeError?.NotOnError),
            BoundDivideRemainder d => StoreOrKids(Hit(d.Quotient.Place) || Hit(d.Remainder), StoreKind.Write,
                d.SizeError?.OnError, d.SizeError?.NotOnError),
            BoundCompute c => StoreOrKids(ReceiversHit(c.Targets), StoreKind.Write, c.SizeError?.OnError, c.SizeError?.NotOnError),
            BoundComputeBoolean cb => cb.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None,   // §14.9.8 F2 — no size-error phrase
            BoundCorresponding co => StoreOrKids(co.Pairs.Any(p => Hit(p.Target)),
                co.Verb == CorrVerb.Move ? StoreKind.Write : StoreKind.ReadWrite,
                co.SizeError?.OnError, co.SizeError?.NotOnError),
            BoundInitialize ini => InitStores(ini.Actions, item) ? StoreKind.Write : StoreKind.None,

            // ── SET family ──────────────────────────────────────────────────────────────────────────────
            BoundSetConditions sc => sc.Sets.Any(x => Hit(x.Parent)) ? StoreKind.Write : StoreKind.None,
            BoundSetTo st => st.Targets.Any(TargetHit) ? StoreKind.Write : StoreKind.None,
            BoundSetUpDown su => su.Targets.Any(TargetHit) ? StoreKind.ReadWrite : StoreKind.None,

            // ── SEARCH ──────────────────────────────────────────────────────────────────────────────────
            BoundSearch se => StoreOrKids(TargetHit(se.AlsoVaried), StoreKind.ReadWrite,
                se.AtEnd, [.. se.Whens.SelectMany(w => w.Statements)]),

            // ── ACCEPT / STRING / UNSTRING / INSPECT ────────────────────────────────────────────────────
            BoundAccept ac => Hit(ac.Target) ? StoreKind.Write : StoreKind.None,
            BoundStringStmt ss => StoreOrKids(Hit(ss.Into) || Hit(ss.Pointer),
                StoreKind.ReadWrite,   // Into: GR7 read-modify-write; Pointer: GR4 read + GR8 writeback
                ss.OnOverflow, ss.NotOnOverflow),
            BoundUnstringStmt us =>
                Hit(us.Pointer) || Hit(us.Tallying)
                    ? StoreKind.ReadWrite                                   // GR11a/GR13 + GR14 read-then-add
                    : StoreOrKids(us.Receivers.Any(r => Hit(r.Target) || Hit(r.DelimiterIn) || Hit(r.CountIn)),
                        StoreKind.Write, us.OnOverflow, us.NotOnOverflow),  // GR11c/d/e pure stores
            BoundInspect ins =>
                (ins.Replacing.Count > 0 || ins.Converting is not null) && Hit(ins.Target)
                    ? StoreKind.ReadWrite                                   // image read, modified, stored
                    : ins.Tallying.Any(tl => Hit(tl.Counter))
                        ? StoreKind.ReadWrite                               // GR11 counter accumulate
                        : StoreKind.None,

            // ── File I/O (FILE STATUS / record-area stores route through FileModel — never the temp) ────
            BoundWrite wr => StoreOrKids(wr.From is not null && Hit(wr.Record),
                StoreKind.ReadWrite, wr.AtEop, wr.NotAtEop),               // FROM-move then read as the image
            BoundRead rd => StoreOrKids(Hit(rd.Into), StoreKind.Write, rd.AtEnd, rd.NotAtEnd),
            BoundRewrite rw => rw.From is not null && Hit(rw.Record) ? StoreKind.ReadWrite : StoreKind.None,
            BoundKeyedRead kr => StoreOrKids(Hit(kr.Into), StoreKind.Write,
                kr.AtEnd, kr.NotAtEnd, kr.InvalidKey?.Invalid, kr.InvalidKey?.NotInvalid),
            BoundKeyedWrite kw => StoreOrKids(kw.From is not null && Hit(kw.Record), StoreKind.ReadWrite,
                kw.InvalidKey?.Invalid, kw.InvalidKey?.NotInvalid),
            BoundKeyedRewrite krw => StoreOrKids(krw.From is not null && Hit(krw.Record), StoreKind.ReadWrite,
                krw.InvalidKey?.Invalid, krw.InvalidKey?.NotInvalid),
            BoundKeyedDeleteFile kdf => Kids(kdf.OnException, kdf.NotOnException),
            BoundKeyedStart ks => Kids(ks.InvalidKey?.Invalid, ks.InvalidKey?.NotInvalid),
            BoundRelease rl => rl.From is not null && Hit(rl.Record) ? StoreKind.ReadWrite : StoreKind.None,
            BoundReturn rt => StoreOrKids(Hit(rt.RecordArea) || Hit(rt.Into) || Hit(rt.Varying?.Depending),
                Hit(rt.RecordArea) ? StoreKind.ReadWrite : StoreKind.Write,  // area: stored then INTO-source
                rt.AtEnd, rt.NotAtEnd),

            // ── CALL / INVOKE (BY REFERENCE crossings: copy-in + writeback = ReadWrite) ─────────────────
            BoundCallProgram cp =>
                cp.Args.Any(a => a.Mode == CobolPassMode.Reference && Hit(a.Place))
                    ? StoreKind.ReadWrite
                    : StoreOrKids(Hit(cp.Returning), StoreKind.Write, cp.OnException, cp.NotOnException),
            BoundInvoke inv =>
                (inv.Args?.Any(a => a.WriteBack && Hit(a.Source)) ?? false) ? StoreKind.ReadWrite
                : Hit(inv.Returning) ? StoreKind.Write
                : StoreKind.None,

            // Anything else: outside the classified taxonomy — the caller stages LOUD (never guess).
            _ => null,
        };
    }
}
