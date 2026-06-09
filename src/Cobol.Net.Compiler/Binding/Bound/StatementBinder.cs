// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Runtime;
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
        _ when s.openStatement() is { } o => BindOpen(o),
        _ when s.closeStatement() is { } c => BindClose(c),
        _ when s.writeStatement() is { } w => BindWrite(w),
        _ when s.readStatement() is { } r => BindRead(r),
        _ when s.rewriteStatement() is { } rw => BindRewrite(rw),
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

    // ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────

    private BoundStatement BindOpen(Core.OpenStatementContext o)
    {
        var opens = new List<(FileModel, BoundOpenMode, string?)>();
        foreach (var clause in o.openClause())
        {
            BoundOpenMode mode = MapOpenMode(clause.openMode());
            foreach (var spec in clause.openFileSpec())
            {
                string name = spec.dataReference().GetText();
                if (!data.FilesByName.TryGetValue(name, out var file))
                    return new BoundUnsupported($"OPEN of undeclared file '{name}'");
                opens.Add((file, mode, UnsupportedOrg(file, "OPEN")));
            }
        }
        return new BoundOpen(opens);
    }

    private BoundStatement BindClose(Core.CloseStatementContext c)
    {
        var closes = new List<(FileModel, BoundCloseKind)>();
        foreach (var phrase in c.closeFilePhrase())
        {
            string name = phrase.fileName().GetText();
            if (!data.FilesByName.TryGetValue(name, out var file))
                return new BoundUnsupported($"CLOSE of undeclared file '{name}'");
            BoundCloseKind kind = phrase.closeOption() is { } opt
                ? opt.LOCK() is not null ? BoundCloseKind.WithLock
                : opt.REEL() is not null || opt.UNIT() is not null ? BoundCloseKind.ReelUnit
                : BoundCloseKind.Normal
                : BoundCloseKind.Normal;
            closes.Add((file, kind));
        }
        return new BoundClose(closes);
    }

    private BoundStatement BindWrite(Core.WriteStatementContext w)
    {
        Place? record = null;
        FileModel? file = null;
        if (w.recordName()?.dataReference() is { } rn && refs.Resolve(rn) is { } place)
        {
            record = place;
            file = FileOfRecord(place);
        }
        else if (w.fileName() is { } fn && data.FilesByName.TryGetValue(fn.GetText(), out var f) && f.Records.Count > 0)
        {
            file = f;
            record = refs.ResolveItem(f.Records[0]);
        }
        if (file is null || record is null)
            return new BoundUnsupported($"WRITE record '{w.recordName()?.GetText() ?? w.fileName()?.GetText()}' not resolvable to a file");

        return new BoundWrite(file, record, WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal()),
            BindAdvancing(w.writeBeforeAfter()), UnsupportedOrg(file, "WRITE"));
    }

    private BoundStatement BindRead(Core.ReadStatementContext r)
    {
        string name = r.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"READ of undeclared file '{name}'");
        Place? into = r.readInto()?.dataReference() is { } d ? refs.Resolve(d) : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
        {
            var blocks = ae.statementBlock();
            if (blocks.Length >= 1) atEnd = BindBlocks([blocks[0]]);
            if (blocks.Length >= 2) notAtEnd = BindBlocks([blocks[1]]);
        }
        return new BoundRead(file, into, atEnd, notAtEnd, UnsupportedOrg(file, "READ"));
    }

    private BoundStatement BindRewrite(Core.RewriteStatementContext rw)
    {
        Place? record = rw.recordName()?.dataReference() is { } rn ? refs.Resolve(rn) : null;
        FileModel? file = record is not null ? FileOfRecord(record) : null;
        if (file is null || record is null)
            return new BoundUnsupported($"REWRITE record '{rw.recordName()?.GetText()}' not resolvable to a file");
        return new BoundRewrite(file, record, WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal()),
            UnsupportedOrg(file, "REWRITE"));
    }

    /// <summary>The FROM operand of a WRITE/REWRITE (a data reference or a literal), or null when absent.</summary>
    private BoundOperand? WriteSource(Core.DataReferenceContext? dref, Core.LiteralContext? lit) =>
        lit is not null ? LiteralOperand(lit) : dref is not null ? FieldOperand(dref) : null;

    /// <summary>Bind the <c>{BEFORE|AFTER} ADVANCING …</c> phrase (ISO §14.9.46), or null for a plain WRITE.</summary>
    private BoundAdvancing? BindAdvancing(Core.WriteBeforeAfterContext? ctx)
    {
        if (ctx is null) return null;
        bool before = ctx.BEFORE() is not null;
        if (ctx.PAGE() is not null) return new BoundAdvancing(before, true, null);
        BoundOperand lines =
            ctx.integerLiteral() is { } il ? new BoundNumericLiteral(il.GetText())
            : ctx.dataReference() is { } d ? FieldOperand(d)
            : ctx.literal() is { } lit ? LiteralOperand(lit)
            : new BoundNumericLiteral("1");
        return new BoundAdvancing(before, false, lines);
    }

    private static BoundOpenMode MapOpenMode(Core.OpenModeContext m) =>
        m.OUTPUT() is not null ? BoundOpenMode.Output
        : m.EXTEND() is not null ? BoundOpenMode.Extend
        : m.I_O() is not null ? BoundOpenMode.IO
        : BoundOpenMode.Input;

    /// <summary>The owning <see cref="FileModel"/> of a record reference: the file whose records include the
    /// reference's top-level (01) record. Null if the reference is not an FD record.</summary>
    private FileModel? FileOfRecord(Place record)
    {
        DataItem root = record.Item;
        while (root.Parent is { } p) root = p;
        foreach (var f in data.Files)
            if (f.Records.Contains(root)) return f;
        return null;
    }

    /// <summary>A loud-reason string when <paramref name="file"/>'s organization is not yet implemented (relative /
    /// indexed in the sequential slice), so the verb emits a runtime not-implemented guard; null when supported.</summary>
    private static string? UnsupportedOrg(FileModel file, string verb) =>
        file.IsSequential ? null : $"{verb} on {file.Organization} file '{file.CobolName}' (sequential slice; relative/indexed are later)";

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
        var sizeErr = BindSizeError(add.arithmeticOnSizeError());
        if (add.addGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §14.9.1 Format 3: the TO operand is an addend, NOT a
            // receiver; only the GIVING operands receive). Previously the TO operand was dropped from the sum.
            if (add.addToPhrase() is { } toAddend)
                addends.AddRange(DataRefs(toAddend).Select(BindExpr));
            return new BoundAddGiving(addends, Receivers(giving.receivingArithmeticOperand()), sizeErr);
        }
        if (add.addToPhrase() is { } to)
            return new BoundAddTo(addends, Receivers(to.receivingArithmeticOperand()), sizeErr);
        return new BoundUnsupported("ADD form");
    }

    private BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.subtractOperandList() is not { } operands) return new BoundUnsupported("SUBTRACT CORRESPONDING");
        var minuends = operands.subtractOperand().Select(BindExpr).ToList();
        var sizeErr = BindSizeError(sub.arithmeticOnSizeError());
        if (sub.subtractGivingPhrase() is { } giving && sub.subtractFromPhrase()?.subtractFromOperand() is { } from)
            return new BoundSubtractGiving(minuends, BindExpr(from), Receivers(giving.receivingArithmeticOperand()), sizeErr);
        if (sub.subtractFromPhrase()?.subtractFromOperand() is { } targets)
            return new BoundSubtractFrom(minuends, Receivers(targets.receivingArithmeticOperand()), sizeErr);
        return new BoundUnsupported("SUBTRACT form");
    }

    private BoundStatement BindMultiply(Core.MultiplyStatementContext mul)
    {
        if (mul.multiplyOperand() is not { } aCtx) return new BoundUnsupported("MULTIPLY form");
        var a = BindExpr(aCtx);
        var byOps = mul.multiplyByOperand();
        var sizeErr = BindSizeError(mul.arithmeticOnSizeError());
        if (mul.multiplyGivingPhrase() is { } giving && byOps.Length > 0)
            return new BoundMultiplyGiving(a, BindExpr(byOps[0]), Receivers(giving.receivingArithmeticOperand()), sizeErr);
        // In-place: each BY operand is itself the receiver (target ← target × a).
        return new BoundMultiplyBy(a, Receivers(byOps), sizeErr);
    }

    private BoundStatement BindDivide(Core.DivideStatementContext div)
    {
        if (div.divideRemainderPhrase() is not null) return new BoundUnsupported("DIVIDE … REMAINDER");
        if (div.divideOperand() is not { } aCtx) return new BoundUnsupported("DIVIDE form");
        var a = BindExpr(aCtx);   // INTO: the divisor; BY: the dividend
        var sizeErr = BindSizeError(div.arithmeticOnSizeError());
        if (div.divideIntoPhrase() is { } into)
        {
            return div.divideGivingPhrase() is { } giving
                ? new BoundDivideGiving(BindExpr(into.divideIntoOperand()), a, Receivers(giving.receivingArithmeticOperand()), sizeErr)
                : new BoundDivideInto(a, Receivers(into.divideIntoOperand().receivingArithmeticOperand()), sizeErr);   // target ← target ÷ a
        }
        if (div.divideByPhrase() is { } byPhrase && div.divideGivingPhrase() is { } g)
            return new BoundDivideGiving(a, BindExpr(byPhrase.divideOperand()), Receivers(g.receivingArithmeticOperand()), sizeErr);
        return new BoundUnsupported("DIVIDE form");
    }

    private BoundStatement BindCompute(Core.ComputeStatementContext compute)
    {
        if (compute.arithmeticExpression() is not { } expr) return new BoundUnsupported("COMPUTE without an expression");
        var rhs = BindExpr(expr);
        return new BoundCompute(rhs, Receivers(compute.computeStore()), BindSizeError(compute.computeOnSizeError()));
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

    // ── ON SIZE ERROR phrase (ISO §14.7.5) ───────────────────────────────────────────────────────────────────

    private SizeErrorPhrase? BindSizeError(Core.ArithmeticOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), StartsWithNot(ctx));

    private SizeErrorPhrase? BindSizeError(Core.ComputeOnSizeErrorContext? ctx) =>
        ctx is null ? null : BuildSizeError(ctx.statementBlock(), StartsWithNot(ctx));

    /// <summary>Build the phrase from the (1 or 2) statement blocks. Both <c>arithmeticOnSizeError</c> and
    /// <c>computeOnSizeError</c> have the shape <c>ON SIZE ERROR b1 (NOT ON SIZE ERROR b2)? | NOT ON SIZE ERROR b1</c>;
    /// the NOT-only alternative is detected by its leading <c>NOT</c> token.</summary>
    private SizeErrorPhrase BuildSizeError(Core.StatementBlockContext[] blocks, bool notOnly)
    {
        if (notOnly) return new SizeErrorPhrase(null, BindBlocks([blocks[0]]));
        var onErr = blocks.Length >= 1 ? BindBlocks([blocks[0]]) : null;
        var notErr = blocks.Length >= 2 ? BindBlocks([blocks[1]]) : null;
        return new SizeErrorPhrase(onErr, notErr);
    }

    private static bool StartsWithNot(IParseTree ctx) =>
        ctx.ChildCount > 0 && ctx.GetChild(0) is ITerminalNode t && t.Symbol.Type == CobolLexer.NOT;

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

    // ── ROUNDED phrase → rounding mode + receiver resolution (ISO §14.7.4) ───────────────────────────────────

    /// <summary>The rounding mode a (possibly absent) ROUNDED phrase selects (ISO §14.7.4.3). No phrase → TRUNCATION
    /// (rule 2); a bare <c>ROUNDED</c> → the program's DEFAULT ROUNDED mode (rule 1 / §11.9.6 — the OPTIONS
    /// <c>DEFAULT ROUNDED MODE IS x</c> clause, defaulting to NEAREST-AWAY-FROM-ZERO when absent); an explicit
    /// <c>MODE IS x</c> → the named mode (via the shared <see cref="RoundingModes"/> mapping).</summary>
    private CobolRounding RoundingOf(Core.RoundedPhraseContext? phrase) =>
        phrase is null ? CobolRounding.Truncation
        : phrase.roundingModeName() is { } mode ? RoundingModes.Map(mode)
        : data.Options.DefaultRounding;

    /// <summary>Resolve <c>receivingArithmeticOperand</c>s (the GIVING / TO / FROM / INTO resultants) to
    /// <see cref="Receiver"/>s, each carrying its own ROUNDED mode; an unresolvable reference is dropped.</summary>
    private List<Receiver> Receivers(IEnumerable<Core.ReceivingArithmeticOperandContext> ops) =>
        ops.Select(o => refs.Resolve(o.dataReference()) is { } p ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the in-place <c>MULTIPLY … BY</c> receivers (<c>multiplyByOperand</c> = receiving operand +
    /// optional ROUNDED), each carrying its own mode; a literal BY operand (only valid in a GIVING form) is dropped.</summary>
    private List<Receiver> Receivers(IEnumerable<Core.MultiplyByOperandContext> ops) =>
        ops.Select(o => o.receivingOperand()?.dataReference() is { } d && refs.Resolve(d) is { } p
                ? new Receiver(p, RoundingOf(o.roundedPhrase())) : null)
           .OfType<Receiver>().ToList();

    /// <summary>Resolve the <c>COMPUTE</c> resultants (<c>computeStore</c> = data reference + optional ROUNDED).</summary>
    private List<Receiver> Receivers(IEnumerable<Core.ComputeStoreContext> stores) =>
        stores.Select(s => refs.Resolve(s.dataReference()) is { } p ? new Receiver(p, RoundingOf(s.roundedPhrase())) : null)
              .OfType<Receiver>().ToList();

    /// <summary>Bind any numeric node (expression, operand wrapper, literal, or data reference) to a bound expression.</summary>
    private BoundExpr BindExpr(IParseTree node) => node switch
    {
        Core.ArithmeticExpressionContext a => BindExpr(a.GetChild(0)),
        Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext => BindChain(node),
        Core.PowerExpressionContext p => BindPower(p),
        Core.UnaryExpressionContext u => u.primaryExpression() is { } pr ? BindExpr(pr)
            : u.addOp().GetText() == "-" ? new BoundNegate(BindExpr(u.unaryExpression())) : BindExpr(u.unaryExpression()),
        Core.PrimaryExpressionContext pe => BindPrimary(pe),
        Core.LiteralContext l => NumLiteral(l),
        Core.DataReferenceContext d => refs.Resolve(d) is { } p ? new BoundNumRef(p) : new BoundExprError($"reference '{d.GetText()}'"),
        _ => BindOperandExpr(node),   // operand wrappers (addOperand, multiplyByOperand, …)
    };

    /// <summary>A numeric literal expression from a <c>literal</c> node, mapping a figurative ZERO (incl. <c>ALL ZEROS</c>)
    /// to <c>0</c> (ISO §8.3.1.2 — ZERO is a valid numeric operand); a non-numeric figurative (SPACE / HIGH-VALUE / …)
    /// in a numeric context is a loud error rather than the raw word rendered as an identifier.</summary>
    private static BoundExpr NumLiteral(Core.LiteralContext lit) =>
        lit.nonNumericLiteral()?.figurativeConstant() is { } fig
            ? fig.ZERO() is not null ? new BoundNumLiteral("0")
                : new BoundExprError($"figurative constant '{fig.GetText()}' in a numeric context")
            : new BoundNumLiteral(lit.GetText());

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
            if (c is Core.LiteralContext l) return NumLiteral(l);
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
            if (c is Core.LiteralContext l) return NumLiteral(l);
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
