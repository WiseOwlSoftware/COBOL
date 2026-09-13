// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ <b>THE ONE MATERIALIZATION OF A SENDING VALUE</b> — kb/Work PB394.
///
/// <para><b>The rule, in two verbs.</b> ISO §14.9.25.4 GR1 (MOVE): "If identifier-1 is reference-modified,
/// subscripted, or is a function-identifier, the reference modifier, subscript, or function-identifier is
/// evaluated only once, immediately before data is moved to the first of the receiving operands", and the rule
/// then writes the required shape out as an EQUIVALENCE — <c>MOVE a (b) TO b, c (b)</c> is "equivalent to:
/// MOVE a (b) TO temp / MOVE temp TO b / MOVE temp to c (b) … where 'temp' is an intermediate result item
/// provided by the implementor". ISO §14.9.13.4 GR3 (EVALUATE): "At the beginning of the execution of the
/// EVALUATE statement, each selection subject is evaluated and assigned a value, a range of values, or a truth
/// value". Both sentences say ONE thing: <b>the sending value is materialized, once, before it is used.</b></para>
///
/// <para><b>Why it is a mechanism and not two edits (CLAUDE.md rule 5).</b> Before this, neither verb had
/// anywhere to PUT a value, so each re-ran the source EXPRESSION at every place it needed one — MOVE once per
/// receiver (<c>MoveEmitter.Emit</c>'s per-target loop re-rendered <c>BoundMove.Source</c> in all four
/// <c>MoveKind</c> arms), EVALUATE once per selection pair and TWICE per THRU arm. The two defects had one
/// cause, and the repair is the missing noun: an intermediate result item. Every future verb with one sender
/// and many uses (<c>SET … TO</c>, <c>ADD … TO</c>, <c>STRING … INTO</c>) asks this class for one rather than
/// growing its own hoist.</para>
///
/// <para><b>The item's description is the OPERAND's own</b>, so the store into it is an identity move and the
/// equivalence is exact rather than approximate:</para>
/// <list type="bullet">
///   <item><b>A data item</b> (<see cref="BoundFieldOperand"/> over an undecorated place,
///     <see cref="BoundCurrentRecord"/>) — the item's description, cloned. <c>DataBinder.CreateCompilerTemp</c>
///     copies the PICTURE and the description clauses at the root and deep-clones a group's subtree, and it does
///     NOT copy the root's OCCURS, which is exactly right: the sender <c>a (b)</c> is ONE OCCURRENCE, and the
///     temp holds one occurrence's value.</item>
///   <item><b>A reference-modified data item</b> — §8.4.3.3.4 GR6's "unique data item", which "has the same
///     class, category, and usage as that defined for identifier-1" with the three lettered exceptions
///     (<see cref="RefModPlace.Category"/> is the ONE reader of that rule). Its LENGTH is a run-time value, so
///     the temp is a RUN-TIME-LENGTH item — see below.</item>
///   <item><b>A function-identifier</b> — §15.4: "The evaluation of a function produces a returned value in a
///     temporary elementary data item", and §15.4.1: "When native arithmetic is in effect, the characteristics
///     and representation of the returned value are defined by the implementor". A NUMERIC result lands in the
///     same §15.4 temporary description this compiler already provides for a function-bearing subscript
///     (<see cref="FunctionValuePic"/>); an alphanumeric / national / boolean result has a run-time length and
///     lands in a run-time-length item.</item>
/// </list>
///
/// <para><b>The run-time-length carrier</b> is a dynamic-length elementary item (§8.5.1.10), because
/// §8.5.1.10.4 states precisely the property the intermediate needs: "A dynamic-length elementary item that is
/// used as a sending operand or is reference-modified is treated as a fixed-length data item whose length is
/// the dynamic-length elementary item's current length." ⚠ §8.5.1.10.1's "may be category alphanumeric or
/// national" constrains what a programmer may DECLARE with the §13.18.19 DYNAMIC LENGTH clause; this item is
/// not declared by anyone — it is §14.9.25.4 GR1's "intermediate result item provided by the implementor" and
/// §15.4.1's implementor-defined representation — so a BOOLEAN slice's carrier keeps category boolean and the
/// emitter's category-aware store (<c>RuntimeApi.StrStoreBoolean</c>) still sees the right class.</para>
///
/// <para><b>What is NOT materialized, and why each is a decision rather than an omission:</b></para>
/// <list type="bullet">
///   <item>A <b>literal</b> or <b>figurative constant</b> — GR1's antecedent names only a reference-modified,
///     subscripted or function-identifier operand, a literal's value cannot change, and a figurative /
///     <c>ALL "literal"</c> operand has NO description of its own to clone: §8.3.3.6.4 GR2 sizes it from THE
///     RECEIVER, so materializing it at one receiver's width would corrupt the others.</item>
///   <item>A <b>variable-length group</b> (§8.5.1.12) — the clone would carry an <c>OCCURS … DEPENDING ON</c>
///     whose data-name still refers to the ORIGINAL program item, so the temp's extent would be re-read after
///     the first store instead of frozen. That is the one sentence of GR1 this mechanism does not yet
///     discharge ("The length of the data item referenced by identifier-1 is evaluated only once"), and it is
///     recorded as UNVERIFIED on row GR-14.9.25.4-1 rather than silently claimed.</item>
///   <item>A <b>boolean expression</b> or an <b>error operand</b> — neither is a §14.9.25 sending operand this
///     path can reach with more than one use (a boolean-expression sender is COMPUTE Format 2's channel), and
///     an error operand was already diagnosed.</item>
/// </list>
/// </summary>
internal sealed class SendingValueTemp(BinderContext ctx)
{
    /// <summary>The §15.4 temporary's description for a NUMERIC function-identifier value — THE SAME shape
    /// <c>StatementBinder.MaterializeSubscriptSegment</c> gives the §15.4 temporary of a function-bearing
    /// subscript, written once here so the two cannot drift. §15.4.1: "When native arithmetic is in effect, the
    /// characteristics and representation of the returned value are defined by the implementor."
    /// <para>21 integer digits × 9 fraction digits — 30 total, so the item takes the <c>Int128</c> wide tier
    /// (<c>PicInfo.IsWide</c>, &gt;18 digits) and no function value a program can consume overflows it; the
    /// fraction is carried because a scale-0 temp would TRUNCATE, turning a legal non-integer result into a
    /// different value. 9 is also the working scale an intrinsic renders at when no receiver widens it
    /// (<c>ReceiverContext</c>), so for every receiver of scale ≤ 9 the materialized value is bit-identical to
    /// the un-hoisted render.</para></summary>
    internal static readonly PicInfo FunctionValuePic =
        new(PicCategory.Numeric, Usage.Display, Length: 30, Digits: 30, Scale: 9, Signed: true);

    /// <summary>Materialize <paramref name="op"/>'s value into the implementor's intermediate result item and
    /// return the <see cref="Place"/> holding it, or <see langword="null"/> when the operand needs no
    /// materialization (a literal / figurative constant) or carries a description this clone cannot freeze (a
    /// variable-length group). The store registers as a statement-scoped PRE-op on the shared
    /// <c>DataBinder.PendingPreOps</c>, which the <c>BindStatement</c> chokepoint drains into a
    /// <c>BoundSequence</c> ahead of the carrying statement — "immediately before data is moved to the first of
    /// the receiving operands" (§14.9.25.4 GR1) and "at the beginning of the execution of the EVALUATE
    /// statement" (§14.9.13.4 GR3) are the same position in the generated code.</summary>
    /// <param name="op">The sending operand / selection subject to freeze.</param>
    /// <param name="tag">A short source tag for the temp's synthesized names (diagnostics and readability only).</param>
    internal Place? Materialize(BoundOperand op, string tag)
    {
        if (Model(op) is not { } model) return null;
        var temp = ctx.Data.CreateCompilerTemp(model.Item, "__SENDVAL-", "__sendval", tag);
        // The run-time-length attributes are not part of the cloned DESCRIPTION (CreateCompilerTemp copies the
        // PICTURE and the description clauses); §8.5.1.10 is a storage property, set here for the carrier shapes.
        temp.IsDynamicLength = model.DynLimit > 0;
        temp.DynLengthLimit = model.DynLimit;
        if (ctx.Refs.ResolveItem(temp) is not { } place) return null;
        // The store is a plain BoundMove, NOT a re-entry into MoveBinder.BindMoveOf: the statement's own syntax
        // rules (the SR5 edition gates, the Table-16 legality, SR2's strong-typing check, SR9's §8.5.1.12
        // compatibility screen) are about the operands the PROGRAMMER wrote and have already been applied to
        // them; re-running them against the implementor's intermediate would report the same source twice and —
        // for a strongly-typed sender, whose clone is a DIFFERENT type-name — report a violation that no rule
        // states. The move itself is an identity copy by construction (the temp's description IS the sender's).
        ctx.Data.PendingPreOps.Add(new BoundMove(op, [place]));
        return place;
    }

    /// <summary>The intermediate result item's description: the cloned model plus, for a run-time-length
    /// carrier, its §8.5.1.10 limit (0 = a fixed-length clone). Null when the operand is not materialized.</summary>
    private readonly record struct TempModel(DataItem Item, int DynLimit);

    /// <summary>⛔ THE ONE description derivation — every <see cref="BoundOperand"/> leaf is named, so a leaf
    /// added tomorrow is a COMPILE error here rather than a silent fall-through to "no hoist" (the
    /// feedback_a_dead_lookup_is_also_unverified shape: a default arm that quietly means "correct as it is").</summary>
    private TempModel? Model(BoundOperand op) => op switch
    {
        // §8.3.3.6.4 GR2 sizes a figurative / ALL "literal" from the RECEIVER, and §14.9.25.4 GR1's antecedent
        // names neither; a literal's value cannot change. No description of their own ⇒ no intermediate.
        BoundNumericLiteral or BoundStringLiteral or BoundFigurative or BoundAllLiteral => null,
        BoundOperandError => null,                 // already diagnosed
        BoundBoolOperand => null,                  // the §8.8.2 boolean-expression channel (COMPUTE Format 2)
        BoundFieldOperand f => OfPlace(f.Place),
        BoundCurrentRecord cr => OfPlace(cr.Area),
        BoundComputedOperand c => OfComputed(c),
        // Every leaf of the hierarchy is named above; the arm exists because C# cannot prove a public abstract
        // record's leaf set is closed. It is LOUD rather than a silent "no hoist", which would let a new operand
        // form inherit the very defect this class removes (feedback_a_dead_lookup_is_also_unverified).
        _ => throw new InvalidOperationException(
            $"SendingValueTemp: the BoundOperand leaf '{op.GetType().Name}' has no §14.9.25.4 GR1 / §14.9.13.4 "
            + "GR3 intermediate-result description — name it in SendingValueTemp.Model"),
    };

    /// <summary>A data item's intermediate: the item's own description, or §8.4.3.3.4 GR6's "unique data item"
    /// for a reference-modified one.</summary>
    private TempModel? OfPlace(Place place)
    {
        // A reference modifier's LENGTH is an arithmetic expression (§8.4.3.3.3 SR4) — a run-time value — so the
        // unique data item GR6 describes is carried by a run-time-length item, whose current length IS the slice
        // length (§8.5.1.10.4). Its class/category/usage are GR6's, read through the ONE reader.
        if (place is RefModPlace rm)
            return new TempModel(
                new DataItem
                {
                    Level = 1, CobolName = "__SENDVAL-REFMOD", CsName = "__sendvalRefMod",
                    Pic = new PicInfo(rm.Category, UsageOf(rm.Inner.Item), Length: 0, Digits: 0, Scale: 0, Signed: false),
                },
                DynLimit: Math.Max(1, rm.Inner.Item.ImageWidth));
        var item = place.Item;
        // A DYNAMIC-LENGTH elementary sender IS frozen exactly, by the carrier the standard defines for it:
        // §8.5.1.10.4 — "A dynamic-length elementary item that is used as a sending operand … is treated as a
        // fixed-length data item whose length is the dynamic-length elementary item's current length" — so the
        // intermediate is a dynamic-length item of the same limit, and GR1's "The length of the data item
        // referenced by identifier-1 is evaluated only once" holds for it.
        if (item is { IsDynamicLength: true, IsGroup: false })
            return new TempModel(item, DynLimit: Math.Max(1, item.DynLengthLimit));
        // ⛔ A GROUP WITH A RUN-TIME EXTENT IS NOT FROZEN BY A CLONE, AND THAT IS MEASURED, NOT ASSUMED. The
        // intermediate result item is a CLONED DESCRIPTION, and a description clone carries a length that is
        // FIXED at compile time: an OCCURS DEPENDING member's clone would name data-name-1 with no resolved item
        // (DataBinder.OdoResolve runs over the DATA DIVISION forest, long before any procedure-time temp exists)
        // and so behaves as the §8.5.1.8 physical capacity, while a dynamic-length member's or dynamic-capacity
        // table's clone carries neither the limit nor the capacity. The MAXIMUM extent is not the sender's
        // length, and §14.9.25.4 GR1 requires the sender's: measured on this tree with a 3-of-5 occurs-depending
        // group into a JUSTIFIED PIC X(5) receiver — the single-receiver path gives "  125" (§13.18.38.4 GR8 a)'s
        // current extent, right-aligned by §13.18.34) and a maximum-extent intermediate gives "125  ".
        // <para>So this shape is left UN-MATERIALIZED: its sending operand is still re-read per receiver, which
        // is the pre-existing state of GR1's "The length of the data item referenced by identifier-1 is
        // evaluated only once" for a variable-extent sender. Recorded as the residual on GR-14.9.25.4-1 rather
        // than papered over with an intermediate whose length is a different number. Freezing it needs a SECOND
        // temp holding data-name-1's value at the hoist, with the clone's OCCURS DEPENDING pointed at it — a
        // change to the shared temp constructor, not to this switch.</para>
        if (item.IsGroup && (VariableLengthCompatibility.IsVariableLength(item) || HasRunTimeExtent(item)))
            return null;
        return new TempModel(item, DynLimit: 0);
    }

    /// <summary>The §15.4 temporary of a function-identifier's returned value. A NUMERIC result takes the
    /// <see cref="FunctionValuePic"/> shape §15.4.1 leaves to the implementor; an alphanumeric / national /
    /// boolean result has a run-time length (FUNCTION TRIM's is its argument's content, CONCAT's the sum of its
    /// arguments') and takes the §8.5.1.10.4 carrier.</summary>
    private TempModel? OfComputed(BoundComputedOperand c)
    {
        // §15.2 classifies every function; the bound node carries the call's RESOLVED category
        // (BoundIntrinsicCall.ResultCategory — never the declared column, for the twenty argument-typed rows).
        // A folded arithmetic sum (FUNCTION LENGTH of a variable-length group, §15.50.4 r7) and every other
        // arithmetic-expression operand are numeric by §8.8.1.
        PicCategory cat = c.Expr is BoundIntrinsicCall ic ? ic.ResultCategory : PicCategory.Numeric;
        if (cat is PicCategory.Numeric)
            return new TempModel(
                new DataItem { Level = 1, CobolName = "__SENDVAL-FN", CsName = "__sendvalFn", Pic = FunctionValuePic },
                DynLimit: 0);
        return new TempModel(
            new DataItem
            {
                Level = 1, CobolName = "__SENDVAL-FNS", CsName = "__sendvalFns",
                Pic = new PicInfo(cat, cat is PicCategory.National ? Usage.National : Usage.Display,
                                  Length: 0, Digits: 0, Scale: 0, Signed: false),
            },
            DynLimit: FunctionTextLimit);
    }

    /// <summary>The implementor's maximum length for a §15.4 returned value of a character category — the bound
    /// §15.4 names ("If the length of the returned value exceeds the maximum length specified by the implementor
    /// for a returned value, an EC-ARGUMENT-FUNCTION exception condition is set to exist"). 32 767 character
    /// positions, the largest fixed-length alphanumeric item this compiler admits.</summary>
    private const int FunctionTextLimit = 32767;

    /// <summary>The usage the §8.4.3.3.4 GR6 unique data item inherits ("the same class, category, and usage as
    /// that defined for identifier-1") — DISPLAY for a group, which has no PICTURE of its own.</summary>
    private static Usage UsageOf(DataItem inner) => inner.Pic?.Usage ?? Usage.Display;

    /// <summary>Does any subordinate give this group a length that is decided at RUN TIME — an OCCURS DEPENDING
    /// or OCCURS DYNAMIC table (ISO §13.18.38 Formats 2 and 4), or a dynamic-length elementary item (§8.5.1.10)?
    /// The last two also make it a §8.5.1.12.1 variable-length group; the FIRST does not, which is exactly why
    /// this walk exists beside <c>VariableLengthCompatibility.IsVariableLength</c> rather than inside it — that
    /// predicate answers §8.5.1.12's question about GR9 compatibility, and this one answers §14.9.25.4 GR1's
    /// question about whether a cloned description can hold the sender's LENGTH.</summary>
    private static bool HasRunTimeExtent(DataItem item)
    {
        foreach (var c in item.Children)
            if (c.IsDynamicLength || c.OccursSpec is { DependingName: not null } or { IsDynamic: true }
                || HasRunTimeExtent(c))
                return true;
        return false;
    }
}
