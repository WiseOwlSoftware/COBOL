// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The shared operand/expression/receiving spine (P7 Step 10q — the expression-spine flip; the plan’s
/// `Binding/Procedure/ExpressionBinder`, a SIBLING of <see cref="BinderContext"/>/<see cref="PhraseBlocks"/>,
/// deliberately NOT `Verbs/` — every verb binder consumes it). Three families over <see cref="BinderContext"/>:
/// the OPERAND family (LiteralOperand / national §8.3.3.5 / boolean §8.3.3.4 / figurative §8.3.3.6.4 /
/// FieldOperand with the keyword-omitted-function, LINAGE-COUNTER §8.4.3.14, RW-counter §8.4.3.15 and
/// index-name §13.18.38 interceptions), the EXPRESSION spine (<see cref="BindExpr"/> / BindChain / BindPower /
/// BindPrimary / the breadth-first <see cref="BindOperandExpr"/> wrapper descent / NumLiteral, with
/// <see cref="CheckLiteral"/> carrying its DECIMAL-POINT normalization + edition digit-cap window AS-IS — the
/// sanctioned binder-side pattern), and the RECEIVING family (<see cref="ResolveReceiving"/> — THE one
/// receiving-side chokepoint — ResolveTargets / Receivers×3 / RoundingOf §14.7.4). Host edges that remain
/// (host.Intrinsic / host.Rw) flip at 10t; the generic tree statics DataRefs/Children STAY on
/// <see cref="StatementBinder"/> (they are not expression-specific).
/// </summary>
internal sealed class ExpressionBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>The C# <c>long</c> index field when <paramref name="dref"/> is a bare INDEXED BY index-name
    /// (ISO §13.18.38 — index-names are a separate name class living in <see cref="DataBinder.IndexFields"/>,
    /// not the data-item tree), else <see langword="null"/>.</summary>
    public string? IndexFieldOf(Core.DataReferenceContext dref) =>
        dref.dataReferenceSuffix().Length == 0 && dref.cobolWord()?.GetText() is { } w
        && ctx.Symbols.TryResolveIndex(w, ctx.ActiveScope, out var f) ? f : null;

    // ── Operands & expressions ─────────────────────────────────────────────────────────────────────────────

    public BoundOperand LiteralOperand(Core.LiteralContext lit)
    {
        var nn = lit.nonNumericLiteral();
        if (nn?.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (nn?.STRINGLIT() is { } s) return new BoundStringLiteral(CobolLiteral.Decode(s.GetText()));
        // National N"…" (§8.3.3.5) / boolean B"…" (§8.3.3.4) literals — LIVE (Phase 4a): the introduction
        // gate rides every occurrence (0900 below 2002); content/size guards are the 0814 band. The lexer
        // already restricts a BOOLLIT's content to [01]+ (CobolLexer.g4).
        if (nn?.NATLIT() is { } nat) return NationalLiteralOperand(nat.GetText());
        if (nn?.BOOLLIT() is { } b) return BooleanLiteralOperand(b.GetText());
        return new BoundNumericLiteral(CheckLiteral(lit.GetText()));   // edition digit cap (ISO §8.3.1.2)
    }

    /// <summary>Bind an <c>N"…"</c> national literal (ISO §8.3.3.5): SR1 caps the length at 8,191 national
    /// positions; the track-(a) repertoire is Latin-1 (chars ≤ U+00FF, D-N4) — a wider character needs the
    /// staged alphanumeric↔national correspondence (§8.3.3.5 SR2/GR3 + §8.1.2) and errors 0814, never a
    /// silent mojibake store.</summary>
    public BoundStringLiteral NationalLiteralOperand(string raw)
    {
        // NationalData2002 (the N"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        string value = CobolLiteral.Decode(raw);
        if (value.Length > 8191)
            ctx.Edition.Error("COBOLNET0814", $"national literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.5 SR1)");
        if (value.Any(c => c > 'ÿ'))
            ctx.Edition.Error("COBOLNET0814", "national literal contains a character outside the Latin-1 "
                + "repertoire — the alphanumeric↔national correspondence for wider characters is not yet "
                + "implemented (Phase 4a residue; ISO §8.3.3.5 SR2/GR3, §8.1.2)");
        return new BoundStringLiteral(value) { Category = PicCategory.National };
    }

    /// <summary>Bind a <c>B"…"</c> boolean literal (ISO §8.3.3.4): SR1 caps the length at 8,191 boolean
    /// positions; SR2 ('0'/'1' only) is lexer-enforced.</summary>
    public BoundStringLiteral BooleanLiteralOperand(string raw)
    {
        // BooleanData2002 (the B"…" literal introduction) gates on RECOGNITION in the VersionConformancePass
        // parse-arm (VisitNonNumericLiteral, statement-scoped); Step 14h.4b.
        string value = CobolLiteral.Decode(raw);
        if (value.Length > 8191)
            ctx.Edition.Error("COBOLNET0814", $"boolean literal of {value.Length} positions exceeds the "
                + "8,191-position maximum (ISO §8.3.3.4 SR1)");
        return new BoundStringLiteral(value) { Category = PicCategory.Boolean };
    }

    /// <summary>Bind a figurative constant to a bound operand. <c>ALL "literal"</c> (a multi-character figurative,
    /// ISO §8.3.3.6.4 Format 6) → <see cref="BoundAllLiteral"/>; <c>ALL ZEROS</c> etc. are the single-character
    /// figurative repeated to width, identical to the bare word. (ALL HEXLIT / NULL stay a later slice.)</summary>
    public static BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig)
    {
        if (fig.STRINGLIT() is { } allLit) return new BoundAllLiteral(CobolLiteral.Decode(allLit.GetText()));
        if (fig.ZERO() is not null) return new BoundFigurative('Z');
        if (fig.SPACE() is not null) return new BoundFigurative('S');
        if (fig.HIGH_VALUE() is not null) return new BoundFigurative('H');
        if (fig.LOW_VALUE() is not null) return new BoundFigurative('L');
        if (fig.QUOTE_() is not null) return new BoundFigurative('Q');
        if (fig.NULL_() is not null) return new BoundFigurative('N');
        return new BoundOperandError($"figurative constant '{fig.GetText()}'");
    }

    public BoundOperand FieldOperand(Core.DataReferenceContext dref) =>
        host.Intrinsic.KeywordOmittedFunction(dref) is { } kof ? IntrinsicBinder.OperandOf(kof)   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundComputedOperand(new BoundLinageCounterRef(lcf))
                : new BoundOperandError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15) — RWCS registers, intercepted ahead of name resolution
        // (the LINAGE-COUNTER idiom); a BoundExprError inside the computed wrapper stays loud (§1.4).
        : host.Rw.CounterExpr(dref) is { } rcx ? new BoundComputedOperand(rcx)
        : IndexFieldOf(dref) is { } ix ? new BoundComputedOperand(new BoundIndexRef(ix))
        : ctx.Refs.Resolve(dref) is { } p ? new BoundFieldOperand(p) : new BoundOperandError(RefFailure(dref));

    /// <summary>The loud-failure text for an unresolvable data reference — when the name belongs to a REJECTED
    /// shared-storage class (a Tier-C / national REDEFINES, an unsupported cell shape), the class's
    /// <c>RejectReason</c> rides along so the runtime loud names WHY, not just the reference (the
    /// design's "references then fail loud" contract, made self-explanatory).</summary>
    private string RefFailure(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        string? reason = ctx.Symbols.TryResolve(name, ctx.ActiveScope, out var named)
            ? named.Select(i => i.Class)
                .FirstOrDefault(c => c is { Tier: RedefinesTier.Rejected, RejectReason: not null })
                ?.RejectReason
            : null;
        return reason is null ? $"reference '{dref.GetText()}'" : $"reference '{dref.GetText()}' — {reason}";
    }

    /// <summary>Bind a data reference in a numeric-expression position: an INDEXED BY index-name reads its
    /// occurrence number (valid in SET/SEARCH/relations, ISO §13.18.38); the LINAGE-COUNTER register reads its
    /// file's runtime counter (ISO §8.4.3.14 GR1 — an unsigned integer); otherwise the resolved item's value.
    /// The ONE dataReference→<see cref="BoundExpr"/> mapping, used by every expression path.</summary>
    private BoundExpr RefExpr(Core.DataReferenceContext dref) =>
        host.Intrinsic.KeywordOmittedFunction(dref) is { } kof ? kof   // §8.4.3.2 SR2 — a repository intrinsic/function name + (args) without FUNCTION
        : dref.LINAGE_COUNTER() is not null
            ? LinageFileOf(dref) is { } lcf ? new BoundLinageCounterRef(lcf)
                : new BoundExprError($"LINAGE-COUNTER reference '{dref.GetText()}' (ISO §8.4.3.14)")
        // LINE-COUNTER / PAGE-COUNTER (ISO §8.4.3.15): in the PROCEDURE DIVISION the registers may appear
        // wherever an integer item may (SR1) — read from the report's engine instance, never storage.
        : host.Rw.CounterExpr(dref) is { } rcx ? rcx
        : IndexFieldOf(dref) is { } ix ? new BoundIndexRef(ix)
        : ctx.Refs.Resolve(dref) is { } p ? new BoundNumRef(p)
        : new BoundExprError(RefFailure(dref));

    /// <summary>Resolve a LINAGE-COUNTER reference to its file (ISO §8.4.3.14): in the grammar alternative
    /// <c>LINAGE_COUNTER ((OF|IN) cobolWord)?</c> the cobolWord IS the file-name qualifier. Unqualified, the
    /// register resolves only when exactly ONE file has a LINAGE clause — with several, qualification is
    /// required (§8.4.3.14 SR3 / §8.4.2.2). Null (the caller binds a loud error) for no/an ambiguous match,
    /// with a bind-time diagnostic naming the rule.</summary>
    private FileModel? LinageFileOf(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord() is { } q)   // qualified: LINAGE-COUNTER OF/IN file-name
        {
            if (ctx.Data.FilesByName.TryGetValue(q.GetText(), out var named) && named.Linage is not null) return named;
            ctx.Edition.Error("COBOLNET0863", $"LINAGE-COUNTER OF '{q.GetText()}': the qualifier shall name a "
                + "file whose file description entry contains a LINAGE clause (ISO §8.4.3.14 / §13.18.34 GR7a)");
            return null;
        }
        var linageFiles = ctx.Data.Files.Where(f => f.Linage is not null).ToList();
        if (linageFiles.Count == 1) return linageFiles[0];
        ctx.Edition.Error("COBOLNET0864", linageFiles.Count == 0
            ? "LINAGE-COUNTER referenced, but no file description entry contains a LINAGE clause (ISO §8.4.3.14 — "
              + "the register is generated by the presence of a LINAGE clause)"
            : "unqualified LINAGE-COUNTER with more than one LINAGE file: qualify by file-name (ISO §8.4.3.14 "
              + "SR3 / §8.4.2.2 Qualification)");
        return null;
    }

    // Receiving references resolve through ResolveReceiving below — the ONE
    // receiving-side chokepoint: a report counter receiver is rejected at bind (LINE-COUNTER illegal per ISO
    // §8.4.3.15 SR3; PAGE-COUNTER staged loud) instead of being SILENTLY dropped by .OfType<Place>() (§1.4).
    public List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) =>
        targets.Select(ResolveReceiving).OfType<Place>().ToList();

    // ── ROUNDED phrase → rounding mode + receiver resolution (ISO §14.7.4) ───────────────────────────────────

    /// <summary>The rounding mode a (possibly absent) ROUNDED phrase selects (ISO §14.7.4.3). No phrase → TRUNCATION
    /// (rule 2); a bare <c>ROUNDED</c> → the program's DEFAULT ROUNDED mode (rule 1 / §11.9.6 — the OPTIONS
    /// <c>DEFAULT ROUNDED MODE IS x</c> clause, defaulting to NEAREST-AWAY-FROM-ZERO when absent); an explicit
    /// <c>MODE IS x</c> → the named mode (via the shared <see cref="RoundingModes"/> mapping).</summary>
    public CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase)
    {
        if (phrase is null) return CobolRounding.Truncation;
        if (phrase.roundingModeName() is { } mode)
        {
            // The explicit MODE IS phrase (and the 8-mode set) is ISO 2014+ (§14.7.4); at 85/2002 a bare ROUNDED
            // means the single nearest-away-from-zero rounding. The RoundedModeIs2014 introduction gate fires on
            // RECOGNITION in the VersionConformancePass parse-arm (VisitRoundedPhrase, roundingModeName != null); 14h.4a.
            return RoundingModes.Map(mode);
        }
        return ctx.Options.DefaultRounding;
    }

    // ── The RECEIVING chokepoint (hoisted from the ReportWriter partial at 10f; HOME here since 10q —
    //    the shared receiving spine the arithmetic/MOVE/SET pipelines consume). ──

    /// <summary>Resolve a RECEIVING data reference to its <see cref="Place"/> — the ONE receiving-side
    /// chokepoint (MOVE targets, arithmetic resultants, SET receivers). A report counter here is rejected at
    /// bind time: LINE-COUNTER shall not be a receiving operand (ISO §8.4.3.15 SR3 — illegal); PAGE-COUNTER as a
    /// receiver is legal but not yet implemented (staged loud). Without this guard the
    /// <c>.OfType&lt;Place&gt;()</c> receiver pipelines would DROP the counter silently — a silent-miscompile
    /// hazard (§1.4).</summary>
    public Place? ResolveReceiving(Core.DataReferenceContext dref)
    {
        if (dref.LINE_COUNTER() is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportLineCounterReceiving,
                "LINE-COUNTER shall not be referenced as a receiving operand (ISO §8.4.3.15.3 SR3)");
            return null;
        }
        if (dref.PAGE_COUNTER() is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.ReportPageCounterReceiving, "PAGE-COUNTER as a receiving operand (ISO §8.4.3.15 — legal; the "
                + "program assigns page numbers) is not yet implemented");
            return null;
        }
        var place = ctx.Refs.Resolve(dref);
        // The OCCURS DYNAMIC CAPACITY register (§13.18.38 SR30–32; D9) is set ONLY by a SET Format 14 statement
        // (which reroutes BEFORE this chokepoint). Any other receiving use — MOVE/arithmetic resultant/ordinary SET
        // receiver — is illegal; reject it here rather than reach CapacityRegisterPlace.Write (an internal throw).
        if (place is CapacityRegisterPlace cap)
        {
            ctx.Edition.Error("COBOLNET1523", $"the CAPACITY register '{cap.RegisterItem.CobolName}' shall not be a "
                + "receiving operand except in a SET statement Format 14 (ISO §13.18.38 SR30–32)");
            return null;
        }
        return place;
    }

    /// <summary>Resolve <c>receivingArithmeticOperand</c>s (the GIVING / TO / FROM / INTO resultants) to
    /// <see cref="Receiver"/>s, each carrying its own ROUNDED mode; an unresolvable reference is dropped.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.ReceivingArithmeticOperandContext> ops) =>
        ops.Select(o => ResolveReceiving(o.dataReference()) is { } p ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the in-place <c>MULTIPLY … BY</c> receivers (<c>multiplyByOperand</c> = receiving operand +
    /// optional ROUNDED), each carrying its own mode; a literal BY operand (only valid in a GIVING form) is dropped.</summary>
    public List<Receiver> Receivers(IEnumerable<Core.MultiplyByOperandContext> ops) =>
        ops.Select(o => o.receivingOperand()?.dataReference() is { } d && ResolveReceiving(d) is { } p
                ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the <c>COMPUTE</c> resultants (<c>computeStore</c> = data reference + optional ROUNDED).</summary>
    public List<Receiver> Receivers(IEnumerable<Core.ComputeStoreContext> stores) =>
        stores.Select(s => ResolveReceiving(s.dataReference()) is { } p ? new Receiver(p, RoundingOf(s.roundedPhrase())) : null)
              .OfType<Receiver>().ToList();

    /// <summary>Bind any numeric node (expression, operand wrapper, literal, or data reference) to a bound expression.</summary>
    public BoundExpr BindExpr(IParseTree node) => node switch
    {
        Core.ArithmeticExpressionContext a => BindExpr(a.GetChild(0)),
        Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext => BindChain(node),
        Core.PowerExpressionContext p => BindPower(p),
        Core.UnaryExpressionContext u => u.primaryExpression() is { } pr ? BindExpr(pr)
            : u.addOp().GetText() == "-" ? new BoundNegate(BindExpr(u.unaryExpression())) : BindExpr(u.unaryExpression()),
        Core.PrimaryExpressionContext pe => BindPrimary(pe),
        Core.LiteralContext l => NumLiteral(l),
        Core.DataReferenceContext d => RefExpr(d),
        _ => BindOperandExpr(node),   // operand wrappers (addOperand, multiplyByOperand, …)
    };

    /// <summary>A numeric literal expression from a <c>literal</c> node, mapping a figurative ZERO (incl. <c>ALL ZEROS</c>)
    /// to <c>0</c> (ISO §8.3.1.2 — ZERO is a valid numeric operand); a non-numeric figurative (SPACE / HIGH-VALUE / …)
    /// in a numeric context is a loud error rather than the raw word rendered as an identifier. A national or
    /// boolean literal is NOT a numeric operand (§8.8.1.1 — arithmetic operands shall be numeric): COBOLNET0844
    /// at bind, never raw literal text spliced into the generated expression.</summary>
    private BoundExpr NumLiteral(Core.LiteralContext lit)
    {
        if (lit.nonNumericLiteral()?.figurativeConstant() is { } fig)
            return fig.ZERO() is not null ? new BoundNumLiteral("0")
                : new BoundExprError($"figurative constant '{fig.GetText()}' in a numeric context");
        if (lit.nonNumericLiteral() is { } nn && (nn.NATLIT() ?? nn.BOOLLIT()) is not null)
        {
            ctx.Edition.Error("COBOLNET0844", $"a {(nn.NATLIT() is not null ? "national" : "boolean")} "
                + "literal is not a numeric operand (ISO §8.8.1.1 — arithmetic operands shall be numeric)");
            return new BoundExprError($"literal '{lit.GetText()}' in a numeric context");
        }
        return new BoundNumLiteral(CheckLiteral(lit.GetText()));
    }

    /// <summary>Normalize the decimal separator (DECIMAL-POINT IS COMMA, ISO §12.3.7 GR14a — the comma form
    /// canonicalizes to dot-decimal so every emit-side decoder sees one shape) and edition-gate the digit count
    /// (ISO §8.3.1.2 — 1..18 at COBOL-85, 1..31 at 2002+). The ONE literal chokepoint for the expression paths.</summary>
    public string CheckLiteral(string text)
    {
        text = ctx.Data.NormalizeNumericLiteral(text);
        int digits = text.Count(char.IsAsciiDigit);
        ctx.Edition.CheckDigitCapacity(digits, $"numeric literal '{text}'");
        return text;
    }

    private BoundExpr BindChain(IParseTree node)
    {
        BoundExpr? acc = null;
        char op = '+';
        foreach (var child in StatementBinder.Children(node))
        {
            if (child is Core.AddOpContext or Core.MulOpContext) op = child.GetText()[0];
            else { var x = BindExpr(child); acc = acc is null ? x : new BoundBinary(acc, op, x); }
        }
        return acc ?? new BoundNumLiteral("0");
    }

    private BoundExpr BindPower(Core.PowerExpressionContext p)
    {
        var bases = p.unaryExpression();
        BoundExpr acc = BindExpr(bases[0]);
        for (int i = 1; i < bases.Length; i++) acc = new BoundPower(acc, BindExpr(bases[i]));
        return acc;
    }

    private BoundExpr BindPrimary(Core.PrimaryExpressionContext pe)
    {
        if (pe.numericLiteral() is { } num) return new BoundNumLiteral(CheckLiteral(num.GetText()));
        if (pe.ZERO_ARITH() is not null) return new BoundNumLiteral("0");
        if (pe.dataReference() is { } dref) return RefExpr(dref);
        if (pe.arithmeticExpression() is { } paren) return BindExpr(paren);
        // FUNCTION call (ISO §15; the 1989 Intrinsic Function Module) — StatementBinder.Intrinsics.cs.
        if (pe.functionCall() is { } fc) return host.Intrinsic.BindIntrinsic(fc);
        return new BoundExprError("primary-expression operand");
    }

    /// <summary>Descend an operand-wrapper node to its inner arithmetic expression, or its leaf literal / data
    /// ref. The wrapper chain can nest the expression MORE than one level deep (<c>comparisonOperand →
    /// valueOperand → arithmeticExpression</c>, CobolExpressions.g4), so the walk is BREADTH-FIRST to the
    /// shallowest match — a depth-first leaf grab would collapse a multi-term operand to its first data
    /// reference (a sign condition's operand is the WHOLE expression, ISO §8.8.4.3 — NC250A IF--TEST-55/56).</summary>
    public BoundExpr BindOperandExpr(IParseTree node)
    {
        var queue = new Queue<IParseTree>();
        queue.Enqueue(node);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            for (int i = 0; i < n.ChildCount; i++)
            {
                var c = n.GetChild(i);
                if (c is Core.ArithmeticExpressionContext ae) return BindExpr(ae);
                if (c is Core.LiteralContext l) return NumLiteral(l);
                if (c is Core.DataReferenceContext d) return RefExpr(d);
                queue.Enqueue(c);
            }
        }
        return new BoundNumLiteral("0");
    }
}
