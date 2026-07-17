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
    // A simple boolean condition (ISO §8.8.4.3.4 GR1): true iff the boolean value is 1.
    public string Visit(BoundBooleanCondition n) => $"CobolBool.IsTrue({BooleanRenderer.Render(n.Expr)})";
    public string Visit(BoundClassCondition n) => RenderClass(n);
    // A user-defined class (§8.8.4.1.4 / §12.3.7): operand consists entirely of the class's member characters.
    public string Visit(BoundUserClassCondition n) => n.Negated
        ? $"!CobolClass.IsInClass({OperandText.AsString(n.Operand, num)}, {EmitText.CsLiteral(n.Members)})"
        : $"CobolClass.IsInClass({OperandText.AsString(n.Operand, num)}, {EmitText.CsLiteral(n.Members)})";
    public string Visit(BoundConditionError n) => EmitText.LoudValue("bool", n.Feature);
    // A per-evaluation user-function window (ISO §8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2): the activations run each
    // time THIS condition text evaluates — an IIFE, so a while-header re-runs them per iteration and a
    // short-circuited &&/|| operand position skips them exactly when COBOL's rule 1 skips the operand.
    public string Visit(BoundUdfEvaluated n) =>
        $"new Func<bool>(() => {{ {string.Join(" ", n.Activations.Select(Calls.FunctionActivationText))} "
        + $"return {Render(n.Inner)}; }})()";

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
        // Data-pointer relations (Phase-4b; §8.8.4.1.3 — ManagedPointer.SameTarget: both-NULL / same-storage;
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
        // Program-pointer relations (P10 Step 7; §8.8.4.1.3 — ProgramPointer.SameTarget: both-NULL / the same
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
        NumX l = num.AsNum(r.Left, ReceiverContext.None), rr = num.AsNum(r.Right, ReceiverContext.None);
        // A float operand (D16): compare the algebraic values natively in IEEE double (§8.8.4.2.4). IEEE
        // NaN-unordered (every relation but != is false) and +0.0 == -0.0 fall out of C# — spec-conformant, no epsilon.
        if (l.Real || rr.Real)
            return $"{NumericRenderer.Real(l)} {r.Op} {NumericRenderer.Real(rr)}";
        // A STANDARD-DECIMAL intermediate compares algebraically in SDIDI form (§8.8.1.5).
        if (l.Dec || rr.Dec)
            return $"CobolDec.Compare({num.DecOperand(l)}, {num.DecOperand(rr)}) {r.Op} 0";
        int s = Math.Max(l.Scale, rr.Scale);
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    /// <summary>A relational comparison where one side is a figurative constant — it materializes to the other
    /// operand's category and width (ISO §8.8.4.1.1): a numeric anchor → ZERO is 0; an alphanumeric/group anchor →
    /// the figurative is a string of the anchor's width.</summary>
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
            int width = AnchorWidth(anchor);
            var anchorCat = StringCategoryOf(anchor);
            // A boolean/national anchor exempts the ALPHANUMERIC program collating sequence: boolean
            // comparisons are value comparisons (§8.8.4.2.8; only ZERO reaches here — the class mix is
            // bind-rejected 0844) and national comparisons order under the NATIONAL sequence (§8.8.4.2.9 —
            // the D-N3 ordinal identity, or __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL; the
            // alphanumeric 256-entry weight table would alias national chars through `& 0xFF`).
            string collate = anchorCat is PicCategory.Boolean ? ""
                : anchorCat is PicCategory.National ? ctx.NatCollateArg : ctx.CollateArg;
            // A boolean anchor right-extends the shorter operand with boolean ZEROS (§8.8.4.2.8) — the same
            // pad the direct-relation and level-88 legs thread; pad and collate never coexist (a boolean
            // anchor forces collate empty). The figurative materializes category-aware (national/boolean
            // HIGH/LOW-VALUE = the category's own sequence — the explicit national PCS extremes when one is
            // declared, else the D-N3 pin — never the alphanumeric PCS extreme).
            string pad = anchorCat is PicCategory.Boolean ? ", pad: '0'" : "";
            return $"CobolString.Compare({FigOrString(r.Left, width, anchorCat)}, {FigOrString(r.Right, width, anchorCat)}{pad}{collate}) {r.Op} 0";
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
        BoundFieldOperand { Place: RefModPlace rm } => rm.Inner.Item.Pic?.Category switch
        {
            PicCategory.National => PicCategory.National,
            PicCategory.Boolean => PicCategory.Boolean,
            _ => PicCategory.Alphanumeric,
        },
        BoundFieldOperand f => f.Place.Item.Pic?.Category,
        _ => null,
    };

    /// <summary>Read a relation operand as a '0'/'1' boolean string (for a boolean-expression relation): a
    /// boolean expression via <see cref="BooleanRenderer"/>, a boolean field via its <c>Place.Read()</c>, a
    /// boolean literal via its value, and figurative ZERO as "0" (CobolBool.Equal zero-extends it to the other
    /// operand's width — §8.3.3.6.4 GR4 boolean zeros).</summary>
    private static string BoolRead(BoundOperand o) => o switch
    {
        BoundBoolOperand b => BooleanRenderer.Render(b.Expr),
        BoundFieldOperand f => PlaceRenderer.Read(f.Place),
        BoundStringLiteral { Category: PicCategory.Boolean } s => EmitText.CsLiteral(s.Value),
        BoundFigurative { Kind: 'Z' } => "\"0\"",
        _ => EmitText.LoudValue("string", $"boolean relation operand '{o.GetType().Name}'"),
    };

    private static int AnchorWidth(BoundOperand op) => op switch
    {
        BoundFieldOperand f => f.Place.Item.Pic?.Length ?? f.Place.Item.ImageWidth,
        BoundStringLiteral s => Math.Max(s.Value.Length, 1),
        BoundAllLiteral a => Math.Max(a.Literal.Length, 1),
        _ => 1,
    };

    private string FigOrString(BoundOperand op, int width, PicCategory? anchorCat = null) => op switch
    {
        // PCS-aware for alphanumeric anchors: HIGH-/LOW-VALUE materialize as the program sequence's extreme
        // characters (§8.3.3.6 GR6/7); a national/boolean anchor uses the D-N3 pin (FigFill's category arm).
        BoundFigurative f => $"new string({FigurativeConstants.Fill(f.Kind, ctx.Data.Collating, anchorCat, ctx.Data.NationalCollating)}, {width})",
        BoundAllLiteral a => EmitText.CsLiteral(EmitText.RepeatToWidth(a.Literal, width)),   // ALL "literal" → repeated to width (GR2)
        _ => OperandText.AsString(op, num),
    };

    private NumX FigOrNum(BoundOperand op) => op switch
    {
        BoundFigurative { Kind: 'Z' } => EmitText.UnscaledLit("0"),
        BoundFigurative f => new NumX(EmitText.LoudValue("long", $"figurative '{f.Kind}' in a numeric comparison"), 0),
        _ => num.AsNum(op, ReceiverContext.None),
    };

    private string RenderSign(BoundSignCondition s)
    {
        NumX v = num.Render(s.Expr, ReceiverContext.None);
        string test = s.Kind switch { 'P' => $"{v.Expr} > 0", 'N' => $"{v.Expr} < 0", _ => $"{v.Expr} == 0" };
        return s.Negated ? $"!({test})" : $"({test})";
    }

    /// <summary>A class condition (ISO §8.8.4.1.4). A typed-numeric operand IS NUMERIC folds to <c>true</c> ONLY
    /// when its storage is the native long/Int128 (it can only hold digits — COBOLNET_DESIGN §6.6); a numeric item
    /// whose storage is a CHARACTER window (a REDEFINES view, or a whole-group-aliased StoreAsImage leaf) can hold
    /// arbitrary characters and tests its image at run time — sign-aware for a signed zoned item (§8.8.4.4 r3,
    /// NC174A CLASS-TEST-GF-8/10: S9(18) REDEFINES X(18) holding letters is NOT numeric).</summary>
    private string RenderClass(BoundClassCondition c)
    {
        var fld = c.Operand as BoundFieldOperand;
        bool numericCategory = fld?.Place.Item.Pic?.Category is PicCategory.Numeric;
        bool numericField = numericCategory && fld!.Place is not RedefViewPlace && !fld.Place.Item.StoreAsImage;
        string arg = OperandText.AsString(c.Operand, num);
        string numericTest = numericCategory && fld!.Place.Item.Pic is { Signed: true } sp
            ? $"CobolClass.IsNumericZoned({arg}, {(sp.SignKind.Contains("Separate") ? "2" : "1")}, leading: {(sp.SignKind.Contains("Leading") ? "true" : "false")})"
            : $"CobolClass.IsNumeric({arg})";
        string test = c.ClassKind switch
        {
            'N' => numericField ? "true" : numericTest,
            'A' => $"CobolClass.IsAlphabetic({arg})",
            'U' => $"CobolClass.IsAlphabeticUpper({arg})",
            'L' => $"CobolClass.IsAlphabeticLower({arg})",
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
        var tests = c.Condition.Values.Select(v => RenderMembershipTest(read, c.Parent.Item, isString, v.Low, v.High));
        return "(" + string.Join(" || ", tests) + ")";
    }

    /// <summary>One VALUE-set membership test: equality for a singleton, an inclusive bound test for a THRU range.</summary>
    private string RenderMembershipTest(string read, DataItem parent, bool isString, string low, string? high)
    {
        if (isString)
        {
            // A level-88 VALUE compares against the conditional variable, so a figurative ALL "literal" is repeated to
            // the variable's width (ISO §8.3.3.6.4 GR2); a plain literal is decoded as-is. A BOOLEAN conditional
            // variable compares by value with boolean-zero extension and never the alphanumeric PCS (§8.8.4.2.8);
            // a NATIONAL one orders under the NATIONAL sequence (§8.8.4.2.9 — the D-N3 ordinal identity, or
            // __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL; never the alphanumeric PCS weights).
            var cat = parent.Pic?.Category;
            string collate = cat is PicCategory.Boolean ? ""
                : cat is PicCategory.National ? ctx.NatCollateArg : ctx.CollateArg;
            string pad = cat is PicCategory.Boolean ? ", pad: '0'" : "";
            int width = parent.Pic?.Length ?? parent.ImageWidth;
            string lo = EmitText.CsLiteral(StringMembershipValue(low, width));
            if (high is null) return $"CobolString.Compare({read}, {lo}{pad}{collate}) == 0";
            return $"(CobolString.Compare({read}, {lo}{pad}{collate}) >= 0 && CobolString.Compare({read}, {EmitText.CsLiteral(StringMembershipValue(high, width))}{pad}{collate}) <= 0)";
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

    /// <summary>A string level-88 VALUE operand's character value: a figurative <c>ALL "literal"</c> repeated to the
    /// conditional variable's <paramref name="width"/> (ISO §8.3.3.6.4 GR2), a bare figurative WORD (QUOTE / SPACE /
    /// HIGH-VALUE / LOW-VALUE / ZERO — §8.3.1.2, materialized to the variable's width, NC250A IF--TEST-26/27),
    /// else the decoded literal.</summary>
    private string StringMembershipValue(string raw, int width) =>
        EmitText.AllLiteralText(raw) is { } lit ? EmitText.RepeatToWidth(lit, width)
        : FigurativeFillChar(raw) is { } fill ? new string(fill, width)
        : CobolLiteral.Decode(raw);

    /// <summary>The fill character of a bare figurative-constant word (with or without a leading <c>ALL</c> —
    /// the same figurative either way, ISO §8.3.1.2), or null when the text is not a figurative word. The fill
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
    /// (ISO §8.3.1.2 — a valid numeric operand); otherwise the literal is scaled. Without this a figurative VALUE word
    /// (e.g. <c>88 IS-ZERO VALUE ZERO</c>) would reach <c>UnscaledAtScale("ZERO", …)</c> and emit a bare identifier.</summary>
    private static string NumericMembershipValue(string raw, int scale) =>
        raw.ToUpperInvariant() is "ZERO" or "ZEROS" or "ZEROES" ? "0L" : EmitText.UnscaledAtScale(raw, scale);
}
