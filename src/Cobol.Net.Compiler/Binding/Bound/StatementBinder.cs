// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolSharp.Compiler.Generated;
using Microsoft.CodeAnalysis.CSharp;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

/// <summary>
/// Builds the <see cref="BoundProgram"/> from a parsed program unit: it resolves every reference to a
/// <see cref="Place"/>, decodes every literal, and binds every expression / condition / statement into a bound node
/// exactly once (COBOLNET_DESIGN §2). The backend then renders the bound tree — it never re-walks the parse tree.
/// </summary>
public sealed class StatementBinder(DataBinder data, ReferenceResolver refs)
{
    private readonly List<(string Cobol, string Method, Core.ParagraphDefinitionContext Ctx)> _paras = [];
    private readonly Dictionary<string, int> _paraIndex = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bind a program unit's PROCEDURE DIVISION into a <see cref="BoundProgram"/>.</summary>
    public BoundProgram Bind(Core.ProgramUnitContext program)
    {
        if (program.procedureDivision() is not { } pd) return new BoundProgram([]);
        CollectParagraphs(pd);

        var bound = new List<BoundParagraph>(_paras.Count);
        foreach (var (cobol, _, ctx) in _paras)
        {
            var stmts = new List<BoundStatement>();
            foreach (var sentence in ctx.sentence())
                foreach (var statement in sentence.statement())
                    stmts.Add(BindStatement(statement));
            bound.Add(new BoundParagraph(cobol, stmts));
        }
        return new BoundProgram(bound);
    }

    // ── Paragraph table ────────────────────────────────────────────────────────────────────────────────────

    private void CollectParagraphs(Core.ProcedureDivisionContext pd)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        void Add(Core.ParagraphDefinitionContext p)
        {
            string name = p.paragraphName().GetText();
            string baseName = "P_" + name.Replace('-', '_').Replace('.', '_');
            string method = baseName;
            for (int n = 2; !used.Add(method); n++) method = $"{baseName}_{n}";
            _paraIndex.TryAdd(name, _paras.Count);   // first definition wins for PERFORM resolution
            _paras.Add((name, method, p));
        }

        foreach (var unit in pd.procedureUnit())
        {
            if (unit.paragraphDefinition() is { } para) Add(para);
            else if (unit.sectionDefinition() is { } section)
                foreach (var p in section.paragraphDefinition()) Add(p);
        }
    }

    /// <summary>The emitted paragraphs (name + method + statements), exposed for the backend's method loop.</summary>
    public IReadOnlyList<(string Cobol, string Method, Core.ParagraphDefinitionContext Ctx)> Paragraphs => _paras;

    private string MethodOf(string cobolName) =>
        _paraIndex.TryGetValue(cobolName, out int i) ? _paras[i].Method : "P_" + cobolName.Replace('-', '_');

    // ── Statements ─────────────────────────────────────────────────────────────────────────────────────────

    private BoundStatement BindStatement(Core.StatementContext s) => s switch
    {
        _ when s.displayStatement() is { } d => BindDisplay(d),
        _ when s.moveStatement() is { } m => BindMove(m),
        _ when s.addStatement() is { } a => BindAdd(a),
        _ when s.subtractStatement() is { } sub => BindSubtract(sub),
        _ when s.multiplyStatement() is { } mul => BindMultiply(mul),
        _ when s.divideStatement() is { } div => BindDivide(div),
        _ when s.computeStatement() is { } c => BindCompute(c),
        _ when s.ifStatement() is { } iff => BindIf(iff),
        _ when s.performStatement() is { } p => BindPerform(p),
        _ when s.setStatement() is { } set => BindSet(set),
        _ when s.goToStatement() is { } g => BindGoTo(g),
        _ when s.exitStatement() is { } e => BindExit(e),
        _ when s.continueStatement() is not null => new BoundNop(),
        _ when s.stopStatement() is not null || s.gobackStatement() is not null => new BoundStop(),
        _ => new BoundUnsupported($"statement '{FirstToken(s)}'"),
    };

    private BoundStatement BindGoTo(Core.GoToStatementContext g)
    {
        var names = g.procedureName();
        if (g.dataReference() is { } sel && names.Length >= 1)   // GO TO p1 p2 … DEPENDING ON sel
        {
            var targets = new List<int>();
            foreach (var n in names)
            {
                if (PcOf(n.GetText()) is not { } pc) return new BoundUnsupported($"GO TO unknown paragraph '{n.GetText()}'");
                targets.Add(pc);
            }
            return new BoundGoToDepending(FieldOperand(sel), targets);
        }
        if (names.Length == 0) return new BoundUnsupported("GO TO (altered / no target)");   // ALTER form — later
        return PcOf(names[0].GetText()) is { } target
            ? new BoundGoTo(target)
            : new BoundUnsupported($"GO TO unknown paragraph '{names[0].GetText()}'");
    }

    private static BoundStatement BindExit(Core.ExitStatementContext e)
    {
        if (e.PARAGRAPH() is not null) return new BoundExitParagraph();
        if (e.PERFORM() is not null) return new BoundExitPerform(e.CYCLE() is not null);
        if (e.PROGRAM() is not null) return new BoundNop();        // EXIT PROGRAM in the main program is a no-op
        if (e.SECTION() is not null) return new BoundUnsupported("EXIT SECTION");        // needs section bounds — later
        if (e.METHOD() is not null || e.FUNCTION() is not null) return new BoundUnsupported("EXIT METHOD/FUNCTION");
        return new BoundNop();   // bare EXIT
    }

    /// <summary>The pc index a paragraph name resolves to, or null if unknown.</summary>
    private int? PcOf(string name) => _paraIndex.TryGetValue(name, out int i) ? i : null;

    private BoundStatement BindDisplay(Core.DisplayStatementContext display)
    {
        var ops = new List<BoundOperand>();
        foreach (IParseTree child in Children(display))
            switch (child)
            {
                case Core.LiteralContext lit: ops.Add(LiteralOperand(lit)); break;
                case Core.DataReferenceContext dref: ops.Add(FieldOperand(dref)); break;
            }
        return new BoundDisplay(ops, display.displayNoAdvancing() is not null);
    }

    private BoundStatement BindMove(Core.MoveStatementContext move)
    {
        if (move.moveSendingOperand() is not { } send || move.moveReceivingPhrase()?.dataReferenceList() is not { } targets)
            return new BoundUnsupported("MOVE CORRESPONDING / unsupported MOVE form");
        BoundOperand source = send.literal() is { } lit ? LiteralOperand(lit)
            : send.dataReference() is { } dref ? FieldOperand(dref)
            : new BoundOperandError("MOVE source");
        return new BoundMove(source, ResolveTargets(targets.dataReference()));
    }

    private BoundStatement BindAdd(Core.AddStatementContext add)
    {
        if (add.addOperandList() is not { } operands) return new BoundUnsupported("ADD CORRESPONDING");
        var addends = operands.addOperand().Select(BindExpr).ToList();
        if (add.addGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §14.9.1 Format 3: the TO operand is an addend, NOT a
            // receiver; only the GIVING operands receive). Previously the TO operand was dropped from the sum.
            if (add.addToPhrase() is { } toAddend)
                addends.AddRange(DataRefs(toAddend).Select(BindExpr));
            return new BoundAddGiving(addends, ResolveTargets(DataRefs(giving)));
        }
        if (add.addToPhrase() is { } to)
            return new BoundAddTo(addends, ResolveTargets(DataRefs(to)));
        return new BoundUnsupported("ADD form");
    }

    private BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.subtractOperandList() is not { } operands) return new BoundUnsupported("SUBTRACT CORRESPONDING");
        var minuends = operands.subtractOperand().Select(BindExpr).ToList();
        if (sub.subtractGivingPhrase() is { } giving && sub.subtractFromPhrase()?.subtractFromOperand() is { } from)
            return new BoundSubtractGiving(minuends, BindExpr(from), ResolveTargets(DataRefs(giving)));
        if (sub.subtractFromPhrase()?.subtractFromOperand() is { } targets)
            return new BoundSubtractFrom(minuends, ResolveTargets(DataRefs(targets)));
        return new BoundUnsupported("SUBTRACT form");
    }

    private BoundStatement BindMultiply(Core.MultiplyStatementContext mul)
    {
        if (mul.multiplyOperand() is not { } aCtx) return new BoundUnsupported("MULTIPLY form");
        var a = BindExpr(aCtx);
        var byOps = mul.multiplyByOperand();
        if (mul.multiplyGivingPhrase() is { } giving && byOps.Length > 0)
            return new BoundMultiplyGiving(a, BindExpr(byOps[0]), ResolveTargets(DataRefs(giving)));
        // In-place: each BY operand is itself the receiver (target ← target × a).
        return new BoundMultiplyBy(a, byOps.SelectMany(DataRefs).Select(refs.Resolve).OfType<Place>().ToList());
    }

    private BoundStatement BindDivide(Core.DivideStatementContext div)
    {
        if (div.divideRemainderPhrase() is not null) return new BoundUnsupported("DIVIDE … REMAINDER");
        if (div.divideOperand() is not { } aCtx) return new BoundUnsupported("DIVIDE form");
        var a = BindExpr(aCtx);   // INTO: the divisor; BY: the dividend
        if (div.divideIntoPhrase() is { } into)
        {
            var dividend = BindExpr(into.divideIntoOperand());
            return div.divideGivingPhrase() is { } giving
                ? new BoundDivideGiving(dividend, a, ResolveTargets(DataRefs(giving)))
                : new BoundDivideInto(a, ResolveTargets(DataRefs(into)));   // target ← target ÷ a
        }
        if (div.divideByPhrase() is { } byPhrase && div.divideGivingPhrase() is { } g)
            return new BoundDivideGiving(a, BindExpr(byPhrase.divideOperand()), ResolveTargets(DataRefs(g)));
        return new BoundUnsupported("DIVIDE form");
    }

    private BoundStatement BindCompute(Core.ComputeStatementContext compute)
    {
        if (compute.arithmeticExpression() is not { } expr) return new BoundUnsupported("COMPUTE without an expression");
        var rhs = BindExpr(expr);
        var targets = compute.computeStore().Select(st => st.dataReference()).OfType<Core.DataReferenceContext>()
            .Select(refs.Resolve).OfType<Place>().ToList();
        return new BoundCompute(rhs, targets);
    }

    private BoundStatement BindIf(Core.IfStatementContext iff)
    {
        var thenBlocks = new List<Core.StatementBlockContext>();
        var elseBlocks = new List<Core.StatementBlockContext>();
        bool seenElse = false;
        foreach (var child in Children(iff))
        {
            if (child is ITerminalNode t && t.Symbol.Type == CobolLexer.ELSE) seenElse = true;
            else if (child is Core.StatementBlockContext sb) (seenElse ? elseBlocks : thenBlocks).Add(sb);
        }
        return new BoundIf(BindCondition(iff.condition()), BindBlocks(thenBlocks), BindBlocks(elseBlocks));
    }

    private List<BoundStatement> BindBlocks(IEnumerable<Core.StatementBlockContext> blocks) =>
        blocks.SelectMany(b => b.statement()).Select(BindStatement).ToList();

    private BoundStatement BindPerform(Core.PerformStatementContext p)
    {
        var names = p.procedureName();
        if (names.Length == 0)
            return new BoundInlinePerform(BindControl(p.performOptions().FirstOrDefault(), p), BindBlocks(p.statementBlock()));

        // Out-of-line: the resolved pc range [start, end] — a single paragraph (start==end) or the THRU range.
        if (PcOf(names[0].GetText()) is not { } start)
            return new BoundUnsupported($"PERFORM unknown paragraph '{names[0].GetText()}'");
        int end = start;
        if ((p.THRU() is not null || p.THROUGH() is not null) && names.Length >= 2)
        {
            if (PcOf(names[1].GetText()) is not { } thru) return new BoundUnsupported($"PERFORM THRU unknown paragraph '{names[1].GetText()}'");
            end = thru;
        }

        BoundPerformControl control =
            p.performTimes() is { } times ? new PerformTimes(CountOperand(times))
            : p.performUntil() is { } until ? new PerformUntil(BindCondition(until.condition()), until.AFTER() is not null)
            : p.performVarying() is not null ? Unsupported("PERFORM VARYING (out-of-line)")
            : new PerformOnce();
        return new BoundOutOfLinePerform(start, end, control);
    }

    private BoundPerformControl BindControl(Core.PerformOptionsContext? opt, Core.PerformStatementContext p)
    {
        if (opt is null) return new PerformOnce();
        if (opt.performTimes() is { } t) return new PerformTimes(CountOperand(t));
        if (opt.performUntil() is { } u) return new PerformUntil(BindCondition(u.condition()), u.AFTER() is not null);
        return Unsupported("inline PERFORM VARYING");
    }

    private static BoundPerformControl Unsupported(string feature) => new PerformTimes(new BoundOperandError(feature));

    private BoundOperand CountOperand(Core.PerformTimesContext t) =>
        t.integerLiteral() is { } lit ? new BoundNumericLiteral(lit.GetText())
        : t.dataReference() is { } d ? FieldOperand(d)
        : new BoundNumericLiteral("1");

    private BoundStatement BindSet(Core.SetStatementContext set)
    {
        if (set.setBooleanStatement() is not { } b || b.TRUE_() is null)
            return new BoundUnsupported($"SET form '{set.GetText()}'");
        var sets = new List<(Place, Condition88)>();
        foreach (var dref in b.dataReference())
        {
            if (ConditionOf(dref) is not { } cond) return new BoundUnsupported($"SET '{dref.GetText()}' TO TRUE (not a condition-name)");
            if (refs.ResolveItem(cond.Parent) is not { } parent) return new BoundUnsupported($"SET subscripted condition '{cond.Name}'");
            sets.Add((parent, cond));
        }
        return new BoundSetConditions(sets);
    }

    // ── Operands & expressions ─────────────────────────────────────────────────────────────────────────────

    private BoundOperand LiteralOperand(Core.LiteralContext lit)
    {
        var nn = lit.nonNumericLiteral();
        if (nn?.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (nn?.STRINGLIT() is { } s) return new BoundStringLiteral(DecodeCobolString(s.GetText()));
        return new BoundNumericLiteral(lit.GetText());
    }

    /// <summary>Bind a figurative constant to a <see cref="BoundFigurative"/> (the ALL "x" forms are a later slice).</summary>
    private static BoundOperand FigurativeOperand(Core.FigurativeConstantContext fig)
    {
        if (fig.ALL() is not null) return new BoundOperandError($"figurative constant '{fig.GetText()}'");
        if (fig.ZERO() is not null) return new BoundFigurative('Z');
        if (fig.SPACE() is not null) return new BoundFigurative('S');
        if (fig.HIGH_VALUE() is not null) return new BoundFigurative('H');
        if (fig.LOW_VALUE() is not null) return new BoundFigurative('L');
        if (fig.QUOTE_() is not null) return new BoundFigurative('Q');
        if (fig.NULL_() is not null) return new BoundFigurative('N');
        return new BoundOperandError($"figurative constant '{fig.GetText()}'");
    }

    private BoundOperand FieldOperand(Core.DataReferenceContext dref) =>
        refs.Resolve(dref) is { } p ? new BoundFieldOperand(p) : new BoundOperandError($"reference '{dref.GetText()}'");

    private List<Place> ResolveTargets(IEnumerable<Core.DataReferenceContext> targets) =>
        targets.Select(refs.Resolve).OfType<Place>().ToList();

    /// <summary>Bind any numeric node (expression, operand wrapper, literal, or data reference) to a bound expression.</summary>
    private BoundExpr BindExpr(IParseTree node) => node switch
    {
        Core.ArithmeticExpressionContext a => BindExpr(a.GetChild(0)),
        Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext => BindChain(node),
        Core.PowerExpressionContext p => BindPower(p),
        Core.UnaryExpressionContext u => u.primaryExpression() is { } pr ? BindExpr(pr)
            : u.addOp().GetText() == "-" ? new BoundNegate(BindExpr(u.unaryExpression())) : BindExpr(u.unaryExpression()),
        Core.PrimaryExpressionContext pe => BindPrimary(pe),
        Core.LiteralContext l => new BoundNumLiteral(l.GetText()),
        Core.DataReferenceContext d => refs.Resolve(d) is { } p ? new BoundNumRef(p) : new BoundExprError($"reference '{d.GetText()}'"),
        _ => BindOperandExpr(node),   // operand wrappers (addOperand, multiplyByOperand, …)
    };

    private BoundExpr BindChain(IParseTree node)
    {
        BoundExpr? acc = null;
        char op = '+';
        foreach (var child in Children(node))
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
        if (pe.numericLiteral() is { } num) return new BoundNumLiteral(num.GetText());
        if (pe.ZERO_ARITH() is not null) return new BoundNumLiteral("0");
        if (pe.dataReference() is { } dref) return refs.Resolve(dref) is { } p ? new BoundNumRef(p) : new BoundExprError($"reference '{dref.GetText()}'");
        if (pe.arithmeticExpression() is { } paren) return BindExpr(paren);
        return new BoundExprError("function-call operand");
    }

    /// <summary>Descend an operand-wrapper node to its inner arithmetic expression, or its leaf literal / data ref.</summary>
    private BoundExpr BindOperandExpr(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
            if (node.GetChild(i) is Core.ArithmeticExpressionContext ae) return BindExpr(ae);
        for (int i = 0; i < node.ChildCount; i++)
        {
            var c = node.GetChild(i);
            if (c is Core.LiteralContext l) return new BoundNumLiteral(l.GetText());
            if (c is Core.DataReferenceContext d) return refs.Resolve(d) is { } p ? new BoundNumRef(p) : new BoundExprError($"reference '{d.GetText()}'");
            if (FindLeaf(c) is { } inner) return inner;
        }
        return new BoundNumLiteral("0");
    }

    private BoundExpr? FindLeaf(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var c = node.GetChild(i);
            if (c is Core.LiteralContext l) return new BoundNumLiteral(l.GetText());
            if (c is Core.DataReferenceContext d) return refs.Resolve(d) is { } p ? new BoundNumRef(p) : new BoundExprError($"reference '{d.GetText()}'");
            if (FindLeaf(c) is { } inner) return inner;
        }
        return null;
    }

    // ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────

    private BoundCondition BindCondition(IParseTree node) => node switch
    {
        Core.ConditionContext c => BindCondition(c.GetChild(0)),
        Core.LogicalOrExpressionContext orExpr when orExpr.abbreviatedAndChain().Length == 0 =>
            Combine("||", orExpr.logicalXorExpression()),
        Core.LogicalXorExpressionContext xorExpr => Combine("^", xorExpr.logicalAndExpression()),
        Core.LogicalAndExpressionContext andExpr when andExpr.abbreviatedRelation().Length == 0 =>
            Combine("&&", andExpr.unaryLogicalExpression()),
        Core.UnaryLogicalExpressionContext u => u.NOT() is not null
            ? new BoundNot(BindCondition(u.primaryCondition())) : BindCondition(u.primaryCondition()),
        Core.PrimaryConditionContext p => p.comparisonExpression() is { } cmp ? BindComparison(cmp)
            : p.condition() is { } inner ? BindCondition(inner)
            : new BoundConditionError("boolean-literal condition"),
        _ => new BoundConditionError("unsupported condition form"),
    };

    private BoundCondition Combine(string op, IEnumerable<IParseTree> parts)
    {
        var list = parts.Select(BindCondition).ToList();
        return list.Count == 1 ? list[0] : new BoundLogical(op, list);
    }

    private BoundCondition BindComparison(Core.ComparisonExpressionContext cmp)
    {
        var operands = cmp.comparisonOperand();
        bool not = cmp.NOT() is not null;

        if (cmp.className() is { } cls)
        {
            char? kind = cls.NUMERIC() is not null ? 'N'
                : cls.ALPHABETIC() is not null ? 'A'
                : cls.ALPHABETIC_UPPER() is not null ? 'U'
                : cls.ALPHABETIC_LOWER() is not null ? 'L'
                : null;
            return kind is { } k && operands.Length >= 1
                ? new BoundClassCondition(ComparisonOperand(operands[0]), k, not)
                : new BoundConditionError($"class condition '{cls.GetText()}'");   // user-defined CLASS — later
        }

        if (cmp.POSITIVE() is not null || cmp.NEGATIVE() is not null || cmp.ZERO() is not null)
        {
            char kind = cmp.POSITIVE() is not null ? 'P' : cmp.NEGATIVE() is not null ? 'N' : 'Z';
            return new BoundSignCondition(BindOperandExpr(operands[0]), kind, not);
        }

        if (cmp.comparisonOperator() is { } opCtx && operands.Length >= 2)
            return new BoundRelational(ComparisonOperand(operands[0]), MapOperator(opCtx.GetText()), ComparisonOperand(operands[1]));

        // Bare single operand → a level-88 condition-name.
        if (operands.Length == 1
            && operands[0].valueOperand()?.arithmeticExpression() is { } expr
            && SoleDataRef(expr) is { } dref && ConditionOf(dref) is { } cond)
            return refs.ResolveItem(cond.Parent) is { } parent
                ? new BoundCondition88(parent, cond)
                : new BoundConditionError($"subscripted condition-name '{cond.Name}'");

        return new BoundConditionError($"condition '{cmp.GetText()}'");
    }

    /// <summary>Bind a comparison operand: a non-numeric literal, a sole data reference, or a numeric expression.</summary>
    private BoundOperand ComparisonOperand(Core.ComparisonOperandContext operand)
    {
        var vo = operand.valueOperand();
        if (vo?.nonNumericLiteral()?.figurativeConstant() is { } fig) return FigurativeOperand(fig);
        if (vo?.nonNumericLiteral()?.STRINGLIT() is { } s) return new BoundStringLiteral(DecodeCobolString(s.GetText()));
        if (vo?.arithmeticExpression() is { } expr)
            return SoleDataRef(expr) is { } dref ? FieldOperand(dref) : new BoundComputedOperand(BindExpr(expr));
        return new BoundOperandError("comparison operand");
    }

    private Condition88? ConditionOf(Core.DataReferenceContext dref)
    {
        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        return data.Conditions.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;
    }

    // ── Operator mapping + helpers (ported from the former emitter) ──────────────────────────────────────────

    private static string MapOperator(string raw)
    {
        string t = raw.ToUpperInvariant().Replace("IS", "").Replace("THAN", "").Replace("TO", "");
        if (t.Contains("<>")) return "!=";
        bool not = t.Contains("NOT");
        bool orEqual = t.Contains(">=") || t.Contains("<=") || t.Contains("OREQUAL");
        string baseOp =
            t.Contains('>') || t.Contains("GREATER") ? (orEqual ? ">=" : ">")
            : t.Contains('<') || t.Contains("LESS") ? (orEqual ? "<=" : "<")
            : "==";
        if (!not) return baseOp;
        return baseOp switch { ">" => "<=", ">=" => "<", "<" => ">=", "<=" => ">", "==" => "!=", _ => "==" };
    }

    private static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr)
    {
        IParseTree n = expr;
        while (n is not Core.PrimaryExpressionContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return ((Core.PrimaryExpressionContext)n).dataReference();
    }

    private static IEnumerable<Core.DataReferenceContext> DataRefs(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.DataReferenceContext dref) yield return dref;
            else foreach (var inner in DataRefs(child)) yield return inner;
        }
    }

    private static string DecodeCobolString(string raw) =>
        raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"' ? raw[1..^1].Replace("\"\"", "\"") : raw;

    private static IEnumerable<IParseTree> Children(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++) yield return node.GetChild(i);
    }

    private static string FirstToken(IParseTree node) =>
        node.ChildCount > 0 ? node.GetChild(0).GetText() : node.GetText();
}
