// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;        // Place subtypes (RefModPlace, …), PicCategory, Usage
using CobolNet.Binding.Bound;
using CobolNet.CodeGen;
using CobolNet.Editions;
using CobolNet.Runtime;   // CobolPassMode (CALL argument passing mode)

namespace CobolNet.Validation;

/// <summary>
/// THE single post-bind edition-conformance pass (rearch PHASE-03 Step 14;
/// <c>docs/rearchitecture/DESIGN-version-conformance-pipeline.md</c> §2.4). It runs over the BOUND tree AFTER bind
/// and BEFORE emit and is the sole owner of edition gating: it re-identifies every version-gated construct from the
/// bound tree — a self-identifying bound-node TYPE, or a semantic ATTRIBUTE the binder already resolved onto the node
/// / <c>DataItem</c> / <c>FileModel</c> — and routes it through the ONE <see cref="ConstructRegistry.Check"/> funnel.
/// The binder is thereby edition-AGNOSTIC (zero <c>Check</c> calls of its own). No bound node carries a raw parse
/// context — the <c>BoundTree.cs</c> invariant stands; identity is the node's type + resolved facts, never a
/// <c>.Syntax</c> back-reference (the documented refinement of the design's §2.2/§6 provisions).
/// </summary>
/// <remarks>
/// TWO-ARM by design (§1 of the pipeline design): a BOUND-tree arm (here) walks every unit / class / factory /
/// method statement tree; a later PARSE-tree arm (Step 14e — absorbing <c>EditionValidator</c>) covers the
/// syntactic obsolete-element (FD / config / comment / segment) gates + the §8.9 reserved-word funnel, whose
/// constructs bind to NOTHING and so have no bound-node representation. Migration is incremental (Step 14b: the
/// self-identifying statement gates; 14c: attribute + flag statement gates; 14d: data / PICTURE / OO gates; 14e:
/// the parse-arm), each commit byte-identical — a <c>Check</c> fires exactly once, at its original binder site OR
/// here, never both.
/// </remarks>
internal sealed class VersionConformancePass
{
    private readonly EditionInfo _edition;
    private readonly IDiagnosticSink _sink;

    private VersionConformancePass(EditionInfo edition, IDiagnosticSink sink)
    {
        _edition = edition;
        _sink = sink;
    }

    /// <summary>Gate every version-gated construct in the bound <paramref name="group"/>, reporting to
    /// <paramref name="sink"/>. The driver runs this between bind and emit and HALTs before emit if the sink then
    /// carries errors (rearch exit criterion 9 — no codegen on an errored tree).</summary>
    public static void Run(CSharpEmitter.BoundRunUnit group, EditionInfo edition, IDiagnosticSink sink)
    {
        var pass = new VersionConformancePass(edition, sink);
        foreach (var unit in group.Units)
            pass.WalkProgram(unit.Bound);
        foreach (var cls in group.Classes)
        {
            // A class body's OBJECT and FACTORY halves are two bound programs over one class; both carry statements
            // (each METHOD-ID is a pc slice of the ONE dispatch space, so walking Paragraphs covers every method).
            pass.WalkProgram(cls.Bound);
            pass.WalkProgram(cls.FactoryBound);
        }
    }

    private void WalkProgram(BoundProgram? prog)
    {
        if (prog is null) return;
        foreach (var para in prog.Paragraphs)
            foreach (var stmt in para.Statements)
                WalkStatement(stmt);
    }

    private void WalkStatement(BoundStatement s)
    {
        GateStatement(s);
        Recurse(s);
    }

    private void WalkList(IReadOnlyList<BoundStatement>? list)
    {
        if (list is null) return;
        foreach (var s in list) WalkStatement(s);
    }

    private void Check(string constructId, string where)
        => ConstructRegistry.Check(_edition, _sink, constructId, where);

    // ── The bound-tree arm — statement-level edition gates ──────────────────────────────────────────────────
    // Step 14b: the constructs whose IDENTITY is the bound-node TYPE itself (a distinctive node the binder produced
    // only for that construct). Each Check keeps the exact constructId + where-string of its former binder site, so
    // the diagnostic text is byte-identical. (Attribute-conditioned + flag-backed statement gates land in 14c.)
    private void GateStatement(BoundStatement s)
    {
        switch (s)
        {
            case BoundUnlock:
                Check(Constructs.UnlockStatement2002, "the UNLOCK statement"); break;
            case BoundAllocate:
                Check(Constructs.Allocate2002, "the ALLOCATE statement"); break;
            case BoundFree:
                Check(Constructs.Free2002, "the FREE statement"); break;
            case BoundSetObjectRef:
                Check(Constructs.SetObjectReference2002, "the SET … TO object-reference statement (Format 5)"); break;
            case BoundSetPointerUpDown:
                Check(Constructs.PointerArithmetic2002, "SET pointer UP/DOWN BY (ISO §14.9.39 Format 10)"); break;
            case BoundAlter:
                Check(Constructs.AlterRemoved2002, "the ALTER statement"); break;
            case BoundKeyedDeleteFile:
                Check(Constructs.DeleteFile2023, "the DELETE FILE statement"); break;
            // SET ADDRESS OF (§14.9.39 Format 7) has two bound shapes from the one PtrBindSetAddress site: the
            // receiver form (SET ADDRESS OF x TO p) is a distinctive BoundSetAddressOfBased; the sender form
            // (SET p TO ADDRESS OF x) is a BoundSetPointer carrying an ADDRESS-OF source. Both gate identically.
            case BoundSetAddressOfBased:
            case BoundSetPointer { Address: not null }:
                Check(Constructs.SetAddress2002, "SET ADDRESS OF (ISO §14.9.39 Format 7)"); break;

            // ── Step 14c: gates conditioned on a resolved node ATTRIBUTE the binder already recorded ──────────
            case BoundOpen { SharingOverride: not null }:
                Check(Constructs.FileSharingClause2002, "the OPEN SHARING phrase"); break;
            case BoundGoback { ReturningSource: not null }:
                Check(Constructs.GobackReturning2002, "GOBACK … RETURNING"); break;
            case BoundCallProgram cp:
                // A user-defined FUNCTION reference lowers to a hoisted BoundCallProgram (IsFunction) carrying the
                // function name in LiteralName (§9.4 / §12.3.8, COBOL-2002). A regular CALL has IsFunction=false and
                // no function name, so these arms are mutually exclusive per node.
                if (cp.IsFunction)
                    Check(Constructs.UserFunctionInvocation2002, $"FUNCTION {cp.LiteralName?.ToUpperInvariant()}");
                // CALL … BY VALUE (§14.9.4). The binder fired once per explicit BY VALUE argument; the bound node
                // keeps each argument's pass mode, so gate once when the CALL uses value passing (the argument
                // list Any-check — the tested single-argument case is diagnostically identical).
                if (cp.Args.Any(a => a.Mode == CobolPassMode.Value))
                    Check(Constructs.CallByValue2002, "the CALL … BY VALUE phrase");
                // ON OVERFLOW spelling (the COBOL-74 synonym for ON EXCEPTION) — REMOVED at ISO 2023; gate AFTER
                // BY VALUE (the binder's order: args bind before the exception phrases).
                if (cp.UsedOverflowSpelling)
                    Check(Constructs.CallOnOverflowRemoved2023, "the CALL statement");
                break;
            case BoundStop { HasStatusPhrase: true }:
                Check(Constructs.StopRunStatus2002, "the STOP RUN … WITH NORMAL/ERROR STATUS phrase"); break;
            case BoundInvoke or BoundInvokeUniversal:
                Check(Constructs.Invoke2002, "the INVOKE statement"); break;
            case BoundAccept { HasEndTerminator: true }:
                Check(Constructs.EndAccept2002, "the ACCEPT statement"); break;
            case BoundMove mv:
                GateMove(mv); break;
            case BoundKeyedRead kr:
                // Two independent 2002 phrases on one READ; both gate, in the binder's order (§14.9.30).
                if (kr.Kind == KeyedReadKind.Previous)
                    Check(Constructs.ReadPrevious2002, "READ … PREVIOUS");
                if (kr.AdvancingOnLock)
                    Check(Constructs.RecordLockPhrase2002, "the READ … ADVANCING ON LOCK phrase");
                break;
            case BoundKeyedStart ks:
                // START FIRST/LAST positioning (§14.9.41) and the WITH LENGTH partial-key phrase — two independent
                // 2002 introductions; both gate, in the binder's order.
                if (ks.Mode is KeyedStartMode.First or KeyedStartMode.Last)
                    Check(Constructs.StartFirstLast2002, $"START {(ks.Mode == KeyedStartMode.Last ? "LAST" : "FIRST")}");
                if (ks.Length is not null)
                    Check(Constructs.StartWithLength2002, "the START … WITH LENGTH phrase");
                break;
        }
    }

    // ── MOVE figurative-constant category gates (ISO §14.9.25.3 SR5) ─────────────────────────────────────────
    // Genuinely SEMANTIC: which of the three edition rows applies depends on the source figurative × each
    // receiver's RESOLVED picture — re-derived here from the bound MOVE (Group B). Mirrors the binder's former
    // MoveFigurativeEditionGates classification EXACTLY (same figText, same per-target exemptions, same
    // integer/QUOTE/digit-only split, same where-string); the binder keeps only the SR1 class-index error (0809,
    // version-invariant) and the pre-removal StoreAsImage marking.
    private void GateMove(BoundMove m)
    {
        var all = m.Source as BoundAllLiteral;
        string figText = m.Source switch
        {
            BoundFigurative { Kind: 'S' } => "SPACE",
            BoundFigurative { Kind: 'Q' } => "QUOTE",
            BoundFigurative { Kind: 'H' } => "HIGH-VALUE",
            BoundFigurative { Kind: 'L' } => "LOW-VALUE",
            BoundAllLiteral a => $"ALL \"{a.Literal}\"",
            _ => string.Empty,
        };
        if (figText.Length == 0) return;   // not an alphanumeric-figurative / ALL source — SR5 does not reach it
        foreach (var t in m.Targets)
        {
            // Exemptions (§14.9.25.3 SR5): a ref-mod receiver (unique elementary alphanumeric), a group receiver
            // (a conversion-free character copy), a non-numeric receiver, or class index (SR1-errored in the binder).
            if (t is RefModPlace || t.Item.IsGroup || t.Item.Pic is not { } pic) continue;
            if (pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited)) continue;
            if (pic.Usage is Usage.Index) continue;
            string where = $"MOVE {figText} TO {t.Item.CobolName}";
            bool integerReceiver = pic is { Category: PicCategory.Numeric, IsFloat: false, Scale: <= 0 };
            if (all is { IsDigitOnly: true, Literal.Length: 1 } && integerReceiver)
                // SR5's surviving exception — valid everywhere, obsolete at 2023 (0903; SR5 NOTE / Annex F.2).
                Check(Constructs.MoveAllDigitIntegerObsolete2023, where);
            else if (m.Source is BoundFigurative { Kind: 'Q' })
                // QUOTE→numeric — obsolete 2014 (Annex E.2 item 21) then removed 2023 (dual-window row).
                Check(Constructs.MoveQuoteNumericObsolete2014, where);
            else
                // Every other shape — REMOVED by ISO 2023 (Annex E.2 item 1 bullet 1; 0902 — VCR row 1).
                Check(Constructs.MoveAlphanumericFigurativeRemoved2023, where);
        }
    }

    // ── The complete nested-statement traversal ─────────────────────────────────────────────────────────────
    // EVERY container that can hold a gated statement is descended, so a gate nested inside IF / EVALUATE / PERFORM
    // / SEARCH / an ON-phrase escapes nothing. Cross-checked against the binder's own traversals
    // (BoundStores.StoreKindOf + the phrase fields); a missed container would silently drop a nested gate. Leaves
    // (the default arm) yield no children.
    private void Recurse(BoundStatement s)
    {
        switch (s)
        {
            case BoundSequence x: WalkList(x.Steps); break;
            case BoundEcChecked x: WalkStatement(x.Inner); break;
            case BoundIf x: WalkList(x.Then); WalkList(x.Else); break;
            case BoundEvaluate x:
                foreach (var w in x.Whens) WalkList(w.Statements);
                WalkList(x.Other); break;
            case BoundInlinePerform x: WalkList(x.Body); break;
            case BoundAddTo x: WalkSizeErr(x.SizeError); break;
            case BoundAddGiving x: WalkSizeErr(x.SizeError); break;
            case BoundSubtractFrom x: WalkSizeErr(x.SizeError); break;
            case BoundSubtractGiving x: WalkSizeErr(x.SizeError); break;
            case BoundMultiplyBy x: WalkSizeErr(x.SizeError); break;
            case BoundMultiplyGiving x: WalkSizeErr(x.SizeError); break;
            case BoundDivideInto x: WalkSizeErr(x.SizeError); break;
            case BoundDivideGiving x: WalkSizeErr(x.SizeError); break;
            case BoundDivideRemainder x: WalkSizeErr(x.SizeError); break;
            case BoundCompute x: WalkSizeErr(x.SizeError); break;
            case BoundCorresponding x: WalkSizeErr(x.SizeError); break;
            case BoundSearch x:
                WalkList(x.AtEnd);
                foreach (var w in x.Whens) WalkList(w.Statements); break;
            case BoundStringStmt x: WalkList(x.OnOverflow); WalkList(x.NotOnOverflow); break;
            case BoundUnstringStmt x: WalkList(x.OnOverflow); WalkList(x.NotOnOverflow); break;
            case BoundWrite x: WalkList(x.AtEop); WalkList(x.NotAtEop); break;
            case BoundRead x: WalkList(x.AtEnd); WalkList(x.NotAtEnd); break;
            case BoundKeyedRead x:
                WalkList(x.AtEnd); WalkList(x.NotAtEnd);
                WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedWrite x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedRewrite x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedDelete x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedStart x: WalkList(x.InvalidKey?.Invalid); WalkList(x.InvalidKey?.NotInvalid); break;
            case BoundKeyedDeleteFile x: WalkList(x.OnException); WalkList(x.NotOnException); break;
            case BoundReturn x: WalkList(x.AtEnd); WalkList(x.NotAtEnd); break;
            case BoundCallProgram x: WalkList(x.OnException); WalkList(x.NotOnException); break;
            default: break;   // a leaf statement — no nested statements
        }
    }

    private void WalkSizeErr(SizeErrorPhrase? p)
    {
        if (p is null) return;
        WalkList(p.OnError);
        WalkList(p.NotOnError);
    }
}
