// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The STRING / UNSTRING verb emitter (P7 Step 9d — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter.StringUnstring partial). Every runtime-member
/// fragment routes through <see cref="RuntimeApi"/>.</summary>
internal sealed class StringEmitter(EmitContext ctx, NumericRenderer num, ArithmeticEmitter arith, EcEmitter ec)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the ON/NOT-ON
    /// OVERFLOW phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>STRING (ISO §14.9.43): the receiver's character image is materialized into ONE working local (its
    /// CURRENT content — GR7 preserves every position the transfer does not touch; there is no space filling), each
    /// sending operand transfers into it via <c>StringTransfer</c> in statement order (GR3), and the
    /// final image stores back through the receiver's own store path. The pointer initializes from the POINTER item
    /// (GR4) or 1 (GR5), advances only with the per-character moves (GR6 — the runtime kernel), and writes back —
    /// before the overflow phrases run, which may inspect it — only when the phrase was written. The overflow flag
    /// is latched across sendings by the kernel (GR8a) and dispatches ON / NOT ON OVERFLOW per GR8c/GR8e/GR9 (the
    /// 2002+ EC-OVERFLOW-STRING name, GR8b, awaits the EC model; with no phrase the nonfatal condition continues
    /// execution, §14.6.13.1.4, so no code is needed).</summary>
    public void EmitString(BoundStringStmt s)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextStrUnstr();
        string ptr = $"__strPtr{id}", ovf = $"__strOvf{id}", acc = $"__strInto{id}";
        w.Line(s.Pointer is { } p0
            ? $"long {ptr} = (long)({NumericRenderer.Align(num.AsNum(new BoundFieldOperand(p0), ReceiverContext.None), 0)});"   // GR4 — the user's initial value (by VALUE — kb/Work PB86)
            : $"long {ptr} = 1;");                                                    // GR5 — implicit pointer of 1
        w.Line($"bool {ovf} = false;");
        w.Line($"string {acc} = {ReadImage(s.Into)};");
        foreach (var snd in s.Sendings)
        {
            // GR3a: the sender's CONTENT transfers per the alphanumeric-to-alphanumeric move mechanics — its raw
            // character image (a numeric sender contributes its sign-carrying zoned image), not a converted value.
            string src = OperandText.AsString(snd.Value, num);
            string delim = snd.BySize || snd.Delimiter is null ? "null" : OperandText.AsString(snd.Delimiter, num);
            w.Line($"{acc} = {RuntimeApi.StrTransfer(acc, src, delim, ptr, ovf)};");
        }
        WriteImage(s.Into, acc);
        if (s.Pointer is { } p) arith.StoreArith(p, new NumX(ptr, 0), CobolRounding.Truncation);
        EmitOverflow(ovf, "EC-OVERFLOW-STRING", s.OnOverflow, s.NotOnOverflow);   // GR8b
    }

    /// <summary>UNSTRING (ISO §14.9.48): the sender's image and the delimiter values are read ONCE at initiation
    /// (operand overlap is undefined, GR18), the pointer initializes from the POINTER item or 1 (GR11a) and the
    /// tally from the TALLYING item's CURRENT value (GR14 — the statement ADDS to it). An initiation pointer
    /// outside [1, size(sender)] is the GR15a overflow and TERMINATES the operation before any transfer (GR16a — a
    /// check the legacy engine performed but did not honor with termination); otherwise each receiving area gets
    /// one <c>UnstringExtract</c> (GR11b–f), its result stored per the MOVE rules (GR11c — so two
    /// contiguous delimiters space-fill an alphanumeric receiver and ZERO-fill a numeric one, GR8), DELIMITER IN /
    /// COUNT IN stored per GR11d/e, and the tally bumped — all skipped when the sender was already exhausted
    /// (GR11g: that receiver is not acted upon; exhaustion is NOT overflow, GR15). After the receivers, unexamined
    /// sender characters with every receiver acted upon raise the GR15b overflow. Pointer/tally write back before
    /// the ON / NOT ON OVERFLOW dispatch (GR16c/GR16e/GR17; EC-OVERFLOW-UNSTRING, GR16b, awaits the EC model).</summary>
    public void EmitUnstring(BoundUnstringStmt s)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextStrUnstr();
        string src = $"__unsSrc{id}", dels = $"__unsDel{id}", alls = $"__unsAll{id}",
               ptr = $"__unsPtr{id}", tly = $"__unsTly{id}", ovf = $"__unsOvf{id}";
        w.Line($"string {src} = {OperandText.AsString(s.Source, num)};");   // DA4: an operand (may be a function)
        if (s.Delimiters.Count > 0)
        {
            // GR10: applied in statement order (the kernel's earliest-match-then-first-listed scan); a figurative
            // is its single character (GR7); a field delimiter is its FULL content — trailing spaces included
            // (GR9: the delimiter is the content of the item; the legacy's TrimEnd was a deviation).
            w.Line($"string[] {dels} = {{ {string.Join(", ", s.Delimiters.Select(d => OperandText.AsString(d.Value, num)))} }};");
            w.Line($"bool[] {alls} = {{ {string.Join(", ", s.Delimiters.Select(d => d.All ? "true" : "false"))} }};");
        }
        else
        {
            w.Line($"string[] {dels} = System.Array.Empty<string>();");
            w.Line($"bool[] {alls} = System.Array.Empty<bool>();");
        }
        w.Line(s.Pointer is { } p0
            ? $"long {ptr} = (long)({NumericRenderer.Align(num.AsNum(new BoundFieldOperand(p0), ReceiverContext.None), 0)});"   // GR11a / GR12 — user-initialized (by VALUE — kb/Work PB86)
            : $"long {ptr} = 1;");                                                    // GR11a — leftmost position
        w.Line(s.Tallying is { } t0
            ? $"long {tly} = (long)({NumericRenderer.Align(num.AsNum(new BoundFieldOperand(t0), ReceiverContext.None), 0)});"   // GR14 — adds to the current value (by VALUE — kb/Work PB86)
            : $"long {tly} = 0;");
        w.Line($"bool {ovf} = false;");
        using (w.Block($"if ({ptr} < 1 || {ptr} > {src}.Length)"))
            w.Line($"{ovf} = true;");                                                 // GR15a; GR16a terminates — no transfer
        using (w.Block("else"))
        {
            for (int k = 0; k < s.Receivers.Count; k++)
            {
                var r = s.Receivers[k];
                string cnt = $"__unsCnt{id}_{k}", fld = $"__unsFld{id}_{k}", dlm = $"__unsDlm{id}_{k}";
                w.Line($"long {cnt} = {RuntimeApi.UnstringExtract(src, dels, alls, $"{r.NoDelimSize}", ptr, fld, dlm)};");
                using (w.Block($"if ({cnt} >= 0)"))                                   // −1: not acted upon (GR11g)
                {
                    MoveString(r.Target, fld);                                        // GR11c — per the MOVE rules
                    if (r.DelimiterIn is { } di) MoveString(di, dlm);                 // GR11d — "" ⇒ space fill via the move
                    if (r.CountIn is { } ci) arith.StoreArith(ci, new NumX(cnt, 0), CobolRounding.Truncation);   // GR11e
                    w.Line($"{tly} += 1;");                                           // GR14 — per receiver acted upon
                }
            }
            w.Line($"if ({ptr} <= {src}.Length) {ovf} = true;   // unexamined characters remain (ISO §14.9.48.4 GR15b)");
        }
        if (s.Pointer is { } p) arith.StoreArith(p, new NumX(ptr, 0), CobolRounding.Truncation);     // GR13
        if (s.Tallying is { } t) arith.StoreArith(t, new NumX(tly, 0), CobolRounding.Truncation);    // GR14
        EmitOverflow(ovf, "EC-OVERFLOW-UNSTRING", s.OnOverflow, s.NotOnOverflow);   // GR16b
    }

    /// <summary>The shared ON / NOT ON OVERFLOW dispatch (STRING GR8c/8e/GR9; UNSTRING GR16c/16e/GR17): the ON
    /// imperative runs exactly when the flag is set, the NOT imperative exactly when it is not; with neither
    /// phrase the (nonfatal) condition lets execution continue, §14.6.13.1.4. Under enabled EC-OVERFLOW checking
    /// (>>TURN, §7.3.25) the raise (status + the no-phrase F3 selection) precedes the phrase branch — STRING
    /// GR8b / UNSTRING GR16b via the <see cref="EcEmitter"/> overflow emission.</summary>
    private void EmitOverflow(
        string flag, string ecName, IReadOnlyList<BoundStatement>? onOverflow, IReadOnlyList<BoundStatement>? notOnOverflow)
    {
        var w = ctx.Writer;
        ec.EmitOverflow(flag, ecName, hasPhrase: onOverflow is not null);
        if (onOverflow is { } on)
        {
            using (w.Block($"if ({flag})")) Statements.EmitStatementList(on);
            if (notOnOverflow is { } notAlso)
                using (w.Block("else")) Statements.EmitStatementList(notAlso);
        }
        else if (notOnOverflow is { } notOnly)
            using (w.Block($"if (!{flag})")) Statements.EmitStatementList(notOnly);
    }

    /// <summary>The character image of a STRING/UNSTRING character-position operand — its raw content (a group's
    /// concatenated image, a numeric-DISPLAY item's sign-carrying zoned image): the verbs operate on character
    /// positions, never converted values (STRING GR3a / UNSTRING GR11).</summary>
    private string ReadImage(Place p) => OperandText.AsString(new BoundFieldOperand(p), num);

    /// <summary>Store a full-width character image back into the STRING receiver, preserving its storage shape
    /// (§14.9.43.4 GR7 — the image already carries the untouched positions): a character-image group distributes
    /// via <c>FromImage</c>; a Tier-B view / reference window splices through its own <c>Write</c>; a long-stored
    /// numeric-DISPLAY receiver (SR1 admits usage-display numeric) decodes the updated zoned image back to its
    /// value; an alphanumeric / image-stored receiver assigns the image directly (same width by construction).</summary>
    private void WriteImage(Place p, string imageExpr)
    {
        var w = ctx.Writer;
        // A reference-modified identifier-3 is the elementary alphanumeric unique item of §8.4.3.3.4 GR6 — the
        // updated image splices into it (kb/Work PB70: over a GROUP inner it used to fall into the group arm).
        if (p is RefModPlace) { w.Line(PlaceRenderer.Write(p, imageExpr)); return; }
        // A group identifier-3 (§14.9.43.4 GR3a — the alphanumeric MOVE rules): the ONE group-image store.
        if (p.Item.IsGroup) { w.Line(PlaceRenderer.WriteGroupImage(p, imageExpr, "STRING INTO group")); return; }
        if (p is not RedefViewPlace && !p.Item.StoreAsImage
            && p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
        {
            w.Line(PlaceRenderer.Write(p, ArithmeticEmitter.Narrow(RuntimeApi.NumParseDisplay(imageExpr, p.Item.ProfileName), p.Item)));
            return;
        }
        if (p is not RedefViewPlace && !p.Item.StoreAsImage
            && p.Item.Pic is not { Category: PicCategory.Alphanumeric })
        {
            w.Line(LoudStmt($"STRING INTO receiver '{p.Item.CobolName}' (usage display required, ISO §14.9.43.3 SR1)"));
            return;
        }
        w.Line(PlaceRenderer.Write(p, imageExpr));
    }

    /// <summary>Store a run-time string (the UNSTRING examined characters / matched delimiter) into a receiver
    /// "according to the rules for the MOVE statement" with the source treated as an ELEMENTARY ALPHANUMERIC item
    /// (ISO §14.9.48.4 GR11c/GR11d) — the same conversions the bound-operand MOVE path applies, re-rendered here
    /// for a C# string expression: group ⇒ the GR4 whole-group fill; alphanumeric ⇒ left-justified space-fill /
    /// right-truncate (a JUSTIFIED receiver right-justifies per §14.9.25.4 GR6c — DataItem.Justified);
    /// alphanumeric-edited ⇒ the insertion mask; numeric / numeric-edited ⇒ the alphanumeric sender is an UNSIGNED
    /// integer (§14.9.25.4 GR6), so an empty extraction stores 0 — the GR8 zero-fill — through the receiver's
    /// PICTURE store (or its edit mask).</summary>
    private void MoveString(Place target, string valueExpr)
    {
        var w = ctx.Writer;
        if (target is RefModPlace)
        {
            // A reference-modified receiver: SpliceInto left-justifies, space-fills, and truncates to the slice.
            w.Line(PlaceRenderer.Write(target, valueExpr));
            return;
        }
        if (target.Item.IsGroup)
        {
            // ⛔ V59 RESIDUE (DA5): IsImageCapable, not the pre-V59 IsCharacterImage. This is a POSITIONAL
            // character transfer INTO the group's storage — the same job a group MOVE does, and
            // MoveEmitter already admits a COMP/PACKED group here (§14.9.25.4 GR4: no conversion, filled
            // without consideration for the individual items). Refusing it while MOVE allows it made two
            // verbs disagree about the same receiver — and that is not merely a consistency argument:
            // §14.9.43.4 GR3a says STRING transfers into identifier-3 "in accordance with the MOVE
            // statement rules for alphanumeric-to-alphanumeric moves", so whatever an alphanumeric MOVE
            // may deposit into a group, STRING may deposit into the SAME group; §14.9.22.3 SR1 goes
            // further and names "an alphanumeric or national group item" as a valid INSPECT identifier-1
            // outright, applying its usage requirement only to an ELEMENTARY operand. Both cite.py-checked. ⚠ "BYTES ARE NOT TEXT" does NOT apply: that rule
            // governs RENDERING a COMP leaf's VALUE as text (DisplayTextWidth), not writing characters
            // positionally over its bytes. A USAGE INDEX group is still imageless and stays loud (kb/Work
            // PB164) — hence the leaf-kind wording now matches the predicate actually tested.
            // The ONE group-image store (MOVE rules — §14.9.48.4 GR11c: a GR8a current-extent splice for an
            // occurs-depending receiver, the Tier-B window, the Tier-C loud island; kb/Work PB80).
            w.Line(PlaceRenderer.WriteGroupImage(target, RuntimeApi.StrStore(valueExpr, $"{target.Item.ImageWidth}"), "UNSTRING INTO group"));
            return;
        }
        switch (target.Item.Pic)
        {
            case { Category: PicCategory.NumericEdited } npic:
            {
                string unsignedInt = RuntimeApi.NumFromAlphanumeric(valueExpr);   // the form dispatch is EditFormatFor's (D21/PB66)
                w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatFor(npic, new NumX(unsignedInt, 0), unsignedInt, "0", ctx.EditCfg(target.Item.Pic) + RuntimeApi.EditsArg(target.Item.Pic!.EditingRules))));
                return;
            }
            case { Category: PicCategory.Alphanumeric, EditMask: { } amask }:
                w.Line(PlaceRenderer.Write(target, RuntimeApi.EditFormatAlphanumeric(valueExpr, CsLiteral(amask))));
                return;
            case { Category: PicCategory.Alphanumeric, Length: var len }:
                // A JUSTIFIED receiver right-justifies — left space-fill / left truncation (§14.9.25.4 GR6c).
                // An ANY LENGTH receiver stores at the CARRIER's current length (ISO §13.18.2 GR1 — n is fixed
                // by the activation's argument), never its one-symbol Pic.Length.
                string wS = target.Item.IsAnyLength ? $"{PlaceRenderer.Read(target)}.Length" : $"{len}";
                w.Line(PlaceRenderer.Write(target, RuntimeApi.StrStoreAligned(valueExpr, wS, target.Item.Justified)));
                return;
            case { Category: PicCategory.Numeric, IsFloat: false, Usage: not Usage.Index }:
                string stored = RuntimeApi.NumStore(RuntimeApi.NumFromAlphanumeric(valueExpr), "0", target.Item.ProfileName);
                w.Line(PlaceRenderer.Write(target, target.Item.StoreAsImage
                    ? RuntimeApi.NumFormatImage(stored, target.Item.ProfileName)
                    : ArithmeticEmitter.Narrow(stored, target.Item)));
                return;
            default:
                w.Line(LoudStmt($"UNSTRING receiver '{target.Item.CobolName}' (usage display, category alphabetic/alphanumeric/numeric — ISO §14.9.48.3 SR4)"));
                return;
        }
    }
}
