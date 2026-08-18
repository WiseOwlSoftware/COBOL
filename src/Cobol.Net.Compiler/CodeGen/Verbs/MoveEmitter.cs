// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The MOVE-family emitter (P7 Step 9g — a real collaborator over the per-unit
/// <see cref="EmitContext"/>, extracted from the orchestrator partial): the pure per-kind MOVE renderer
/// (the dispatch travels on <c>BoundMove.Kinds</c>, P7 Step 7) and <c>ConvertSource</c> — the ONE MOVE
/// conversion path Report Writer's synthetic print items and INITIALIZE's implicit stores share.</summary>
internal sealed class MoveEmitter(EmitContext ctx, NumericRenderer num, ReferenceResolver refs)
{
    /// <summary>Render one MOVE — a PURE per-kind renderer since P7 Step 7: the dispatch travels on the node
    /// (<see cref="BoundMove.Kinds"/>, classified once by <see cref="MoveClassifier"/> at construction — ISO
    /// §14.9.25.4 GR4's elementary-vs-group decision incl. the sender side, NC105A MOVE-TEST-F1-16/-17/-20/-36/-38);
    /// the emitter re-derives nothing.</summary>
    public void Emit(BoundMove m)
    {
        for (int i = 0; i < m.Targets.Count; i++)
        {
            var target = m.Targets[i];
            switch (m.Kinds[i])
            {
                case MoveKind.RefModSlice:
                    // The slice takes the source's characters (SpliceInto left-justifies, space-fills, and
                    // truncates to the slice length), so pass the raw image, not a full-width store. EXCEPT a
                    // figurative source, which fills EVERY position of the slice (ISO §8.3.3.6.4 GR2 — repeated
                    // to the associated fixed-length item; §8.4.3.3 GR5/GR6 make the slice a unique fixed-length
                    // item); the fill char is category-aware (national/boolean = the D-N3 pin, not the PCS extreme).
                    var rmp = (RefModPlace)target;
                    ctx.Writer.Line(m.Source is BoundFigurative fig
                        ? PlaceRenderer.WriteFill(rmp, FigurativeConstants.Fill(fig.Kind, ctx.Data.Collating, rmp.Inner.Item.Pic?.Category, ctx.Data.NationalCollating))
                        : PlaceRenderer.Write(rmp, OperandText.AsString(m.Source, num)));
                    break;
                case MoveKind.Group:
                    EmitGroupMove(target, m.Source);
                    break;
                case MoveKind.GroupToElementary:
                    EmitGroupToElementaryMove(target, m.Source);
                    break;
                case MoveKind.FigurativeToNumericImage:
                    EmitFigurativeToNumericImage(target, m.Source);
                    break;
                case MoveKind.Convert:
                    // An ANY LENGTH receiver stores at the CARRIER's current length (ISO §13.18.2 GR1 — the
                    // item is n repetitions of its picture symbol, n = the activating argument's length).
                    ctx.Writer.Line(PlaceRenderer.Write(target, ConvertSource(m.Source, target.Item,
                        target.Item.IsAnyLength ? $"{PlaceRenderer.Read(target)}.Length" : null)));
                    break;
            }
        }
    }

    /// <summary>MOVE of an alphanumeric figurative constant (SPACE / QUOTE / HIGH-VALUE / LOW-VALUE) or an ALL
    /// "literal" containing a non-digit into an ELEMENTARY NUMERIC receiver — the PRE-REMOVAL semantics of a
    /// construct ISO/IEC 1989:2023 REMOVED (§14.9.25.3 SR5; Annex E.2 item 1 bullet 1 — permitted through 2014).
    /// The binder's "move-alphanumeric-figurative-removed-2023" gate carries the 0902 diagnostics; this emit path
    /// exists regardless, reachable at --std 85/2002/2014 and at 2023 --permissive. Provisional (ratified decision
    /// 1 — the legacy oracle is the interim authority for pre-2023 semantics of removed constructs): the fill
    /// CHARACTERS are deposited as the receiver's character image, repeated to its width (§8.3.3.6.4 GR2), exactly
    /// the legacy byte engine's fill — MOVE QUOTE TO PIC 9(3) leaves three quotation marks, IS NUMERIC is then
    /// false, and a later numeric read decodes deterministically (§14.6.13.2: a non-digit contributes no digit).
    /// The binder flagged an eligible numeric-DISPLAY receiver <see cref="DataItem.StoreAsImage"/> (REUSING the
    /// §14.9 MOVE GR4 whole-group image substrate — never a parallel mechanism), so the store is a plain image
    /// write; a Tier-B REDEFINES window / NumericImagePlace writes its character image directly. Only a
    /// non-DISPLAY numeric receiver (BINARY / PACKED / COMP-5 / float — no character image in the typed-native
    /// model) remains a NARROW loud guard (§1.4). Eligibility is the NODE's <see cref="MoveKind"/> (classified
    /// once — figurative ZERO and digit-only ALL classify as Convert, VALUE moves not fills).</summary>
    private void EmitFigurativeToNumericImage(Place target, BoundOperand source)
    {
        // The KIND already decided eligibility (MoveClassifier — numeric receiver + S/Q/H/L figurative or
        // non-digit ALL); an unmatched shape here is a classifier/renderer drift bug, not a fallthrough.
        string image = source switch
        {
            BoundFigurative { Kind: 'S' or 'Q' or 'H' or 'L' } f =>
                $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating)}, {target.Item.ImageWidth})",
            BoundAllLiteral { IsDigitOnly: false } a =>
                CsLiteral(EmitText.RepeatToWidth(a.Literal, target.Item.ImageWidth)),
            _ => throw new InvalidOperationException(
                $"MoveKind.FigurativeToNumericImage with a non-fill source ({source.GetType().Name}) — "
                + "MoveClassifier and this renderer have drifted"),
        };
        ctx.Writer.Line(target.Item.StoreAsImage || target is RedefViewPlace or NumericImagePlace
            ? PlaceRenderer.Write(target, image)
            : LoudStmt($"MOVE of an alphanumeric figurative constant into the numeric item "
                + $"'{target.Item.CobolName}' without image-backed storage (a BINARY/PACKED/COMP-5/float or "
                + "Tier-A shared-storage receiver has no character image to fill — narrow pre-2023 residue of "
                + "the move ISO 2023 removed, §14.9.25.3 SR5 / Annex E.2 item 1)"));
    }

    // IsGroupSender lives on MoveClassifier since P7 Step 7 (the ONE GR4 sender-side test).

    /// <summary>MOVE from a GROUP sender into an ELEMENTARY receiver — a GROUP MOVE (ISO §14.9.25.4 GR4): treated
    /// "exactly as if it were an alphanumeric to alphanumeric elementary move, except that there is no conversion
    /// of data from one form of internal representation to another", the receiving area "filled without
    /// consideration for the individual elementary or group items" — NO numeric conversion (F1-16), NO editing
    /// into a numeric-edited or alphanumeric-edited mask (F1-38 / F1-20, F1-36; GR5 editing applies only to valid
    /// ELEMENTARY moves), NO de-editing. Alignment is §14.6.8 alphanumeric: left-justified, right space-fill /
    /// right truncation — and the receiver's JUSTIFIED still applies ("exactly as if … elementary move";
    /// §13.18.34 attaches to the receiver). The raw image is then deposited by the receiver's STORAGE shape:
    /// string-backed receivers store the width-fitted image directly; a native typed numeric receiver deposits
    /// then decodes through the ONE storage-form bridge (<c>StoreDisplay</c> — the deterministic zoned
    /// decode of possibly-incompatible content that §14.6.13.2 permits; EC-DATA-INCOMPATIBLE is a later EC
    /// slice). A float receiver has no character image — loud (§1.4, the Tier-C island rule).</summary>
    private void EmitGroupToElementaryMove(Place target, BoundOperand source)
    {
        var item = target.Item;
        if (!item.IsImageCapable)
        {
            ctx.Writer.Line(LoudStmt(TierCIsland.Reason(item, "group MOVE into")));
            return;
        }
        // The width-fitted image (§14.6.8): receiver character-position count via the ONE canonical ImageWidth
        // (V occupies no position; SIGN SEPARATE adds one; P adds none — §13.18.40). deSign is moot for a group.
        // An ANY LENGTH receiver's width exists only at runtime (§13.18.2 GR1 — the carrier's current length).
        string gw = item.IsAnyLength ? $"{PlaceRenderer.Read(target)}.Length" : $"{item.ImageWidth}";
        string image = item.Justified
            ? RuntimeApi.StrStoreJustified(OperandText.AsString(source, num), gw)
            : RuntimeApi.StrStore(OperandText.AsString(source, num), gw);
        // A native typed numeric receiver (long/Int128 backing) needs the decode half of the bridge; every
        // string-backed shape — alphanumeric [edited], numeric-edited, StoreAsImage numeric, a Tier-B
        // RedefViewPlace char window, a NumericImagePlace (its Write IS the decode) — stores the image as-is.
        bool nativeNumeric = item.Pic is { Category: PicCategory.Numeric } && !item.StoreAsImage
            && target is not RedefViewPlace and not NumericImagePlace;
        ctx.Writer.Line(nativeNumeric
            ? PlaceRenderer.Write(target, RuntimeApi.NumStoreDisplay(image, item.ProfileName, PlaceRenderer.Read(target)))
            : PlaceRenderer.Write(target, image));
    }

    /// <summary>MOVE into a whole group (alphanumeric semantics, ISO §14.9 MOVE GR4 — no conversion, filled without
    /// consideration for subordinate items): the source's character image fills the group's leaves via
    /// <c>FromImage</c>. Handles any image-capable group — alphanumeric, numeric-edited, numeric-DISPLAY (native or
    /// stored as its character image, <see cref="DataItem.StoreAsImage"/>), and BINARY/PACKED leaves (their image
    /// slice is the zoned digit form, the §13.18.60 USAGE GR4 implementor representation — COBOLNET_DESIGN §14.4).
    /// Only a group with a float/COMP-5/INDEX leaf stays the genuine Tier-C byte-island (deferred, loud).</summary>
    private void EmitGroupMove(Place target, BoundOperand source)
    {
        if (!target.Item.IsCharacterImage)
        {
            // A group MOVE is a content copy of the underlying representation, without conversion (ISO
            // §14.9.25.4 GR4 — both operands treated as elementary alphanumeric). When the source is a group
            // whose flattened leaf LAYOUT is positionally identical to the receiver's (same usage / digits /
            // scale / sign / width leaf-by-leaf — NC107A's all-COMP U5 → U9), that representation copy is
            // exactly a memberwise leaf copy — kept FIRST: under the digit-image representation both paths are
            // correct (GR4's representation copy ≡ memberwise copy for identical layouts), but the memberwise
            // path skips the encode/decode round trip and is the locked NC107A shape.
            if (source is BoundFieldOperand { Place.Item: { IsGroup: true } srcGroup }
                && AlignedLeafPairs(srcGroup, target.Item) is { } pairs
                && pairs.Select(p => (S: refs.ResolveItem(p.Src), T: refs.ResolveItem(p.Tgt))).ToList()
                    is { } resolved && resolved.All(r => r.S is not null && r.T is not null))
            {
                foreach (var (s, t) in resolved)
                    ctx.Writer.Line(PlaceRenderer.Write(t!, PlaceRenderer.Read(s!)));
                return;
            }
            // Non-aligned layouts (ST127A's 10-leaf WS twin → 11-leaf SD record) and class-view sources
            // (ST134A's SAME-RECORD-AREA window → SD record) fall through to the image path below: an
            // image-capable receiver distributes the source's character image via FromImage exactly like a
            // character group — its fixed-point leaves decode their zoned slices (GR4: filled without
            // consideration for the individual items, over the implementor digit-image representation). Only
            // the genuinely incapable receiver (float/COMP-5/INDEX leaf) stays loud (§1.4).
            if (!target.Item.IsImageCapable)
            {
                ctx.Writer.Line(LoudStmt(TierCIsland.Reason(target.Item, "MOVE to group")));
                return;
            }
        }
        int width = target.Item.ImageWidth;
        // §14.9.25.4 GR4: a GROUP receiver makes the move NON-elementary — "treated as an alphanumeric to alphanumeric
        // elementary move, EXCEPT that there is no conversion of data from one form of internal representation to
        // another". GR6a's operational-sign drop applies only to valid ELEMENTARY moves, so a signed-numeric elementary
        // source keeps its DISPLAY overpunch image here (deSign:false — the sign-aware CobolNum.FormatDisplay), copied
        // verbatim into the group's leaves; this matches the already-correct GROUP-sender path (OperandText.AsImage
        // keeps the sign). A no-op for non-numeric / unsigned-numeric senders. (CA28.)
        // ALL "literal" repeats to the RECEIVER width (ISO §8.3.3.6.4 GR2) — space-padding it would fill the group's
        // tail leaves with blanks instead of the repeated pattern (NC243A's 7-dim table seed).
        string image = source is BoundFigurative f
            ? $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating)}, {width})"
            : source is BoundAllLiteral all
            ? CsLiteral(EmitText.RepeatToWidth(all.Literal, width))
            : RuntimeApi.StrStore(OperandText.AsString(source, num, deSign: false), $"{width}");
        // ISO §13.18.38 GR8: an occurs-depending group RECEIVER with data-name-1 OUTSIDE the group uses only the
        // CURRENT-count part (positions past the count are not modified, GR8a); with data-name-1 INSIDE, the MAXIMUM
        // length is used (GR8b — the normal full-width FromImage). A Tier-B REDEFINES group view's image IS its
        // character window. A normal record-struct group distributes the image into its typed leaves via FromImage.
        ctx.Writer.Line(target switch
        {
            OdoGroupPlace { DependingInside: false } odo => PlaceRenderer.ReceiveInto(odo, image),
            RedefViewPlace => PlaceRenderer.Write(target, image),
            // A group receiver nested under an OCCURS DYNAMIC level (data-model D9): distribute the image through the
            // RECEIVING accessor (RefReceiving grows-and-seeds past the current capacity, §8.5.1.9.3), NOT target.Read()
            // (=RefSending, which drops an out-of-capacity write into benign scratch — silent data loss).
            DynTablePlace dyn => $"{PlaceRenderer.RenderPath(dyn.Path, AccessDir.Receiving)}.FromImage({image});",
            _ => $"{PlaceRenderer.Read(target)}.FromImage({image});",
        });
    }

    /// <summary>The positionally-paired leaves of two groups whose flattened layouts are IDENTICAL — each pair
    /// shares usage, digit count, scale, sign and storage shape, and neither group involves OCCURS, REDEFINES,
    /// RENAMES or reference ambiguity (so <see cref="ReferenceResolver.ResolveItem"/> is exact). Null when the
    /// layouts differ — the caller falls back to the Tier-C loud guard. (ISO §14.9.25.4 GR4: the group move
    /// copies the underlying representation; identical layouts make that a memberwise copy.)</summary>
    private static List<(DataItem Src, DataItem Tgt)>? AlignedLeafPairs(DataItem source, DataItem target)
    {
        // An OCCURS anywhere on the ancestor chain means the group reference is an ELEMENT (needs subscripts
        // this item-level resolution cannot supply) — not this fast path.
        for (var a = source; a is not null; a = a.Parent) if (a.Occurs is not null) return null;
        for (var a = target; a is not null; a = a.Parent) if (a.Occurs is not null) return null;
        var src = new List<DataItem>();
        var tgt = new List<DataItem>();
        if (!Flatten(source, src) || !Flatten(target, tgt) || src.Count != tgt.Count || src.Count == 0)
            return null;
        for (int i = 0; i < src.Count; i++)
        {
            var (a, b) = (src[i].Pic!, tgt[i].Pic!);
            if (a.Usage != b.Usage || a.Category != b.Category || a.Digits != b.Digits || a.Scale != b.Scale
                || a.Signed != b.Signed || a.Length != b.Length || src[i].StoreAsImage != tgt[i].StoreAsImage)
                return null;
        }
        return src.Zip(tgt).ToList();

        // Declaration-order leaves; false when the subtree has a shape the pairing cannot prove equivalent.
        static bool Flatten(DataItem item, List<DataItem> leaves)
        {
            if (item.Occurs is not null || item.RedefinesTargetName is not null
                || item.Renames66.Count > 0 || item.Class is not null)
                return false;
            if (!item.IsGroup)
            {
                if (item.Pic is null) return false;
                leaves.Add(item);
                return true;
            }
            foreach (var c in item.Children)
                if (!Flatten(c, leaves))
                    return false;
            return true;
        }
    }

    /// <summary>The compile-time numeric value of a digit-only <c>ALL "literal"</c> moving to a numeric receiver
    /// (ISO §8.3.3.6.4 GR2 repetition + §14.9.25.4 GR6d3b — the digit string spans the receiver's digit positions,
    /// fraction digits included, so the unscaled value IS the repeated digit run at the receiver's scale). ≤18
    /// digit positions fold to a native <c>long</c> literal; a WIDE receiver (19–31 digits, COBOL-2002+ — ISO
    /// §8.3.1.2) decodes its digit run through the ONE deterministic digit decode (<c>FromAlphanumeric</c>,
    /// Int128 — numeric design D1).</summary>
    private static NumX AllDigitFill(string literal, PicInfo pic)
    {
        string digits = EmitText.RepeatToWidth(literal, Math.Max(pic.Digits, 1));
        return digits.Length <= 18
            ? new NumX($"{long.Parse(digits, System.Globalization.CultureInfo.InvariantCulture)}L", pic.Scale)
            : new NumX(RuntimeApi.NumFromAlphanumeric(CsLiteral(digits)), pic.Scale);
    }

    /// <summary>True when a MOVE source carries a NUMERIC VALUE for the §14.9.25.4 GR5 editing path into a
    /// numeric-edited receiver: a numeric literal/expression, figurative ZERO, a numeric data item, OR a
    /// numeric-EDITED data item (GR5 + GR6d1 — a numeric-edited sender is DE-EDITED to its numeric value, which may be
    /// signed, before being re-edited into the receiver mask). A plain alphanumeric source is NOT numeric — it moves
    /// as characters (the §14.9.25.3 Table-16 unsigned-integer edit at the fall-through arm).</summary>
    private static bool IsNumericOperand(BoundOperand source) => source switch
    {
        BoundNumericLiteral or BoundComputedOperand => true,
        BoundFigurative { Kind: 'Z' } => true,
        BoundFieldOperand f => f.Place.Item.Pic?.Category is PicCategory.Numeric or PicCategory.NumericEdited,
        _ => false,
    };

    /// <summary>The C# expression a MOVE source converts to when stored into <paramref name="target"/>.
    /// <paramref name="runtimeWidth"/> — the receiver's RUNTIME character count expression for an ANY LENGTH
    /// receiver (ISO §13.18.2 GR1: the item behaves as n repetitions of its picture symbol where n is the
    /// activating argument's length, so a MOVE stores at the CARRIER's current length, never Pic.Length=1);
    /// null (every other receiver) keeps the compile-time width.</summary>
    public string ConvertSource(BoundOperand source, DataItem target, string? runtimeWidth = null)
    {
        var pic = target.Pic!;
        // A DYNAMIC LENGTH receiver (ISO §8.5.1.10.4 / §13.18.19): store the sender's DISPLAY image, replacing the
        // old content, with the new length = the SENDING length TRUNCATED ON THE RIGHT to the LIMIT and NO padding
        // (the minimum length is zero, §13.18.19.4 GR1). A figurative constant OTHER THAN `ALL literal` has a
        // sending length of ONE character (§8.3.3.6.4 GR3b) — so MOVE SPACE/ZERO/HIGH-VALUE/… each store a single
        // fill character (length 1), never length 0; a zero-length literal "" is a BoundStringLiteral whose AsString
        // is "" (length 0 — honoring §14.9.25.4 GR2's NOT-SPACE-substituted rule for the dynamic-length receiver).
        // AsString already renders every source correctly (a figurative as new string(fill,1)), so the general
        // DynStore path is exhaustive. A PIC X/N dynamic-length item carries no edit mask, so this precedes the
        // fixed-width figurative/ALL and numeric-edited paths below.
        if (target.IsDynamicLength)
            return RuntimeApi.DynStore(OperandText.AsString(source, num, deSign: true), target.DynLengthLimit.ToString());
        string wN = runtimeWidth ?? pic.Length.ToString();   // the string-category store width (§13.18.2 GR1)
        // A figurative constant fills the receiver to its width (ISO §8.3.1.2 / §14.9.24) — EXCEPT figurative
        // ZERO into a numeric-edited receiver, which is the numeric value 0 EDITED into the mask (§14.9.25.4 GR5;
        // 'ZZ9.99' shows '  0.00', not a zero-fill), and EXCEPT an alphanumeric-EDITED receiver, whose insertion
        // positions keep their characters under the editing move (GR5 — MOVE SPACES TO 'XXXBXX/XX' yields '/' at
        // its position, NC223A INI-TEST-GF-1; handled in the switch below).
        if (source is BoundFigurative f && pic.Category is not PicCategory.Numeric
            && !(f.Kind is 'Z' && pic.Category is PicCategory.NumericEdited)
            && !(pic.Category is PicCategory.Alphanumeric && pic.EditMask is not null))
            // Category-aware fill: a national/boolean receiver's HIGH/LOW-VALUE reads its OWN sequence — the
            // explicit national PCS extremes when declared, else the D-N3 pin — never the ALPHANUMERIC
            // program collating sequence's extreme (§8.3.3.6 GR6/GR7 over the national sequence).
            return $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating, pic.Category, ctx.Data.NationalCollating)}, {wN})";
        // ALL "literal" repeats the literal to the receiver width (ISO §8.3.3.6.4 GR2). An ANY LENGTH
        // receiver's width exists only at runtime — repeat at runtime, then width-fit (§13.18.2 GR1).
        if (source is BoundAllLiteral a && pic.Category is not PicCategory.Numeric)
            return runtimeWidth is null
                ? CsLiteral(EmitText.RepeatToWidth(a.Literal, pic.Length))
                : RuntimeApi.StrStore(RuntimeApi.StrRepeat(CsLiteral(a.Literal), runtimeWidth), runtimeWidth);

        switch (pic.Category)
        {
            // A NUMERIC source moving to a numeric-edited receiver is EDITED into the receiver's picture
            // (ISO §14.9.25.4 GR5 — alignment + editing); an alphanumeric source stays a plain character move.
            case PicCategory.NumericEdited when IsNumericOperand(source):
                NumX e = num.AsNum(source, SenderContext(target));
                // A float (Real) source lands into the edited receiver via the runtime's ToScaled at the MASK's fraction
                // scale (MOVE truncates toward zero, §14.6.8.2) — the edit Format takes a scaled Int128, not a double
                // (D16 review: the numeric-edited path was missed by the Real integration → CS1503). NB the mask scale
                // is the runtime's MaskScale, NOT pic.Scale (a numeric-edited item's Scale is 0 — the point is in the mask).
                int ems = RuntimeApi.MaskScale(pic.EditMask!, '$', ctx.Data.DecimalPointIsComma);
                // A STANDARD-DECIMAL intermediate lands at the MASK's scale (the §14.7 final transfer — the same
                // form ArithmeticEmitter's edited path uses; fix-queue PB65: MOVE FUNCTION E under the mode handed
                // the CobolDec to the Int128 edit path, CS1503 on conforming source).
                string editVal = e.Real ? RuntimeApi.FloatToScaled(e.Expr, $"{ems}", CobolRounding.Truncation)
                    : e.Dec ? RuntimeApi.DecToUnscaled(e.Expr, $"{ems}", CobolRounding.Truncation)
                    : e.Expr;
                int editScale = e.Real || e.Dec ? ems : e.Scale;
                return RuntimeApi.EditFormat(editVal, $"{editScale}", CsLiteral(pic.EditMask!), ArithmeticEmitter.BwzFlag(target) + ctx.EditCfg(pic) + RuntimeApi.EditsArg(pic.EditingRules));
            // An ELEMENTARY ALPHANUMERIC source into a numeric-edited receiver IS a legal move (§14.9.25.3
            // Table 16): the sending characters are treated as an unsigned integer and EDITED into the mask
            // (§14.9.25.4 GR5 — NC104A MOVE-TEST-F1-39: "12345" → $12,345.00), never a plain character copy.
            // (A GROUP sender never reaches here — GR4 makes that a group move, no editing: EmitGroupToElementaryMove.)
            case PicCategory.NumericEdited:
                return RuntimeApi.EditFormat(RuntimeApi.NumFromAlphanumeric(OperandText.AsString(source, num, deSign: true)), "0", CsLiteral(pic.EditMask!), ArithmeticEmitter.BwzFlag(target) + ctx.EditCfg(pic) + RuntimeApi.EditsArg(pic.EditingRules));
            // An ALPHANUMERIC-EDITED receiver places the source's characters into its X/A/9 positions with B 0 /
            // insertion (ISO §14.9.25.4 GR5 — alignment + editing; §13.18.40 simple insertion).
            case PicCategory.Alphanumeric when pic.EditMask is { } amask:
                // A figurative source supplies its fill for EVERY data position (§8.3.1.2 — repeated to width).
                string aeSrc = source is BoundFigurative ff
                    ? $"new string({FigurativeConstants.Fill(ff.Kind, ctx.Data.Collating)}, {pic.Length})"
                    : OperandText.AsString(source, num, deSign: true);
                return RuntimeApi.EditFormatAlphanumeric(aeSrc, CsLiteral(amask));
            case PicCategory.Alphanumeric:
                // A signed numeric source drops its operational sign into an alphanumeric receiver (ISO §14.9.25.4 GR6a);
                // a JUSTIFIED receiver right-justifies (left space-fill / left truncation, §14.9.25.4 GR6c).
                // wN: an ANY LENGTH receiver stores at its runtime length (§13.18.2 GR1), else Pic.Length.
                return target.Justified
                    ? RuntimeApi.StrStoreJustified(OperandText.AsString(source, num, deSign: true), wN)
                    : RuntimeApi.StrStore(OperandText.AsString(source, num, deSign: true), wN);
            // A NATIONAL receiver stores exactly like alphanumeric on the character substrate (§14.6.8.5 —
            // left-justify, national-space pad, right truncation; JUSTIFIED per §13.18.32): A→N widening,
            // N→N, 9→N digit imaging, and boolean→N all ride AsString under the D-N4 Latin-1 identity
            // correspondence (§14.9.25.4 GR6/GR6a).
            case PicCategory.National:
                return target.Justified
                    ? RuntimeApi.StrStoreJustified(OperandText.AsString(source, num, deSign: true), wN)
                    : RuntimeApi.StrStore(OperandText.AsString(source, num, deSign: true), wN);
            // A BOOLEAN receiver pads/left-fills with boolean ZEROS (§14.6.8.6; JUSTIFIED §13.18.32 GR2).
            // Figurative ZERO already early-returned above as a '0' fill; the SR7-illegal figurative shapes
            // never reach emit (bind-rejected, MoveCategoryLegality).
            case PicCategory.Boolean:
                return RuntimeApi.StrStoreBoolean(OperandText.AsString(source, num, deSign: true), wN, target.Justified);
            case PicCategory.Numeric:
                // A digit-only ALL "literal" repeats across the RECEIVER's digit positions (ISO §8.3.3.6.4 GR2 —
                // repetition to the associated item's size, truncated from the right; §14.9.25.4 GR6d3b — a
                // figurative sending operand takes the receiver's digit count, fraction digits included), so
                // every digit position holds the pattern: ALL "5" → PIC 9(3) stores 555; → PIC 9V9 stores 5.5
                // (unscaled 55 at scale 1 — legacy-oracle confirmed). Valid at EVERY edition for the
                // single-digit-ALL → integer case (§14.9.25.3 SR5, obsolete-flagged 0903 at 2023 by the binder);
                // the non-integer / multi-character shapes are 0902-gated at 2023 and keep these pre-removal
                // semantics at 85/2002/2014 and under --permissive (ALL "57" → PIC 9(3) stores 575, the legacy
                // oracle's '85-obsolete-element behavior — provisional, ratified decision 1). This was the
                // BoundAllLiteral runtime-loud latent bug (W2 track A): the move compiled then died in AsNum.
                // A float RECEIVER (COMP-1/2/FLOAT-*, D16) holds the algebraic value in a native float/double —
                // no PICTURE, no scaled-integer store, no SIZE ERROR (an arithmetic ±Inf is a valid value; §14.6.8.3
                // GR1). §14.6.13.2 dash-3: the source read is EC-DATA-NOT-FINITE-EXEMPT only when sending and receiving
                // share the SAME floating-point usage specification (a verbatim copy, endianness aside — Usage enum
                // equality; endianness is a separate phrase, not a Usage value). A fixed source or a DIFFERENT float
                // usage (COMP-1↔COMP-2, and the reachable narrowing) keeps the CobolFloat.Sending wrap.
                if (pic.IsFloat)
                {
                    bool sameUsage = source is BoundFieldOperand fsrc
                        && fsrc.Place.Item.Pic is { IsFloat: true } sp && sp.Usage == pic.Usage;
                    string srcD = NumericRenderer.Real(num.AsNum(source, ReceiverContext.None, floatSendingExempt: sameUsage));
                    // §14.9.25.4 GR4 step 4a: a MOVE into a SINGLE-precision receiver raises EC-DATA-OVERFLOW when a
                    // finite source overflows to ±Inf (StoreSingleChecked). A double receiver cannot overflow from a
                    // finite double, so it keeps the bare cast.
                    return pic.IsSingle ? RuntimeApi.FloatStoreSingleChecked(srcD) : $"({pic.ClrType})({srcD})";
                }
                NumX n = source is BoundAllLiteral { IsDigitOnly: true } allDigit
                    ? AllDigitFill(allDigit.Literal, pic)
                    : num.AsNum(source, SenderContext(target));
                // A float SOURCE lands into the fixed receiver via the runtime's ToScaled at the receiver scale (MOVE
                // truncates toward zero — §14.6.8.2 GR2/GR4 implementor-defined) then the ordinary store funnel
                // (rescale identity ⇒ no double-rounding; the digit-capacity + SIZE ERROR check still applies).
                int recvScaleM = target.Pic!.Scale;
                // A STANDARD-DECIMAL intermediate stores through the SDIDI Store overload (the §14.7 final
                // transfer), exactly as the arithmetic path does — fix-queue PB65: the MOVE arm was the one
                // numeric consumer without the Dec case, so MOVE FUNCTION E was a backend CS1503.
                // …and every carrier stores through the ONE store (NumericRenderer.StoreExpr — kb/Work PB84), so
                // the Dec/float/native switch is written once for MOVE, the arithmetic store and INVOKE alike.
                string stored = ArithmeticEmitter.Narrow(NumericRenderer.StoreExpr(n, recvScaleM, target.ProfileName), target);
                // A whole-group-aliased numeric-DISPLAY receiver stores its character image, not the raw long.
                return target.StoreAsImage ? RuntimeApi.NumFormatImage(stored, target.ProfileName) : stored;
            default:
                return "default";
        }
    }

    /// <summary>The RECEIVER-SCALE context a numeric MOVE source renders under (fix-queue PB60 /
    /// RV-15.68.4-1 half 2). <c>Receiverless</c> STAYS TRUE — the float family keeps its deliberate binary64
    /// sending value (only a genuinely receiver-less render keeps a float in binary64, per
    /// <see cref="ReceiverContext"/>'s own doc; MOVE's float landing owns the <c>ToScaled</c> at the receiver
    /// scale) — but the SCALE and the Int128-headroom cap now carry the ACTUAL receiver, so an exact-family
    /// intrinsic source renders at the precision the receiver can store instead of the receiver-less floor.
    /// Measured: <c>MOVE FUNCTION NUMVAL-C("$0.123456789") TO PIC S9(4)V9(9)</c> stored 0.123456000 while
    /// COMPUTE stored the exact value — §15.68.4 r1 carries no channel qualification. The scale mirrors
    /// <c>ArithmeticEmitter.ScaleOf</c> (a numeric-edited receiver's scale is its MASK's) and the digits
    /// <c>ArithmeticEmitter.IntDigitsOf</c> (from <c>DigitPositions</c>, never the '9' count — under-counting
    /// breaks the PB13 saturation-is-visible invariant).</summary>
    private ReceiverContext SenderContext(DataItem target)
    {
        if (target.Pic is not { } pic) return ReceiverContext.None;
        int scale = pic is { Category: PicCategory.NumericEdited, EditMask: { } m }
            ? RuntimeApi.MaskScale(m, '$', ctx.Data.DecimalPointIsComma)
            : pic.Scale;
        return ReceiverContext.None with { Scale = scale, IntegerDigits = Math.Max(0, pic.DigitPositions - scale), MoveSender = true };
    }
}
