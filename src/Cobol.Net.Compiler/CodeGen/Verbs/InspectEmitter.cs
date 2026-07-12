// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The INSPECT verb emitter (P7 Step 9d — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the CSharpEmitter partial of the same name). Every
/// runtime-member fragment routes through <see cref="RuntimeApi"/>.</summary>
internal sealed class InspectEmitter(EmitContext ctx, NumericRenderer num, CSharpEmitter host)
{
    /// <summary>
    /// INSPECT (ISO §14.9.22) → one image snapshot + runtime cycle calls. Identifier-1's character image is read
    /// ONCE (GR6 item identification; GR4 — an edited/unsigned-numeric DISPLAY item inspects as its redefined
    /// character image, GR4d — a signed numeric item inspects de-signed); a format 3 then runs the tallying pass
    /// FOLLOWED by the replacing pass over that same image (GR19 — two successive statements; tallying never
    /// writes identifier-1, so the one snapshot is exact). Tally counts ADD into their counters (GR11); a
    /// REPLACING/CONVERTING result stores back through the target's <see cref="Place"/>, re-signing a signed
    /// numeric target with its retained original sign (GR4d).
    /// </summary>
    public void Emit(BoundInspect ins)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextInspectTmp();
        string img = $"__ins{id}";
        w.Line($"string {img} = {OperandText.AsString(new BoundFieldOperand(ins.Target), deSign: true)};");
        string back = ins.Backward ? "true" : "false";

        if (ins.Tallying.Count > 0)
        {
            var t = ins.Tallying;
            string kinds = string.Join(", ", t.Select(x => RuntimeApi.InspectTallyKindText(x.Kind)));
            string pats = string.Join(", ", t.Select(x => OperandTextOf(x.Pattern)));
            string befs = string.Join(", ", t.Select(x => OperandTextOf(x.Before)));
            string afts = string.Join(", ", t.Select(x => OperandTextOf(x.After)));
            w.Line($"long[] __cnt{id} = {RuntimeApi.InspectTally(img, kinds, pats, befs, afts, back)};");
            // One add per operand, in source order — the same counter may appear under several operands and
            // accumulates each count (GR11 — INSPECT adds, it never initializes).
            for (int k = 0; k < t.Count; k++)
                host.StoreArith(t[k].Counter,
                    num.Combine(num.FieldNum(t[k].Counter), "+", new NumX($"__cnt{id}[{k}]", 0), ReceiverContext.None),
                    CobolRounding.Truncation);
        }

        bool mutated = false;
        if (ins.Replacing.Count > 0)
        {
            var r = ins.Replacing;
            string kinds = string.Join(", ", r.Select(x => RuntimeApi.InspectReplaceKindText(x.Kind)));
            string pats = string.Join(", ", r.Select(x => OperandTextOf(x.Pattern)));
            string reps = string.Join(", ", r.Select(x => OperandTextOf(x.Replacement)));
            string befs = string.Join(", ", r.Select(x => OperandTextOf(x.Before)));
            string afts = string.Join(", ", r.Select(x => OperandTextOf(x.After)));
            w.Line($"{img} = {RuntimeApi.InspectReplace(img, kinds, pats, reps, befs, afts, back)};");
            mutated = true;
        }

        if (ins.Converting is { } cv)
        {
            w.Line($"{img} = {RuntimeApi.InspectConvert(img, OperandTextOf(cv.From), OperandTextOf(cv.To), OperandTextOf(cv.Before), OperandTextOf(cv.After), back)};");
            mutated = true;
        }

        if (mutated) EmitStore(ins.Target, img);
    }

    /// <summary>Store the replaced/converted image back into identifier-1 by its storage shape: a character-image
    /// group distributes via <c>FromImage</c> (a Tier-B view group splices its window); a string-stored elementary
    /// item (alphanumeric / numeric-edited / zoned image) takes the image directly; a native-stored numeric DISPLAY
    /// item re-encodes the digit image — re-applying the RETAINED original sign for a signed item (§14.9.22.4
    /// GR4d). A replacement that left a non-digit in a numeric item decodes by digit positions only — deterministic
    /// where the spec leaves the result undefined (§14.6.13.2 incompatible data).</summary>
    private void EmitStore(Place p, string img)
    {
        var w = ctx.Writer;
        // ISO §13.18.38 GR7 + §14.6.4 step 6: an occurs-depending group is INSPECTed over its current-count extent;
        // the replaced image (already current-count, read via the GR8 sending slice) splices back over exactly that
        // extent, leaving positions past the count unmodified.
        if (p is OdoGroupPlace odo) { w.Line(odo.ReceiveInto(img)); return; }
        if (p.Item.IsGroup)
        {
            if (p is RedefViewPlace) { w.Line(p.Write(img)); return; }
            if (!p.Item.IsCharacterImage)
            {
                w.Line(LoudStmt($"INSPECT REPLACING/CONVERTING into mixed-usage group '{p.Item.CobolName}' with a COMP/binary leaf (Tier-C byte path, deferred)"));
                return;
            }
            w.Line($"{p.Read()}.FromImage({img});");
            return;
        }
        if (p.Item.Pic is { Category: PicCategory.Numeric, IsFloat: false } pic)
        {
            bool stringStored = p.Item.StoreAsImage || p is RedefViewPlace || p is RefModPlace;
            if (!stringStored)
            {
                if (pic.Signed)
                {
                    // GR4d: the original sign is retained — the (still-unmodified) field supplies it.
                    string mag = $"__insMag{ctx.Names.NextInspectTmp()}";
                    w.Line($"var {mag} = {ArithmeticEmitter.Narrow(RuntimeApi.NumFromAlphanumeric(img), p.Item)};");
                    w.Line(p.Write($"({p.Read()} < 0 ? -{mag} : {mag})"));
                }
                else
                    w.Line(p.Write(ArithmeticEmitter.Narrow(RuntimeApi.NumFromAlphanumeric(img), p.Item)));
                return;
            }
            if (pic.Signed)
            {
                // A string-stored signed zoned image (whole-group-aliased / Tier-B view): decode the original for
                // its sign, re-encode the replaced magnitude with that sign in the item's sign convention (GR4d).
                string mag = $"__insMag{ctx.Names.NextInspectTmp()}";
                w.Line($"Int128 {mag} = {RuntimeApi.NumFromAlphanumeric(img)};");
                w.Line(p.Write(RuntimeApi.NumFormatDisplay(
                    $"{RuntimeApi.NumParseDisplay(p.Read(), p.Item.ProfileName)} < 0 ? -{mag} : {mag}", p.Item.ProfileName)));
                return;
            }
        }
        w.Line(p.Write(img));   // string-stored: alphanumeric, numeric-edited, unsigned zoned image, ref-mod slice
    }

    /// <summary>An INSPECT operand as the runtime's C# string argument, or <c>null</c> when absent (a CHARACTERS
    /// pattern / an omitted delimiter). An identifier operand reads its FULL raw image at run time — current
    /// content, no trimming (GR5; a PIC X(2) holding "A " is the two-character pattern "A "); a signed numeric
    /// operand reads de-signed (GR4d).</summary>
    private static string OperandTextOf(BoundOperand? op) =>
        op is null ? "null" : OperandText.AsString(op, deSign: true);
}
