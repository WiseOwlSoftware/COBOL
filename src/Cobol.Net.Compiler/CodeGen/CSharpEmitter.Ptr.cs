// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The data-pointer statement emitters (Phase-4b increment 2 — ISO §14.9.39 Formats 7/10, §14.9.3, §14.9.15;
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Every pointer VALUE renders onto
/// the ONE <c>ManagedPointer</c> carrier; every runtime rule lives in <c>CobolPtr</c> (Deref/UpBy/Allocate/
/// Free) — the emitters only wire places to helpers.
/// </summary>
public sealed partial class CSharpEmitter
{

    /// <summary>The C# expression for an <c>ADDRESS OF identifier</c> value (ISO §8.4.3.11 GR1): a BASED
    /// item's value IS its implicit data-address pointer (§8.6.5 :8791); a cell-forced record renders a
    /// window pointer at the item's class offset; an EXTERNAL record's cell is the run-unit
    /// <c>ExternalStore</c> cell (the increment-2 unification — no special case). The bind pass guaranteed
    /// cell-backing, so the fall-through is a loud guard, never silence.</summary>
    private string PtrAddressOfText(BoundAddressOf a)
    {
        var item = a.Item;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        if (root.Class is not { } cls)
            return LoudValue("ManagedPointer", $"ADDRESS OF '{item.CobolName}' without cell storage");
        if (cls.BasedPointerField is { } addr)
            // The based item itself reads its implicit pointer (§8.6.5 :8791); a SUBORDINATE's address is
            // the base displaced by its class offset (§8.4.3.11 GR1 — the address OF THE ITEM; the review's
            // ClassOffset-drop finding). UpBy's GR18 null trap is the right posture: taking the address of a
            // child of an unallocated based record has no address to take.
            return item.ClassOffset == 0 && ReferenceEquals(item, root)
                ? addr
                : $"CobolPtr.UpBy({addr}, {item.ClassOffset})";
        if (_ctx.Data.PtrAddressableCellOf.TryGetValue(cls, out var cell))
            return $"ManagedPointer.At({cell}, {item.ClassOffset})";
        if (_ctx.Data.CallExternalBackings.FirstOrDefault(b => b.BackingCsName == cls.BackingCsName) is { } ext)
            return $"ManagedPointer.At(ExternalStore.Cell({CsLiteral(ext.ExternalName)}, "
                + $"{CsLiteral(ext.InitImage)}), {item.ClassOffset})";
        return LoudValue("ManagedPointer", $"ADDRESS OF '{item.CobolName}' — unrecognized cell backing");
    }

    /// <summary><c>SET ADDRESS OF based TO pointer</c> (ISO §14.9.39 F7 GR12–13): assign the address VALUE —
    /// a snapshot, never live tracking.</summary>
    private void PtrEmitSetAddressOfBased(BoundSetAddressOfBased s)
    {
        if (s.Based.Class?.BasedPointerField is not { } addr)
        {
            _ctx.Writer.Line(LoudStmt($"SET ADDRESS OF '{s.Based.CobolName}' — the based item has no pointer bridge "
                + $"({s.Based.Class?.RejectReason ?? "unclassified"})"));
            return;
        }
        _ctx.Writer.Line($"{addr} = {s.Source.Read()};   // SET ADDRESS OF (ISO §14.9.39 F7 GR12-13 — a snapshot)");
    }

    /// <summary><c>SET pointer… {UP|DOWN} BY n</c> (ISO §14.9.39 Format 10): the amount evaluates ONCE, then
    /// each pointer moves by n bytes (GR20; character positions in this model). NULL → EC-DATA-PTR-NULL
    /// inside <c>CobolPtr.UpBy</c> (GR18). A SCALED amount keeps its fraction into <c>UpByScaled</c>, whose
    /// divisibility test realizes GR19 EXACTLY (a non-integer VALUE → EC-SIZE-ADDRESS fatal; 2.0 moves by 2 —
    /// never the silent Align-truncation the review caught).</summary>
    private void PtrEmitSetPointerUpDown(BoundSetPointerUpDown s)
    {
        var w = _ctx.Writer;
        NumX x = _num.Render(s.Amount, ReceiverContext.None);
        string tmp = $"__ptrBy{_ctx.Names.NextPtr()}";
        w.Line($"long {tmp} = (long)({x.Expr});");
        string call = x.Scale == 0
            ? $"CobolPtr.UpBy({{0}}, {(s.Down ? $"-{tmp}" : tmp)})"
            : $"CobolPtr.UpByScaled({{0}}, {(s.Down ? $"-{tmp}" : tmp)}, {x.Scale})";
        foreach (var t in s.Targets)
            w.Line(t.Write(string.Format(call, t.Read()))
                + "   // SET pointer UP/DOWN BY (ISO §14.9.39 F10 GR19/GR20)");
    }

    /// <summary>ALLOCATE (ISO §14.9.3). Form 1: a fresh cell of ⌈expr⌉ characters (GR1 — a fractional request
    /// rounds UP; GR2 — ≤0 yields NULL, no EC) delivered to RETURNING; INITIALIZED = binary-zero fill (GR6).
    /// Form 2: the BASED item's implicit pointer takes a cell of its template width (GR3/GR4a), RETURNING
    /// also set when present (GR4b).</summary>
    private void PtrEmitAllocate(BoundAllocate s)
    {
        var w = _ctx.Writer;
        if (s.Based is { } based)
        {
            if (based.Class?.BasedPointerField is not { } addr || based.Class is not { } cls)
            {
                w.Line(LoudStmt($"ALLOCATE '{based.CobolName}' — the based item has no pointer bridge "
                    + $"({based.Class?.RejectReason ?? "unclassified"})"));
                return;
            }
            w.Line($"{addr} = CobolPtr.Allocate({cls.Width});   // ALLOCATE based-item (ISO §14.9.3 GR3/GR4a)");
            if (s.Returning is { } ret2)
                w.Line(ret2.Write(addr) + "   // GR4b — the RETURNING pointer also receives the address");
            return;
        }
        NumX x = _num.Render(s.Chars!, ReceiverContext.None);
        string size = x.Scale == 0
            ? $"(long)({x.Expr})"
            : $"(long)CobolNum.Rescale({x.Expr}, {x.Scale}, 0, CobolRounding.AwayFromZero)";   // GR1 — round UP
        w.Line(s.Returning!.Write($"CobolPtr.Allocate({size}{(s.Initialized ? ", zeroFill: true" : "")})")
            + "   // ALLOCATE n CHARACTERS (ISO §14.9.3 GR1/GR2" + (s.Initialized ? "/GR6" : "") + ")");
    }

    /// <summary>FREE (ISO §14.9.15 GR1/GR2 — per operand, left to right): the helper runs the three-way;
    /// the nonfatal EC-STORAGE-NOT-ALLOC (GR1c) reports through the TurnState-gated status block ONLY when
    /// its checking is enabled (§14.6.13.1.4 — an unchecked nonfatal condition is not raised).</summary>
    private void PtrEmitFree(BoundFree s)
    {
        var w = _ctx.Writer;
        bool checkNotAlloc = _ecInfo?.Enabled.Any(e => e.Ec == "EC-STORAGE-NOT-ALLOC") == true;
        foreach (var op in s.Operands)
        {
            string na = $"__notAlloc{_ctx.Names.NextPtr()}";
            w.Line($"bool {na};");
            w.Line(op.Write($"CobolPtr.Free({op.Read()}, out {na})") + "   // FREE (ISO §14.9.15 GR1)");
            if (checkNotAlloc)
            {
                var (stmt, loc) = EcStmtLoc(_ecInfo!);
                using (w.Block($"if ({na})"))
                {
                    w.Line($"ExceptionState.Set(\"EC-STORAGE-NOT-ALLOC\", false, {stmt}, {loc});   // GR1c — nonfatal (§14.6.13.1.1)");
                    // §14.6.13.1.3 #5: the F3 selection runs; a nonfatal condition with no handler (or
                    // RESUME NEXT / a completed declarative) simply continues (the review finding — the
                    // status set alone never consulted the declarative model).
                    int id = _ctx.Names.NextPtr();
                    w.Line($"int __fr{id} = {EcDispatchExpr("\"EC-STORAGE-NOT-ALLOC\"", "\"\"")};");
                    w.Line($"if (__fr{id} >= 0) {{ __pc = __fr{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                }
            }
            else
                w.Line($"_ = {na};   // EC-STORAGE-NOT-ALLOC checking not enabled (§14.6.13.1.4 — not raised)");
        }
    }
}
