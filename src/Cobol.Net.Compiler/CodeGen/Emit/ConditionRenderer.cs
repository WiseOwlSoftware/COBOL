// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound condition to a side-effect-free C# boolean expression (COBOLNET_DESIGN §11): relational
/// comparisons (numeric scale-aligned, or alphanumeric via <c>CobolString.Compare</c>), logical AND/OR/XOR/NOT,
/// level-88 membership over the conditional variable, and sign conditions. An unbound condition fails loud (§1.4).
/// ONE deliberate exception to side-effect freedom: <see cref="BoundUdfEvaluated"/> — a per-evaluation
/// user-function window — renders as an immediately-invoked <c>Func&lt;bool&gt;</c> whose body runs the
/// activations then returns the inner predicate, so the activation executes exactly when (and only when) the
/// containing condition text evaluates (ISO §8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2 — loop headers re-evaluate it per
/// iteration, a short-circuited <c>&amp;&amp;</c>/<c>||</c> operand skips it entirely).
/// </summary>
internal sealed class ConditionRenderer(NumericRenderer num, EmitContext ctx) : IBoundConditionVisitor<string>
{
    /// <summary>The CALL emitter — property-wired by <see cref="UnitEmitters"/> (the per-evaluation
    /// <see cref="BoundUdfEvaluated"/> window renders the SAME activation text the statement hoist emits;
    /// the ctor cannot take it — CallEmitter builds after this renderer).</summary>
    internal CallEmitter Calls { get; set; } = null!;

    /// <summary>The statement emitter — property-wired by <see cref="UnitEmitters"/> for the same reason
    /// <see cref="Calls"/> is. A per-evaluation window's pre-ops are not all CALLs: a D18 function-bearing
    /// subscript hoists a §15.4 temporary STORE (fix-queue PB17), which is rendered by capturing this emitter's
    /// output so the store gets the ONE arithmetic store path (scale alignment, truncation, the wide tier) rather
    /// than a second hand-written renderer.</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>Render a bound condition as a C# boolean expression. Dispatch is the generated exhaustive
    /// <see cref="IBoundConditionVisitor{T}"/> (PHASE-07 Step 6e): every BoundCondition leaf has a Visit below, so a
    /// new leaf is a COMPILE error — the former loud <c>_ =></c> default is gone.</summary>
    public string Render(BoundCondition c)
    {
        // A condition is a receiver-less numeric context: clear the float-receiver flag so a stale one from a prior
        // arithmetic store cannot promote a fixed-operand comparison to IEEE double (the H1 staleness discipline, D16
        // review). Idempotent under the recursive Render calls below.
        return c.Accept(this);
    }

    public string Visit(BoundRelational n) => RenderRelational(n);
    // An EMPTY logical is the tautology (EVALUATE's ANY object composes as an AND over zero terms).
    public string Visit(BoundLogical n) => n.Operands.Count == 0
        ? "true"
        : "(" + string.Join($" {n.Op} ", n.Operands.Select(Render)) + ")";
    public string Visit(BoundNot n) => $"!({Render(n.Operand)})";
    public string Visit(BoundCondition88 n) => RenderCondition88(n);
    // A switch-status condition (ISO §8.8.4.6 GR1): true when the external switch is at the posited position.
    public string Visit(BoundSwitchCondition n) => n.TestsOn
        ? $"ExternalSwitches.Get({EmitText.CsLiteral(n.ImplementorName)})"
        : $"!ExternalSwitches.Get({EmitText.CsLiteral(n.ImplementorName)})";
    public string Visit(BoundSignCondition n) => RenderSign(n);

    /// <summary>§8.8.4.8 (kb/Work PB133 wave C) — the formal carrier's IsNull IS the omitted-argument
    /// condition; the CA10-checked GR12 raise lives in the carrier's accessors, never here (testing presence
    /// is one of the rule's two sanctioned reference forms).</summary>
    public string Visit(BoundOmittedCondition n) => n.Negated
        ? $"(!{n.CarrierField}.IsNull)"
        : $"({n.CarrierField}.IsNull)";
    // A simple boolean condition (ISO §8.8.4.3.4 GR1): true iff the boolean value is 1.
    public string Visit(BoundBooleanCondition n) => $"CobolBool.IsTrue({BooleanRenderer.Render(n.Expr, num)})";
    public string Visit(BoundClassCondition n) => RenderClass(n);
    // An EVALUATE WHEN alphanumeric/national THRU range (§14.7.8): ThruMember sets EC-RANGE-INVALID (nonfatal) for an
    // inverted range (Lo collating after Hi) and returns false (empty range), else the inclusive-bound membership.
    // Produced only under EC-RANGE-INVALID checking (else the plain BoundLogical of two relations renders — byte-identical).
    public string Visit(BoundRangeMembership n)
    {
        string collate = IsNationalOperand(n.Left) || IsNationalOperand(n.Lo) ? ctx.NatCollateArg : ctx.CollateArg;
        return RuntimeApi.ThruMember(
            OperandText.AsString(n.Left, num), OperandText.AsString(n.Lo, num), OperandText.AsString(n.Hi, num), collate);
    }

    private static bool IsNationalOperand(BoundOperand op) => op switch
    {
        BoundStringLiteral { Category: PicCategory.National } => true,
        BoundFieldOperand { Place.Item.Pic.Category: PicCategory.National } => true,
        _ => false,
    };
    // A user-defined class (§8.8.4.4 / §12.3.7): operand consists entirely of the class's member characters.
    public string Visit(BoundUserClassCondition n) => n.Negated
        ? $"!CobolClass.IsInClass({OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition)}, {EmitText.CsLiteral(n.Members)})"
        : $"CobolClass.IsInClass({OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition)}, {EmitText.CsLiteral(n.Members)})";
    // An alphabet-name class (§8.8.4.4.4 GR3 a; kb/Work PB109): membership of the alphabet's coded character set
    // (routed through the RuntimeApi façade — the P7 Step 4b ratchet forbids NEW bare runtime accesses here).
    public string Visit(BoundCodedSetClassCondition n) => n.Negated
        ? $"!{RuntimeApi.ClassInCodedSet(OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition), n.Kind)}"
        : RuntimeApi.ClassInCodedSet(OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition), n.Kind);
    public string Visit(BoundConditionError n) => EmitText.LoudValue("bool", n.Feature);
    // A per-evaluation user-function window (ISO §8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2): the activations run each
    // time THIS condition text evaluates — an IIFE, so a while-header re-runs them per iteration and a
    // short-circuited &&/|| operand position skips them exactly when COBOL's rule 1 skips the operand.
    public string Visit(BoundUdfEvaluated n) =>
        $"new Func<bool>(() => {{ {string.Join(" ", n.Activations.Select(PreOpText))} "
        + $"return {Render(n.Inner)}; }})()";

    /// <summary>One pending PRE-op rendered for EXPRESSION position (inside the IIFE above).
    /// <para>The two arms are a SEMANTIC split, not an incidental one. A user-function activation must NOT reuse
    /// the statement-position CALL text: <see cref="CallEmitter.FunctionActivationText"/> deliberately omits
    /// <c>siteHandlesPropagation</c>, because a declarative RESUME pickup is a <c>__pc</c>-anchored statement
    /// surface that cannot run inside an expression. Every other pre-op — today a D18 function-bearing subscript's
    /// §15.4 temporary store (fix-queue PB17) — has no such expression/statement divergence, so it renders through
    /// the ONE statement emitter, captured as text.</para></summary>
    private string PreOpText(BoundStatement s) => s switch
    {
        BoundCallProgram c => Calls.FunctionActivationText(c),
        _ => ctx.Writer.CaptureText(() => Statements.EmitStatement(s)).Replace('\n', ' '),
    };

    private string RenderRelational(BoundRelational r)
    {
        // Object relations FIRST (D-U8; §8.8.4.2.15 :9769 — reference IDENTITY): the figurative branch
        // below would materialize NULL against a width — nonsense for references. The only legal operand
        // shapes reached here are an object-reference field and the NULL figurative (bind-checked, 0868);
        // C# implicit upcasts cover typed-vs-universal mixes (both are CobolObject-rooted).
        static bool IsObj(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.ObjectReference;
        if (IsObj(r.Left) || IsObj(r.Right))
        {
            static string ObjRead(BoundOperand o) => o is BoundFieldOperand f ? PlaceRenderer.Read(f.Place) : "null";
            string core = $"object.ReferenceEquals({ObjRead(r.Left)}, {ObjRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Data-pointer relations (Phase-4b; §8.8.4.2.16 — ManagedPointer.SameTarget: both-NULL / same-storage;
        // the NULL figurative renders as the null carrier). Before the figurative branch (NULL must not
        // width-materialize against a pointer).
        static bool IsPtr(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.Pointer;
        if (IsPtr(r.Left) || IsPtr(r.Right))
        {
            static string PtrRead(BoundOperand o) => o is BoundFieldOperand f ? PlaceRenderer.Read(f.Place) : "null";
            string core = $"ManagedPointer.SameTarget({PtrRead(r.Left)}, {PtrRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Program-pointer relations (P10 Step 7; §8.8.4.2.16 — ProgramPointer.SameTarget: both-NULL / the same
        // program's identity; the NULL figurative renders as the Null carrier — a struct, never C# null).
        static bool IsPp(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.Pic?.Category == PicCategory.ProgramPointer;
        if (IsPp(r.Left) || IsPp(r.Right))
        {
            static string PpRead(BoundOperand o) =>
                o is BoundFieldOperand f ? PlaceRenderer.Read(f.Place) : "ProgramPointer.Null";
            string core = $"ProgramPointer.SameTarget({PpRead(r.Left)}, {PpRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Boolean-EXPRESSION relations (ISO §8.8.4.2.2 Format 2): when either side is a boolean expression
        // (a B-op tier, BoundBoolOperand), render BOTH sides as '0'/'1' strings and compare by VALUE with
        // right-zero-extension (§8.8.4.2.8) via CobolBool.Equal. A bare boolean item/literal mixed with an
        // expression reads the same '0'/'1' form. Equality-only + boolean purity are bind-enforced (1511).
        // (Bare item↔item boolean compares — no expression — ride the CobolString.Compare(pad:'0') branch
        // below, which is the identical zero-extension under D-B1.)
        if (r.Left is BoundBoolOperand || r.Right is BoundBoolOperand)
        {
            string core = $"CobolBool.Equal({BoolRead(r.Left)}, {BoolRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // A figurative operand (a single-character constant OR an ALL "literal") is materialized against the OTHER
        // operand's width (ISO §8.3.3.6.4 GR2), so it routes through the width-aware figurative path.
        if (r.Left is BoundFigurative or BoundAllLiteral || r.Right is BoundFigurative or BoundAllLiteral)
            return RenderFigurativeRelational(r);
        // BOOLEAN relations (§8.8.4.2.2 Format 2 / §8.8.4.2.8): a VALUE comparison, usage-independent, the
        // shorter operand right-extended with boolean ZEROS — never the alphanumeric program collating
        // sequence (equality-only + class purity are bind-enforced, 0844).
        if (StringCategoryOf(r.Left) is PicCategory.Boolean || StringCategoryOf(r.Right) is PicCategory.Boolean)
            return $"CobolString.Compare({OperandText.AsString(r.Left, num)}, {OperandText.AsString(r.Right, num)}, pad: '0') {r.Op} 0";
        // NATIONAL relations (§8.8.4.2.9/.10): full ordering under the NATIONAL collating sequence — the
        // default is the UTF-16 code-unit ordinal (D-N3; NATIVE/UCS-4 are that same identity), and a
        // NON-native ALPHABET … FOR NATIONAL sequence rides ctx.NatCollateArg (§12.3.6 GR11). The
        // ALPHANUMERIC program collating sequence never applies (ctx.CollateArg — whose 256-entry weight
        // table would alias national chars through `& 0xFF` — is deliberately absent). A mixed alphanumeric
        // operand converts to national by the D-N4 Latin-1 identity (§8.8.4.2.6).
        if (StringCategoryOf(r.Left) is PicCategory.National || StringCategoryOf(r.Right) is PicCategory.National)
            return $"CobolString.Compare({OperandText.AsString(r.Left, num, deSign: true)}, {OperandText.AsString(r.Right, num, deSign: true)}{ctx.NatCollateArg}) {r.Op} 0";
        if (OperandText.IsString(r.Left) || OperandText.IsString(r.Right))
            // A signed numeric compared against an alphanumeric operand drops its sign (ISO §8.8.4.2.5 → §14.9.25.4 GR6a).
            return $"CobolString.Compare({OperandText.AsString(r.Left, num, deSign: true)}, {OperandText.AsString(r.Right, num, deSign: true)}{ctx.CollateArg}) {r.Op} 0";
        // Each side renders knowing the OTHER side's static scale (fix-queue PB60 / RV-15.68.4-1 half 2):
        // §8.8.4.2.4 compares ALGEBRAIC VALUES, so `IF FUNCTION NUMVAL-C(A) = 0.123456789` must see the
        // function's value at (at least) the literal's 9 fraction digits — the bare receiver-less context
        // floored the exact family at 6 and the relation agreed with a TRUNCATED value no channel should
        // hold. Receiverless STAYS TRUE (the float family keeps its deliberate binary64 compare above);
        // only the working-scale request carries the comparand.
        NumX l = num.AsNum(r.Left, ReceiverContext.None with { Scale = StaticScaleOf(r.Right) }),
             rr = num.AsNum(r.Right, ReceiverContext.None with { Scale = StaticScaleOf(r.Left) });
        // A float operand under NATIVE arithmetic (D16): compare the algebraic values natively in IEEE double
        // (§8.8.4.2.4 — "when native arithmetic is in effect, comparison proceeds by the rules of native
        // arithmetic"). IEEE NaN-unordered (every relation but != is false) and +0.0 == -0.0 fall out of C# —
        // spec-conformant, no epsilon. Under STANDARD-DECIMAL this branch is SKIPPED so the float lifts to SDIDI below.
        if ((l.Real || rr.Real) && !num.StandardDecimal)
            return $"{NumericRenderer.Real(l)} {r.Op} {NumericRenderer.Real(rr)}";
        // Under standard-decimal, §8.8.4.2.4 requires EACH operand converted to standard-decimal intermediate form
        // and compared decimally — a float lifts via the §8.8.1.5.1 float→SDIDI conversion (DecOperand →
        // CobolDec.FromDouble) and a fixed operand lifts EXACTLY (CobolDec.From), preserving decimal precision that a
        // native (double)-rounded compare would lose. A native STANDARD-DECIMAL intermediate (.Dec) also lands here.
        if (l.Dec || rr.Dec || l.Real || rr.Real)
            return $"CobolDec.Compare({num.DecOperand(l)}, {num.DecOperand(rr)}) {r.Op} 0";
        // An UNSIGNED-WIDE operand (a 16-byte unsigned COMP-5 read or the HIGHEST-ALGEBRAIC fold literal —
        // kb/Work R10) compares by algebraic VALUE over the full [0, 2^128) range: CobolNum.CompareU's overload
        // set covers U-vs-U and either mixed order, so the comparison never narrows through the Int128 funnel
        // (which would be loud for exactly the values this relation exists to test).
        if (l.U || rr.U)
            return $"{RuntimeApi.NumCompareU(l.Expr, $"{l.Scale}", rr.Expr, $"{rr.Scale}")} {r.Op} 0";
        int s = Math.Max(l.Scale, rr.Scale);
        // ⛔ DIFFERING SCALES COMPARE WITHOUT WIDENING (fix-queue PB65): aligning to the common scale first
        // wrapped silently past 38 aligned digits, and IF BIGV > SMLV over legal in-range items answered FALSE.
        // A comparison has a defined answer for every legal pair (§8.8.4.2.4), so it rides the exact
        // sign-split/magnitude compare — the same shape the unsigned lane above always used.
        if (l.Scale != rr.Scale)
            return $"{RuntimeApi.NumCompareScaled(NumericRenderer.Align(l, l.Scale), $"{l.Scale}", NumericRenderer.Align(rr, rr.Scale), $"{rr.Scale}")} {r.Op} 0";
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    /// <summary>A relational comparison where one side is a figurative constant — it materializes to the other
    /// operand's category and width (ISO §8.3.3.6.4 r2 sizes it from the associated operand; §8.8.4.2.1
    /// treats a group anchor as an elementary alphanumeric item — kb/Work PB182 corrected the phantom
    /// §8.8.4.1.1 this used to cite): a numeric anchor → ZERO is 0; an alphanumeric/group anchor →
    /// the figurative is a string as long as the anchor's own value is.
    /// <para>⛔ THE WIDTH IS THE ANCHOR'S OWN RUNTIME LENGTH, NEVER A COMPILE-TIME TABLE (kb/Work PB297).
    /// This site used to carry an <c>AnchorWidth</c> switch over operand KINDS, and every arm that was not an
    /// unmodified elementary field was wrong: a <c>RefModPlace</c> field reported the BASE item's width where
    /// §8.4.3.3.4 GR5 makes the slice "a unique data item" of the ref-mod's own length (so <c>X(1:1) =
    /// LOW-VALUE</c> over <c>PIC X(2)</c> compared <c>"\0\0"</c> with <c>"\0"</c> → space-padded → FALSE), a
    /// computed operand fell through to the <c>_ =&gt; 1</c> default (so <c>FUNCTION UPPER-CASE(A) = ALL "AB"</c>
    /// compared one character), and with figuratives on BOTH sides each was sized to the OTHER's length instead
    /// of the §8.3.3.6.4 GR3 b/c length of its own. A ref-mod with computed bounds has no compile-time width at
    /// all, which is why no table could have been completed: the sizing belongs where the length is known, in
    /// <c>CobolString.CompareFig</c>.</para></summary>
    private string RenderFigurativeRelational(BoundRelational r)
    {
        static bool IsFig(BoundOperand o) => o is BoundFigurative or BoundAllLiteral;
        // A NON-NUMERIC figurative (SPACE/QUOTE/HIGH/LOW-VALUE — anything but ZERO) or an ALL "literal" makes the
        // comparison ALPHANUMERIC even against a numeric item (ISO §8.8.4.2.1 — the figurative is alphanumeric
        // class, so the numeric operand participates via its character image, at its own width).
        static bool NonNumericFig(BoundOperand o) => o is BoundFigurative { Kind: not 'Z' } or BoundAllLiteral;
        BoundOperand anchor = IsFig(r.Left) ? r.Right : r.Left;
        if (IsFig(anchor) || OperandText.IsString(anchor) || NonNumericFig(r.Left) || NonNumericFig(r.Right))
        {
            // The category that decides BOTH the figurative's fill and the collating sequence is the CONTEXT's,
            // not the anchor slot's: §8.3.3.6.4 GR1 — "When a figurative constant is used in a context requiring
            // national characters, the figurative constant represents a national character value" — and GR6/GR7
            // — "If the context of the figurative constant requires national characters, the national program
            // collating sequence is used". With figuratives on BOTH sides the anchor slot holds a figurative,
            // which has no category of its own, so the context can only come from the other side: §8.3.3.6.3 SR2
            // gives ALL literal-1 its literal's class ("Literal-1 shall be an alphanumeric, boolean, or national
            // literal"). Reading only the anchor made `IF ALL N"Z" = LOW-VALUE` take the ALPHANUMERIC program
            // collating sequence and answer the OPPOSITE of the same test written over a national ITEM.
            var anchorCat = StringCategoryOf(anchor) ?? (IsFig(anchor) ? StringCategoryOf(r.Left) : null);
            // A boolean/national anchor exempts the ALPHANUMERIC program collating sequence: boolean
            // comparisons are value comparisons (§8.8.4.2.8; only ZERO reaches here — the class mix is
            // bind-rejected 0844) and national comparisons order under the NATIONAL sequence (§8.8.4.2.9 —
            // the D-N3 ordinal identity, or __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL; the
            // alphanumeric 256-entry weight table would alias national chars through `& 0xFF`).
            string collate = ctx.CollateArgFor(anchorCat);
            // A boolean anchor right-extends the shorter operand with boolean ZEROS (§8.8.4.2.8) — the same
            // pad the direct-relation and level-88 legs thread; pad and collate never coexist (a boolean
            // anchor forces collate empty). The figurative materializes category-aware (national/boolean
            // HIGH/LOW-VALUE = the category's own sequence — the explicit national PCS extremes when one is
            // declared, else the D-N3 pin — never the alphanumeric PCS extreme).
            string pad = anchorCat is PicCategory.Boolean ? ", pad: '0'" : "";
            // BOTH sides figurative — there is no associated data item, so §8.3.3.6.4 GR2 does not apply and
            // GR3 gives each operand its OWN length: one character for a plain figurative word (GR3 b) and
            // literal-1's length for ALL literal-1 (GR3 c). That is exactly each side's SEED, unrepeated.
            if (IsFig(r.Left) && IsFig(r.Right))
                return $"{RuntimeApi.StrCompare(FigSeed(r.Left, anchorCat), FigSeed(r.Right, anchorCat), pad + collate)} {r.Op} 0";
            // Exactly one side figurative — GR2 repeats its seed to the ASSOCIATED operand's own character-position
            // count, which the runtime reads off that operand's rendered value (§8.4.3.3.4 GR5: a ref-modified
            // operand's positions are the slice's, and with computed bounds they exist only at runtime).
            bool figLeft = IsFig(r.Left);
            string fig = FigSeed(figLeft ? r.Left : r.Right, anchorCat),
                   other = OperandText.AsString(anchor, num);
            return $"{RuntimeApi.StrCompareFig(figLeft ? fig : other, figLeft ? other : fig, figLeft, pad + collate)} {r.Op} 0";
        }
        NumX l = FigOrNum(r.Left), rr = FigOrNum(r.Right);
        if (l.Real || rr.Real)   // a float vs ZERO figurative — native IEEE compare (D16, §8.8.4.2.4)
            return $"{NumericRenderer.Real(l)} {r.Op} {NumericRenderer.Real(rr)}";
        int s = Math.Max(l.Scale, rr.Scale);
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    /// <summary>An operand's STRING data category for the relation dispatch — literals carry their own tag
    /// (<see cref="BoundStringLiteral.Category"/>); a reference-modified field is the unique item of its
    /// inner's class view (alphanumeric for the classic categories, §8.4.3.3 GR6 — but national/boolean
    /// ref-mod stays national/boolean, GR1/GR5a); null for figuratives/computed/error shapes.</summary>
    private static PicCategory? StringCategoryOf(BoundOperand o) => o switch
    {
        BoundStringLiteral sl => sl.Category,
        BoundAllLiteral al => al.Category,
        // The ONE ref-mod category reader (kb/Work PB70/PB73) — GR6's rewrites, incl. numeric-national → national.
        BoundFieldOperand { Place: RefModPlace rm } => rm.Category,
        // THE ONE category reader (D20/PB79): an elementary item's picture, a bit / national group's as-if picture;
        // an alphanumeric group has none and takes the alphanumeric (image) branch.
        BoundFieldOperand f => f.Place.Item.OperandPic?.Category,
        // A COMPUTED operand with a string-class function result — its category is the function's type (§15.2;
        // kb/Work PB68 — the fifth site of the class-boolean rule: two boolean function results compared each
        // other rode the alphanumeric collate-and-space-pad branch instead of the boolean right-zero-extension).
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Boolean or PicCategory.National or PicCategory.Alphanumeric } ic } => ic.ResultCategory,
        _ => null,
    };

    /// <summary>Read a relation operand as a '0'/'1' boolean string (for a boolean-expression relation): a
    /// boolean expression via <see cref="BooleanRenderer"/>, a boolean field via its <c>Place.Read()</c>, a
    /// boolean literal via its value, and figurative ZERO as "0" (CobolBool.Equal zero-extends it to the other
    /// operand's width — §8.3.3.6.4 GR4 boolean zeros).</summary>
    private string BoolRead(BoundOperand o) => o switch
    {
        BoundBoolOperand b => BooleanRenderer.Render(b.Expr, num),
        // A bit GROUP's boolean value is its bit string (AsBits — OperandText's as-if arm), not the struct (D20/PB79).
        BoundFieldOperand f => f.Place.Item.IsAsIfElementary ? OperandText.FieldImage(f.Place) : PlaceRenderer.Read(f.Place),
        BoundStringLiteral { Category: PicCategory.Boolean } s => EmitText.CsLiteral(s.Value),
        BoundFigurative { Kind: 'Z' } => "\"0\"",
        // A boolean-result function reference — its '0'/'1' image through the ONE string channel (kb/Work PB68).
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Boolean } } => OperandText.AsString(o, num),
        _ => EmitText.LoudValue("string", $"boolean relation operand '{o.GetType().Name}'"),
    };

    /// <summary>A figurative operand's SEED — the string §8.3.3.6.4 GR2 repeats to the associated operand's
    /// character-position count, and (unrepeated) the whole of its GR3 b/c value when nothing sizes it: ONE fill
    /// character for a figurative word, literal-1 for <c>ALL literal-1</c>. PCS-aware for alphanumeric anchors —
    /// HIGH-/LOW-VALUE are the program sequence's extreme characters (§8.3.3.6.4 GR6/GR7) — while a
    /// national/boolean anchor reads its OWN sequence (the D-N3 pin), through the ONE fill service.
    /// ⛔ No width here by construction: sizing is <c>CobolString.CompareFig</c>'s (kb/Work PB297).</summary>
    private string FigSeed(BoundOperand op, PicCategory? anchorCat) => op switch
    {
        BoundFigurative f => FigurativeConstants.FillText(f.Kind, ctx.Data.Collating, anchorCat, ctx.Data.NationalCollating),
        BoundAllLiteral a => EmitText.CsLiteral(a.Literal),
        // Unreachable by construction — both call sites select an operand that already satisfied IsFig. Loud
        // rather than a silent fall-through to the operand's own text, which would look like a working
        // comparison while sizing nothing (the failure shape this whole change exists to remove).
        _ => EmitText.LoudValue("string", $"figurative seed for '{op.GetType().Name}'"),
    };

    private NumX FigOrNum(BoundOperand op) => op switch
    {
        BoundFigurative { Kind: 'Z' } => EmitText.UnscaledLit("0"),
        BoundFigurative f => new NumX(EmitText.LoudValue("long", $"figurative '{f.Kind}' in a numeric comparison"), 0),
        _ => num.AsNum(op, ReceiverContext.None),
    };

    private string RenderSign(BoundSignCondition s)
    {
        // §14.6.13.2 rule 3 dash-2: a float sending item referenced in a SIGN condition is EXEMPT from
        // EC-DATA-NOT-FINITE — render the whole operand sub-tree with the finiteness wrap suppressed (a NaN/±Inf
        // sign test is well-defined: NaN is neither >0, <0, nor ==0, so a compound sibling like `AND Y > 0.0`
        // still raises on its own read).
        // ⛔ RULE 2 HAS NO SUCH DASH, and SendingRef is what lets the two lists differ (kb/Work PB230): a
        // FIXED-POINT sending item in a sign condition IS still checked for EC-DATA-INCOMPATIBLE. The asymmetry
        // is the standard's own — §8.8.4.7.4 GR2 gives a float sign test a defined answer for NaN by reading the
        // IEEE sign bit, and there is no corresponding rule making `IF N IS POSITIVE` meaningful over digits that
        // are not digits.
        NumX v = num.Render(s.Expr, ReceiverContext.None, SendingRef.SignCondition);
        // §8.8.4.7.4 GR2 (Format 2 — a bare standard-float name): POSITIVE/NEGATIVE test the IEEE sign BIT, not the
        // algebraic value, "regardless of whether the content would evaluate to true in a NUMERIC class test or a
        // ZERO sign test" — so +0.0 IS POSITIVE and −0.0 IS NEGATIVE. double.IsNegative reads the sign bit (true for
        // −0.0 and a negative-signed NaN; false for +0.0). ZERO (GR2c) is sign-agnostic. Format 1 keeps the algebraic
        // test. Widening FLOAT-SHORT→double preserves the sign of zero and NaN, so the single-precision case is covered.
        string test = s.Format2Float
            ? s.Kind switch { 'P' => $"!double.IsNegative({NumericRenderer.Real(v)})", 'N' => $"double.IsNegative({NumericRenderer.Real(v)})", _ => $"{NumericRenderer.Real(v)} == 0.0" }
            // An unsigned-wide operand (kb/Work R10) tests its sign by VALUE over the full range via CompareU —
            // C# defines no UInt128-vs-int operator, and the Widen funnel would be loud for exactly the large
            // values a sign test must accept. (NEGATIVE is structurally false for an unsigned item; the compare
            // form keeps the three kinds one mechanism.)
            // (The zero is cast — an int constant converts implicitly to BOTH Int128 and UInt128, and the
            // uncast form is a CS0121 ambiguity in the generated code.)
            : v.U
            ? s.Kind switch { 'P' => $"{RuntimeApi.NumCompareU(v.Expr, "0", "(Int128)0", "0")} > 0", 'N' => $"{RuntimeApi.NumCompareU(v.Expr, "0", "(Int128)0", "0")} < 0", _ => $"{RuntimeApi.NumCompareU(v.Expr, "0", "(Int128)0", "0")} == 0" }
            // An SDIDI intermediate (§8.8.1.5.2 — every STANDARD-DECIMAL arithmetic expression, and under native
            // arithmetic an integer power, kb/Work PB69) tests the sign of its significand: exact at every
            // exponent, and never a landing that could overflow (kb/Work PB84 — `IF 9 ** TWO + (180 - 90) IS
            // NOT POSITIVE`, NIST NC250A, was a Roslyn CS0019 on `CobolDec > 0`).
            : v.Dec
            ? s.Kind switch { 'P' => $"{RuntimeApi.DecSign(v.Expr)} > 0", 'N' => $"{RuntimeApi.DecSign(v.Expr)} < 0", _ => $"{RuntimeApi.DecSign(v.Expr)} == 0" }
            : s.Kind switch { 'P' => $"{v.Expr} > 0", 'N' => $"{v.Expr} < 0", _ => $"{v.Expr} == 0" };
        return s.Negated ? $"!({test})" : $"({test})";
    }

    /// <summary>A class condition (ISO §8.8.4.4). A typed-numeric operand IS NUMERIC folds to <c>true</c> ONLY
    /// when its storage is the native long/Int128 (it can only hold digits — COBOLNET_DESIGN §6.6); a numeric item
    /// whose storage is a CHARACTER window (a REDEFINES view, or a whole-group-aliased StoreAsImage leaf) can hold
    /// arbitrary characters and tests its image at run time — sign-aware for a signed zoned item (§8.8.4.4.4 GR3 n)1.a,
    /// NC174A CLASS-TEST-GF-8/10: S9(18) REDEFINES X(18) holding letters is NOT numeric).
    /// <para>⛔ A WINDOWED numeric leaf tests through <c>CobolNum.IsNumericImage</c> over its RAW WINDOW — the ONE
    /// §8.8.4.4.4 GR3 n)1 predicate, which §14.6.13.2 rule 2's checked sending read also calls, because the standard
    /// defines rule 2's test BY REFERENCE to this one ("would evaluate to false in a numeric class condition").
    /// Writing that rule twice is how the two answers drift, so it is written once (kb/Work PB230).</para>
    /// <para>THE RAW WINDOW IS THE POINT, and it is what the previous form could not reach: <c>arg</c> is
    /// <c>OperandText.AsString</c>, which for a NON-ZONED window DECODES the bytes and re-renders them as a
    /// DISPLAY image — so <c>IsNumeric(arg)</c> was asking whether a reformatted image is all digits, which it
    /// always is. A packed window with a non-decimal nibble, and a binary window whose value exceeds its
    /// PICTURE's range, were both reported NUMERIC; GR3 n)1.c asks instead for "a valid representation for the
    /// usage" and "the numeric value is within the range of values implied by the PICTURE clause", and only the
    /// undecoded bytes can answer either. The ZONED case is unchanged in behaviour: there the window IS its text,
    /// and the predicate delegates straight back to the same two <c>CobolClass</c> helpers this arm used to
    /// spell inline — so the sign-aware answer NC174A pins is the same code it always was.</para>
    /// <para>A REF-MOD operand is excluded: its result is an elementary ALPHANUMERIC item whatever the underlying
    /// item's category (§8.4.3.3.4 GR6), so GR3 n)2's plain all-digits test governs it, not n)1.</para></summary>
    private string RenderClass(BoundClassCondition c)
    {
        var fld = c.Operand as BoundFieldOperand;
        bool numericCategory = fld?.Place.Item.Pic?.Category is PicCategory.Numeric;
        bool numericField = numericCategory && fld!.Place is not RedefViewPlace && !fld.Place.Item.StoreAsImage;
        // The complement, minus the ref-mod shape: a numeric leaf whose storage IS a character window.
        bool windowedNumeric = numericCategory && fld!.Place is not (RefModPlace or TableAllPlace)
            && (fld.Place is RedefViewPlace || fld.Place.Item.StoreAsImage);
        // §14.6.13.2 dash-1 of rules 1, 2 AND 3: a sending item referenced in a CLASS condition is EXEMPT from
        // every one of them — the class test inspects the content precisely in order to CATEGORIZE it, so raising
        // on the very content it was asked to report would leave it unable to answer.
        string arg = OperandText.AsString(c.Operand, num, sending: SendingRef.ClassCondition);
        string numericTest = windowedNumeric
            ? RuntimeApi.NumIsNumericImage(PlaceRenderer.Read(fld!.Place), fld.Place.Item.ProfileName)
            : numericCategory && fld!.Place.Item.Pic is { Signed: true } sp
            ? $"CobolClass.IsNumericZoned({arg}, {(sp.SignKind.Contains("Separate") ? "2" : "1")}, leading: {(sp.SignKind.Contains("Leading") ? "true" : "false")})"
            // §8.8.4.4.4 GR3 n)2 — a NON-numeric-category operand (alphanumeric / edited / national) is numeric iff
            // its content "consists entirely of the characters 0, 1, 2, …, 9", with no operational sign admitted.
            : $"CobolClass.IsNumeric({arg})";
        // ALPHABETIC / -UPPER / -LOWER under a CHARACTER CLASSIFICATION (ISO §8.8.4.4.4 GR3 b1/c1/d1 — the classification
        // locale's LC_CTYPE, resolved at the module's activation into __CLASSIFY; kb/Work PB64 T5); without one the
        // coded-character-set rule (b2/c2/d2 — the closed Latin set) stands, exactly as before.
        string classify = ctx.Data.Classification is not null ? $", __CLASSIFY.For({(IsNationalOperand(c.Operand) ? "true" : "false")})" : "";
        string test = c.ClassKind switch
        {
            'N' => numericField ? "true" : numericTest,
            'A' => $"CobolClass.IsAlphabetic({arg}{classify})",
            'U' => $"CobolClass.IsAlphabeticUpper({arg}{classify})",
            'L' => $"CobolClass.IsAlphabeticLower({arg}{classify})",
            _ => EmitText.LoudValue("bool", "class condition"),
        };
        return c.Negated ? $"!({test})" : $"({test})";
    }

    private string RenderCondition88(BoundCondition88 c)
    {
        bool isString = c.Parent.Item.IsGroup || c.Parent.Item.Pic?.Category is PicCategory.Alphanumeric
            or PicCategory.NumericEdited or PicCategory.National or PicCategory.Boolean;
        // ISO §8.8.4.5 GR2: a condition-name test compares the conditional variable by the RELATION-CONDITION rules, so
        // the variable is rendered as a comparison operand exactly as a relation condition renders it — an alphanumeric
        // GROUP is treated as an elementary alphanumeric data item (§8.8.4.1), i.e. its character IMAGE, not the raw
        // struct. (The numeric branch reads the scaled value directly; a numeric view stored as its image is a later
        // slice.)
        // A NUMERIC conditional variable goes through the ONE numeric read path (NumericRenderer.FieldNum) — a
        // whole-group-aliased / Tier-B-view leaf is string-STORED (StoreAsImage) and must decode via ParseDisplay,
        // never compare its raw image to an unscaled long (diagnosis B3).
        string read = isString ? OperandText.AsString(new BoundFieldOperand(c.Parent), num) : num.FieldNum(c.Parent).Expr;
        var tests = c.Condition.Values.Select(v => RenderMembershipTest(read, c.Parent.Item, isString, v.Low, v.High, c.CheckRangeInvalid));
        return "(" + string.Join(" || ", tests) + ")";
    }

    /// <summary>One VALUE-set membership test: equality for a singleton, an inclusive bound test for a THRU range.
    /// When <paramref name="checkRangeInvalid"/> and the range is alphanumeric/national (§14.7.8 rule 2), the range
    /// test routes through <c>CobolString.ThruMember</c> which sets the nonfatal EC-RANGE-INVALID for an inverted
    /// range (lo collating after hi) and treats it as empty.</summary>
    private string RenderMembershipTest(string read, DataItem parent, bool isString, string low, string? high,
        bool checkRangeInvalid = false)
    {
        if (isString)
        {
            // A level-88 VALUE compares against the conditional variable, so a figurative ALL "literal" is repeated to
            // the variable's width (ISO §8.3.3.6.4 GR2); a plain literal is decoded as-is. A BOOLEAN conditional
            // variable compares by value with boolean-zero extension and never the alphanumeric PCS (§8.8.4.2.8);
            // a NATIONAL one orders under the NATIONAL sequence (§8.8.4.2.9 — the D-N3 ordinal identity, or
            // __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL; never the alphanumeric PCS weights).
            var cat = parent.Pic?.Category;
            string collate = ctx.CollateArgFor(cat);
            string pad = cat is PicCategory.Boolean ? ", pad: '0'" : "";
            string lo = StringMembershipExpr(low, parent);
            if (high is null) return $"CobolString.Compare({read}, {lo}{pad}{collate}) == 0";
            string hi = StringMembershipExpr(high, parent);
            // §14.7.8 rule 2: an alphanumeric/national THRU range under checking routes through ThruMember (sets the
            // nonfatal EC-RANGE-INVALID for an inverted range, then treats it as empty — the empty behaviour is
            // otherwise emergent from the inclusive test). Boolean/other categories keep the inline byte-identical form.
            if (checkRangeInvalid && cat is PicCategory.Alphanumeric or PicCategory.National)
                return RuntimeApi.ThruMember(read, lo, hi, collate);
            return $"(CobolString.Compare({read}, {lo}{pad}{collate}) >= 0 && CobolString.Compare({read}, {hi}{pad}{collate}) <= 0)";
        }
        // A float (COMP-1/2/FLOAT-*) conditional variable: `read` is the native double `(double)(X)`, so the VALUE
        // literal must render as a native double too — NOT scaled-integer at the float item's Scale 0, which would
        // DROP the fraction (88 IS-HALF VALUE 0.5 became `== 0L`, the exact-inverse membership bug). (D16 review.)
        if (parent.Pic is { IsFloat: true })
        {
            string loF = FloatMembershipValue(low);
            if (high is null) return $"{read} == {loF}";
            return $"({read} >= {loF} && {read} <= {FloatMembershipValue(high)})";
        }
        int scale = parent.Pic?.Scale ?? 0;
        string loN = NumericMembershipValue(low, scale);
        if (high is null) return $"{read} == {loN}";
        return $"({read} >= {loN} && {read} <= {NumericMembershipValue(high, scale)})";
    }

    /// <summary>A numeric level-88 VALUE operand on a FLOAT conditional variable → a native C# <c>double</c> literal
    /// (D16). A figurative ZERO → <c>0.0</c>; a floating-point literal (1.5E3) is already valid C# double syntax;
    /// otherwise the fixed-point literal takes the <c>d</c> suffix. Keeps both sides of the membership test IEEE
    /// doubles (§8.8.4.2.4 algebraic compare), matching the direct relation-condition path.</summary>
    private static string FloatMembershipValue(string raw) =>
        raw.ToUpperInvariant() is "ZERO" or "ZEROS" or "ZEROES" ? "0.0"
        : raw.IndexOf('E') >= 0 || raw.IndexOf('e') >= 0 ? raw.Trim().TrimStart('+')
        : $"{raw.Trim().TrimStart('+')}d";

    /// <summary>A string level-88 VALUE operand's character value: a NUMERIC-EDITED conditional variable's numeric
    /// literal (or figurative ZERO at >= 2023) is its EDITED image — ISO §13.18.63.3 SR6 converts a numeric-edited
    /// item's numeric VALUE literals "according to the rules for the MOVE statement" in formats 1, 2 AND 4, and
    /// §8.8.4.5 GR2 then compares by the relation-condition rules (kb/Work PB97: the raw text "10" was compared to the
    /// image " 10.00" — every such condition-name was silently false); a figurative <c>ALL "literal"</c> repeated to
    /// the conditional variable's width (ISO §8.3.3.6.4 GR2), a bare figurative WORD (QUOTE / SPACE / HIGH-VALUE /
    /// LOW-VALUE / ZERO — §8.3.3.6.4 r2, materialized to the variable's width, NC250A IF--TEST-26/27), else the decoded
    /// literal.</summary>
    /// <summary>The membership operand as a C# EXPRESSION: a format-2 (LOCALE) conditional variable's numeric
    /// VALUE composes its edited image AT RUNTIME under the locale then current (§13.18.40.5 r11 — no
    /// compile-time image exists; the ONE producer, <see cref="RuntimeApi.LocaleEditCompose"/>; falling back to
    /// comparing raw literal text is precisely the PB97 defect shape); everything else is the compile-time
    /// <see cref="StringMembershipValue"/> as a string literal.</summary>
    private string StringMembershipExpr(string raw, DataItem parent)
    {
        if (parent.Pic is { LocaleEdit: not null } lpic
            && !raw.StartsWith('"') && !raw.StartsWith('\'')
            && ValueInitializer.TryParseNumeric(raw, out var uv, out int sc))
            return RuntimeApi.LocaleEditCompose(lpic, uv, sc, parent.BlankWhenZero);
        return EmitText.CsLiteral(StringMembershipValue(raw, parent));
    }

    private string StringMembershipValue(string raw, DataItem parent)
    {
        int width = parent.Pic?.Length ?? parent.ImageWidth;
        if (parent.Pic is { Category: PicCategory.NumericEdited } npic
            && ValueInitializer.EditedImageOfNumericValue(ctx, parent, npic, raw) is { } edited)
            return edited;
        return EmitText.AllLiteralText(raw) is { } lit ? EmitText.RepeatToWidth(lit, width)
            : FigurativeFillChar(raw) is { } fill ? new string(fill, width)
            : CobolLiteral.Decode(raw);
    }

    /// <summary>The fill character of a bare figurative-constant word (with or without a leading <c>ALL</c> —
    /// the same figurative either way, ISO §8.3.3.6.2), or null when the text is not a figurative word. The fill
    /// characters match <see cref="FigurativeConstants.FillChar"/> (HIGH/LOW = U+00FF/U+0000, COBOLNET_DESIGN §14.9).</summary>
    private char? FigurativeFillChar(string raw)
    {
        string t = raw.Trim();
        if (t.StartsWith("ALL", StringComparison.OrdinalIgnoreCase) && t.Length > 3 && char.IsWhiteSpace(t[3]))
            t = t[3..].Trim();
        // The membership fill takes the RAW character (no category pin — this site's historical semantics;
        // the pin question is the flagged §8.3.3.6 GR6/GR7 divergence in FigurativeConstants' doc), NULL not
        // admitted here. ONE service (P7 Step 4).
        return FigurativeConstants.KindOf(t) is { } k
            ? FigurativeConstants.FillChar(k, ctx.Data.Collating) : null;
    }

    /// <summary>A numeric level-88 VALUE operand → its unscaled-<c>long</c> text. A figurative ZERO maps to <c>0</c>
    /// (ISO §8.3.3.6.4 r4 — the zero format represents the numeric value 0); otherwise the literal is scaled. Without this a figurative VALUE word
    /// (e.g. <c>88 IS-ZERO VALUE ZERO</c>) would reach <c>UnscaledAtScale("ZERO", …)</c> and emit a bare identifier.</summary>
    private static string NumericMembershipValue(string raw, int scale) =>
        raw.ToUpperInvariant() is "ZERO" or "ZEROS" or "ZEROES" ? "0L" : EmitText.UnscaledAtScale(raw, scale);

    /// <summary>The statically known fraction-digit count of a relation operand — a numeric literal's digits
    /// right of the decimal point, a field's declared scale (a numeric-edited comparand cannot appear in a
    /// NUMERIC relation, so the bare <c>Pic.Scale</c> suffices) — 0 when the shape carries no static scale,
    /// which is exactly the former blanket behavior. Feeds the OTHER side's working-scale request (fix-queue
    /// PB60 / RV-15.68.4-1): over-asking is safe (the Int128-headroom cap binds), under-asking was the
    /// measured relation-agrees-with-a-truncated-value defect.</summary>
    private static int StaticScaleOf(BoundOperand op) => op switch
    {
        BoundNumericLiteral nl => nl.Text.IndexOfAny(['.', ',']) is >= 0 and var dp ? nl.Text.Length - dp - 1 : 0,
        BoundFieldOperand f => f.Place.Item.Pic?.Scale ?? 0,
        _ => 0,
    };
}
