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
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design; P7 Step 9d — a real collaborator over
/// the per-unit <see cref="EmitContext"/>). Every pointer VALUE renders onto the ONE <c>ManagedPointer</c>
/// carrier; every runtime rule lives in <c>CobolPtr</c> (Deref/UpBy/Allocate/Free) — the emitters only wire
/// places to helpers, through the <see cref="RuntimeApi"/> façade.
/// </summary>
internal sealed class PtrEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, EcEmitter ec)
{
    /// <summary>The C# expression for an <c>ADDRESS OF identifier</c> value (ISO §8.4.3.11 GR1): a BASED
    /// item's value IS its implicit data-address pointer (§8.6.5 :8791); a cell-forced record renders a
    /// window pointer at the item's class offset; an EXTERNAL record's cell is the run-unit
    /// <c>ExternalStore</c> cell (the increment-2 unification — no special case). The bind pass guaranteed
    /// cell-backing, so the fall-through is a loud guard, never silence.</summary>
    public string AddressOfText(BoundAddressOf a)
    {
        var item = a.Item;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        if (root.Class is not { } cls)
            return LoudValue("ManagedPointer", $"ADDRESS OF '{item.CobolName}' without cell storage");
        // A SUBSCRIPTED operand addresses the OCCURRENCE (§8.4.3.11 GR1): the class offset plus the binder's
        // occurrence displacement — the occurrences lie end-to-end in the ONE cell image.
        string off = a.OccursDisplacement is { } disp ? $"{item.ClassOffset} + {disp}" : $"{item.ClassOffset}";
        if (cls.BasedPointerField is { } addr)
            // The based item itself reads its implicit pointer (§8.6.5 :8791); a SUBORDINATE's address is
            // the base displaced by its class offset (§8.4.3.11 GR1 — the address OF THE ITEM; the review's
            // ClassOffset-drop finding). UpBy's GR18 null trap is the right posture: taking the address of a
            // child of an unallocated based record has no address to take.
            return item.ClassOffset == 0 && ReferenceEquals(item, root) && a.OccursDisplacement is null
                ? addr
                : RuntimeApi.PtrUpBy(addr, off);
        if (ctx.Data.PtrAddressableCellOf.TryGetValue(cls, out var cell))
            return $"ManagedPointer.At({cell}, {off})";
        // ⛔ The SAME seed expression the backing property emits (DataEmitter.ExternalCellSeed — the ONE
        // composer): whichever of the two runs first CREATES the run-unit cell, so a divergence here would make
        // the record's initial content depend on statement order.
        if (ctx.Data.CallExternalBackings.FirstOrDefault(b => b.BackingCsName == cls.BackingCsName) is { } ext)
            return $"ManagedPointer.At(ExternalStore.Cell({CsLiteral(ext.ExternalName)}, "
                + $"{new DataEmitter(ctx).ExternalCellSeed(ext)}), {off})";
        return LoudValue("ManagedPointer", $"ADDRESS OF '{item.CobolName}' — unrecognized cell backing");
    }

    /// <summary><c>SET ADDRESS OF based TO pointer</c> (ISO §14.9.39 F7 GR12–13): assign the address VALUE —
    /// a snapshot, never live tracking.</summary>
    public void EmitSetAddressOfBased(BoundSetAddressOfBased s)
    {
        if (s.Based.Class?.BasedPointerField is not { } addr)
        {
            ctx.Writer.Line(LoudStmt($"SET ADDRESS OF '{s.Based.CobolName}' — the based item has no pointer bridge "
                + $"({s.Based.Class?.RejectReason ?? "unclassified"})"));
            return;
        }
        ctx.Writer.Line(s.Source is { } src
            ? $"{addr} = {PlaceRenderer.Read(src)};   // SET ADDRESS OF (ISO §14.9.39 F7 GR12-13 — a snapshot)"
            : $"{addr} = ManagedPointer.Null;   // SET ADDRESS OF … TO NULL (ISO §14.9.39 F7 SR19 — disassociated; kb/Work PB89)");
    }

    /// <summary><c>SET pointer… {UP|DOWN} BY n</c> (ISO §14.9.39 Format 10): the amount evaluates ONCE, then
    /// each pointer moves by n bytes (GR20; character positions in this model). NULL → EC-DATA-PTR-NULL
    /// inside the runtime's <c>UpBy</c> (GR18). A SCALED amount keeps its fraction into <c>UpByScaled</c>, whose
    /// divisibility test realizes GR19 EXACTLY (a non-integer VALUE → EC-SIZE-ADDRESS fatal; 2.0 moves by 2 —
    /// never the silent Align-truncation the review caught).</summary>
    public void EmitSetPointerUpDown(BoundSetPointerUpDown s)
    {
        var w = ctx.Writer;
        // The amount lands through the ONE landing (kb/Work PB84): `SET P UP BY A ** 2` — an SDIDI intermediate
        // under native arithmetic since PB69, and every STANDARD-DECIMAL expression — was `(long)(CobolDec)`.
        NumX x = num.Landed(NumericRenderer.DeU(num.Render(s.Amount, ReceiverContext.None)), ReceiverContext.None);
        string tmp = $"__ptrBy{ctx.Names.NextPtr()}";
        // A NATIVE-FLOAT amount keeps its double all the way to CobolPtr.UpByReal, whose GR19 integrality
        // test runs on the DOUBLE — the old `(long)(double)` truncation bypassed the raise entirely
        // (kb/Work PB151; the same Scale==0/Real split EmitAllocate mispriced).
        if (x.Real)
        {
            w.Line($"double {tmp} = ({x.Expr});");
            string amountR = s.Down ? $"-{tmp}" : tmp;
            foreach (var t in s.Targets)
                w.Line(PlaceRenderer.Write(t, RuntimeApi.PtrUpByReal(PlaceRenderer.Read(t), amountR))
                    + "   // SET pointer UP/DOWN BY float (ISO §14.9.39 F10 GR19)");
            return;
        }
        w.Line($"long {tmp} = (long)({x.Expr});");
        string amount = s.Down ? $"-{tmp}" : tmp;
        foreach (var t in s.Targets)
            w.Line(PlaceRenderer.Write(t, x.Scale == 0
                    ? RuntimeApi.PtrUpBy(PlaceRenderer.Read(t), amount)
                    : RuntimeApi.PtrUpByScaled(PlaceRenderer.Read(t), amount, $"{x.Scale}"))
                + "   // SET pointer UP/DOWN BY (ISO §14.9.39 F10 GR19/GR20)");
    }

    /// <summary>ALLOCATE (ISO §14.9.3). Form 1: a fresh cell of ⌈expr⌉ characters (GR1 — a fractional request
    /// rounds UP; GR2 — ≤0 yields NULL, no EC) delivered to RETURNING; INITIALIZED = binary-zero fill (GR6).
    /// Form 2: the BASED item's implicit pointer takes a cell of its template width (GR3/GR4b), RETURNING
    /// also set when present (GR4a); the form-2 INITIALIZED effect (GR7) is NOT emitted here — the binder
    /// lowers it to the spec's own INITIALIZE expansion (a <c>BoundInitialize</c> sequenced after this node),
    /// so it renders through the ONE INITIALIZE/MOVE path.</summary>
    public void EmitAllocate(BoundAllocate s)
    {
        var w = ctx.Writer;
        // GR6/GR8/GR9: INITIALIZED = binary zeros; else the OPTIONS INITIALIZE clause's specified-fill-
        // character when written (GR8/GR9 key on the CLAUSE, not on its section list — the allocated storage
        // belongs to no section; the CONFORMANCE.md GR8 determination); else space (content undefined —
        // space-filling is the conformant choice).
        string fill = s.Initialized ? "'\\0'" : OptionsFillCharLiteral();
        string na = $"__notAvail{ctx.Names.NextPtr()}";
        w.Line($"bool {na};");
        if (s.Based is { } based)
        {
            if (based.Class?.BasedPointerField is not { } addr || based.Class is not { } cls)
            {
                // Unreachable from a compiled program since PB151's bind-time COBOLNET1695 — the belt stays.
                w.Line(LoudStmt($"ALLOCATE '{based.CobolName}' — the based item has no pointer bridge "
                    + $"({based.Class?.RejectReason ?? "unclassified"})"));
                return;
            }
            w.Line($"{addr} = {RuntimeApi.PtrAllocate($"(System.Int128){cls.Width}", fill, na)};   // ALLOCATE based-item (ISO §14.9.3.4 GR3/GR4b)");
            if (s.Returning is { } ret2)
                w.Line(PlaceRenderer.Write(ret2, addr) + "   // GR4a — the RETURNING pointer also receives the address");
            EmitStorageNotAvail(w, na);   // GR5c — data-name-2 and the based address already hold NULL
            return;
        }
        NumX x = num.Landed(NumericRenderer.DeU(num.Render(s.Chars!, ReceiverContext.None)), ReceiverContext.None);   // kb/Work PB84
        // GR1's round-UP per lane (kb/Work PB151/PB142). §14.9.3.4 GR1 and §14.7.9.3 GR1 (RETRY n TIMES) are the
        // standard's ONLY two "rounded up to the next whole number" clauses, so the scaled lane's round-up is
        // NumericRenderer.AlignRoundedUp — the one place that rule is written — and not a second local ceiling.
        // The NATIVE-FLOAT lane stays inside CobolPtr.AllocateReal, which fuses the same ceiling with GR2's
        // ≤ 0 ⇒ NULL and the storage-not-available outcomes only ALLOCATE defines (the old `(long)(double)`
        // truncated 2.5 → 2, a silently undersized cell). Either way the size travels as the FULL Int128 — the
        // old `(long)` narrowing wrapped a 20-digit request into a small VALID allocation (the PB22 cast family).
        string alloc = x.Real
            ? RuntimeApi.PtrAllocateReal($"({x.Expr})", fill, na)
            : RuntimeApi.PtrAllocate($"(System.Int128)({NumericRenderer.AlignRoundedUp(x)})", fill, na);
        w.Line(PlaceRenderer.Write(s.Returning!, alloc)
            + "   // ALLOCATE n CHARACTERS (ISO §14.9.3 GR1/GR2/GR5" + (s.Initialized ? "/GR6" : "/GR8") + ")");
        EmitStorageNotAvail(w, na);
    }

    /// <summary>The GR5c raise: storage not available → the nonfatal EC-STORAGE-NOT-AVAIL reports through
    /// the checking-gated block (§14.6.13.1.4 — an unchecked nonfatal condition is not raised; the EmitFree
    /// EC-STORAGE-NOT-ALLOC pattern). GR5a/b need no code here — the runtime already returned NULL into
    /// every address the statement assigns.</summary>
    private void EmitStorageNotAvail(CodeWriter w, string na)
    {
        if (ecState.Info?.Enabled.Any(e => e.Ec == "EC-STORAGE-NOT-AVAIL") != true)
        {
            w.Line($"_ = {na};   // EC-STORAGE-NOT-AVAIL checking not enabled (§14.6.13.1.4 — not raised; the pointers hold NULL per GR5a/b)");
            return;
        }
        using (w.Block($"if ({na})"))
        {
            w.Line($"ExceptionState.Set(\"EC-STORAGE-NOT-AVAIL\", false);   // §14.9.3.4 GR5c — set to exist (nonfatal)");
            int did = ctx.Names.NextPtr();
            w.Line($"int __pa{did} = {ec.EcDispatchExpr("\"EC-STORAGE-NOT-AVAIL\"", "\"\"")};");
            w.Line($"if (__pa{did} >= 0) {{ __pc = __pa{did}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
        }
    }

    /// <summary>The OPTIONS INITIALIZE clause's fill character as a C# char literal (§14.9.3.4 GR8/GR9;
    /// kb/Work PB151's arm).
    /// <para>⛔ This USED to be the decode itself — two private methods, invisible to every other consumer, whose
    /// <c>raw[0]</c> fall-through silently accepted a fill literal §11.9.10.3 SR1 forbids and took its first
    /// character, and whose private §11.9.10.4 GR5 map spelled HIGH-VALUES as U+FFFF where the rest of the
    /// compiler says U+00FF (kb/Work PB152). literal-1's byte is now decoded ONCE at bind time, where SR1 gets
    /// its diagnostic, and the figurative arms resolve through the compiler's single definition — all three
    /// initial-state arms reading one fact through <see cref="InitialStateBackground"/>.</para></summary>
    private string OptionsFillCharLiteral() => _background.AllocateFillLiteral();

    /// <summary>The shared §14.6.2.3.2 background resolver, held once rather than constructed per ALLOCATE:
    /// it caches the section-root sets it may need, and an emitter-lifetime field is the same lifetime
    /// <c>ValueInitializer</c> gives it.</summary>
    private readonly InitialStateBackground _background = new(ctx);

    /// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> (ISO §14.9.39 Format 9 + §8.4.3.13;
    /// P10 Step 7): resolve the named OUTERMOST program through the run-unit ProgramTable ONCE, assign the
    /// result to every target. Not locatable → GR4: the value is the NULL program address and
    /// EC-PROGRAM-NOT-FOUND is set to exist — reported through the checking-gated block (§14.6.13.1.4 — an
    /// unchecked condition is not raised; the EmitFree EC pattern).</summary>
    public void EmitSetEntry(BoundSetEntry s)
    {
        var w = ctx.Writer;
        string nameExpr = s.NameLiteral is { } lit
            ? EmitText.CsLiteral(lit)
            : $"({PlaceRenderer.Read(s.NamePlace!)}).Trim()";   // §8.4.3.13 GR1a — the identifier's value names the program
        int id = ctx.Names.NextPtr();
        string nf = $"__ppNf{id}";
        w.Line($"bool {nf};");
        w.Line($"var __ppE{id} = ProgramRegistry.EntryOf({nameExpr}, out {nf});   // SET … TO ENTRY (ISO §8.4.3.13 GR1/GR4)");
        foreach (var t in s.Targets)
            w.Line(PlaceRenderer.Write(t, $"__ppE{id}") + "   // SET program-pointer (ISO §14.9.39 Format 9)");
        bool checkNotFound = ecState.Info?.Enabled.Any(e => e.Ec == "EC-PROGRAM-NOT-FOUND") == true;
        if (checkNotFound)
        {
            using (w.Block($"if ({nf})"))
            {
                // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14).
                w.Line($"ExceptionState.Set(\"EC-PROGRAM-NOT-FOUND\", true);   // §8.4.3.13 GR4 — set to exist");
                int did = ctx.Names.NextPtr();
                w.Line($"int __pe{did} = {ec.EcDispatchExpr("\"EC-PROGRAM-NOT-FOUND\"", "\"\"")};");
                w.Line($"if (__pe{did} >= 0) {{ __pc = __pe{did}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            }
        }
        else
            w.Line($"_ = {nf};   // EC-PROGRAM-NOT-FOUND checking not enabled (§14.6.13.1.4 — not raised; the value is NULL per GR4)");
    }

    /// <summary>FREE (ISO §14.9.15 GR1/GR2 — per operand, left to right): the helper runs the three-way;
    /// the nonfatal EC-STORAGE-NOT-ALLOC (GR1c) reports through the TurnState-gated status block ONLY when
    /// its checking is enabled (§14.6.13.1.4 — an unchecked nonfatal condition is not raised).</summary>
    public void EmitFree(BoundFree s)
    {
        var w = ctx.Writer;
        bool checkNotAlloc = ecState.Info?.Enabled.Any(e => e.Ec == "EC-STORAGE-NOT-ALLOC") == true;
        foreach (var op in s.Operands)
        {
            string na = $"__notAlloc{ctx.Names.NextPtr()}";
            w.Line($"bool {na};");
            w.Line(PlaceRenderer.Write(op, RuntimeApi.PtrFree(PlaceRenderer.Read(op), na)) + "   // FREE (ISO §14.9.15 GR1)");
            if (checkNotAlloc)
            {
                using (w.Block($"if ({na})"))
                {
                    // The §15.32.3 r2 pair rides the ambient statement context (kb/Work R14).
                    w.Line($"ExceptionState.Set(\"EC-STORAGE-NOT-ALLOC\", false);   // GR1c — nonfatal (§14.6.13.1.1)");
                    // §14.6.13.1.3 #5: the F3 selection runs; a nonfatal condition with no handler (or
                    // RESUME NEXT / a completed declarative) simply continues (the review finding — the
                    // status set alone never consulted the declarative model).
                    int id = ctx.Names.NextPtr();
                    w.Line($"int __fr{id} = {ec.EcDispatchExpr("\"EC-STORAGE-NOT-ALLOC\"", "\"\"")};");
                    w.Line($"if (__fr{id} >= 0) {{ __pc = __fr{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
                }
            }
            else
                w.Line($"_ = {na};   // EC-STORAGE-NOT-ALLOC checking not enabled (§14.6.13.1.4 — not raised)");
        }
    }
}
