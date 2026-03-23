// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Generated;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.Semantics.Bound;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// The Binder lowers a BoundProgram (typed, symbol-resolved) into an IrModule.
/// It never touches the parse tree — all syntax is pre-resolved by BoundTreeBuilder.
///
/// Paragraph methods return int (next PC):
///   fall-through → myIndex + 1
///   GO TO X      → indexOf(X)
///   STOP RUN     → -1
/// Main dispatches via: while (pc >= 0 && pc &lt; N) pc = paragraphs[pc]();
/// </summary>
public sealed class Binder
{
    private readonly SemanticModel _semantic;
    private readonly RecordLayoutBuilder _layout;
    private readonly DiagnosticBag _diagnostics;
    private static readonly SourceLocation s_noLocation = new("<source>", 0, 0, 0);
    private static readonly TextSpan s_noSpan = new(0, 0);
    private readonly IrValueFactory _valueFactory = new();
    private readonly Dictionary<string, IrMethod> _paragraphMethods =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _paragraphIndices =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _paragraphsByIndex = new();

    // ALTER support: maps alterable paragraph names to their alter table slot indices
    private readonly Dictionary<string, int> _alterSlots =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<int> _alterDefaults = new();

    // Tracks which paragraph is currently being lowered (for ALTER slot detection in LowerGoTo)
    private string? _currentParagraphName;

    /// <summary>
    /// The block representing the start of the next sentence.
    /// Set during sentence lowering; NEXT SENTENCE emits a jump to this block.
    /// </summary>
    private IrBasicBlock? _currentSentenceEnd;

    /// <summary>
    /// Stack of exit blocks for active PERFORM statements.
    /// EXIT PERFORM jumps to the top of this stack (innermost active PERFORM).
    /// </summary>
    private readonly Stack<IrBasicBlock> _performExitStack = new();

    /// <summary>
    /// The end block for the paragraph currently being lowered.
    /// EXIT PARAGRAPH jumps here (fall-through semantics).
    /// </summary>
    private IrBasicBlock? _paragraphEndBlock;

    /// <summary>
    /// The return index for EXIT SECTION: first paragraph index after the current section.
    /// Null if the current paragraph is not in a section.
    /// </summary>
    private int? _sectionExitReturnIndex;

    private readonly CompilationOptions _options;

    public Binder(SemanticModel semantic, DiagnosticBag diagnostics, CompilationOptions? options = null)
    {
        _semantic = semantic;
        _layout = new RecordLayoutBuilder();
        _diagnostics = diagnostics;
        _options = options ?? new CompilationOptions();
    }

    /// <summary>
    /// Build BoundProgram from parse tree, then lower to IrModule.
    /// </summary>
    public IrModule Bind(CobolParserCore.CompilationUnitContext tree)
    {
        // Phase 1: Build bound tree from parse tree + symbols
        var builder = new BoundTreeBuilder(_semantic, _diagnostics, _options);
        var boundProgram = builder.Build(tree);

        // Phase 1.5: Flow analysis on bound program
        Semantics.ProcedureGraph.Analyze(boundProgram, _semantic, _diagnostics);

        // Phase 1.6: Expression type enforcement on bound tree
        Semantics.Bound.BoundTreeValidator.Validate(boundProgram, _diagnostics);

        // Phase 2: Build record types
        var module = new IrModule(boundProgram.Program.Name);
        BuildRecordTypes(module);

        // Phase 3: Create paragraph method stubs (return Int32 for PC)
        int paraIndex = 0;
        foreach (var para in boundProgram.Paragraphs)
        {
            var method = new IrMethod($"Para_{para.Symbol.Name}", returnType: IrPrimitiveType.Int32);
            method.Blocks.Add(new IrBasicBlock($"{para.Symbol.Name}_entry"));
            _paragraphMethods[para.Symbol.Name] = method;
            _paragraphIndices[para.Symbol.Name] = paraIndex;
            _paragraphsByIndex.Add(para.Symbol.Name);
            module.Methods.Add(method);
            paraIndex++;
        }

        // Phase 3.5: Pre-scan for ALTER targets — identify which paragraphs are alterable
        foreach (var para in boundProgram.Paragraphs)
        {
            foreach (var sentence in para.Sentences)
            {
                foreach (var stmt in sentence.Statements)
                {
                    if (stmt is BoundAlterStatement alter)
                    {
                        foreach (var entry in alter.Entries)
                        {
                            string name = entry.TargetParagraph.Name;
                            if (!_alterSlots.ContainsKey(name))
                            {
                                int slot = _alterSlots.Count;
                                _alterSlots[name] = slot;
                                _alterDefaults.Add(-1); // placeholder, updated during LowerGoTo
                            }
                        }
                    }
                }
            }
        }

        // Phase 4: Lower bound statements into IR, preserving sentence structure
        foreach (var para in boundProgram.Paragraphs)
        {
            if (_paragraphMethods.TryGetValue(para.Symbol.Name, out var method))
            {
                int myIndex = _paragraphIndices[para.Symbol.Name];
                var block = method.Blocks[0];
                _currentParagraphName = para.Symbol.Name;

                // Set up EXIT PARAGRAPH target: a block at the end of the paragraph body
                var paraEnd = method.CreateBlock($"{para.Symbol.Name}_exit");
                _paragraphEndBlock = paraEnd;

                // Set up EXIT SECTION target: return index of first paragraph after this section
                var sectionName = _semantic.GetParagraphSection(para.Symbol.Name);
                if (sectionName != null)
                {
                    var sectionParas = _semantic.GetSectionParagraphs(sectionName);
                    if (sectionParas != null && sectionParas.Count > 0
                        && _paragraphIndices.TryGetValue(sectionParas[^1], out var lastIdx))
                    {
                        _sectionExitReturnIndex = lastIdx + 1;
                    }
                    else
                    {
                        _sectionExitReturnIndex = null;
                    }
                }
                else
                {
                    _sectionExitReturnIndex = null;
                }

                for (int si = 0; si < para.Sentences.Count; si++)
                {
                    var sentence = para.Sentences[si];

                    // Create the block that represents the start of the next sentence.
                    // NEXT SENTENCE jumps here; normal flow falls through.
                    var sentenceEnd = new IrBasicBlock($"{para.Symbol.Name}_sent{si}_end");
                    _currentSentenceEnd = sentenceEnd;

                    foreach (var stmt in sentence.Statements)
                        block = LowerStatement(stmt, method, block);

                    // Fall through into the sentence-end block
                    block.Instructions.Add(new IrJump(sentenceEnd));
                    method.Blocks.Add(sentenceEnd);
                    block = sentenceEnd;
                }

                _currentSentenceEnd = null;
                _paragraphEndBlock = null;
                _sectionExitReturnIndex = null;
                _currentParagraphName = null;

                // Normal flow: jump to paragraph end block
                block.Instructions.Add(new IrJump(paraEnd));
                method.Blocks.Add(paraEnd);

                // Fall-through: return next paragraph index
                paraEnd.Instructions.Add(new IrReturnConst(myIndex + 1));
            }
        }

        // Phase 4.5: Store alter table defaults in IR module (zero-cost if no ALTER used)
        module.AlterDefaults.AddRange(_alterDefaults);

        // Phase 5: Create entry point (PC dispatch loop)
        CreateEntryPoint(module, boundProgram);

        return module;
    }

    // ── Record layout ──

    private void BuildRecordTypes(IrModule module)
    {
        foreach (var record in _semantic.DataRecords)
        {
            var layout = _layout.Build(record);
            module.Types.Add(layout.RecordType);
        }
    }

    // ── Entry point ──

    private void CreateEntryPoint(IrModule module, BoundProgram boundProgram)
    {
        var main = new IrMethod("Main", returnType: IrPrimitiveType.Void);
        var block = new IrBasicBlock("main_entry");

        // Initialize file manager
        block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.Init", Array.Empty<IrValue>()));

        // Register file handlers at startup for each SELECT
        foreach (var fileSym in _semantic.Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            // Resolve external path: literal ASSIGN target uses the target value,
            // identifier ASSIGN target (like NIST's XXXXX055) uses the COBOL file name
            string externalName = (fileSym.AssignIsLiteral && fileSym.AssignTarget != null)
                ? fileSym.AssignTarget
                : fileSym.Name;
            string externalPath = FileRuntime.ResolveHostPath(externalName);

            int recordLength = fileSym.RecordLength;
            if (recordLength == 0 && fileSym.Record != null)
            {
                var recLoc = _semantic.GetStorageLocation(fileSym.Record);
                if (recLoc.HasValue)
                    recordLength = recLoc.Value.Length;
            }
            if (recordLength == 0) recordLength = 132; // Default for print files

            // Line-sequential for SEQUENTIAL org (default) and LINE SEQUENTIAL
            bool lineSequential = fileSym.Organization == null
                || fileSym.Organization == "SEQUENTIAL"
                || fileSym.Organization == "LINE SEQUENTIAL";

            string org = fileSym.Organization ?? "SEQUENTIAL";
            int keyOffset = 0, keyLength = 0;

            // For INDEXED files, resolve RECORD KEY to get offset/length
            if (org == "INDEXED" && fileSym.RecordKey != null)
            {
                var keySym = _semantic.ResolveData(fileSym.RecordKey);
                if (keySym != null)
                {
                    var keyLoc = _semantic.GetStorageLocation(keySym);
                    if (keyLoc.HasValue)
                    {
                        // Key offset is relative to the record's start
                        var recordSym = fileSym.Record;
                        if (recordSym != null)
                        {
                            var recordLoc = _semantic.GetStorageLocation(recordSym);
                            if (recordLoc.HasValue)
                                keyOffset = keyLoc.Value.Offset - recordLoc.Value.Offset;
                        }
                        keyLength = keyLoc.Value.Length;
                    }
                }
            }

            var nameVal = _valueFactory.Next(IrPrimitiveType.String);
            var pathVal = _valueFactory.Next(IrPrimitiveType.String);
            var recLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
            var lineSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
            var orgVal = _valueFactory.Next(IrPrimitiveType.String);
            var keyOffVal = _valueFactory.Next(IrPrimitiveType.Int32);
            var keyLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
            block.Instructions.Add(new IrLoadConst(nameVal, fileSym.Name));
            block.Instructions.Add(new IrLoadConst(pathVal, externalPath));
            block.Instructions.Add(new IrLoadConst(recLenVal, recordLength));
            block.Instructions.Add(new IrLoadConst(lineSeqVal, lineSequential));
            block.Instructions.Add(new IrLoadConst(orgVal, org));
            block.Instructions.Add(new IrLoadConst(keyOffVal, keyOffset));
            block.Instructions.Add(new IrLoadConst(keyLenVal, keyLength));
            block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.RegisterFileHandlerWithOrg",
                new[] { nameVal, pathVal, recLenVal, lineSeqVal, orgVal, keyOffVal, keyLenVal }));

            // Register alternate keys for INDEXED files
            if (org == "INDEXED")
            {
                foreach (var altKey in fileSym.AlternateKeys)
                {
                    var altKeySym = _semantic.ResolveData(altKey.DataName);
                    if (altKeySym == null) continue;
                    var altKeyLoc = _semantic.GetStorageLocation(altKeySym);
                    if (!altKeyLoc.HasValue) continue;

                    int altKeyOffset = altKeyLoc.Value.Offset;
                    var recordSym2 = fileSym.Record;
                    if (recordSym2 != null)
                    {
                        var recordLoc2 = _semantic.GetStorageLocation(recordSym2);
                        if (recordLoc2.HasValue)
                            altKeyOffset = altKeyLoc.Value.Offset - recordLoc2.Value.Offset;
                    }

                    var altNameVal = _valueFactory.Next(IrPrimitiveType.String);
                    var altOffVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var altLenVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var altDupVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    block.Instructions.Add(new IrLoadConst(altNameVal, fileSym.Name));
                    block.Instructions.Add(new IrLoadConst(altOffVal, altKeyOffset));
                    block.Instructions.Add(new IrLoadConst(altLenVal, altKeyLoc.Value.Length));
                    block.Instructions.Add(new IrLoadConst(altDupVal, altKey.AllowDuplicates));
                    block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.RegisterAlternateKey",
                        new[] { altNameVal, altOffVal, altLenVal, altDupVal }));
                }
            }
        }

        // Collect paragraph methods in declaration order for Entry method
        foreach (var para in boundProgram.Paragraphs)
        {
            if (_paragraphMethods.TryGetValue(para.Symbol.Name, out var m))
                module.ParagraphDispatchOrder.Add(m);
        }

        // Main calls Entry(Array.Empty<CobolDataPointer>()) — dispatch loop is in Entry
        block.Instructions.Add(new IrRuntimeCall(null, "Self.Entry", Array.Empty<IrValue>()));

        main.Blocks.Add(block);
        module.Methods.Insert(0, main);
    }

    // ── Statement lowering ──

    private IrBasicBlock LowerStatement(BoundStatement stmt, IrMethod method, IrBasicBlock block)
    {
        switch (stmt)
        {
            case BoundCompoundStatement compound:
                foreach (var s in compound.Statements)
                    block = LowerStatement(s, method, block);
                return block;
            case BoundDisplayStatement disp:
                LowerDisplay(disp, block);
                break;
            case BoundMoveStatement mv:
                LowerMove(mv, block);
                break;
            case BoundCorrespondingStatement corr:
                return LowerCorresponding(corr, method, block);
            case BoundPerformStatement perf:
                return LowerPerform(perf, method, block);
            case BoundEvaluateStatement eval:
                return LowerEvaluate(eval, method, block);
            case BoundWriteStatement wr:
                LowerWrite(wr, block);
                break;
            case BoundIfStatement iff:
                return LowerIf(iff, method, block);
            case BoundArithmeticStatement arith:
                return LowerArithmetic(arith, method, block);
            case BoundGoToStatement gt:
                LowerGoTo(gt, block);
                break;
            case BoundAlterStatement alter:
                LowerAlter(alter, block);
                break;
            case BoundStopStatement:
                block.Instructions.Add(new IrStopRun());
                break;
            case BoundExitProgramStatement:
                block.Instructions.Add(new IrExitProgram());
                break;
            case BoundGoBackStatement:
                block.Instructions.Add(new IrGoBack());
                break;
            case BoundExitStatement:
                // EXIT is a no-op; fall-through return handles it
                break;
            case BoundExitPerformStatement:
                return LowerExitPerform(method, block);
            case BoundExitParagraphStatement:
                return LowerExitParagraph(method, block);
            case BoundExitSectionStatement:
                return LowerExitSection(method, block);
            case BoundNextSentenceStatement:
                return LowerNextSentence(method, block);
            case BoundOpenStatement open:
                LowerOpen(open, block);
                break;
            case BoundCloseStatement close:
                LowerClose(close, block);
                break;
            case BoundReadStatement read:
                return LowerRead(read, method, block);
            case BoundRewriteStatement rw:
                LowerRewrite(rw, block);
                break;
            case BoundInitializeStatement init:
                LowerInitialize(init, block);
                break;
            case BoundAcceptStatement acc:
                LowerAccept(acc, block);
                break;
            case BoundInspectStatement insp:
                LowerInspect(insp, block);
                break;
            case BoundSetConditionStatement setCond:
                LowerSetCondition(setCond, block);
                break;
            case BoundSetIndexStatement setIdx:
                LowerSetIndex(setIdx, block);
                break;
            // Subtract, Divide, Compute handled by BoundArithmeticStatement above
            case BoundSearchStatement search:
                return LowerSearch(search, method, block);
            case BoundSearchAllStatement searchAll:
                return LowerSearchAll(searchAll, method, block);
            case BoundStringStatement str:
                return LowerString(str, method, block);
            case BoundUnstringStatement unstr:
                return LowerUnstring(unstr, method, block);
            case BoundDeleteStatement del:
                return LowerDelete(del, method, block);
            case BoundStartStatement start:
                return LowerStart(start, method, block);
            case BoundReturnStatement ret:
                return LowerReturn(ret, method, block);
            case BoundCallStatement call:
                return LowerCall(call, method, block);
        }
        return block;
    }

    // ── Subscript-aware storage resolution ──

    /// <summary>
    /// Resolve a BoundIdentifierExpression to an IrLocation.
    /// - Non-subscripted: returns IrStaticLocation (compile-time offset).
    /// - All-constant subscripts: folds to IrStaticLocation (compile-time offset).
    /// - Any variable subscript: returns IrElementRef (runtime offset computation).
    /// Supports 1D, 2D, and 3D OCCURS (COBOL-85 max 3 dimensions).
    /// Returns null only if the symbol has no registered storage location.
    /// </summary>
    private IrLocation? ResolveLocation(BoundIdentifierExpression id)
    {
        var baseLoc = _semantic.GetStorageLocation(id.Symbol);
        if (!baseLoc.HasValue) return null;

        // Non-subscripted: static location
        if (!id.IsSubscripted)
            return new IrStaticLocation(baseLoc.Value);

        // Collect OCCURS dimension info by walking the symbol tree
        // from the item upward, collecting each OCCURS level
        var occursLevels = new List<(Semantics.DataSymbol sym, int count)>();
        var current = id.Symbol;
        while (current != null)
        {
            if (current.Occurs != null)
                occursLevels.Insert(0, (current, current.Occurs.MaxOccurs));
            current = current.Parent;
        }

        // Two sizes to distinguish:
        // - stepSize: size of one occurrence of the innermost OCCURS level (for subscript arithmetic)
        // - leafSize: size of the leaf element being addressed (for PIC descriptor and IrElementRef)
        //
        // For direct OCCURS items (ITEM PIC X OCCURS 5), stepSize == leafSize.
        // For children of OCCURS groups (VAL PIC 9 within ROW OCCURS 3),
        // stepSize = ROW's element size, leafSize = VAL's size.
        int leafSize = id.Symbol.ElementSize;
        if (leafSize == 0)
            leafSize = baseLoc.Value.Length;

        int stepSize = occursLevels.Count > 0
            ? occursLevels[^1].sym.ElementSize
            : leafSize;
        if (stepSize == 0)
            stepSize = leafSize;

        // Build element PicDescriptor with the leaf element's storage length
        var arrayPic = baseLoc.Value.Pic;
        var elementPic = new Runtime.PicDescriptor(
            arrayPic.TotalDigits, arrayPic.FractionDigits,
            arrayPic.IsSigned, arrayPic.IsNumeric, arrayPic.IsAlphanumeric,
            arrayPic.HasEditing, leafSize, arrayPic.Usage,
            arrayPic.Category, arrayPic.SignStorage, arrayPic.Editing,
            arrayPic.BlankWhenZero, arrayPic.LeadingScaleDigits,
            arrayPic.TrailingScaleDigits, arrayPic.EditPattern);

        // Compute multipliers using stepSize (OCCURS group element size for subscript arithmetic)
        // For 1D [N]:          multipliers = [stepSize]
        // For 2D [N,M]:        multipliers = [M*stepSize, stepSize]
        // For 3D [X,Y,Z]:      multipliers = [Y*Z*stepSize, Z*stepSize, stepSize]
        var multipliers = ComputeMultipliers(occursLevels, stepSize);

        // Try all-constant fold: if every subscript is a literal, compute offset at compile time
        var subs = id.Subscripts!;
        bool allConstant = true;
        int effectiveOffset = baseLoc.Value.Offset;
        for (int i = 0; i < subs.Count && i < multipliers.Count; i++)
        {
            if (subs[i] is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                int val = (int)d;
                if (val < 1) return null;
                effectiveOffset += (val - 1) * multipliers[i];
            }
            else
            {
                allConstant = false;
                break;
            }
        }

        if (allConstant)
        {
            return new IrStaticLocation(
                new StorageLocation(baseLoc.Value.Area, effectiveOffset, leafSize, elementPic));
        }

        // Variable/expression subscripts → IrElementRef for runtime offset computation.
        // Subscripts are carried as BoundExpressions — the emitter evaluates each one
        // via EmitExpression, which handles identifiers, arithmetic, and any expression.
        return new IrElementRef(baseLoc.Value, subs, multipliers, leafSize, elementPic);
    }

    /// <summary>
    /// Compute row/plane/element multipliers for multi-dimensional OCCURS.
    /// multiplier[i] = product of all OCCURS counts at dimensions > i, times elementSize.
    /// </summary>
    /// <summary>
    /// Compute per-dimension multipliers for multi-dimensional OCCURS.
    /// Each multiplier is the ElementSize of the OCCURS group at that level —
    /// this correctly accounts for non-OCCURS siblings (e.g., ENTRY-1 alongside
    /// GRP2-ENTRY OCCURS 10) that occupy space within each occurrence.
    /// </summary>
    private static List<int> ComputeMultipliers(
        List<(Semantics.DataSymbol sym, int count)> occursLevels, int elementSize)
    {
        var multipliers = new List<int>(occursLevels.Count);
        for (int i = 0; i < occursLevels.Count; i++)
            multipliers.Add(occursLevels[i].sym.ElementSize);
        return multipliers;
    }

    /// <summary>
    /// Resolve a DataSymbol (non-subscriptable reference) to an IrLocation.
    /// Used for record buffers, file status variables, INITIALIZE items,
    /// PERFORM VARYING index, and condition parents — cases where subscripts
    /// are structurally impossible.
    /// </summary>
    private IrLocation? ResolveLocation(DataSymbol sym)
    {
        var loc = _semantic.GetStorageLocation(sym);
        if (!loc.HasValue) return null;
        return new IrStaticLocation(loc.Value);
    }

    /// <summary>
    /// Resolve any data-reference BoundExpression to an IrLocation.
    /// Handles BoundIdentifierExpression (with subscripts) and
    /// BoundReferenceModificationExpression (subscripts + substring).
    /// This is the single entry point for all lowering methods that need a location.
    /// </summary>
    private IrLocation? ResolveExpressionLocation(BoundExpression expr)
    {
        return expr switch
        {
            BoundIdentifierExpression id => ResolveLocation(id),
            BoundReferenceModificationExpression refMod => ResolveRefModLocation(refMod),
            _ => null
        };
    }

    private IrLocation? ResolveRefModLocation(BoundReferenceModificationExpression refMod)
    {
        var baseLoc = ResolveLocation(refMod.Base);
        if (baseLoc == null) return null;

        int baseLen = baseLoc switch
        {
            IrStaticLocation s => s.Location.Length,
            IrElementRef e => e.ElementSize,
            _ => 0
        };

        return new IrRefModLocation(baseLoc, refMod.Start, refMod.Length, baseLen);
    }

    /// <summary>
    /// Get the PicDescriptor for an IrLocation (static or element).
    /// </summary>
    private static Runtime.PicDescriptor GetPicForLocation(IrLocation loc)
    {
        return loc switch
        {
            IrStaticLocation s => s.Location.Pic,
            IrElementRef e => e.ElementPic,
            IrRefModLocation r => GetPicForLocation(r.Base),
            _ => throw new InvalidOperationException($"Unknown IrLocation type: {loc.GetType().Name}")
        };
    }

    /// <summary>
    /// Format a numeric literal for MOVE to alphanumeric field.
    /// Per ISO §14.19.4: the literal is treated as an unsigned integer field
    /// whose size is the number of digits specified in the literal.
    /// </summary>
    private static string FormatLiteralForAlphanumeric(string originalText)
    {
        // Strip sign and decimal point, keep all digits (preserves leading zeros)
        var sb = new System.Text.StringBuilder(originalText.Length);
        foreach (char c in originalText)
        {
            if (char.IsDigit(c))
                sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "0";
    }

    private static string FormatLiteralForAlphanumeric(decimal d)
    {
        decimal abs = Math.Abs(d);
        string raw = abs.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return raw.Replace(".", "");
    }

    /// <summary>
    /// Walk a BoundExpression tree and pre-resolve all data-reference leaf nodes
    /// (identifiers, ref-mod) to IrLocations. Used when embedding BoundExpression
    /// trees inside IR instructions (IrComputeStore) so the emitter never needs
    /// to resolve locations itself.
    /// </summary>
    private Dictionary<BoundExpression, IrLocation> PreResolveExpressionLocations(BoundExpression expr)
    {
        var result = new Dictionary<BoundExpression, IrLocation>();
        WalkExpressionForLocations(expr, result);
        return result;
    }

    private void WalkExpressionForLocations(BoundExpression expr, Dictionary<BoundExpression, IrLocation> result)
    {
        switch (expr)
        {
            case BoundIdentifierExpression:
            case BoundReferenceModificationExpression:
                var loc = ResolveExpressionLocation(expr);
                if (loc != null)
                    result[expr] = loc;
                break;

            case BoundBinaryExpression bin:
                WalkExpressionForLocations(bin.Left, result);
                WalkExpressionForLocations(bin.Right, result);
                break;

            // Literals, figuratives, etc. — no location to resolve
        }
    }

    // ── DISPLAY ──

    private void LowerDisplay(BoundDisplayStatement disp, IrBasicBlock block)
    {
        var operands = new List<IR.DisplayOperand>();
        foreach (var op in disp.Operands)
        {
            if (op is BoundFigurativeExpression fig)
            {
                // In DISPLAY context, figuratives display a single character
                string figStr = ((Runtime.FigurativeKind)fig.FigurativeKind) switch
                {
                    Runtime.FigurativeKind.Space => " ",
                    Runtime.FigurativeKind.Zero => "0",
                    Runtime.FigurativeKind.HighValue => "\xFF",
                    Runtime.FigurativeKind.LowValue => "\x00",
                    Runtime.FigurativeKind.Quote => "\"",
                    _ => fig.AllLiteral ?? " "
                };
                operands.Add(new IR.DisplayLiteralOperand(figStr));
            }
            else if (op is BoundLiteralExpression lit && lit.Value is string s)
            {
                operands.Add(new IR.DisplayLiteralOperand(s));
            }
            else if (op is BoundLiteralExpression numLit && numLit.Value is decimal d)
            {
                operands.Add(new IR.DisplayLiteralOperand(
                    d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            else if (op is BoundIdentifierExpression or BoundReferenceModificationExpression)
            {
                var loc = ResolveExpressionLocation(op);
                if (loc != null)
                    operands.Add(new IR.DisplayFieldOperand(loc));
                else if (op is BoundIdentifierExpression failedId)
                    operands.Add(new IR.DisplayLiteralOperand($"[{failedId.Symbol.Name}]"));
            }
            else
            {
                operands.Add(new IR.DisplayLiteralOperand(op.ToString() ?? ""));
            }
        }

        block.Instructions.Add(new IR.IrPicDisplay(operands));
    }

    // ── CORRESPONDING (MOVE, ADD, SUBTRACT) ──

    private IrBasicBlock LowerCorresponding(BoundCorrespondingStatement corr, IrMethod method, IrBasicBlock block)
    {
        if (corr.CorrespondingKind == CorrespondingKind.Move)
        {
            // MOVE CORRESPONDING: PIC-aware field-to-field move per pair
            foreach (var (src, dst) in corr.Pairs)
            {
                var srcLoc = ResolveLocation(src);
                var dstLoc = ResolveLocation(dst);
                if (srcLoc == null || dstLoc == null) continue;

                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dstLoc,
                    GetPicForLocation(srcLoc), GetPicForLocation(dstLoc)));
            }
            return block;
        }

        // ADD/SUBTRACT CORRESPONDING: accumulator pattern per pair,
        // consistent with scalar ADD/SUBTRACT lowering.
        block.Instructions.Add(new IrInitArithmeticStatus());
        int rounding = corr.IsRounded ? 1 : 0;

        foreach (var (src, dst) in corr.Pairs)
        {
            var srcLoc = ResolveLocation(src);
            var dstLoc = ResolveLocation(dst);
            if (srcLoc == null || dstLoc == null) continue;

            var accum = _valueFactory.Next(IrPrimitiveType.Decimal);
            block.Instructions.Add(new IrInitAccumulator(accum));
            block.Instructions.Add(new IrAccumulateField(accum, srcLoc));

            if (corr.CorrespondingKind == CorrespondingKind.Add)
                block.Instructions.Add(new IrAddAccumulatedToTarget(accum, dstLoc, rounding));
            else
                block.Instructions.Add(new IrSubtractAccumulatedFromTarget(accum, dstLoc, rounding));
        }

        return LowerSizeError(corr.SizeError, method, block);
    }

    // ── MOVE ──

    private void LowerMove(BoundMoveStatement mv, IrBasicBlock block)
    {
        foreach (var t in mv.Targets)
        {
            var destLoc = ResolveExpressionLocation(t);
            if (destLoc == null) continue;

            // Source dispatch: figurative, literal, or data reference
            if (mv.Source is BoundFigurativeExpression fig)
            {
                if (fig.AllLiteral != null)
                    block.Instructions.Add(new IrMoveAllLiteral(destLoc, fig.AllLiteral));
                else
                    block.Instructions.Add(new IrMoveFigurative(destLoc, fig.FigurativeKind));
            }
            else if (mv.Source is BoundLiteralExpression lit)
            {
                var destPic = GetPicForLocation(destLoc);
                if (lit.Value is string s)
                {
                    if (destPic.Category == Runtime.CobolCategory.NumericEdited)
                    {
                        // String literal to numeric-edited: parse as numeric, format with edit pattern
                        if (decimal.TryParse(s, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var numVal))
                            block.Instructions.Add(new IrPicMoveLiteralNumeric(destLoc, numVal));
                        else
                            block.Instructions.Add(new IrMoveStringToField(destLoc, s));
                    }
                    else
                    {
                        // AlphanumericEdited: CilEmitter handles edit pattern dispatch
                        block.Instructions.Add(new IrMoveStringToField(destLoc, s));
                    }
                }
                else if (lit.Value is decimal d)
                {
                    if (destPic.Category.IsNumericLike())
                        block.Instructions.Add(new IrPicMoveLiteralNumeric(
                            destLoc, d, mv.IsRounded ? 1 : 0));
                    else
                    {
                        // Numeric literal to alphanumeric: use display representation.
                        // Per ISO §14.19.4: "the literal is treated as if it were an
                        // unsigned integer field whose size is the number of digits
                        // specified in the literal." OriginalText preserves leading zeros.
                        string display = lit.OriginalText != null
                            ? FormatLiteralForAlphanumeric(lit.OriginalText)
                            : FormatLiteralForAlphanumeric(d);
                        block.Instructions.Add(new IrMoveStringToField(destLoc, display));
                    }
                }
            }
            else
            {
                // Data reference source (identifier, ref mod, or any expression with a location)
                var srcLoc = ResolveExpressionLocation(mv.Source);
                if (srcLoc != null)
                {
                    block.Instructions.Add(new IrMoveFieldToField(
                        srcLoc, destLoc,
                        GetPicForLocation(srcLoc), GetPicForLocation(destLoc),
                        mv.IsRounded));
                }
            }
        }
    }

    // ── PERFORM ──

    private IrBasicBlock LowerPerform(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // PERFORM VARYING: recursive loop structure with optional AFTER nesting
        if (perf.Varying != null)
        {
            // The outermost loop's end block is the EXIT PERFORM target
            var varyEnd = method.CreateBlock("vary.exit");
            _performExitStack.Push(varyEnd);
            var result = LowerPerformVarying(perf.Varying, perf, method, block);
            // The recursive lowering returns the outermost loopEnd block,
            // but EXIT PERFORM jumps to varyEnd. Wire them together.
            result.Instructions.Add(new IrJump(varyEnd));
            method.Blocks.Add(varyEnd);
            _performExitStack.Pop();
            return varyEnd;
        }

        // PERFORM para N TIMES: expression-based loop count
        if (perf.TimesExpression != null)
        {
            return LowerPerformTimes(perf, method, block);
        }

        // PERFORM UNTIL (no VARYING): simple condition loop
        if (perf.UntilCondition != null)
        {
            var loopStart = method.CreateBlock("perf.until.start");
            var loopBody = method.CreateBlock("perf.until.body");
            var loopEnd = method.CreateBlock("perf.until.end");

            _performExitStack.Push(loopEnd);

            block.Instructions.Add(new IrJump(loopStart));

            method.Blocks.Add(loopStart);
            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(perf.UntilCondition, condVal, loopStart);
            loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
            loopStart.Instructions.Add(new IrJump(loopEnd));

            method.Blocks.Add(loopBody);
            var bodyCurrent = LowerPerformBody(perf, method, loopBody);
            bodyCurrent.Instructions.Add(new IrJump(loopStart));

            method.Blocks.Add(loopEnd);
            _performExitStack.Pop();
            return loopEnd;
        }

        // Inline PERFORM (no options, no target): execute block once
        if (perf.InlineStatements is { Count: > 0 })
        {
            var current = block;
            foreach (var stmt in perf.InlineStatements)
                current = LowerStatement(stmt, method, current);
            return current;
        }

        // Simple PERFORM (no UNTIL/VARYING): just call paragraphs
        LowerPerformSimple(perf, block);
        return block;
    }

    /// <summary>
    /// Recursive VARYING lowering. For each level:
    ///   1. Initialize index from FROM value
    ///   2. Loop: test UNTIL; if true exit; execute body (or inner VARYING); increment; repeat
    /// AFTER clauses become nested inner levels via v.Next.
    /// </summary>
    private IrBasicBlock LowerPerformVarying(BoundPerformVarying v, BoundPerformStatement perf,
        IrMethod method, IrBasicBlock block)
    {
        var indexLoc = ResolveLocation(v.Index);
        if (indexLoc == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0500, s_noLocation, s_noSpan, v.Index.Name);
            return block;
        }

        // 1. Initialize: MOVE initial TO index
        EmitVaryingMove(v.Initial, indexLoc, block);

        // 2. Loop structure
        var loopStart = method.CreateBlock("vary.start");
        var loopBody = method.CreateBlock("vary.body");
        var loopEnd = method.CreateBlock("vary.end");

        block.Instructions.Add(new IrJump(loopStart));

        // Test UNTIL (top-tested)
        method.Blocks.Add(loopStart);
        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        LowerCondition(v.UntilCondition, condVal, loopStart);
        loopStart.Instructions.Add(new IrBranchIfFalse(condVal, loopBody));
        loopStart.Instructions.Add(new IrJump(loopEnd));

        // Body
        method.Blocks.Add(loopBody);
        IrBasicBlock bodyCurrent;

        if (v.Next != null)
        {
            // Nested AFTER: recurse into inner level
            bodyCurrent = LowerPerformVarying(v.Next, perf, method, loopBody);
        }
        else
        {
            // Innermost level: execute the actual body
            bodyCurrent = LowerPerformBody(perf, method, loopBody);
        }

        // Increment: ADD step TO index
        EmitVaryingAdd(v.Step, indexLoc, bodyCurrent);

        bodyCurrent.Instructions.Add(new IrJump(loopStart));

        method.Blocks.Add(loopEnd);
        return loopEnd;
    }

    private void EmitVaryingMove(BoundExpression source, IrLocation dest, IrBasicBlock block)
    {
        if (source is BoundLiteralExpression lit && lit.Value is decimal d)
        {
            block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, d, 0));
        }
        else if (source is BoundIdentifierExpression id)
        {
            var srcLoc = ResolveLocation(id);
            if (srcLoc != null)
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dest,
                    GetPicForLocation(srcLoc), GetPicForLocation(dest)));
        }
    }

    private void EmitVaryingAdd(BoundExpression step, IrLocation indexLoc, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());
        if (step is BoundLiteralExpression litStep && litStep.Value is decimal dStep)
        {
            block.Instructions.Add(new IrPicAddLiteral(indexLoc, dStep, 0));
        }
        else if (step is BoundIdentifierExpression idStep)
        {
            var stepLoc = ResolveLocation(idStep);
            if (stepLoc != null)
                block.Instructions.Add(new IrPicAdd(stepLoc, indexLoc, 0));
        }
    }

    private IrBasicBlock LowerPerformBody(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Inline statements
        if (perf.InlineStatements is { Count: > 0 })
        {
            var current = block;
            foreach (var stmt in perf.InlineStatements)
                current = LowerStatement(stmt, method, current);
            return current;
        }

        // Target paragraph(s)
        if (perf.Target != null)
        {
            LowerPerformSimple(perf, block);
        }
        return block;
    }

    private void LowerPerformSimple(BoundPerformStatement perf, IrBasicBlock block)
    {
        if (perf.Target == null) return;
        if (!_paragraphIndices.TryGetValue(perf.Target.Name, out int startIdx))
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0501, s_noLocation, s_noSpan, perf.Target.Name);
            return;
        }

        int endIdx = startIdx;
        if (perf.ThruTarget != null &&
            _paragraphIndices.TryGetValue(perf.ThruTarget.Name, out int thruIdx))
            endIdx = thruIdx;

        // Ensure valid range (section THRU may produce reversed indices)
        if (endIdx < startIdx)
            (startIdx, endIdx) = (endIdx, startIdx);

        if (startIdx == endIdx)
        {
            var paraName = _paragraphsByIndex[startIdx];
            if (_paragraphMethods.TryGetValue(paraName, out var paraMethod))
                block.Instructions.Add(new IrPerform(paraMethod));
        }
        else
        {
            var methods = new List<IrMethod>();
            for (int i = startIdx; i <= endIdx; i++)
            {
                var paraName = _paragraphsByIndex[i];
                if (_paragraphMethods.TryGetValue(paraName, out var paraMethod))
                    methods.Add(paraMethod);
                else
                    methods.Add(null!);
            }
            block.Instructions.Add(new IrPerformThru(startIdx, endIdx, methods));
        }
    }

    /// <summary>
    /// PERFORM para N TIMES: runtime expression-based loop.
    /// Evaluates the TIMES expression, loops that many iterations.
    /// </summary>
    private IrBasicBlock LowerPerformTimes(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Inline PERFORM N TIMES: loop over inline statements
        if (perf.Target == null && perf.InlineStatements is { Count: > 0 })
        {
            return LowerInlinePerformTimes(perf, method, block);
        }

        if (perf.Target == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0502, s_noLocation, s_noSpan);
            return block;
        }
        if (!_paragraphIndices.TryGetValue(perf.Target.Name, out int startIdx))
            return block;

        int endIdx = startIdx;
        if (perf.ThruTarget != null &&
            _paragraphIndices.TryGetValue(perf.ThruTarget.Name, out int thruIdx))
            endIdx = thruIdx;

        // Evaluate TIMES expression to a counter and loop
        // Lower as: counter = TIMES expr; while counter > 0: body; counter--
        var loopStart = method.CreateBlock("perf.times.start");
        var loopBody = method.CreateBlock("perf.times.body");
        var loopEnd = method.CreateBlock("perf.times.end");

        _performExitStack.Push(loopEnd);

        // Emit a single IrPerformTimes instruction. The emitter manages the
        // CIL loop counter as a local int, evaluates CountExpression once at entry,
        // and calls the paragraph method(s) in a loop.
        var methods = new List<IrMethod>();
        IrMethod? singleMethod = null;
        if (startIdx == endIdx)
        {
            var paraName = _paragraphsByIndex[startIdx];
            _paragraphMethods.TryGetValue(paraName, out singleMethod);
        }
        else
        {
            for (int i = startIdx; i <= endIdx; i++)
            {
                var pn = _paragraphsByIndex[i];
                _paragraphMethods.TryGetValue(pn, out var pm);
                methods.Add(pm!);
            }
        }

        var resolvedLocs = PreResolveExpressionLocations(perf.TimesExpression!);
        block.Instructions.Add(new IrPerformTimes(
            singleMethod ?? methods[0], startIdx, endIdx, methods,
            perf.TimesExpression!, resolvedLocs));

        _performExitStack.Pop();
        return block;
    }

    /// <summary>
    /// Inline PERFORM N TIMES: loop over inline statements N times.
    /// Uses PERFORM UNTIL pattern with a synthetic counter condition.
    /// </summary>
    /// <summary>
    /// Inline PERFORM N TIMES: emits IrPerformInlineTimes.
    /// The emitter manages a CIL-local int counter, evaluates CountExpression
    /// once at entry, then loops over the lowered body instructions.
    /// Works for both literal and identifier count expressions.
    /// </summary>
    private IrBasicBlock LowerInlinePerformTimes(BoundPerformStatement perf, IrMethod method, IrBasicBlock block)
    {
        // Lower inline body into a temporary block to collect its IR instructions
        var tempBlock = new IrBasicBlock("perf.inline.times.temp");
        var bodyCurrent = tempBlock;
        foreach (var stmt in perf.InlineStatements!)
            bodyCurrent = LowerStatement(stmt, method, bodyCurrent);

        // Collect body instructions from tempBlock (single block for simple bodies)
        var bodyInstructions = new List<IrInstruction>(tempBlock.Instructions);

        // Pre-resolve data references in the count expression
        var resolvedLocs = PreResolveExpressionLocations(perf.TimesExpression!);

        // Emit IrPerformInlineTimes — the emitter:
        //   1. Evaluates CountExpression → CIL local int counter
        //   2. Loops: while counter > 0, execute body, counter--
        block.Instructions.Add(new IrPerformInlineTimes(
            perf.TimesExpression!, bodyInstructions, resolvedLocs));

        return block;
    }

    // ── WRITE ──

    private void LowerWrite(BoundWriteStatement wr, IrBasicBlock block)
    {
        string fileName = wr.File?.Name ?? "PRINT-FILE";

        // Try to get storage location for the record
        var recordLoc = ResolveLocation(wr.Record);
        if (recordLoc != null)
        {
            // WRITE FROM: MOVE source TO record before writing
            if (wr.From != null)
            {
                var fromLoc = ResolveExpressionLocation(wr.From);
                if (fromLoc != null)
                    block.Instructions.Add(new IrMoveFieldToField(
                        fromLoc, recordLoc,
                        GetPicForLocation(fromLoc), GetPicForLocation(recordLoc)));
            }

            if (wr.AdvancingLines.HasValue)
            {
                block.Instructions.Add(new IrWriteAdvancing(
                    fileName, recordLoc, wr.AdvancingLines.Value, !wr.IsAfterAdvancing));
            }
            else
            {
                block.Instructions.Add(new IrWriteRecordFromStorage(fileName, recordLoc));
            }
        }
        else
        {
            // Fallback: write placeholder via WriteText
            var fileNameVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fileNameVal, fileName));
            var textVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(textVal, $"[RECORD: {wr.Record.Name}]"));
            block.Instructions.Add(new IrRuntimeCall(
                null, "CobolRuntime.WriteText",
                new[] { fileNameVal, textVal }));
        }

        // Update FILE STATUS if declared
        if (wr.File != null)
            EmitFileStatus(wr.File, block);
    }

    // ── OPEN ──

    private void LowerOpen(BoundOpenStatement open, IrBasicBlock block)
    {
        string runtimeMethod = open.Mode switch
        {
            OpenMode.Input => "FileRuntime.OpenInput",
            OpenMode.Output => "FileRuntime.OpenOutput",
            OpenMode.IO => "FileRuntime.OpenIO",
            OpenMode.Extend => "FileRuntime.OpenExtend",
            _ => "FileRuntime.OpenOutput"
        };

        foreach (var file in open.Files)
        {
            string cobolName = file.Name;
            var fnVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fnVal, cobolName));
            block.Instructions.Add(new IrRuntimeCall(null, runtimeMethod, new[] { fnVal }));

            // Update FILE STATUS if declared
            EmitFileStatus(file, block);
        }
    }

    // ── CLOSE ──

    private void LowerClose(BoundCloseStatement close, IrBasicBlock block)
    {
        foreach (var file in close.Files)
        {
            string cobolName = file.Name;
            var fnVal = _valueFactory.Next(IrPrimitiveType.String);
            block.Instructions.Add(new IrLoadConst(fnVal, cobolName));
            block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.CloseFile", new[] { fnVal }));

            EmitFileStatus(file, block);
        }
    }

    // ── READ ──

    private IrBasicBlock LowerRead(BoundReadStatement read, IrMethod method, IrBasicBlock block)
    {
        string cobolName = read.File.Name;

        // Read into the FD record buffer
        var recordSym = read.File.Record;
        if (recordSym != null)
        {
            var recordLoc = ResolveLocation(recordSym);
            if (recordLoc != null)
            {
                // Keyed read: RANDOM/DYNAMIC access without NEXT → ReadByKey
                bool isKeyedRead = !read.IsNext &&
                    read.File.AccessMode is "RANDOM" or "DYNAMIC" &&
                    read.File.RecordKey != null;

                if (isKeyedRead)
                {
                    var keySym = _semantic.ResolveData(read.File.RecordKey!);
                    var keyLoc = keySym != null ? ResolveLocation(keySym) : null;
                    if (keyLoc != null)
                        block.Instructions.Add(new IrReadByKey(cobolName, recordLoc, keyLoc));
                    else
                        block.Instructions.Add(new IrReadRecordToStorage(cobolName, recordLoc));
                }
                else
                {
                    block.Instructions.Add(new IrReadRecordToStorage(cobolName, recordLoc));
                }
            }
        }

        // Update FILE STATUS
        EmitFileStatus(read.File, block);

        // If INTO specified, MOVE FD record to INTO target
        if (read.Into != null && recordSym != null)
        {
            var srcLoc = ResolveLocation(recordSym);
            var dstLoc = ResolveLocation(read.Into);
            if (srcLoc != null && dstLoc != null)
            {
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dstLoc,
                    GetPicForLocation(srcLoc), GetPicForLocation(dstLoc)));
            }
        }

        // AT END / NOT AT END branching
        if (read.AtEnd.Count > 0 || read.NotAtEnd.Count > 0)
        {
            var atEndResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileAtEnd(cobolName, atEndResult));

            var atEndBlock = method.CreateBlock("read_at_end");
            var notAtEndBlock = method.CreateBlock("read_not_at_end");
            var afterBlock = method.CreateBlock("read_after");

            block.Instructions.Add(new IrBranchIfFalse(atEndResult, notAtEndBlock));
            block.Instructions.Add(new IrJump(atEndBlock));

            // AT END
            method.Blocks.Add(atEndBlock);
            var current = atEndBlock;
            foreach (var stmt in read.AtEnd)
                current = LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            // NOT AT END
            method.Blocks.Add(notAtEndBlock);
            current = notAtEndBlock;
            foreach (var stmt in read.NotAtEnd)
                current = LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    /// <summary>
    /// Emit FILE STATUS update: after each file operation, store the 2-char
    /// status code into the FILE STATUS variable if one was declared.
    /// </summary>
    private void EmitFileStatus(FileSymbol file, IrBasicBlock block)
    {
        if (file.FileStatus == null) return;

        var statusSym = _semantic.ResolveData(file.FileStatus);
        if (statusSym == null) return;

        var statusLoc = ResolveLocation(statusSym);
        if (statusLoc == null) return;

        block.Instructions.Add(new IrStoreFileStatus(file.Name, statusLoc));
    }

    // ── REWRITE ──

    private void LowerRewrite(BoundRewriteStatement rw, IrBasicBlock block)
    {
        string cobolName = rw.File.Name;
        var recordLoc = ResolveLocation(rw.Record);
        if (recordLoc != null)
        {
            // REWRITE FROM: MOVE source TO record before rewriting
            if (rw.From != null)
            {
                var fromLoc = ResolveExpressionLocation(rw.From);
                if (fromLoc != null)
                    block.Instructions.Add(new IrMoveFieldToField(
                        fromLoc, recordLoc,
                        GetPicForLocation(fromLoc), GetPicForLocation(recordLoc)));
            }

            block.Instructions.Add(new IrRewriteRecordFromStorage(cobolName, recordLoc));
        }
        EmitFileStatus(rw.File, block);
    }

    // ── DELETE ──

    private IrBasicBlock LowerDelete(BoundDeleteStatement del, IrMethod method, IrBasicBlock block)
    {
        string cobolName = del.File.Name;
        block.Instructions.Add(new IrDeleteRecord(cobolName));
        EmitFileStatus(del.File, block);

        // INVALID KEY / NOT INVALID KEY branching
        if (del.InvalidKey.Count > 0 || del.NotInvalidKey.Count > 0)
        {
            var invalidResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));

            var invalidBlock = method.CreateBlock("delete.invalid");
            var notInvalidBlock = method.CreateBlock("delete.not.invalid");
            var afterBlock = method.CreateBlock("delete.after");

            block.Instructions.Add(new IrBranchIfFalse(invalidResult, notInvalidBlock));
            block.Instructions.Add(new IrJump(invalidBlock));

            method.Blocks.Add(invalidBlock);
            var ikCurrent = invalidBlock;
            foreach (var stmt in del.InvalidKey)
                ikCurrent = LowerStatement(stmt, method, ikCurrent);
            ikCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(notInvalidBlock);
            var nikCurrent = notInvalidBlock;
            foreach (var stmt in del.NotInvalidKey)
                nikCurrent = LowerStatement(stmt, method, nikCurrent);
            nikCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    // ── START ──

    private IrBasicBlock LowerStart(BoundStartStatement start, IrMethod method, IrBasicBlock block)
    {
        string cobolName = start.File.Name;

        // Resolve key from the file's RECORD KEY
        var recordKey = start.File.RecordKey;
        if (recordKey != null)
        {
            var keySym = _semantic.ResolveData(recordKey);
            if (keySym != null)
            {
                var keyLoc = ResolveLocation(keySym);
                if (keyLoc != null)
                {
                    // Extract key condition from bound tree (default: Equal)
                    int condition = 0; // StartCondition.Equal
                    if (start.KeyCondition is BoundBinaryExpression keyExpr)
                    {
                        condition = keyExpr.OperatorKind switch
                        {
                            BoundBinaryOperatorKind.Equal => 0,
                            BoundBinaryOperatorKind.Greater => 1,
                            BoundBinaryOperatorKind.GreaterOrEqual => 2,
                            BoundBinaryOperatorKind.Less => 3,
                            BoundBinaryOperatorKind.LessOrEqual => 4,
                            _ => 0
                        };
                    }
                    block.Instructions.Add(new IrStartFile(cobolName, keyLoc, condition));
                }
            }
        }

        EmitFileStatus(start.File, block);

        // INVALID KEY / NOT INVALID KEY branching
        if (start.InvalidKey.Count > 0 || start.NotInvalidKey.Count > 0)
        {
            var invalidResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckFileInvalidKey(cobolName, invalidResult));

            var invalidBlock = method.CreateBlock("start.invalid");
            var notInvalidBlock = method.CreateBlock("start.not.invalid");
            var afterBlock = method.CreateBlock("start.after");

            block.Instructions.Add(new IrBranchIfFalse(invalidResult, notInvalidBlock));
            block.Instructions.Add(new IrJump(invalidBlock));

            method.Blocks.Add(invalidBlock);
            var ikCurrent = invalidBlock;
            foreach (var stmt in start.InvalidKey)
                ikCurrent = LowerStatement(stmt, method, ikCurrent);
            ikCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(notInvalidBlock);
            var nikCurrent = notInvalidBlock;
            foreach (var stmt in start.NotInvalidKey)
                nikCurrent = LowerStatement(stmt, method, nikCurrent);
            nikCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    // ── RETURN (sort/merge) ──

    private IrBasicBlock LowerReturn(BoundReturnStatement ret, IrMethod method, IrBasicBlock block)
    {
        // RETURN is for sort/merge, not yet supported — emit stub warning
        block.Instructions.Add(new IR.IrPicDisplay(
            [new IR.DisplayLiteralOperand($"RETURN not implemented: {ret.File.Name}")]));

        // Lower AT END / NOT AT END for structural completeness
        if (ret.AtEnd.Count > 0 || ret.NotAtEnd.Count > 0)
        {
            // Always take the AT END path (stub behavior)
            foreach (var stmt in ret.AtEnd)
                block = LowerStatement(stmt, method, block);
        }

        return block;
    }

    // ── CALL ──

    private IrBasicBlock LowerCall(BoundCallStatement call, IrMethod method, IrBasicBlock block)
    {
        // Build argument list
        var args = new List<IrCallArgument>();
        foreach (var arg in call.Arguments)
        {
            var loc = ResolveExpressionLocation(arg.Expression);
            if (loc != null)
            {
                int mode = arg.Mode switch
                {
                    ParameterMode.ByReference => 0,
                    ParameterMode.ByContent => 1,
                    ParameterMode.ByValue => 2,
                    _ => 0
                };
                args.Add(new IrCallArgument(mode, loc));
            }
        }

        // Resolve RETURNING target
        IrLocation? returningLoc = null;
        if (call.ReturningTarget != null)
            returningLoc = ResolveLocation(call.ReturningTarget);

        // Emit the inter-program CALL
        block.Instructions.Add(new IrCallProgram(
            call.TargetName, call.IsDynamic, args, returningLoc));

        // ON EXCEPTION / NOT ON EXCEPTION branching
        if (call.OnException.Count > 0 || call.NotOnException.Count > 0)
        {
            // The call result (success/fail) is checked by the CIL emitter
            // which branches based on the return value of the Entry method.
            var excBlock = method.CreateBlock("call_exception");
            var notExcBlock = method.CreateBlock("call_not_exception");
            var afterBlock = method.CreateBlock("call_after");

            // Branch on call result (emitter checks last call status)
            var callResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckCallException(call.TargetName, callResult));
            block.Instructions.Add(new IrBranchIfFalse(callResult, notExcBlock));
            block.Instructions.Add(new IrJump(excBlock));

            // ON EXCEPTION
            method.Blocks.Add(excBlock);
            var current = excBlock;
            foreach (var stmt in call.OnException)
                current = LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            // NOT ON EXCEPTION
            method.Blocks.Add(notExcBlock);
            current = notExcBlock;
            foreach (var stmt in call.NotOnException)
                current = LowerStatement(stmt, method, current);
            current.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    // ── INITIALIZE ──

    private void LowerInitialize(BoundInitializeStatement stmt, IrBasicBlock block)
    {
        foreach (var target in stmt.Targets)
            InitializeDataItem(target, stmt, block);
    }

    private void InitializeDataItem(DataSymbol item, BoundInitializeStatement stmt, IrBasicBlock block)
    {
        if (item.IsGroup)
        {
            // Recurse into children (skipping REDEFINES items — they share storage)
            foreach (var child in item.Children)
            {
                if (child.Redefines != null) continue;
                InitializeDataItem(child, stmt, block);
            }
            return;
        }

        // Elementary item: check for category replacement, then default
        var loc = ResolveLocation(item);
        if (loc == null) return;

        var pic = GetPicForLocation(loc);
        var category = ClassifyInitializeCategory(pic.Category);

        // Check for matching category replacement
        foreach (var repl in stmt.CategoryReplacements)
        {
            if (repl.Category == category)
            {
                EmitInitializeAssignment(loc, repl.Value, block);
                return;
            }
        }

        // Default: numeric → zero, everything else → spaces
        if (category == InitializeCategory.Numeric || category == InitializeCategory.NumericEdited)
        {
            block.Instructions.Add(new IrPicMoveLiteralNumeric(loc, 0m));
        }
        else
        {
            block.Instructions.Add(new IrMoveFigurative(loc, FigurativeKind.Space));
        }
    }

    private InitializeCategory ClassifyInitializeCategory(CobolCategory cat)
    {
        return cat switch
        {
            CobolCategory.Numeric => InitializeCategory.Numeric,
            CobolCategory.NumericEdited => InitializeCategory.NumericEdited,
            CobolCategory.AlphanumericEdited => InitializeCategory.AlphanumericEdited,
            _ => InitializeCategory.Alphanumeric
        };
    }

    private void EmitInitializeAssignment(IrLocation dest, BoundExpression value, IrBasicBlock block)
    {
        var pic = GetPicForLocation(dest);
        if (value is BoundLiteralExpression lit)
        {
            if (lit.Value is decimal d)
            {
                if (pic.Category.IsNumericLike())
                    block.Instructions.Add(new IrPicMoveLiteralNumeric(dest, d));
                else
                    block.Instructions.Add(new IrMoveStringToField(dest,
                        d.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            else if (lit.Value is string s)
            {
                block.Instructions.Add(new IrMoveStringToField(dest, s));
            }
        }
        else if (value is BoundIdentifierExpression id)
        {
            var srcLoc = ResolveLocation(id);
            if (srcLoc != null)
                block.Instructions.Add(new IrMoveFieldToField(
                    srcLoc, dest,
                    GetPicForLocation(srcLoc), GetPicForLocation(dest)));
        }
    }

    // ── ACCEPT ──

    private void LowerAccept(BoundAcceptStatement stmt, IrBasicBlock block)
    {
        var loc = ResolveLocation(stmt.Target);
        if (loc == null) return;
        block.Instructions.Add(new IrAccept(loc, stmt.Source));
    }

    // ── INSPECT ──

    private void LowerInspect(BoundInspectStatement stmt, IrBasicBlock block)
    {
        var targetLoc = ResolveLocation(stmt.Target);
        if (targetLoc == null) return;

        foreach (var t in stmt.Tallying)
        {
            var counterLoc = ResolveLocation(t.Counter);
            if (counterLoc == null) continue;

            block.Instructions.Add(new IrInspectTally(
                targetLoc, counterLoc, t.Kind,
                LowerInspectPattern(t.Pattern),
                LowerInspectPattern(t.Region.BeforePattern), t.Region.BeforeInitial,
                LowerInspectPattern(t.Region.AfterPattern), t.Region.AfterInitial));
        }

        foreach (var r in stmt.Replacing)
        {
            block.Instructions.Add(new IrInspectReplace(
                targetLoc, r.Kind,
                LowerInspectPattern(r.Pattern)!,
                LowerInspectPattern(r.Replacement)!,
                LowerInspectPattern(r.Region.BeforePattern), r.Region.BeforeInitial,
                LowerInspectPattern(r.Region.AfterPattern), r.Region.AfterInitial));
        }

        if (stmt.Converting != null)
        {
            block.Instructions.Add(new IrInspectConvert(
                targetLoc,
                LowerInspectPattern(stmt.Converting.FromSet)!,
                LowerInspectPattern(stmt.Converting.ToSet)!,
                LowerInspectPattern(stmt.Converting.Region.BeforePattern),
                stmt.Converting.Region.BeforeInitial,
                LowerInspectPattern(stmt.Converting.Region.AfterPattern),
                stmt.Converting.Region.AfterInitial));
        }
    }

    /// <summary>
    /// Convert a bound InspectPatternValue to an IR IrInspectPatternValue.
    /// Literals pass through. Data refs are resolved to IrLocations.
    /// </summary>
    private IR.IrInspectPatternValue? LowerInspectPattern(InspectPatternValue? pv)
    {
        if (pv == null) return null;
        if (pv.IsLiteral) return IR.IrInspectPatternValue.FromLiteral(pv.Literal!);
        if (pv.IsDataRef)
        {
            var loc = ResolveExpressionLocation(pv.DataRef!);
            if (loc != null) return IR.IrInspectPatternValue.FromLocation(loc);
        }
        return IR.IrInspectPatternValue.FromLiteral("");
    }

    // ── SET ──

    private void LowerSetCondition(BoundSetConditionStatement stmt, IrBasicBlock block)
    {
        var parentSym = stmt.Condition.ParentDataItem;
        var parentLoc = ResolveLocation(parentSym);
        if (parentLoc == null) return;

        if (stmt.SetToTrue)
        {
            // SET condition TO TRUE: move the first defining value into the parent
            var ranges = stmt.Condition.ValueRanges;
            if (ranges.Count == 0) return;

            var firstVal = ranges[0].From;
            if (firstVal.IsNumeric)
                block.Instructions.Add(new IrPicMoveLiteralNumeric(parentLoc, firstVal.NumericValue!.Value));
            else if (firstVal.IsString)
                block.Instructions.Add(new IrMoveStringToField(parentLoc, firstVal.StringValue!));
        }
        else
        {
            // SET condition TO FALSE: move a value that doesn't match any true value
            var parentCat = GetPicForLocation(parentLoc).Category;
            if (parentCat.IsNumericLike())
            {
                var trueVals = stmt.Condition.ValueRanges.Select(r => r.From).ToList();
                decimal falseVal = 0m;
                foreach (var candidate in new[] { 0m, 1m, -1m, 99m })
                {
                    if (!trueVals.Any(v => v.IsNumeric && v.NumericValue == candidate))
                    {
                        falseVal = candidate;
                        break;
                    }
                }
                block.Instructions.Add(new IrPicMoveLiteralNumeric(parentLoc, falseVal));
            }
            else
            {
                block.Instructions.Add(new IrMoveFigurative(parentLoc, FigurativeKind.Space));
            }
        }
    }

    private void LowerSetIndex(BoundSetIndexStatement stmt, IrBasicBlock block)
    {
        var targetLoc = ResolveLocation(stmt.Target);
        if (targetLoc == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0510, s_noLocation, s_noSpan, stmt.Target.Symbol.Name);
            return;
        }

        // Resolve the value expression to either a decimal literal or a field location.
        // Handles literal, identifier, and computed expressions (e.g., unary -5, +5).
        decimal? literalValue = TryEvalConstant(stmt.Value);
        IrLocation? valueLoc = null;
        if (literalValue == null && stmt.Value is BoundIdentifierExpression valId)
            valueLoc = ResolveLocation(valId);

        switch (stmt.Operation)
        {
            case SetOperation.Assign:
                if (literalValue.HasValue)
                {
                    if (GetPicForLocation(targetLoc).Category.IsNumericLike())
                        block.Instructions.Add(new IrPicMoveLiteralNumeric(targetLoc, literalValue.Value));
                    else
                        block.Instructions.Add(new IrMoveStringToField(targetLoc,
                            literalValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                else if (valueLoc != null)
                {
                    block.Instructions.Add(new IrMoveFieldToField(
                        valueLoc, targetLoc,
                        GetPicForLocation(valueLoc), GetPicForLocation(targetLoc)));
                }
                else
                {
                    _diagnostics.Report(DiagnosticDescriptors.COBOL0511, s_noLocation, s_noSpan, stmt.Target.Symbol.Name, stmt.Value.GetType().Name);
                }
                break;

            case SetOperation.UpBy:
                if (literalValue.HasValue)
                    block.Instructions.Add(new IrPicAddLiteral(targetLoc, literalValue.Value));
                else if (valueLoc != null)
                    block.Instructions.Add(new IrPicAdd(valueLoc, targetLoc));
                else
                {
                    _diagnostics.Report(DiagnosticDescriptors.COBOL0512, s_noLocation, s_noSpan, stmt.Target.Symbol.Name, stmt.Value.GetType().Name);
                }
                break;

            case SetOperation.DownBy:
                if (literalValue.HasValue)
                    block.Instructions.Add(new IrPicSubtractLiteral(targetLoc, literalValue.Value));
                else if (valueLoc != null)
                    block.Instructions.Add(new IrPicSubtract(valueLoc, targetLoc));
                else
                {
                    _diagnostics.Report(DiagnosticDescriptors.COBOL0513, s_noLocation, s_noSpan, stmt.Target.Symbol.Name, stmt.Value.GetType().Name);
                }
                break;
        }
    }

    /// <summary>
    /// Try to evaluate a bound expression as a compile-time decimal constant.
    /// Handles literals, unary +/-, and simple constant arithmetic.
    /// Returns null if the expression is not a compile-time constant.
    /// </summary>
    private static decimal? TryEvalConstant(BoundExpression expr)
    {
        if (expr is BoundLiteralExpression lit && lit.Value is decimal d)
            return d;

        // Unary +/- on a literal: the binder produces BoundBinaryExpression(0, +/-, literal)
        // or the arithmetic parser produces a negated literal
        if (expr is BoundBinaryExpression bin)
        {
            var left = TryEvalConstant(bin.Left);
            var right = TryEvalConstant(bin.Right);
            if (left.HasValue && right.HasValue)
            {
                return bin.OperatorKind switch
                {
                    BoundBinaryOperatorKind.Add => left.Value + right.Value,
                    BoundBinaryOperatorKind.Subtract => left.Value - right.Value,
                    BoundBinaryOperatorKind.Multiply => left.Value * right.Value,
                    BoundBinaryOperatorKind.Divide when right.Value != 0 => left.Value / right.Value,
                    _ => (decimal?)null
                };
            }
        }

        return null;
    }

    // ── MULTIPLY ──

    private IrBasicBlock LowerArithmetic(BoundArithmeticStatement arith, IrMethod method, IrBasicBlock block)
        => arith.ArithmeticKind switch
        {
            ArithmeticKind.Add => LowerAdd(arith, method, block),
            ArithmeticKind.Subtract => LowerSubtract(arith, method, block),
            ArithmeticKind.Multiply => LowerMultiply(arith, method, block),
            ArithmeticKind.Divide => LowerDivide(arith, method, block),
            ArithmeticKind.Compute => LowerCompute(arith, method, block),
            _ => throw new InvalidOperationException($"Unknown arithmetic kind: {arith.ArithmeticKind}")
        };

    private IrBasicBlock LowerMultiply(BoundArithmeticStatement mult, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        foreach (var target in mult.Targets)
        {
            var destLoc = ResolveExpressionLocation(target.Target);
            if (destLoc == null) continue;
            int roundingMode = target.IsRounded ? 1 : 0;

            if (mult.IsGiving)
            {
                // GIVING form: result = Operand * ByOperand → stored in target
                // Use IrComputeStore with a synthetic multiply expression
                var mulExpr = new BoundBinaryExpression(
                    mult.Operands[0], BoundBinaryOperatorKind.Multiply, mult.Receiver!,
                    CobolCategory.Numeric);
                block.Instructions.Add(new IrComputeStore(mulExpr, destLoc, roundingMode,
                    PreResolveExpressionLocations(mulExpr)));
            }
            else
            {
                // Non-GIVING: result = Operand * target → stored in target
                if (mult.Operands[0] is BoundLiteralExpression lit && lit.Value is decimal d)
                {
                    block.Instructions.Add(new IrPicMultiplyLiteral(
                        d, destLoc, destLoc, roundingMode));
                }
                else
                {
                    var opLoc = ResolveExpressionLocation(mult.Operands[0]);
                    if (opLoc != null)
                    {
                        block.Instructions.Add(new IrPicMultiply(
                            opLoc, destLoc, destLoc, roundingMode));
                    }
                }
            }
        }

        return LowerSizeError(mult.SizeError, method, block);
    }

    // ── SUBTRACT ──

    private IrBasicBlock LowerSubtract(BoundArithmeticStatement sub, IrMethod method, IrBasicBlock block)
    {
        // One ArithmeticStatus per statement — init once, sticky across all targets
        block.Instructions.Add(new IrInitArithmeticStatus());

        // COBOL spec: "All operands preceding FROM are added together, and this sum
        // is subtracted from each identifier following FROM."
        // Step 1: Accumulate all operands into a decimal sum
        var accum = _valueFactory.Next(IrPrimitiveType.Decimal);
        block.Instructions.Add(new IrInitAccumulator(accum));

        foreach (var operand in sub.Operands)
        {
            if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrAccumulateLiteral(accum, d));
            }
            else if (operand is BoundIdentifierExpression opId)
            {
                var opLoc = ResolveLocation(opId);
                if (opLoc != null)
                {
                    block.Instructions.Add(new IrAccumulateField(accum, opLoc));
                }
            }
        }

        // Step 2: For each target, apply the subtraction
        foreach (var target in sub.Targets)
        {
            var destLoc = ResolveLocation(target.Target);
            if (destLoc == null) continue;
            int roundingMode = target.IsRounded ? 1 : 0;
            if (sub.IsGiving && sub.Receiver != null)
            {
                // GIVING: target = minuend - accumulated
                // Use IrComputeStore with synthetic expression: minuend - accumulated_as_literal
                // Since accumulated is a runtime value, we build: minuend - (sum of operands)
                // Reconstruct the subtraction expression from the bound operands
                BoundExpression sumExpr = sub.Operands[0];
                for (int i = 1; i < sub.Operands.Count; i++)
                {
                    sumExpr = new BoundBinaryExpression(
                        sumExpr, BoundBinaryOperatorKind.Add, sub.Operands[i],
                        CobolCategory.Numeric);
                }
                var subExpr = new BoundBinaryExpression(
                    sub.Receiver, BoundBinaryOperatorKind.Subtract, sumExpr,
                    CobolCategory.Numeric);
                block.Instructions.Add(new IrComputeStore(subExpr, destLoc, roundingMode,
                    PreResolveExpressionLocations(subExpr)));
            }
            else
            {
                // FROM: target = target - accumulated
                block.Instructions.Add(new IrSubtractAccumulatedFromTarget(accum, destLoc, roundingMode));
            }
        }

        return LowerSizeError(sub.SizeError, method, block);
    }

    // ── DIVIDE ──

    private IrBasicBlock LowerDivide(BoundArithmeticStatement div, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        // Quotient accumulator — used by REMAINDER if present
        IrValue? accum = null;

        if (div.Receiver != null)
        {
            // DIVIDE BY GIVING: compute quotient ONCE into accumulator, store to all targets.
            // Critical: the dividend (Receiver) may also be a GIVING target,
            // so we must evaluate the expression before any target is written.
            var divExpr = new BoundBinaryExpression(
                div.Receiver, BoundBinaryOperatorKind.Divide, div.Operands[0],
                CobolCategory.Numeric);
            accum = _valueFactory.Next(IrPrimitiveType.Decimal);
            block.Instructions.Add(new IrComputeIntoAccumulator(accum.Value, divExpr,
                PreResolveExpressionLocations(divExpr)));

            foreach (var target in div.Targets)
            {
                var destLoc = ResolveLocation(target.Target);
                if (destLoc == null) continue;
                int roundingMode = target.IsRounded ? 1 : 0;
                block.Instructions.Add(new IrMoveAccumulatedToTarget(accum.Value, destLoc, roundingMode));
            }
        }
        else
        {
            // DIVIDE INTO (non-GIVING): target = target / divisor.
            // For REMAINDER: must use accumulator pattern to preserve original dividend
            // before the divide overwrites the target.
            bool needRemainder = div.RemainderTarget != null && div.Targets.Count > 0;

            if (needRemainder)
            {
                // Use accumulator pattern: evaluate dividend/divisor into decimals,
                // compute quotient, store to target, then compute remainder.
                var target0 = div.Targets[0];
                var divExpr = new BoundBinaryExpression(
                    target0.Target, BoundBinaryOperatorKind.Divide, div.Operands[0],
                    CobolCategory.Numeric);
                accum = _valueFactory.Next(IrPrimitiveType.Decimal);
                block.Instructions.Add(new IrComputeIntoAccumulator(accum.Value, divExpr,
                    PreResolveExpressionLocations(divExpr)));

                var destLoc = ResolveLocation(target0.Target);
                if (destLoc != null)
                {
                    int roundingMode = target0.IsRounded ? 1 : 0;
                    block.Instructions.Add(new IrMoveAccumulatedToTarget(accum.Value, destLoc, roundingMode));

                    // Remainder uses the target's fraction digits (quotient stored in target)
                    int givingFracDigits = GetPicForLocation(destLoc).FractionDigits;
                    var remLoc = ResolveLocation(div.RemainderTarget!);
                    if (remLoc != null)
                    {
                        block.Instructions.Add(new IrCobolRemainder(
                            target0.Target, div.Operands[0], accum.Value, givingFracDigits, remLoc,
                            PreResolveExpressionLocations(target0.Target),
                            PreResolveExpressionLocations(div.Operands[0])));
                    }
                }
            }
            else
            {
                foreach (var target in div.Targets)
                {
                    var destLoc = ResolveLocation(target.Target);
                    if (destLoc == null) continue;
                    int roundingMode = target.IsRounded ? 1 : 0;

                    if (div.Operands[0] is BoundLiteralExpression litDiv && litDiv.Value is decimal d)
                    {
                        block.Instructions.Add(new IrPicDivideLiteral(d, destLoc, destLoc, roundingMode));
                    }
                    else if (div.Operands[0] is BoundIdentifierExpression divisorId)
                    {
                        var divisorLoc = ResolveLocation(divisorId);
                        if (divisorLoc != null)
                        {
                            block.Instructions.Add(new IrPicDivide(destLoc, divisorLoc, destLoc, roundingMode));
                        }
                    }
                }
            }
        }

        // REMAINDER for GIVING form: dividend - truncatedQuotient × divisor
        if (div.RemainderTarget != null && div.Receiver != null && div.Targets.Count > 0 && accum != null)
        {
            var remLoc = ResolveLocation(div.RemainderTarget);
            if (remLoc != null)
            {
                var givingLoc = ResolveLocation(div.Targets[0].Target);
                int givingFracDigits = givingLoc != null
                    ? GetPicForLocation(givingLoc).FractionDigits : 0;

                block.Instructions.Add(new IrCobolRemainder(
                    div.Receiver, div.Operands[0], accum.Value, givingFracDigits, remLoc,
                    PreResolveExpressionLocations(div.Receiver),
                    PreResolveExpressionLocations(div.Operands[0])));
            }
        }

        return LowerSizeError(div.SizeError, method, block);
    }

    // ── COMPUTE ──

    private IrBasicBlock LowerCompute(BoundArithmeticStatement comp, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        // Emit expression evaluation + store for each target
        foreach (var target in comp.Targets)
        {
            var destLoc = ResolveLocation(target.Target);
            if (destLoc == null) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            block.Instructions.Add(new IrComputeStore(
                comp.Operands[0], destLoc, roundingMode,
                PreResolveExpressionLocations(comp.Operands[0])));
        }

        return LowerSizeError(comp.SizeError, method, block);
    }

    // ── ADD ──

    private IrBasicBlock LowerAdd(BoundArithmeticStatement add, IrMethod method, IrBasicBlock block)
    {
        block.Instructions.Add(new IrInitArithmeticStatus());

        // COBOL spec: "All operands preceding TO are added together, and this sum
        // is added to each identifier following TO."
        // Step 1: Accumulate all operands into a decimal sum
        var accum = _valueFactory.Next(IrPrimitiveType.Decimal);
        block.Instructions.Add(new IrInitAccumulator(accum));

        foreach (var operand in add.Operands)
        {
            if (operand is BoundLiteralExpression lit && lit.Value is decimal d)
            {
                block.Instructions.Add(new IrAccumulateLiteral(accum, d));
            }
            else if (operand is BoundIdentifierExpression srcId)
            {
                var srcLoc = ResolveLocation(srcId);
                if (srcLoc != null)
                {
                    block.Instructions.Add(new IrAccumulateField(accum, srcLoc));
                }
            }
        }

        // Step 2: For each target, apply the accumulated sum
        foreach (var target in add.Targets)
        {
            var destLoc = ResolveLocation(target.Target);
            if (destLoc == null) continue;

            int roundingMode = target.IsRounded ? 1 : 0;
            if (add.IsGiving)
            {
                block.Instructions.Add(new IrMoveAccumulatedToTarget(accum, destLoc, roundingMode));
            }
            else
            {
                block.Instructions.Add(new IrAddAccumulatedToTarget(accum, destLoc, roundingMode));
            }
        }

        return LowerSizeError(add.SizeError, method, block);
    }

    // ── Shared ON SIZE ERROR lowering ──

    private IrBasicBlock LowerSizeError(BoundSizeErrorClause? clause, IrMethod method, IrBasicBlock block)
    {
        if (clause == null || !clause.HasClauses)
            return block;

        var sizeErrorBlock = method.CreateBlock("size.error");
        var notSizeErrorBlock = method.CreateBlock("not.size.error");
        var doneBlock = method.CreateBlock("size.done");

        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrLoadSizeError(condVal));
        block.Instructions.Add(new IrBranchIfFalse(condVal, notSizeErrorBlock));

        method.Blocks.Add(sizeErrorBlock);
        var seCurrent = sizeErrorBlock;
        foreach (var stmt in clause.OnSizeError)
            seCurrent = LowerStatement(stmt, method, seCurrent);
        seCurrent.Instructions.Add(new IrJump(doneBlock));

        method.Blocks.Add(notSizeErrorBlock);
        var nseCurrent = notSizeErrorBlock;
        foreach (var stmt in clause.NotOnSizeError)
            nseCurrent = LowerStatement(stmt, method, nseCurrent);
        nseCurrent.Instructions.Add(new IrJump(doneBlock));

        method.Blocks.Add(doneBlock);
        return doneBlock;
    }

    // ── IF ──

    private IrBasicBlock LowerIf(BoundIfStatement iff, IrMethod method, IrBasicBlock current)
    {
        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        LowerCondition(iff.Condition, condVal, current);

        var thenBlock = method.CreateBlock("if.then");
        var elseBlock = iff.ElseStatements is { Count: > 0 }
            ? method.CreateBlock("if.else")
            : null;
        var joinBlock = method.CreateBlock("if.join");

        // Branch: if condition false → else (or join if no else)
        current.Instructions.Add(new IrBranchIfFalse(condVal,
            elseBlock ?? joinBlock));

        // THEN block
        method.Blocks.Add(thenBlock);
        var thenCurrent = thenBlock;
        foreach (var stmt in iff.ThenStatements)
            thenCurrent = LowerStatement(stmt, method, thenCurrent);
        thenCurrent.Instructions.Add(new IrJump(joinBlock));

        // ELSE block (optional)
        if (elseBlock != null)
        {
            method.Blocks.Add(elseBlock);
            var elseCurrent = elseBlock;
            foreach (var stmt in iff.ElseStatements!)
                elseCurrent = LowerStatement(stmt, method, elseCurrent);
            elseCurrent.Instructions.Add(new IrJump(joinBlock));
        }

        // Subsequent statements go to join block
        method.Blocks.Add(joinBlock);
        return joinBlock;
    }

    // ── EVALUATE ──

    private IrBasicBlock LowerEvaluate(BoundEvaluateStatement eval, IrMethod method, IrBasicBlock block)
    {
        var doneBlock = method.CreateBlock("eval.done");

        for (int i = 0; i < eval.Whens.Count; i++)
        {
            var when = eval.Whens[i];
            var whenBody = method.CreateBlock($"eval.when.{i}");
            var nextWhen = method.CreateBlock($"eval.next.{i}");

            // Build match condition: AND across subjects, OR within each subject's values/ranges
            LowerEvaluateWhenMatch(eval, when, whenBody, nextWhen, method, ref block);

            // WHEN body
            method.Blocks.Add(whenBody);
            var bodyCurrent = whenBody;
            foreach (var stmt in when.Statements)
                bodyCurrent = LowerStatement(stmt, method, bodyCurrent);
            bodyCurrent.Instructions.Add(new IrJump(doneBlock));

            // Next WHEN
            method.Blocks.Add(nextWhen);
            block = nextWhen;
        }

        // WHEN OTHER
        if (eval.WhenOther is { Count: > 0 })
        {
            var otherCurrent = block;
            foreach (var stmt in eval.WhenOther)
                otherCurrent = LowerStatement(stmt, method, otherCurrent);
            otherCurrent.Instructions.Add(new IrJump(doneBlock));
        }
        else
        {
            block.Instructions.Add(new IrJump(doneBlock));
        }

        method.Blocks.Add(doneBlock);
        return doneBlock;
    }

    /// <summary>
    /// For a single WHEN clause, emit the match logic.
    /// Multi-subject: AND across subjects. Within each subject: OR over values/ranges.
    /// EVALUATE TRUE: each SubjectCondition is a BoundEvaluateConditionWhen (standalone condition).
    /// </summary>
    private void LowerEvaluateWhenMatch(
        BoundEvaluateStatement eval,
        BoundEvaluateWhen when,
        IrBasicBlock whenBody,
        IrBasicBlock nextWhen,
        IrMethod method,
        ref IrBasicBlock block)
    {
        int subjectCount = when.SubjectConditions.Count;

        if (subjectCount == 0)
        {
            block.Instructions.Add(new IrJump(nextWhen));
            return;
        }

        // For single subject (common case), simplify: no AND needed
        if (subjectCount == 1)
        {
            LowerEvaluateSubjectMatch(eval, 0, when.SubjectConditions[0],
                whenBody, nextWhen, method, ref block);
            return;
        }

        // Multi-subject: all subjects must match (AND).
        // Strategy: for each subject, if it fails → jump to nextWhen.
        // If all pass, fall through to whenBody.
        for (int k = 0; k < subjectCount; k++)
        {
            var cond = when.SubjectConditions[k];
            if (cond is BoundEvaluateValueCondition vc && vc.IsAny)
                continue; // ANY always matches — skip

            if (k < subjectCount - 1)
            {
                // Not the last subject: if this fails, go to nextWhen
                var nextSubject = method.CreateBlock($"eval.subj.{k + 1}");
                LowerEvaluateSubjectMatch(eval, k, cond, nextSubject, nextWhen, method, ref block);
                method.Blocks.Add(nextSubject);
                block = nextSubject;
            }
            else
            {
                // Last subject: if this passes, go to whenBody; if fails, nextWhen
                LowerEvaluateSubjectMatch(eval, k, cond, whenBody, nextWhen, method, ref block);
            }
        }
    }

    /// <summary>
    /// Match a single subject against its condition.
    /// On match: jump to successBlock. On fail: jump to failBlock.
    /// </summary>
    private void LowerEvaluateSubjectMatch(
        BoundEvaluateStatement eval,
        int subjectIndex,
        BoundEvaluateCondition cond,
        IrBasicBlock successBlock,
        IrBasicBlock failBlock,
        IrMethod method,
        ref IrBasicBlock block)
    {
        if (cond is BoundEvaluateConditionWhen condWhen)
        {
            // EVALUATE TRUE: condition is a standalone boolean
            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(condWhen.Condition, condVal, block);
            block.Instructions.Add(new IrBranchIfFalse(condVal, failBlock));
            block.Instructions.Add(new IrJump(successBlock));
            return;
        }

        if (cond is BoundEvaluateValueCondition vc)
        {
            if (vc.IsAny)
            {
                block.Instructions.Add(new IrJump(successBlock));
                return;
            }

            var subject = eval.Subjects[subjectIndex];

            // OR over values: if any value matches, success
            foreach (var value in vc.Values)
            {
                var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
                LowerEvaluateComparison(subject, value, condVal, block);
                // If true → success
                var tryNext = method.CreateBlock("eval.val.next");
                block.Instructions.Add(new IrBranchIfFalse(condVal, tryNext));
                block.Instructions.Add(new IrJump(successBlock));
                method.Blocks.Add(tryNext);
                block = tryNext;
            }

            // OR over ranges: if any range matches, success
            foreach (var range in vc.Ranges)
            {
                // subject >= from AND subject <= to
                var geVal = _valueFactory.Next(IrPrimitiveType.Bool);
                var leVal = _valueFactory.Next(IrPrimitiveType.Bool);
                LowerEvaluateComparison(subject, range.From, geVal, block,
                    BoundBinaryOperatorKind.GreaterOrEqual);
                LowerEvaluateComparison(subject, range.To, leVal, block,
                    BoundBinaryOperatorKind.LessOrEqual);
                // Both must be true → AND
                var tryNextRange = method.CreateBlock("eval.range.next");
                block.Instructions.Add(new IrBranchIfFalse(geVal, tryNextRange));
                block.Instructions.Add(new IrBranchIfFalse(leVal, tryNextRange));
                block.Instructions.Add(new IrJump(successBlock));
                method.Blocks.Add(tryNextRange);
                block = tryNextRange;
            }

            // None matched → fail
            block.Instructions.Add(new IrJump(failBlock));
            return;
        }

        // Unknown condition type — skip to fail
        block.Instructions.Add(new IrJump(failBlock));
    }

    private void LowerEvaluateComparison(BoundExpression subject, BoundExpression obj,
        IrValue result, IrBasicBlock block,
        BoundBinaryOperatorKind op = BoundBinaryOperatorKind.Equal)
    {
        var cmpExpr = new BoundBinaryExpression(subject, op, obj, CobolCategory.Unknown);
        LowerCondition(cmpExpr, result, block);
    }

    // ── Comparison subsystem: normalize → classify → matrix dispatch ──

    /// <summary>
    /// Normalized operand for comparison dispatch.
    /// Every BoundExpression in a comparison is reduced to one of these.
    /// </summary>
    private enum ComparisonOperandKind { Location, NumericLiteral, StringLiteral, Figurative }

    private sealed class ComparisonOperand
    {
        public ComparisonOperandKind Kind { get; }
        public IrLocation? Location { get; init; }
        public Runtime.CobolCategory Category { get; init; }
        public decimal NumericValue { get; init; }
        public string? StringValue { get; init; }
        public Runtime.FigurativeKind FigurativeKind { get; init; }
        public string? AllLiteral { get; init; }
        public int FieldWidth { get; init; }

        private ComparisonOperand(ComparisonOperandKind kind) { Kind = kind; }

        public static ComparisonOperand FromLocation(IrLocation loc, Runtime.CobolCategory cat, int width) =>
            new(ComparisonOperandKind.Location) { Location = loc, Category = cat, FieldWidth = width };
        public static ComparisonOperand FromNumeric(decimal value) =>
            new(ComparisonOperandKind.NumericLiteral) { NumericValue = value, Category = Runtime.CobolCategory.Numeric };
        public static ComparisonOperand FromString(string value) =>
            new(ComparisonOperandKind.StringLiteral) { StringValue = value, Category = Runtime.CobolCategory.Alphanumeric };
        public static ComparisonOperand FromFigurative(Runtime.FigurativeKind kind, string? allLiteral = null) =>
            new(ComparisonOperandKind.Figurative) { FigurativeKind = kind, AllLiteral = allLiteral };
    }

    /// <summary>
    /// Normalize a BoundExpression into a ComparisonOperand.
    /// </summary>
    private ComparisonOperand? NormalizeOperand(BoundExpression expr)
    {
        switch (expr)
        {
            case BoundIdentifierExpression:
            case BoundReferenceModificationExpression:
            {
                var loc = ResolveExpressionLocation(expr);
                if (loc == null) return null;
                var pic = GetPicForLocation(loc);
                return ComparisonOperand.FromLocation(loc, pic.Category, pic.StorageLength);
            }

            case BoundLiteralExpression lit:
                if (lit.Value is decimal d) return ComparisonOperand.FromNumeric(d);
                if (lit.Value is string s) return ComparisonOperand.FromString(s);
                return null;

            case BoundFigurativeExpression fig:
                return ComparisonOperand.FromFigurative(
                    (Runtime.FigurativeKind)fig.FigurativeKind, fig.AllLiteral);

            // Negative numeric: -(literal) encoded as (0 - literal)
            case BoundBinaryExpression neg
                when neg.OperatorKind == BoundBinaryOperatorKind.Subtract
                     && neg.Left is BoundLiteralExpression zl && zl.Value is decimal zd && zd == 0m
                     && neg.Right is BoundLiteralExpression il && il.Value is decimal id:
                return ComparisonOperand.FromNumeric(-id);

            default:
                return null;
        }
    }

    /// <summary>
    /// Build the string value for a figurative constant, respecting field width.
    /// </summary>
    private static string MakeFigurativeString(Runtime.FigurativeKind kind, int width, string? allLiteral)
    {
        char fillChar = kind switch
        {
            Runtime.FigurativeKind.Space => ' ',
            Runtime.FigurativeKind.Zero => '0',
            Runtime.FigurativeKind.HighValue => '\xFF',
            Runtime.FigurativeKind.LowValue => '\x00',
            Runtime.FigurativeKind.Quote => '"',
            _ => ' '
        };

        if (allLiteral != null)
        {
            // ALL "X" — repeat pattern to fill width
            if (width <= 0) return allLiteral;
            var sb = new System.Text.StringBuilder(width);
            while (sb.Length < width) sb.Append(allLiteral);
            return sb.ToString(0, width);
        }

        return width > 0 ? new string(fillChar, width) : fillChar.ToString();
    }

    private void LowerCondition(BoundExpression cond, IrValue result, IrBasicBlock block)
    {
        // Class condition: IS NUMERIC, IS ALPHABETIC, etc.
        if (cond is BoundClassConditionExpression cc)
        {
            LowerClassCondition(cc, result, block);
            return;
        }

        // Sign condition: IS [NOT] POSITIVE/NEGATIVE/ZERO
        if (cond is BoundSignConditionExpression sc)
        {
            LowerSignCondition(sc, result, block);
            return;
        }

        // Level-88 condition name
        if (cond is BoundConditionNameExpression cn)
        {
            LowerConditionName(cn, result, block);
            return;
        }

        if (cond is BoundBinaryExpression binCond)
        {
            // Boolean composition — recurse
            switch (binCond.OperatorKind)
            {
                case BoundBinaryOperatorKind.Or:
                {
                    var leftVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.Or));
                    return;
                }
                case BoundBinaryOperatorKind.And:
                {
                    var leftVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    var rightVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, leftVal, block);
                    LowerCondition(binCond.Right, rightVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, leftVal, rightVal, IrLogicalOp.And));
                    return;
                }
                case BoundBinaryOperatorKind.Not:
                {
                    var innerVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    LowerCondition(binCond.Left, innerVal, block);
                    block.Instructions.Add(new IrBinaryLogical(result, innerVal, innerVal, IrLogicalOp.Not));
                    return;
                }
            }

            // Relational comparison — normalize and dispatch
            LowerComparison(binCond, result, block);
            return;
        }

        _diagnostics.Report(DiagnosticDescriptors.COBOL0503, s_noLocation, s_noSpan, cond.GetType().Name);
    }

    /// <summary>
    /// Lower a binary relational comparison via normalize → classify → matrix dispatch.
    /// </summary>
    private void LowerComparison(BoundBinaryExpression binCond, IrValue result, IrBasicBlock block)
    {
        var left = NormalizeOperand(binCond.Left);
        var right = NormalizeOperand(binCond.Right);

        if (left == null || right == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0504, s_noLocation, s_noSpan, binCond.Left.GetType().Name, binCond.Right.GetType().Name);
            return;
        }

        int op = (int)binCond.OperatorKind;

        // Canonicalize: ensure location is on the left when possible.
        // If left is non-location and right is location, swap and flip operator.
        if (left.Kind != ComparisonOperandKind.Location && right.Kind == ComparisonOperandKind.Location)
        {
            (left, right) = (right, left);
            op = (int)FlipComparisonOp(binCond.OperatorKind);
        }

        // Determine comparison category: numeric or alphanumeric.
        // COBOL rule: if either operand is numeric AND the other can be treated as numeric,
        // use numeric comparison. Otherwise use alphanumeric.
        bool useNumeric = IsNumericComparison(left, right);

        // Matrix dispatch on (left.Kind, right.Kind)
        switch (left.Kind, right.Kind)
        {
            case (ComparisonOperandKind.Location, ComparisonOperandKind.Location):
                if (useNumeric)
                {
                    block.Instructions.Add(new IrPicCompare(left.Location!, right.Location!, result, op));
                }
                else
                {
                    // Use IrPicCompare (with PIC descriptors) when either side is numeric
                    // so the runtime can do the pseudo-MOVE (sign stripping) for
                    // numeric DISPLAY vs alphanumeric comparisons.
                    // Use IrStringCompare only when both sides are truly alphanumeric.
                    bool eitherNumeric = left.Category == Runtime.CobolCategory.Numeric
                                     || right.Category == Runtime.CobolCategory.Numeric;
                    if (eitherNumeric)
                        block.Instructions.Add(new IrPicCompare(left.Location!, right.Location!, result, op));
                    else
                        block.Instructions.Add(new IrStringCompare(left.Location!, right.Location!, result, op));
                }
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.NumericLiteral):
                block.Instructions.Add(new IrPicCompareLiteral(left.Location!, right.NumericValue, result, op));
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.StringLiteral):
                if (useNumeric && decimal.TryParse(right.StringValue,
                    System.Globalization.CultureInfo.InvariantCulture, out var numFromStr))
                    block.Instructions.Add(new IrPicCompareLiteral(left.Location!, numFromStr, result, op));
                else
                    block.Instructions.Add(new IrStringCompareLiteral(left.Location!, right.StringValue!, result, op));
                break;

            case (ComparisonOperandKind.Location, ComparisonOperandKind.Figurative):
                EmitLocationVsFigurative(left, right, result, op, block);
                break;

            case (ComparisonOperandKind.NumericLiteral, ComparisonOperandKind.NumericLiteral):
                // Compile-time constant comparison
                int cmp = Math.Sign(left.NumericValue.CompareTo(right.NumericValue));
                bool constResult = EvaluateComparisonResult(cmp, (BoundBinaryOperatorKind)op);
                block.Instructions.Add(new IrSetBool(result, constResult));
                break;

            case (ComparisonOperandKind.StringLiteral, ComparisonOperandKind.StringLiteral):
                int strCmp = string.Compare(left.StringValue, right.StringValue, StringComparison.Ordinal);
                bool strResult = EvaluateComparisonResult(Math.Sign(strCmp), (BoundBinaryOperatorKind)op);
                block.Instructions.Add(new IrSetBool(result, strResult));
                break;

            default:
                _diagnostics.Report(DiagnosticDescriptors.COBOL0505, s_noLocation, s_noSpan, left.Kind, right.Kind);
                break;
        }
    }

    /// <summary>
    /// Determine if a comparison should use numeric semantics.
    /// COBOL-85 rule: numeric comparison ONLY when both operands are numeric
    /// (PIC 9, COMP, etc.). If either operand is alphanumeric, alphanumeric-edited,
    /// or numeric-edited, use alphanumeric comparison. Numeric-edited fields
    /// participate in alphanumeric comparison using their displayed form.
    /// </summary>
    private static bool IsNumericComparison(ComparisonOperand left, ComparisonOperand right)
    {
        bool leftIsNumeric = IsStrictlyNumeric(left);
        bool rightIsNumeric = IsStrictlyNumeric(right);

        // Both must be numeric (or numeric-compatible) for numeric comparison
        return leftIsNumeric && rightIsNumeric;
    }

    /// <summary>
    /// Returns true if the operand is strictly numeric (not edited, not alphanumeric).
    /// </summary>
    private static bool IsStrictlyNumeric(ComparisonOperand op)
    {
        return op.Kind switch
        {
            ComparisonOperandKind.Location =>
                op.Category == Runtime.CobolCategory.Numeric,
            ComparisonOperandKind.NumericLiteral => true,
            ComparisonOperandKind.Figurative =>
                op.FigurativeKind == Runtime.FigurativeKind.Zero,
            ComparisonOperandKind.StringLiteral => false,
            _ => false
        };
    }

    /// <summary>
    /// Emit comparison: location vs figurative constant.
    /// </summary>
    private void EmitLocationVsFigurative(ComparisonOperand loc, ComparisonOperand fig,
        IrValue result, int op, IrBasicBlock block)
    {
        var fk = fig.FigurativeKind;
        bool isNumeric = loc.Category.IsNumericLike();

        // ZERO on numeric field → numeric comparison against 0
        if (isNumeric && fk == Runtime.FigurativeKind.Zero)
        {
            block.Instructions.Add(new IrPicCompareLiteral(loc.Location!, 0m, result, op));
            return;
        }

        // All other figuratives → alphanumeric string comparison
        // Fill to field width for correct multi-byte comparison
        string figStr = MakeFigurativeString(fk, loc.FieldWidth, fig.AllLiteral);
        block.Instructions.Add(new IrStringCompareLiteral(loc.Location!, figStr, result, op));
    }

    /// <summary>
    /// Evaluate a compile-time comparison result from a sign value (-1, 0, +1).
    /// </summary>
    private static bool EvaluateComparisonResult(int sign, BoundBinaryOperatorKind op)
    {
        return op switch
        {
            BoundBinaryOperatorKind.Equal => sign == 0,
            BoundBinaryOperatorKind.NotEqual => sign != 0,
            BoundBinaryOperatorKind.Less => sign < 0,
            BoundBinaryOperatorKind.LessOrEqual => sign <= 0,
            BoundBinaryOperatorKind.Greater => sign > 0,
            BoundBinaryOperatorKind.GreaterOrEqual => sign >= 0,
            _ => false
        };
    }

    private static BoundBinaryOperatorKind FlipComparisonOp(BoundBinaryOperatorKind op)
    {
        return op switch
        {
            BoundBinaryOperatorKind.Less => BoundBinaryOperatorKind.Greater,
            BoundBinaryOperatorKind.LessOrEqual => BoundBinaryOperatorKind.GreaterOrEqual,
            BoundBinaryOperatorKind.Greater => BoundBinaryOperatorKind.Less,
            BoundBinaryOperatorKind.GreaterOrEqual => BoundBinaryOperatorKind.LessOrEqual,
            _ => op // Equal, NotEqual are symmetric
        };
    }

    // ── Class condition (IS NUMERIC, IS ALPHABETIC, etc.) ──

    /// <summary>
    /// Sign condition: IS [NOT] POSITIVE/NEGATIVE/ZERO.
    /// Lowered as a comparison against literal zero.
    /// </summary>
    private void LowerSignCondition(BoundSignConditionExpression sc, IrValue result, IrBasicBlock block)
    {
        // Rewrite as: subject OP 0
        var op = sc.SignKind switch
        {
            SignConditionKind.Positive => BoundBinaryOperatorKind.Greater,
            SignConditionKind.Negative => BoundBinaryOperatorKind.Less,
            SignConditionKind.Zero => BoundBinaryOperatorKind.Equal,
            _ => BoundBinaryOperatorKind.Equal
        };

        var zero = new BoundLiteralExpression(0m, CobolCategory.Numeric);
        var comparison = new BoundBinaryExpression(sc.Subject, op, zero, CobolCategory.Unknown);

        if (sc.IsNegated)
        {
            var tmp = _valueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(comparison, tmp, block);
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Not));
        }
        else
        {
            LowerCondition(comparison, result, block);
        }
    }

    private void LowerClassCondition(BoundClassConditionExpression cc, IrValue result, IrBasicBlock block)
    {
        if (cc.Subject is not BoundIdentifierExpression id)
            throw new InvalidOperationException("Class condition subject must be a data item");

        var loc = ResolveLocation(id);
        if (loc == null)
            throw new InvalidOperationException($"Cannot resolve storage for {id.Symbol.Name}");

        var tmp = _valueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrClassCondition(loc, (int)cc.ClassKind, tmp));

        if (cc.IsNegated)
        {
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Not));
        }
        else
        {
            // Copy tmp to result
            block.Instructions.Add(new IrBinaryLogical(result, tmp, tmp, IrLogicalOp.Or));
        }
    }

    // ── Level-88 condition name ──

    private void LowerConditionName(BoundConditionNameExpression cn, IrValue result, IrBasicBlock block)
    {
        var parentSym = cn.Condition.ParentDataItem;
        var parentLoc = ResolveLocation(parentSym);
        if (parentLoc == null)
        {
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var ranges = cn.Condition.ValueRanges;
        if (ranges.Count == 0)
        {
            block.Instructions.Add(new IrSetBool(result, false));
            return;
        }

        var parentCat = GetPicForLocation(parentLoc).Category;

        // Build a list of individual match results
        var matchResults = new List<IrValue>();

        foreach (var range in ranges)
        {
            var from = range.From;
            var to = range.To;
            var matchVal = _valueFactory.Next(IR.IrPrimitiveType.Bool);

            if (to == null)
            {
                // Single value comparison: parent == from
                if (from.IsNumeric)
                {
                    block.Instructions.Add(new IR.IrPicCompareLiteral(
                        parentLoc, from.NumericValue!.Value, matchVal,
                        (int)BoundBinaryOperatorKind.Equal));
                }
                else if (from.IsString)
                {
                    if (parentCat.IsNumericLike() && decimal.TryParse(from.StringValue!,
                        System.Globalization.CultureInfo.InvariantCulture, out var numVal))
                    {
                        block.Instructions.Add(new IR.IrPicCompareLiteral(
                            parentLoc, numVal, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                    else
                    {
                        block.Instructions.Add(new IR.IrStringCompareLiteral(
                            parentLoc, from.StringValue!, matchVal,
                            (int)BoundBinaryOperatorKind.Equal));
                    }
                }
            }
            else
            {
                // Range: parent >= from AND parent <= to
                var geVal = _valueFactory.Next(IR.IrPrimitiveType.Bool);
                var leVal = _valueFactory.Next(IR.IrPrimitiveType.Bool);

                if (from.IsNumeric && to.IsNumeric)
                {
                    block.Instructions.Add(new IR.IrPicCompareLiteral(
                        parentLoc, from.NumericValue!.Value, geVal,
                        (int)BoundBinaryOperatorKind.GreaterOrEqual));
                    block.Instructions.Add(new IR.IrPicCompareLiteral(
                        parentLoc, to.NumericValue!.Value, leVal,
                        (int)BoundBinaryOperatorKind.LessOrEqual));
                }
                else if (from.IsString && to.IsString)
                {
                    block.Instructions.Add(new IR.IrStringCompareLiteral(
                        parentLoc, from.StringValue!, geVal,
                        (int)BoundBinaryOperatorKind.GreaterOrEqual));
                    block.Instructions.Add(new IR.IrStringCompareLiteral(
                        parentLoc, to.StringValue!, leVal,
                        (int)BoundBinaryOperatorKind.LessOrEqual));
                }

                block.Instructions.Add(new IR.IrBinaryLogical(matchVal, geVal, leVal, IR.IrLogicalOp.And));
            }

            matchResults.Add(matchVal);
        }

        // OR all match results together
        if (matchResults.Count == 1)
        {
            // Single value — copy directly
            block.Instructions.Add(new IR.IrBinaryLogical(result, matchResults[0], matchResults[0], IR.IrLogicalOp.Or));
        }
        else
        {
            // Multiple values — chain ORs
            var accumulated = matchResults[0];
            for (int i = 1; i < matchResults.Count; i++)
            {
                var orResult = _valueFactory.Next(IR.IrPrimitiveType.Bool);
                block.Instructions.Add(new IR.IrBinaryLogical(orResult, accumulated, matchResults[i], IR.IrLogicalOp.Or));
                accumulated = orResult;
            }
            // Final result
            block.Instructions.Add(new IR.IrBinaryLogical(result, accumulated, accumulated, IR.IrLogicalOp.Or));
        }

        // Handle NOT condition-name
        if (cn.IsNegated)
        {
            var notResult = _valueFactory.Next(IR.IrPrimitiveType.Bool);
            block.Instructions.Add(new IR.IrBinaryLogical(notResult, result, result, IR.IrLogicalOp.Not));
            // Copy negated result back
            block.Instructions.Add(new IR.IrBinaryLogical(result, notResult, notResult, IR.IrLogicalOp.Or));
        }
    }

    // ── ALTER ──

    private void LowerAlter(BoundAlterStatement alter, IrBasicBlock block)
    {
        foreach (var entry in alter.Entries)
        {
            if (!_alterSlots.TryGetValue(entry.TargetParagraph.Name, out int slot))
                continue;
            if (!_paragraphIndices.TryGetValue(entry.NewDestination.Name, out int newIndex))
                continue;
            block.Instructions.Add(new IrAlter(slot, newIndex));
        }
    }

    // ── GO TO ──

    private void LowerGoTo(BoundGoToStatement gt, IrBasicBlock block)
    {
        // Bare GO TO (no target) — target set by ALTER at runtime
        if (gt.IsBare)
        {
            if (_currentParagraphName != null && _alterSlots.TryGetValue(_currentParagraphName, out int bareSlot))
            {
                // Default remains -1 (STOP RUN) — ALTER will set the real target
                block.Instructions.Add(new IrReturnAlterable(bareSlot));
            }
            else
            {
                // Bare GO TO not referenced by ALTER — undefined behavior, emit STOP
                block.Instructions.Add(new IrReturnConst(-1));
            }
            return;
        }

        if (gt.IsSimple)
        {
            // Check if this paragraph is an ALTER target — use indirection table
            if (_currentParagraphName != null && _alterSlots.TryGetValue(_currentParagraphName, out int slot))
            {
                // Record the default GO TO target for this alter slot
                if (_paragraphIndices.TryGetValue(gt.Target.Name, out int defaultTarget))
                    _alterDefaults[slot] = defaultTarget;
                block.Instructions.Add(new IrReturnAlterable(slot));
                return;
            }

            // Simple GO TO: unconditional branch to paragraph (non-alterable)
            if (_paragraphIndices.TryGetValue(gt.Target.Name, out int targetIndex))
                block.Instructions.Add(new IrReturnConst(targetIndex));
            else
                _diagnostics.Report(DiagnosticDescriptors.COBOL0506, s_noLocation, s_noSpan, gt.Target.Name);
            return;
        }

        // GO TO para1 para2 ... DEPENDING ON selector
        if (gt.DependingOn == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0507, s_noLocation, s_noSpan);
            return;
        }

        var selectorLoc = ResolveExpressionLocation(gt.DependingOn);
        if (selectorLoc == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0508, s_noLocation, s_noSpan, gt.DependingOn);
            return;
        }

        // Build list of target paragraph indices
        var targetIndices = new List<int>();
        foreach (var target in gt.Targets)
        {
            if (_paragraphIndices.TryGetValue(target.Name, out int idx))
                targetIndices.Add(idx);
            else
            {
                _diagnostics.Report(DiagnosticDescriptors.COBOL0506, s_noLocation, s_noSpan, target.Name);
                targetIndices.Add(-1);
            }
        }

        block.Instructions.Add(new IrGoToDepending(selectorLoc, targetIndices));
    }

    // ── NEXT SENTENCE ──

    private IrBasicBlock LowerNextSentence(IrMethod method, IrBasicBlock block)
    {
        if (_currentSentenceEnd is null)
            throw new InvalidOperationException("NEXT SENTENCE used outside a sentence context.");

        // Jump to the end of the current sentence
        block.Instructions.Add(new IrJump(_currentSentenceEnd));

        // Any statements after NEXT SENTENCE in this sentence are unreachable.
        // Create a dead block so subsequent lowering has somewhere to emit into.
        var dead = new IrBasicBlock("dead_after_next_sentence");
        method.Blocks.Add(dead);
        return dead;
    }

    // ── EXIT PERFORM / EXIT PARAGRAPH / EXIT SECTION ──

    private IrBasicBlock LowerExitPerform(IrMethod method, IrBasicBlock block)
    {
        if (_performExitStack.Count == 0)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0509, s_noLocation, s_noSpan, "PERFORM", "PERFORM");
            return block;
        }

        var exitBlock = _performExitStack.Peek();
        block.Instructions.Add(new IrJump(exitBlock));

        var dead = new IrBasicBlock("dead_after_exit_perform");
        method.Blocks.Add(dead);
        return dead;
    }

    private IrBasicBlock LowerExitParagraph(IrMethod method, IrBasicBlock block)
    {
        if (_paragraphEndBlock == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0509, s_noLocation, s_noSpan, "PARAGRAPH", "paragraph");
            return block;
        }

        block.Instructions.Add(new IrJump(_paragraphEndBlock));

        var dead = new IrBasicBlock("dead_after_exit_paragraph");
        method.Blocks.Add(dead);
        return dead;
    }

    private IrBasicBlock LowerExitSection(IrMethod method, IrBasicBlock block)
    {
        if (_sectionExitReturnIndex == null)
        {
            _diagnostics.Report(DiagnosticDescriptors.COBOL0509, s_noLocation, s_noSpan, "SECTION", "section");
            return block;
        }

        // Return the index of the first paragraph after the section,
        // causing the dispatcher to skip remaining paragraphs in this section.
        block.Instructions.Add(new IrReturnConst(_sectionExitReturnIndex.Value));

        var dead = new IrBasicBlock("dead_after_exit_section");
        method.Blocks.Add(dead);
        return dead;
    }

    // ── STRING ──

    private IrBasicBlock LowerString(BoundStringStatement str, IrMethod method, IrBasicBlock block)
    {
        var destLoc = ResolveExpressionLocation(str.Into);
        if (destLoc == null) return block;

        // Pointer: resolve if present, null if no WITH POINTER clause
        IrLocation? ptrLoc = null;
        if (str.Pointer != null)
            ptrLoc = ResolveExpressionLocation(str.Pointer);

        // Build sending specs
        var sendings = new List<IrStringSending>();
        foreach (var sending in str.Sendings)
        {
            string? delimiter = null;
            if (sending.Delimiter is BoundLiteralExpression delimLit && delimLit.Value is string ds)
                delimiter = ds;

            if (sending.Value is BoundLiteralExpression litVal && litVal.Value is string sv)
            {
                sendings.Add(new IrStringSending(sv, null, delimiter, sending.DelimitedBySize));
            }
            else
            {
                var srcLoc = ResolveExpressionLocation(sending.Value);
                if (srcLoc != null)
                    sendings.Add(new IrStringSending(null, srcLoc, delimiter, sending.DelimitedBySize));
            }
        }

        // Emit single IrStringStatement
        var overflowResult = _valueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrStringStatement(destLoc, sendings, ptrLoc, overflowResult));

        // ON OVERFLOW / NOT ON OVERFLOW branching
        if (str.OnOverflow.Count > 0 || str.NotOnOverflow.Count > 0)
        {
            var onBlock = method.CreateBlock("string.overflow");
            var notBlock = method.CreateBlock("string.not.overflow");
            var afterBlock = method.CreateBlock("string.after");

            block.Instructions.Add(new IrBranchIfFalse(overflowResult, notBlock));
            block.Instructions.Add(new IrJump(onBlock));

            method.Blocks.Add(onBlock);
            var onCurrent = onBlock;
            foreach (var stmt in str.OnOverflow)
                onCurrent = LowerStatement(stmt, method, onCurrent);
            onCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(notBlock);
            var notCurrent = notBlock;
            foreach (var stmt in str.NotOnOverflow)
                notCurrent = LowerStatement(stmt, method, notCurrent);
            notCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    // ── UNSTRING ──

    private IrBasicBlock LowerUnstring(BoundUnstringStatement unstr, IrMethod method, IrBasicBlock block)
    {
        var srcLoc = ResolveExpressionLocation(unstr.Source);
        if (srcLoc == null) return block;

        // Resolve delimiter literal
        string? literalDelimiter = null;
        if (unstr.Delimiter is BoundLiteralExpression delimLit && delimLit.Value is string ds)
            literalDelimiter = ds;

        // Pointer: resolve if present
        IrLocation? ptrLoc = null;
        if (unstr.Pointer != null)
            ptrLoc = ResolveExpressionLocation(unstr.Pointer);

        // Tallying: resolve if present
        IrLocation? tallyLoc = null;
        if (unstr.Tallying != null)
            tallyLoc = ResolveExpressionLocation(unstr.Tallying);

        // Build INTO specs
        var intos = new List<IrUnstringInto>();
        foreach (var into in unstr.Intos)
        {
            var targetLoc = ResolveExpressionLocation(into.Target);
            if (targetLoc == null) continue;

            IrLocation? countLoc = null;
            if (into.CountIn != null)
                countLoc = ResolveExpressionLocation(into.CountIn);

            IrLocation? delimInLoc = null;
            if (into.DelimiterIn != null)
                delimInLoc = ResolveExpressionLocation(into.DelimiterIn);

            intos.Add(new IrUnstringInto(targetLoc, countLoc, delimInLoc));
        }

        // Emit single IrUnstringStatement
        var overflowResult = _valueFactory.Next(IrPrimitiveType.Bool);
        block.Instructions.Add(new IrUnstringStatement(srcLoc, literalDelimiter, unstr.DelimitedByAll,
            intos, ptrLoc, tallyLoc, overflowResult));

        // ON OVERFLOW / NOT ON OVERFLOW branching (same pattern as STRING)
        if (unstr.OnOverflow.Count > 0 || unstr.NotOnOverflow.Count > 0)
        {
            var onBlock = method.CreateBlock("unstring.overflow");
            var notBlock = method.CreateBlock("unstring.not.overflow");
            var afterBlock = method.CreateBlock("unstring.after");

            block.Instructions.Add(new IrBranchIfFalse(overflowResult, notBlock));
            block.Instructions.Add(new IrJump(onBlock));

            method.Blocks.Add(onBlock);
            var onCurrent = onBlock;
            foreach (var stmt in unstr.OnOverflow)
                onCurrent = LowerStatement(stmt, method, onCurrent);
            onCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(notBlock);
            var notCurrent = notBlock;
            foreach (var stmt in unstr.NotOnOverflow)
                notCurrent = LowerStatement(stmt, method, notCurrent);
            notCurrent.Instructions.Add(new IrJump(afterBlock));

            method.Blocks.Add(afterBlock);
            return afterBlock;
        }

        return block;
    }

    // ── SEARCH (linear) ──

    private IrBasicBlock LowerSearch(BoundSearchStatement search, IrMethod method, IrBasicBlock block)
    {
        if (search.Index == null) return block;

        var indexLoc = ResolveLocation(search.Index);
        if (indexLoc == null) return block;

        int upperBound = search.Table.Symbol.Occurs?.MaxOccurs ?? 1;

        // COBOL-85 §14.9.38: SEARCH uses the CURRENT index value.
        // If the index already exceeds the table, AT END is triggered immediately.
        // The index is NOT reset — the programmer must SET it before SEARCH.

        // Loop structure
        var loopHeader = method.CreateBlock("search.loop");
        var atEndBlock = method.CreateBlock("search.atend");
        var exitBlock = method.CreateBlock("search.exit");

        block.Instructions.Add(new IrJump(loopHeader));
        method.Blocks.Add(loopHeader);

        // Bounds check: if index > upperBound → AT END
        var boundsResult = _valueFactory.Next(IrPrimitiveType.Bool);
        loopHeader.Instructions.Add(new IrPicCompareLiteral(
            indexLoc, (decimal)upperBound, boundsResult,
            (int)BoundBinaryOperatorKind.Greater));
        var whenChain = method.CreateBlock("search.whens");
        loopHeader.Instructions.Add(new IrBranchIfFalse(boundsResult, whenChain));
        loopHeader.Instructions.Add(new IrJump(atEndBlock));

        // WHEN chain: evaluate each condition, execute body on match
        method.Blocks.Add(whenChain);
        var current = whenChain;

        foreach (var when in search.Whens)
        {
            var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
            LowerCondition(when.Condition, condVal, current);

            var bodyBlock = method.CreateBlock("search.when.body");
            var nextWhen = method.CreateBlock("search.when.next");

            current.Instructions.Add(new IrBranchIfFalse(condVal, nextWhen));

            // WHEN body
            method.Blocks.Add(bodyBlock);
            var bodyCurrent = bodyBlock;
            foreach (var stmt in when.Statements)
                bodyCurrent = LowerStatement(stmt, method, bodyCurrent);
            bodyCurrent.Instructions.Add(new IrJump(exitBlock));

            method.Blocks.Add(nextWhen);
            current = nextWhen;
        }

        // No WHEN matched: increment index and loop
        block.Instructions.Add(new IrInitArithmeticStatus());
        current.Instructions.Add(new IrPicAddLiteral(indexLoc, 1m));
        current.Instructions.Add(new IrJump(loopHeader));

        // AT END
        method.Blocks.Add(atEndBlock);
        var atEndCurrent = atEndBlock;
        foreach (var stmt in search.AtEnd)
            atEndCurrent = LowerStatement(stmt, method, atEndCurrent);
        atEndCurrent.Instructions.Add(new IrJump(exitBlock));

        method.Blocks.Add(exitBlock);
        return exitBlock;
    }

    // ── SEARCH ALL (binary) ──

    private IrBasicBlock LowerSearchAll(BoundSearchAllStatement searchAll, IrMethod method, IrBasicBlock block)
    {
        if (searchAll.Index == null || searchAll.Whens.Count == 0) return block;

        var indexLoc = ResolveLocation(searchAll.Index);
        if (indexLoc == null) return block;

        int upperBound = searchAll.Table.Symbol.Occurs?.MaxOccurs ?? 1;
        var when = searchAll.Whens[0]; // SEARCH ALL allows exactly one WHEN

        // Create temp variables for low, high, mid
        // We use the index field itself for mid (so user code sees the found position)
        // and create synthetic IrLocations for low/high by reusing the index field's
        // storage characteristics.
        //
        // For simplicity, we lower binary search using the index as mid directly:
        //   low = 1, high = upperBound
        //   while low <= high:
        //     index = (low + high) / 2
        //     if condition → body, exit
        //     if element < key → low = mid + 1
        //     else → high = mid - 1
        //   AT END

        // We need temp storage for low and high. Use IrComputeStore with synthetic expressions.
        // Actually, we can use PERFORM-style arithmetic with the index itself.

        // Simpler approach: use COMPUTE expressions via IrComputeStore for mid calculation,
        // and PicMoveLiteralNumeric/PicAddLiteral/PicSubtractLiteral for low/high.

        // For now, implement as linear search (correct behavior, not O(log n)).
        // This can be optimized to true binary search when KEY ASCENDING/DESCENDING
        // is supported. Linear search produces correct results for all test cases.

        // Initialize index to 1
        block.Instructions.Add(new IrPicMoveLiteralNumeric(indexLoc, 1m));

        var loopHeader = method.CreateBlock("searchall.loop");
        var atEndBlock = method.CreateBlock("searchall.atend");
        var exitBlock = method.CreateBlock("searchall.exit");

        block.Instructions.Add(new IrJump(loopHeader));
        method.Blocks.Add(loopHeader);

        // Bounds check: if index > upperBound → AT END
        var boundsResult = _valueFactory.Next(IrPrimitiveType.Bool);
        loopHeader.Instructions.Add(new IrPicCompareLiteral(
            indexLoc, (decimal)upperBound, boundsResult,
            (int)BoundBinaryOperatorKind.Greater));
        var checkBlock = method.CreateBlock("searchall.check");
        loopHeader.Instructions.Add(new IrBranchIfFalse(boundsResult, checkBlock));
        loopHeader.Instructions.Add(new IrJump(atEndBlock));

        // Evaluate WHEN condition
        method.Blocks.Add(checkBlock);
        var condVal = _valueFactory.Next(IrPrimitiveType.Bool);
        LowerCondition(when.Condition, condVal, checkBlock);

        var bodyBlock = method.CreateBlock("searchall.body");
        var incrementBlock = method.CreateBlock("searchall.incr");

        checkBlock.Instructions.Add(new IrBranchIfFalse(condVal, incrementBlock));

        // Match: execute body
        method.Blocks.Add(bodyBlock);
        var bodyCurrent = bodyBlock;
        foreach (var stmt in when.Statements)
            bodyCurrent = LowerStatement(stmt, method, bodyCurrent);
        bodyCurrent.Instructions.Add(new IrJump(exitBlock));

        // No match: increment and loop
        method.Blocks.Add(incrementBlock);
        incrementBlock.Instructions.Add(new IrInitArithmeticStatus());
        incrementBlock.Instructions.Add(new IrPicAddLiteral(indexLoc, 1m));
        incrementBlock.Instructions.Add(new IrJump(loopHeader));

        // AT END
        method.Blocks.Add(atEndBlock);
        var atEndCurrent = atEndBlock;
        foreach (var stmt in searchAll.AtEnd)
            atEndCurrent = LowerStatement(stmt, method, atEndCurrent);
        atEndCurrent.Instructions.Add(new IrJump(exitBlock));

        method.Blocks.Add(exitBlock);
        return exitBlock;
    }
}
