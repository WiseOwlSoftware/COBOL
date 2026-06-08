// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;
using CobolSharp.Compiler.Common;
using CobolSharp.Compiler.CodeGen.Lowering;
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
    private readonly IrValueFactory _valueFactory = new();
    private readonly CompilationOptions _options;

    // ── M002: Lowering context and lowerer instances ──
    // Created in constructor; methods will move to these classes in Stages 2-4.
    internal readonly LoweringContext _ctx;

    public Binder(SemanticModel semantic, DiagnosticBag diagnostics, CompilationOptions? options = null,
        IReadOnlyList<(int Scope, string? FileName)>? inheritedGlobalUseDeclaratives = null)
    {
        _semantic = semantic;
        _layout = new RecordLayoutBuilder();
        _diagnostics = diagnostics;
        _options = options ?? new CompilationOptions();

        // M002: Build lowering context with shared state
        _ctx = new LoweringContext(semantic, diagnostics, _options, _valueFactory);
        if (inheritedGlobalUseDeclaratives is { Count: > 0 })
            _ctx.InheritedGlobalUseDeclaratives = inheritedGlobalUseDeclaratives;

        // M002: Create lowerer instances (empty shells — methods move in Stages 2-4)
        _ctx.Location = new LocationResolver(_ctx);
        _ctx.Expression = new ExpressionLowerer(_ctx);
        _ctx.Condition = new ConditionLowerer(_ctx);
        _ctx.ControlFlow = new ControlFlowLowerer(_ctx);
        _ctx.Arithmetic = new ArithmeticLowerer(_ctx);
        _ctx.DataMovement = new DataMovementLowerer(_ctx);
        _ctx.FileIo = new FileIoLowerer(_ctx);
        _ctx.String = new StringLowerer(_ctx);
        _ctx.LowerStatement = LowerStatement;
    }

    /// <summary>
    /// Build BoundProgram from parse tree, then lower to IrModule.
    /// </summary>
    public IrModule Bind(Antlr4.Runtime.ParserRuleContext tree, bool isClass = false)
    {
        // Phase 1: Build bound tree + validate
        var builder = new BoundTreeBuilder(_semantic, _diagnostics, _options);
        var boundProgram = builder.Build(tree);
        Semantics.ProcedureGraph.Analyze(boundProgram, _semantic, _diagnostics);
        Semantics.Bound.BoundTreeValidator.Validate(boundProgram, _diagnostics, _semantic);
        Semantics.FileStateValidator.Validate(boundProgram, _diagnostics);

        // Phase 1.5: the .NET-native data-model classifier (docs/RECORD_STRUCT_STORAGE_DESIGN.md, S1).
        // Run the complete RecordClassificationPass (Phases A+B+C) over this program and validate its soundness
        // invariants. It is NOT yet consumed by codegen (the Stage-3 typed flip consults it in LocationResolver),
        // so this is byte-identical today — but running it on every program now exercises the procedure-division
        // walker across the whole corpus and makes the invariant check a permanent internal-consistency net.
        _ctx.Classification = new Semantics.RecordClassificationPass().Classify(
            _semantic.DataItemsInOrder,
            s => _semantic.GetStorageLocation(s)?.Pic.Category ?? Runtime.CobolCategory.Unknown,
            boundProgram.Paragraphs.SelectMany(p => p.Sentences).SelectMany(sn => sn.Statements),
            s => _semantic.GetStorageLocation(s) is { } loc ? (loc.Offset, loc.Length) : null);
        _ctx.Classification.ValidateInvariants();

        // Phase 2: Build record types
        var module = new IrModule(boundProgram.Program.Name);
        // OO (ADR §7): a CLASS-ID unit is tagged here, BEFORE CollectTypedFields, so the typed-field collector
        // can treat object data as an always-on typed consumer (OO has no legacy corpus — see the gate below).
        module.IsClass = isClass;
        BuildRecordTypes(module);

        // Phase 2.5: data-model migration S3 — collect the items to flip to typed-native fields
        // (docs/RECORD_STRUCT_STORAGE_DESIGN.md). No-op unless EnableTypedFields is on.
        CollectTypedFields(module);

        // Phase 2.6: Stage-4 pointers — collect USAGE POINTER / BASED items into managed ManagedPointer fields
        // (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10). Always-on (a managed reference is the only correct pointer
        // representation — NOT gated by EnableTypedFields); no-op unless the program declares pointers.
        CollectPointerFields(module);

        // Phase 2.7: OO object-reference fields — collect USAGE OBJECT REFERENCE items into managed reference
        // fields (docs/OO_IMPLEMENTATION_DESIGN.md §E). Always-on; no-op unless the program declares object refs.
        CollectObjectRefFields(module);

        // Phase 3: Create paragraph method stubs
        CreateParagraphStubs(module, boundProgram);

        // Phase 3.5: Pre-scan for ALTER targets
        ScanAlterTargets(boundProgram);

        // Phase 3.6: Classify declarative control points (exit-point + terminating paragraphs) used to place
        // a USE procedure's return at the declarative's designated exit, not the section's physical last
        // paragraph — see FileIoLowerer.EmitPerformDeclarativeSection.
        ScanDeclarativeControlPoints(boundProgram);

        // Phase 4: Lower all paragraph bodies
        LowerAllParagraphs(boundProgram);

        // Phase 5: Populate module metadata + create entry point
        PopulateModuleMetadata(module, boundProgram);
        CreateEntryPoint(module, boundProgram);

        return module;
    }

    private void CreateParagraphStubs(IrModule module, BoundProgram boundProgram)
    {
        int paraIndex = 0;
        foreach (var para in boundProgram.Paragraphs)
        {
            var method = new IrMethod($"Para_{para.Symbol.Name}", returnType: IrPrimitiveType.Int32);
            method.Blocks.Add(new IrBasicBlock($"{para.Symbol.Name}_entry"));
            _ctx.ParagraphMethods[para.Symbol.Name] = method;
            _ctx.ParagraphIndices[para.Symbol.Name] = paraIndex;
            _ctx.ParagraphsByIndex.Add(para.Symbol.Name);
            _ctx.ParagraphSymbolMethods[para.Symbol] = method;
            _ctx.ParagraphSymbolIndices[para.Symbol] = paraIndex;
            module.Methods.Add(method);
            paraIndex++;
        }
    }

    private void ScanAlterTargets(BoundProgram boundProgram)
    {
        foreach (var para in boundProgram.Paragraphs)
            foreach (var sentence in para.Sentences)
                foreach (var stmt in sentence.Statements)
                    if (stmt is BoundAlterStatement alter)
                        foreach (var entry in alter.Entries)
                        {
                            string name = entry.TargetParagraph.Name;
                            if (!_ctx.AlterSlots.ContainsKey(name))
                            {
                                _ctx.AlterSlots[name] = _ctx.AlterSlots.Count;
                                _ctx.AlterDefaults.Add(-1);
                            }
                        }
    }

    /// <summary>
    /// Classify each paragraph for declarative USE-procedure exit placement (see
    /// FileIoLowerer.EmitPerformDeclarativeSection):
    /// <list type="bullet">
    /// <item><b>Exit-point</b> (<see cref="LoweringContext.ExitPointParagraphs"/>): no statements (an empty
    /// label, e.g. an <c>END-DECLS.</c> just before END DECLARATIVES) or solely bare EXIT/CONTINUE (both
    /// lower to <see cref="BoundExitStatement"/>) — the COBOL "common end point" idiom (ISO §14.9.17). Empty
    /// paragraphs MUST count: many CCVS declaratives end with an empty <c>END-DECLS.</c> exit label, and
    /// excluding it would mis-pick an earlier bare-EXIT THRU-target as the exit (the SQ133A-family).</item>
    /// <item><b>Terminating</b> (<see cref="LoweringContext.TerminatingParagraphs"/>): contains a top-level
    /// STOP RUN, EXIT PROGRAM, or GOBACK. A terminating paragraph that FOLLOWS the section's last exit-point
    /// paragraph marks a termination tail the USE procedure must not fall through into (the SQ212A bug).</item>
    /// </list>
    /// </summary>
    private void ScanDeclarativeControlPoints(BoundProgram boundProgram)
    {
        foreach (var para in boundProgram.Paragraphs)
        {
            bool allExit = true;   // vacuously true for an empty paragraph
            bool terminates = false;
            foreach (var sentence in para.Sentences)
                foreach (var stmt in sentence.Statements)
                {
                    if (stmt is not BoundExitStatement) allExit = false;
                    if (stmt is BoundStopStatement or BoundExitProgramStatement or BoundGoBackStatement)
                        terminates = true;
                }
            if (allExit)
                _ctx.ExitPointParagraphs.Add(para.Symbol.Name);
            if (terminates)
                _ctx.TerminatingParagraphs.Add(para.Symbol.Name);
        }
    }

    private void LowerAllParagraphs(BoundProgram boundProgram)
    {
        foreach (var para in boundProgram.Paragraphs)
        {
            // Use symbol-based lookup to correctly handle duplicate paragraph names
            // in different sections. Fall back to name-based for compatibility.
            if (!_ctx.ParagraphSymbolMethods.TryGetValue(para.Symbol, out var method)
                && !_ctx.ParagraphMethods.TryGetValue(para.Symbol.Name, out method))
                continue;

            if (!_ctx.ParagraphSymbolIndices.TryGetValue(para.Symbol, out int myIndex))
                myIndex = _ctx.ParagraphIndices.GetValueOrDefault(para.Symbol.Name);
            var block = method.Blocks[0];
            _ctx.CurrentParagraphName = para.Symbol.Name;

            var paraEnd = method.CreateBlock($"{para.Symbol.Name}_exit");
            _ctx.ParagraphEndBlock = paraEnd;

            // EXIT SECTION target
            var sectionName = _semantic.GetParagraphSection(para.Symbol.Name);
            _ctx.SectionExitReturnIndex = null;
            if (sectionName != null)
            {
                var sectionParas = _semantic.GetSectionParagraphs(sectionName);
                if (sectionParas is { Count: > 0 }
                    && _ctx.ParagraphIndices.TryGetValue(sectionParas[^1], out var lastIdx))
                    _ctx.SectionExitReturnIndex = lastIdx + 1;
            }

            for (int si = 0; si < para.Sentences.Count; si++)
            {
                var sentenceEnd = new IrBasicBlock($"{para.Symbol.Name}_sent{si}_end");
                _ctx.CurrentSentenceEnd = sentenceEnd;

                foreach (var stmt in para.Sentences[si].Statements)
                    block = LowerStatement(stmt, method, block);

                block.Instructions.Add(new IrJump(sentenceEnd));
                method.Blocks.Add(sentenceEnd);
                block = sentenceEnd;
            }

            _ctx.CurrentSentenceEnd = null;
            _ctx.ParagraphEndBlock = null;
            _ctx.SectionExitReturnIndex = null;
            _ctx.CurrentParagraphName = null;

            block.Instructions.Add(new IrJump(paraEnd));
            method.Blocks.Add(paraEnd);
            paraEnd.Instructions.Add(new IrReturnConst(myIndex + 1));
        }
    }

    private void PopulateModuleMetadata(IrModule module, BoundProgram boundProgram)
    {
        module.AlterDefaults.AddRange(_ctx.AlterDefaults);
        module.IsInitial = _semantic.Program.IsInitial;

        foreach (var param in _semantic.ProcedureUsingParameters)
            module.UsingParameterNames.Add(param.Name);

        // The PROCEDURE DIVISION RETURNING item is passed by the caller as a trailing BY-REFERENCE argument
        // (CilEmitter.EmitCallProgram appends it at args[usingCount]), so it is the LAST linkage parameter: it
        // needs a _linkage_<name> field and an args[usingCount] → field mapping in the Entry method exactly like a
        // USING parameter. (Its access inside the callee is resolved by CilLocationEmitter.FindLinkageField, which
        // also consults the RETURNING item.) Appending it here — after the USING names — keeps the arg index
        // aligned with the caller's push order. It is intentionally NOT added to ProcedureUsingParameters, so the
        // USING-only validation (CBL3108) and any arity logic that reads that list are unaffected.
        if (_semantic.ProcedureReturningItem is { } ret)
            module.UsingParameterNames.Add(ret.Name);

        foreach (var para in boundProgram.Paragraphs)
            foreach (var sentence in para.Sentences)
                foreach (var stmt in sentence.Statements)
                    if (stmt is BoundEntryStatement entry)
                        module.EntryPoints.Add((entry.EntryName, entry.UsingParameters));
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

    /// <summary>
    /// Data-model migration S3 (docs/RECORD_STRUCT_STORAGE_DESIGN.md): flip the narrowest typed subset — a
    /// standalone elementary alphanumeric/national/alphabetic WORKING-STORAGE item the classifier marks typed,
    /// with no OCCURS and no figurative/ALL VALUE — to a native .NET <see cref="string"/> field. Byte-backed
    /// items, group members, file/linkage/external, and everything the classifier demotes stay on the byte path
    /// (the §1.6 safety floor). No-op unless <c>EnableTypedFields</c> is on, so the corpus stays byte-identical.
    /// </summary>
    private void CollectTypedFields(IrModule module)
    {
        // OO (ADR §7) is an ALWAYS-ON consumer of the typed pipeline — a CLASS has no legacy byte-identical corpus
        // to preserve, so object data flips to typed-native .NET fields regardless of the global EnableTypedFields
        // migration gate (which still governs ordinary program units). Mirrors CollectPointerFields /
        // CollectObjectRefFields, which are likewise always-on.
        if ((!_options.EnableTypedFields && !module.IsClass) || _ctx.Classification is not { } classification)
            return;

        // First OO-typed slice: flip only CHARACTER object data for a class when the global flag is off (numeric /
        // record-struct / OCCURS object data follow in later OO-typed slices — they additionally need the typed
        // numeric materialize + arithmetic sites made per-instance). This restriction auto-relaxes once the global
        // EnableTypedFields flip lands (then a class gets the full typed treatment like any program).
        bool classCharOnly = module.IsClass && !_options.EnableTypedFields;

        // An elementary item whose typed form is a homogeneous .NET string: classifier-typed, WORKING-STORAGE,
        // alphanumeric/national/alphabetic, no OCCURS, no figurative/ALL VALUE (those byte-back the init).
        bool IsTypedChar(DataSymbol s)
        {
            if (!s.IsElementary || s.Occurs != null || s.FigurativeInit != null || s.AllLiteralPattern != null)
                return false;
            if (s.Area != Semantics.StorageAreaKind.WorkingStorage || !classification.IsTyped(s))
                return false;
            return _semantic.GetStorageLocation(s) is { } l && l.Pic.Category
                is Runtime.CobolCategory.Alphanumeric or Runtime.CobolCategory.National
                or Runtime.CobolCategory.Alphabetic;
        }

        int WidthOf(DataSymbol s) => _semantic.GetStorageLocation(s)!.Value.Length;
        string InitOf(DataSymbol s, int width) => s.InitialValue is { } v
            ? (v.Length >= width ? v[..width] : v.PadRight(width)) : new string(' ', width);

        // S4: a standalone elementary UNSIGNED INTEGER item with a VALUE → a typed `long`. Restricted to the
        // narrowest numeric slice (no sign / V / P, ≤18 digits, has VALUE so init is defined — an uninitialized
        // numeric field shows spaces on the byte path, which a `long` cannot reproduce). USAGE may be DISPLAY,
        // COMP, or BINARY: all three store the value truncated to the PICTURE digit count (% 10^digits) — verified
        // empirically (DEVLOG 416) — so the long model is byte-identical. COMP-5 (native binary, NO picture
        // truncation), COMP-1/COMP-2 (float), and packed (COMP-3) are deliberately excluded — different semantics.
        // The out values are the digit count and the COBOL-correct truncated initial value.
        // S4 core: classify a NUMERIC pic + VALUE into a typed representation — `long` (unsigned integer) or
        // `decimal` (signed/scaled), with the COBOL-correct stored init. Usage DISPLAY/COMP/BINARY only (COMP-5
        // native-binary / COMP-1/2 float / packed excluded, DEVLOG 416), ≤18 digits, VALUE required (an
        // uninitialized numeric shows spaces on the byte path, which a long/decimal cannot reproduce). The decimal
        // init is the EXACT stored value — round-tripped through the byte codec (Encode→Decode) so it equals what
        // the byte field would hold (byte-identical by construction). Shared by the standalone-item predicates and
        // the OCCURS-element branch (which gate elementary/Occurs/area/classifier themselves). <paramref
        // name="byteLen"/> is the element/field byte storage length.
        bool ClassifyTypedNumeric(Runtime.PicDescriptor pic, int byteLen, string? valStr,
            out bool isDecimal, out long longInit, out decimal decInit)
        {
            isDecimal = false; longInit = 0; decInit = 0m;
            if (pic.Category != Runtime.CobolCategory.Numeric)
                return false;
            if (pic.Usage is not (Runtime.UsageKind.Display or Runtime.UsageKind.Comp or Runtime.UsageKind.Binary))
                return false;
            if (pic.TotalDigits is < 1 or > 18)
                return false;
            if (valStr is null
                || !decimal.TryParse(valStr, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out decimal v))
                return false;
            if (IR.IrTypedFieldLocation.IsDecimalRepresented(pic))   // signed/scaled → decimal
            {
                isDecimal = true;
                var scratch = new byte[byteLen];
                Runtime.PicRuntime.EncodeNumeric(scratch, 0, byteLen, pic, v);
                decInit = Runtime.PicRuntime.DecodeNumeric(scratch, 0, byteLen, pic);
                return true;
            }
            decimal mod = 1m;                                        // unsigned integer → long (low-n-digit truncation)
            for (int i = 0; i < pic.TotalDigits; i++) mod *= 10m;
            longInit = (long)(Math.Truncate(Math.Abs(v)) % mod);
            return true;
        }

        bool IsTypedUnsignedInteger(DataSymbol s, out int digits, out long init)
        {
            digits = 0; init = 0;
            if (!s.IsElementary || s.Occurs != null || s.FigurativeInit != null || s.AllLiteralPattern != null)
                return false;
            if (s.Area != Semantics.StorageAreaKind.WorkingStorage || !classification.IsTyped(s))
                return false;
            if (_semantic.GetStorageLocation(s) is not { } loc)
                return false;
            if (!ClassifyTypedNumeric(loc.Pic, loc.Length, s.InitialValue, out bool isDec, out long li, out _) || isDec)
                return false;   // long slice only (signed/scaled is IsTypedDecimal's job)
            digits = loc.Pic.TotalDigits;
            init = li;
            return true;
        }

        // S4: a standalone elementary SIGNED or SCALED numeric item with a VALUE → a typed `decimal` (the
        // signed-or-scaled complement of IsTypedUnsignedInteger).
        bool IsTypedDecimal(DataSymbol s, out decimal init)
        {
            init = 0m;
            if (!s.IsElementary || s.Occurs != null || s.FigurativeInit != null || s.AllLiteralPattern != null)
                return false;
            if (s.Area != Semantics.StorageAreaKind.WorkingStorage || !classification.IsTyped(s))
                return false;
            if (_semantic.GetStorageLocation(s) is not { } loc)
                return false;
            if (!ClassifyTypedNumeric(loc.Pic, loc.Length, s.InitialValue, out bool isDec, out _, out decimal di) || !isDec)
                return false;   // decimal slice only
            init = di;
            return true;
        }

        // S3b/S5: recursively build the typed record-struct for a (sub-)group, registering each leaf descendant in
        // TypedFieldRefs with its instance + the member path from the instance to the leaf's parent. Returns null
        // (the whole group stays byte) if ANY child is not flippable — an OCCURS item, an edited/byte-trigger item,
        // or a non-typed/empty sub-group. pathFromInstance is the live path stack; structName is this struct's name.
        IR.IrTypedRecordDef? BuildTypedRecord(DataSymbol group, string instanceName, string structName,
            List<string> pathFromInstance)
        {
            var members = new List<IR.IrTypedFieldDef>(group.Children.Count);
            foreach (var child in group.Children)
            {
                int width = WidthOf(child);
                if (IsTypedChar(child))
                {
                    members.Add(new IR.IrTypedFieldDef(child.Name, width, InitOf(child, width)));
                    _ctx.TypedFieldRefs[child] = (child.Name, width, instanceName, pathFromInstance.ToArray());
                }
                else if (IsTypedUnsignedInteger(child, out _, out long cnInit))
                {
                    members.Add(new IR.IrTypedFieldDef(child.Name, width, "", IsNumeric: true, NumericInit: cnInit));
                    _ctx.TypedFieldRefs[child] = (child.Name, width, instanceName, pathFromInstance.ToArray());
                }
                else if (IsTypedDecimal(child, out decimal cdInit))
                {
                    members.Add(new IR.IrTypedFieldDef(child.Name, width, "",
                        IsNumeric: true, IsDecimal: true, DecimalInit: cdInit));
                    _ctx.TypedFieldRefs[child] = (child.Name, width, instanceName, pathFromInstance.ToArray());
                }
                else if (child.IsGroup && child.Occurs == null && classification.IsTyped(child)
                         && child.Children.Count > 0)
                {
                    // S5: a flippable sub-group → a nested record struct (recurse). Leaves inside register their own
                    // (deeper) MemberPath; this member is the nested struct itself.
                    pathFromInstance.Add(child.Name);
                    var nested = BuildTypedRecord(child, instanceName, structName + "_" + child.Name, pathFromInstance);
                    pathFromInstance.RemoveAt(pathFromInstance.Count - 1);
                    if (nested is null)
                        return null;   // a descendant was not flippable → the whole group stays byte
                    members.Add(new IR.IrTypedFieldDef(child.Name, 0, "", Nested: nested));
                }
                else
                {
                    return null;   // OCCURS / edited / byte-trigger / empty sub-group → not flippable
                }
            }
            // InstanceName is set by the top-level caller (rec with { InstanceName = … }); nested sub-structs null.
            return new IR.IrTypedRecordDef(structName, null, members);
        }

        foreach (var sym in _semantic.DataItemsInOrder)
        {
            // S4: a fixed-OCCURS table over a flippable CHARACTER or NUMERIC element → a typed .NET array field
            // (string[] / long[] / decimal[]). Runs for table items at ANY level (OCCURS is illegal on 01, so a
            // table is always a child) — BEFORE the top-level-only skip below. Only element-accessed tables reach
            // here: a whole-table operand, or a whole reference to a containing group, demotes the item to byte
            // (RecordClassification / §9.3). Numeric elements require a VALUE (same defined-init rule as standalone
            // numerics — an uninitialized numeric shows spaces, which long/decimal can't reproduce); now byte-
            // identical because the byte engine initializes every occurrence to the VALUE (DEVLOG 424). DEPENDING ON
            // / group-element tables stay byte.
            if (!classCharOnly && sym.IsElementary && sym.Occurs is { DependingOnSymbol: null } occ && occ.MaxOccurs > 0
                && sym.Area == Semantics.StorageAreaKind.WorkingStorage && classification.IsTyped(sym)
                && _semantic.GetStorageLocation(sym) is { } aloc)
            {
                int elemWidth = sym.ElementSize > 0 ? sym.ElementSize : aloc.Length;
                IR.IrTypedFieldDef? elem = null;
                if (aloc.Pic.Category is Runtime.CobolCategory.Alphanumeric or Runtime.CobolCategory.National
                        or Runtime.CobolCategory.Alphabetic)
                    elem = new IR.IrTypedFieldDef(sym.Name, elemWidth, InitOf(sym, elemWidth));
                else if (ClassifyTypedNumeric(aloc.Pic, elemWidth, sym.InitialValue,
                             out bool isDec, out long li, out decimal di))
                    elem = isDec
                        ? new IR.IrTypedFieldDef(sym.Name, elemWidth, "", IsNumeric: true, IsDecimal: true, DecimalInit: di)
                        : new IR.IrTypedFieldDef(sym.Name, elemWidth, "", IsNumeric: true, NumericInit: li);

                if (elem is not null)
                {
                    string name = "_TA_" + sym.Name;
                    module.TypedArrayDefs.Add(new IR.IrTypedArrayDef(name, occ.MaxOccurs, elem));
                    _ctx.TypedArrayRefs[sym] = (name, elemWidth, occ.MaxOccurs);
                    continue;
                }
            }

            if (sym.Parent != null)
                continue;

            // S3a: a standalone elementary alphanumeric item → a flat typed `string` field.
            if (IsTypedChar(sym))
            {
                int width = WidthOf(sym);
                string name = "_T_" + sym.Name;
                module.TypedFieldDefs.Add(new IR.IrTypedFieldDef(name, width, InitOf(sym, width)));
                _ctx.TypedFieldRefs[sym] = (name, width, null, null);
                continue;
            }

            // S4: a standalone elementary unsigned-integer DISPLAY/COMP/BINARY item with a VALUE → a typed `long`
            // field. The field's Width is the BYTE storage width (loc.Length) — for COMP/BINARY that differs from
            // the digit count (PIC 9(5) COMP is 4 bytes); the digit count lives on the PicDescriptor.
            if (!classCharOnly && IsTypedUnsignedInteger(sym, out _, out long ninit))
            {
                int byteWidth = WidthOf(sym);
                string name = "_T_" + sym.Name;
                module.TypedFieldDefs.Add(new IR.IrTypedFieldDef(name, byteWidth, "", IsNumeric: true, NumericInit: ninit));
                _ctx.TypedFieldRefs[sym] = (name, byteWidth, null, null);
                continue;
            }

            // S4: a standalone elementary signed/scaled numeric item with a VALUE → a typed `decimal` field. Width
            // is the BYTE storage width; the scale/sign live on the PicDescriptor.
            if (!classCharOnly && IsTypedDecimal(sym, out decimal decInit))
            {
                int byteWidth = WidthOf(sym);
                string name = "_T_" + sym.Name;
                module.TypedFieldDefs.Add(new IR.IrTypedFieldDef(name, byteWidth, "",
                    IsNumeric: true, IsDecimal: true, DecimalInit: decInit));
                _ctx.TypedFieldRefs[sym] = (name, byteWidth, null, null);
                continue;
            }

            // S3b/S5: a `01` group → a .NET `record struct`. Each direct child is an elementary typed-flippable item
            // (char / unsigned-integer / signed-scaled) OR (S5) itself a flippable sub-group → a nested record
            // struct, built recursively. Leaf access is instance.[nested.]*member via the MemberPath. The whole
            // group flips only if EVERY descendant is flippable (no OCCURS, no edited/byte-trigger item); otherwise
            // it stays byte. Member-level access only — a whole-group operand is handled by the existing classifier
            // group-MOVE demotion. (A contained OCCURS table flips independently via the S4 array branch.)
            if (!classCharOnly && sym.Area == Semantics.StorageAreaKind.WorkingStorage && sym.IsGroup && sym.Occurs == null
                && classification.IsTyped(sym) && sym.Children.Count > 0)
            {
                string instanceName = "_TI_" + sym.Name;
                var rec = BuildTypedRecord(sym, instanceName, "_TS_" + sym.Name, new List<string>());
                if (rec is not null)
                    module.TypedRecordDefs.Add(rec with { InstanceName = instanceName });
            }
        }
    }

    /// <summary>
    /// Stage-4 pointers (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10): register one <c>static ManagedPointer
    /// _PTR_&lt;name&gt;</c> field for every <c>USAGE POINTER</c> elementary item and every <c>BASED</c> item. A
    /// pointer is ALWAYS a managed reference — there is no 8-byte byte handle (DEVLOG 431) — so this pass is
    /// always-on, NOT gated by <c>EnableTypedFields</c>. The pointer's owner (a <c>USAGE POINTER</c> WS item) no
    /// longer occupies WORKING-STORAGE bytes (StorageLayoutComputer skips it); a <c>BASED</c> item already has no
    /// storage (slice 1a). Default <c>ManagedPointer</c> (Buffer null) IS the COBOL NULL initial state, so no
    /// explicit init is emitted (ADR §1.7 exception). The only corpus consumer is
    /// <c>tests/conformance/2002/pointer_data.cob</c>.
    /// </summary>
    private void CollectPointerFields(IrModule module)
    {
        foreach (var sym in _semantic.DataItemsInOrder)
        {
            bool isPointerItem = sym.Usage == UsageKind.Pointer && sym.IsElementary;
            if (!isPointerItem && !sym.IsBased)
                continue;
            string name = "_PTR_" + sym.Name;
            module.PointerFieldDefs.Add(new IR.IrPointerFieldDef(name));
            _ctx.PointerFieldRefs[sym] = name;
        }
    }

    /// <summary>
    /// OO (docs/OO_IMPLEMENTATION_DESIGN.md §E): register one <c>static &lt;class&gt; _OBJ_&lt;name&gt;</c> field for
    /// every elementary <c>USAGE OBJECT REFERENCE</c> item. An object reference is ALWAYS a managed .NET reference
    /// (the object identity), never bytes — so, like the pointer pass, this is always-on (NOT gated by
    /// <c>EnableTypedFields</c>); the item occupies no WORKING-STORAGE (StorageLayoutComputer skips it). Default
    /// (null) IS the COBOL initial NULL, so no explicit init is emitted.
    /// </summary>
    private void CollectObjectRefFields(IrModule module)
    {
        foreach (var sym in _semantic.DataItemsInOrder)
        {
            if (sym.Usage != UsageKind.Object || !sym.IsElementary)
                continue;
            string name = "_OBJ_" + sym.Name;
            module.ObjectRefFieldDefs.Add(new IR.IrObjectRefFieldDef(name, sym.ObjectClassName));
            _ctx.ObjectRefFieldRefs[sym] = name;
        }
    }

    /// <summary>
    /// Stage-4 pointers: lower a <see cref="BoundSetPointerStatement"/> to an <see cref="IrPointerStore"/> against
    /// the target item's <c>_PTR_</c> field (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10).
    /// </summary>
    /// <summary>Lower INVOKE (OO, slice 1) → <see cref="IR.IrInvoke"/>. NEW resolves the constructed class by name
    /// and its RETURNING object-reference field; an instance call resolves the receiver's <c>_OBJ_</c> field and its
    /// declared class for dispatch. (docs/OO_IMPLEMENTATION_DESIGN.md §C/§E.)</summary>
    private void LowerInvoke(BoundInvokeStatement inv, IrBasicBlock block)
    {
        if (inv.IsNew)
        {
            // NEW: the RETURNING item is the object-reference field that receives the new instance.
            string? newReturningField = inv.Returning != null
                && _ctx.ObjectRefFieldRefs.TryGetValue(inv.Returning, out var rf) ? rf : null;
            block.Instructions.Add(new IR.IrInvoke(isNew: true, className: inv.ClassName,
                receiverField: null, receiverClassName: null, methodName: inv.MethodName,
                returningField: newReturningField));
            return;
        }

        // Instance call (incl. SUPER): resolve the receiver field (null for SUPER — receiver is `this`) + USING arg
        // locations + the RETURNING receiver location (marshalled into the ManagedPointer[] ABI by EmitInvoke; the
        // trailing element is RETURNING).
        string? receiverField = inv.TargetObject != null
            && _ctx.ObjectRefFieldRefs.TryGetValue(inv.TargetObject, out var rcv) ? rcv : null;
        var argLocations = new List<IR.IrLocation>();
        foreach (var arg in inv.Args)
            if (_ctx.Location.ResolveExpressionLocation(arg) is { } loc)
                argLocations.Add(loc);
        IR.IrLocation? returningLocation = inv.Returning != null
            ? _ctx.Location.ResolveLocation(inv.Returning) : null;
        block.Instructions.Add(new IR.IrInvoke(isNew: false, className: null, receiverField: receiverField,
            receiverClassName: inv.TargetClassName, methodName: inv.MethodName, returningField: null,
            argLocations: argLocations, returningLocation: returningLocation, isSuper: inv.IsSuper));
    }

    private void LowerSetPointer(BoundSetPointerStatement stmt, IrBasicBlock block)
    {
        if (!_ctx.PointerFieldRefs.TryGetValue(stmt.TargetPointer, out var targetField))
            return;   // target is not a registered pointer/based item — nothing to store

        switch (stmt.SourceKind)
        {
            case PointerSetSourceKind.Null:
                block.Instructions.Add(new IrPointerStore(targetField, PointerStoreKind.Null));
                break;

            case PointerSetSourceKind.FromPointer:
                if (stmt.SourcePointer is { } srcSym
                    && _ctx.PointerFieldRefs.TryGetValue(srcSym, out var srcField))
                    block.Instructions.Add(new IrPointerStore(targetField, PointerStoreKind.FromPointer, srcField));
                break;

            case PointerSetSourceKind.FromAddressOf:
                if (stmt.AddressOfItem is { } addrItem
                    && _ctx.Location.ResolveExpressionLocation(addrItem) is { } addrLoc)
                    block.Instructions.Add(new IrPointerStore(targetField, PointerStoreKind.FromAddressOf,
                        addressOfSource: addrLoc));
                break;
        }
    }

    /// <summary>
    /// Stage-4 pointer arithmetic: lower <c>SET p UP|DOWN BY n</c> to an <see cref="IrPointerAdjust"/> on p's
    /// <c>_PTR_</c> field (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10.4, ISO §14.9.39 Format 10).
    /// </summary>
    private void LowerPointerArith(BoundPointerArithStatement stmt, IrBasicBlock block)
    {
        if (!_ctx.PointerFieldRefs.TryGetValue(stmt.Pointer, out var ptrField))
            return;
        var delta = _ctx.Expression.LowerExpression(stmt.Delta) ?? new IR.IrLiteral(0m);
        block.Instructions.Add(new IrPointerAdjust(ptrField, delta, stmt.IsUp));
    }

    /// <summary>
    /// Stage-4 pointers: lower ALLOCATE (ISO §14.9.3) to an <see cref="IrAllocate"/>. Form 1 lowers the byte-count
    /// expression; form 2 uses the BASED item's byte size and sets its <c>_PTR_</c> field. RETURNING, if present,
    /// also receives the address. (docs/RECORD_STRUCT_STORAGE_DESIGN.md §10.4)
    /// </summary>
    private void LowerAllocate(BoundAllocateStatement stmt, IrBasicBlock block)
    {
        string? returningField =
            stmt.ReturningPointer is { } rp && _ctx.PointerFieldRefs.TryGetValue(rp, out var rf) ? rf : null;

        if (stmt.BasedItem is { } based)
        {
            if (!_ctx.PointerFieldRefs.TryGetValue(based, out var basedField))
                return;
            int size = based.ElementSize > 0
                ? based.ElementSize
                : Semantics.FieldSizeCalculator.ComputeElementSize(based);
            block.Instructions.Add(new IrAllocate(null, size, basedField, returningField));
        }
        else if (stmt.SizeExpr is { } sizeExpr)
        {
            var size = _ctx.Expression.LowerExpression(sizeExpr) ?? new IR.IrLiteral(0m);
            block.Instructions.Add(new IrAllocate(size, 0, basedPtrField: null, returningField));
        }
    }

    // ── Entry point ──

    private void CreateEntryPoint(IrModule module, BoundProgram boundProgram)
    {
        var main = new IrMethod("Main", returnType: IrPrimitiveType.Void);
        var mainBlock = new IrBasicBlock("main_entry");

        // Initialize the file manager. This belongs ONLY to the run-unit main's Main (the assembly
        // entry point): Init disposes the prior CobolFileManager and allocates a fresh one, which
        // would close every file the CALLING program left open. A CALLed subprogram is NEVER entered
        // through Main (it is entered via Entry from CobolProgramRegistry), so its file connectors are
        // registered by RegisterFiles, which is called from Entry once per activation — see
        // CilEmitter.EmitEntryMethodBody. (ISO §14.6 — a called program's internal file connectors are
        // established when the program is activated.)
        mainBlock.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.Init", Array.Empty<IrValue>()));

        // Per-file connector registration lives in its own parameterless RegisterFiles method (not in
        // Main), so that Entry — the path EVERY activation takes, including a CALLed subprogram's —
        // can register this program's files without re-running FileRuntime.Init. Main reaches it via
        // Self.Entry below; CilEmitter guards the RegisterFiles call with a per-program _filesRegistered
        // flag so it runs once per activation and subsequent CALLs preserve the open file/position.
        var regFiles = new IrMethod("RegisterFiles", returnType: IrPrimitiveType.Void);
        var block = new IrBasicBlock("register_files");

        // Register file handlers at startup for each SELECT (skip SD sort-merge files)
        foreach (var fileSym in _semantic.Symbols.Program.GlobalScope.GetAllSymbols<FileSymbol>())
        {
            // Sort-merge files (SD) don't have physical handlers
            if (fileSym.IsSortMerge) continue;

            // Resolve external path: a literal ASSIGN target ("TFIL1", "TF002") is an explicit,
            // possibly-shared physical name (e.g. NIST producer/consumer files) and is used as-is.
            // A non-literal target (an implementor-name / identifier, like NIST's bare XXXXX014)
            // names a file PRIVATE to this program; qualify it with the program-id so two programs
            // that happen to use the same SELECT name (e.g. SQ130A and SQ156A both SELECT SQ-FS1)
            // do not collide on one host file — which would let one program's leftover file defeat
            // another's "file is absent" semantics.
            string externalName = (fileSym.AssignIsLiteral && fileSym.AssignTarget != null)
                ? fileSym.AssignTarget
                : $"{boundProgram.Program.Name}-{fileSym.Name}";
            string externalPath = FileRuntime.ResolveHostPath(externalName);

            int recordLength = fileSym.RecordLength;
            if (recordLength == 0 && fileSym.Record != null)
            {
                if (_semantic.IsVariableLengthRecord(fileSym))
                {
                    // Variable-length records (RECORD VARYING, Format-2 RECORD CONTAINS m TO n, or multiple
                    // differently-sized 01s): the handler's slot/buffer must hold the MAXIMUM record, not the
                    // first 01 (which may be the minimum — RL-FR6's 56-byte 6A precedes its 102-byte 6B), or
                    // LONG records get truncated to the min (ISO §13.18.43). Per-slot/per-record length
                    // framing then recovers each record's actual length on READ.
                    recordLength = _semantic.MaxRecordLength(fileSym);
                }
                else
                {
                    var recLoc = _semantic.GetStorageLocation(fileSym.Record);
                    if (recLoc.HasValue)
                        recordLength = recLoc.Value.Length;
                }
            }
            if (recordLength == 0) recordLength = 132; // Default for print files

            // Per ISO §9.1.2 / §12.4.5.2: ORGANIZATION SEQUENTIAL (and the unspecified default) is RECORD
            // sequential — fixed-length records stored contiguously, no line delimiters — so REWRITE can
            // replace a record in place and READ/REWRITE are length-consistent (a line-sequential file
            // cannot, because trimming + newline framing makes records variable on disk).
            //
            // A line-RENDERED host representation (trimmed text, one line per record) is used for:
            //   • ORGANIZATION LINE SEQUENTIAL (the explicit text organization), and
            //   • printer/report files — those written with WRITE … ADVANCING (§14.9.51 vertical page
            //     positioning) or declared with a LINAGE clause (§13.18.30 logical page). These are the
            //     spec's printer-file features; real implementations key the same decision off the
            //     ASSIGN device (IBM SYSOUT, MF PRINTER), which the NIST suite encodes as XXXXX055. A
            //     printer file's records are page lines, never read back as binary records.
            // Everything else is record-sequential binary.
            bool lineSequential = fileSym.Organization == "LINE SEQUENTIAL"
                || fileSym.WrittenWithAdvancing
                || fileSym.LinageBody > 0;

            string org = fileSym.Organization ?? "SEQUENTIAL";
            int keyOffset = 0, keyLength = 0;

            // For INDEXED files, resolve RECORD KEY to get offset/length
            if (org == "INDEXED" && fileSym.RecordKey != null)
            {
                var keySym = _semantic.ResolveKeyData(fileSym, -1);
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
            // For RELATIVE files, carry the RELATIVE KEY data item's digit capacity in keyLength so
            // the runtime can raise boundary/overflow status when a relative record number exceeds it
            // (ISO §9.1.13.4). Relative files have no record-embedded key offset/length.
            else if (org == "RELATIVE" && fileSym.RelativeKey != null)
            {
                var keySym = _semantic.ResolveData(fileSym.RelativeKey);
                if (keySym != null)
                {
                    var keyLoc = _semantic.GetStorageLocation(keySym);
                    if (keyLoc.HasValue)
                        keyLength = keyLoc.Value.Pic.TotalDigits;
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

            // A record-sequential file with variable-length records (RECORD IS VARYING or multiple 01
            // sizes) stores each record length-framed so lengths round-trip without line delimiters
            // (ISO §13.18.43 — the length-determination method is implementor-defined). Line-sequential
            // files frame by newline instead, so the flag only applies to record-sequential.
            if (!lineSequential && org == "SEQUENTIAL" && _semantic.IsVariableLengthSequential(fileSym))
            {
                var seqVarNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var seqVarVal = _valueFactory.Next(IrPrimitiveType.Bool);
                // Convey the RECORD IS VARYING size bounds (min..max) so the runtime can enforce the
                // ISO §9.1.13 status-44 boundary check on a variable WRITE (a record longer than the
                // largest or shorter than the smallest permitted). min = 0 ⇒ no lower bound.
                var seqVarMinVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var seqVarMaxVal = _valueFactory.Next(IrPrimitiveType.Int32);
                block.Instructions.Add(new IrLoadConst(seqVarNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(seqVarVal, true));
                block.Instructions.Add(new IrLoadConst(seqVarMinVal, fileSym.RecordVaryingMin));
                block.Instructions.Add(new IrLoadConst(seqVarMaxVal, _semantic.MaxRecordLength(fileSym)));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetSequentialVarying",
                    new[] { seqVarNameVal, seqVarVal, seqVarMinVal, seqVarMaxVal }));
            }

            // INDEXED access mode: SEQUENTIAL (or unspecified — the indexed default) deletes/rewrites the
            // current (last-read) record and requires a preceding successful READ (43 if not); RANDOM/DYNAMIC
            // delete/rewrite the record identified by the primary key with no prior read (ISO §9.1.13.6).
            if (org == "INDEXED")
            {
                bool ixSequential = fileSym.AccessMode is null or "SEQUENTIAL";
                var ixNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var ixSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
                block.Instructions.Add(new IrLoadConst(ixNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(ixSeqVal, ixSequential));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetIndexedAccess",
                    new[] { ixNameVal, ixSeqVal }));

                // Variable-length records (RECORD IS VARYING or multiple 01 sizes): each record carries its
                // own length, stored length-framed so a SHORT vs LONG record round-trips with the right
                // length (ISO §13.18.43). Must agree with FileIoLowerer.IsVaryingRecord (INDEXED branch).
                if (fileSym.IsRecordVarying || _semantic.HasMultipleRecordSizes(fileSym))
                {
                    var ixvNameVal = _valueFactory.Next(IrPrimitiveType.String);
                    var ixvVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    // RECORD IS VARYING size bounds (min..max) for the ISO §9.1.13 status-44 WRITE
                    // boundary check; min = 0 ⇒ no lower bound (e.g. multiple-01 sizes, no FROM).
                    var ixvMinVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var ixvMaxVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    block.Instructions.Add(new IrLoadConst(ixvNameVal, fileSym.Name));
                    block.Instructions.Add(new IrLoadConst(ixvVal, true));
                    block.Instructions.Add(new IrLoadConst(ixvMinVal, fileSym.RecordVaryingMin));
                    block.Instructions.Add(new IrLoadConst(ixvMaxVal, _semantic.MaxRecordLength(fileSym)));
                    block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetIndexedVarying",
                        new[] { ixvNameVal, ixvVal, ixvMinVal, ixvMaxVal }));
                }
            }

            // Register alternate keys for INDEXED files
            if (org == "INDEXED")
            {
                for (int ak = 0; ak < fileSym.AlternateKeys.Count; ak++)
                {
                    var altKey = fileSym.AlternateKeys[ak];
                    var altKeySym = _semantic.ResolveKeyData(fileSym, ak);
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

            // SELECT OPTIONAL
            if (fileSym.IsOptional)
            {
                var optNameVal = _valueFactory.Next(IrPrimitiveType.String);
                block.Instructions.Add(new IrLoadConst(optNameVal, fileSym.Name));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetFileOptional",
                    new[] { optNameVal }));
            }

            // LINAGE clause
            if (fileSym.LinageBody > 0)
            {
                var linNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var linBodyVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var linFootVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var linTopVal = _valueFactory.Next(IrPrimitiveType.Int32);
                var linBotVal = _valueFactory.Next(IrPrimitiveType.Int32);
                block.Instructions.Add(new IrLoadConst(linNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(linBodyVal, fileSym.LinageBody));
                block.Instructions.Add(new IrLoadConst(linFootVal, fileSym.LinageFooting));
                block.Instructions.Add(new IrLoadConst(linTopVal, fileSym.LinageTop));
                block.Instructions.Add(new IrLoadConst(linBotVal, fileSym.LinageBottom));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetFileLinage",
                    new[] { linNameVal, linBodyVal, linFootVal, linTopVal, linBotVal }));
            }

            // RELATIVE access mode: RANDOM/DYNAMIC position WRITE/REWRITE/DELETE by the RELATIVE KEY;
            // SEQUENTIAL (or unspecified) appends on WRITE and uses the current record.
            if (org == "RELATIVE")
            {
                bool sequential = fileSym.AccessMode is null or "SEQUENTIAL";
                var relNameVal = _valueFactory.Next(IrPrimitiveType.String);
                var relSeqVal = _valueFactory.Next(IrPrimitiveType.Bool);
                block.Instructions.Add(new IrLoadConst(relNameVal, fileSym.Name));
                block.Instructions.Add(new IrLoadConst(relSeqVal, sequential));
                block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetRelativeAccess",
                    new[] { relNameVal, relSeqVal }));

                // RECORD IS VARYING on a relative file: each slot stores its own length (persisted as a
                // length prefix). Must agree with FileIoLowerer.IsVaryingRecord (relative → explicit clause).
                if (fileSym.IsRecordVarying)
                {
                    var relVarNameVal = _valueFactory.Next(IrPrimitiveType.String);
                    var relVarVal = _valueFactory.Next(IrPrimitiveType.Bool);
                    // RECORD IS VARYING size bounds (min..max) for the ISO §9.1.13 status-44 WRITE
                    // boundary check; min = 0 ⇒ no lower bound.
                    var relVarMinVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    var relVarMaxVal = _valueFactory.Next(IrPrimitiveType.Int32);
                    block.Instructions.Add(new IrLoadConst(relVarNameVal, fileSym.Name));
                    block.Instructions.Add(new IrLoadConst(relVarVal, true));
                    block.Instructions.Add(new IrLoadConst(relVarMinVal, fileSym.RecordVaryingMin));
                    block.Instructions.Add(new IrLoadConst(relVarMaxVal, _semantic.MaxRecordLength(fileSym)));
                    block.Instructions.Add(new IrRuntimeCall(null, "FileRuntime.SetRelativeVarying",
                        new[] { relVarNameVal, relVarVal, relVarMinVal, relVarMaxVal }));
                }
            }
        }

        // Collect paragraph methods for the Entry dispatch in paragraph-index order — INCLUDING
        // declaratives — so ParagraphDispatchOrder[pc] resolves the paragraph whose index is pc.
        // Every pc value (fall-through myIndex+1, GO TO, PERFORM THRU, GO TO DEPENDING) is in this
        // declarative-inclusive index space; excluding declaratives here previously left the switch
        // off by the number of leading DECLARATIVES paragraphs, so any program with declaratives
        // dispatched to the wrong paragraph and looped forever (DEVLOG: SQ105A hang).
        //
        // Declaratives are only ENTERED via PERFORM from the USE handler (EmitPerformDeclarativeSection,
        // which emits IrPerform / IrPerformThru — direct calls, not this switch), so the main loop
        // never lands on a declarative index: it starts at EntryParagraphIndex (the first non-
        // declarative paragraph) and main-flow control never targets a declarative.
        // Symbol-based lookup so each list position holds THIS paragraph's own method — duplicate
        // paragraph names in different sections each get their own entry (a name-based lookup would
        // put the last-defined duplicate's method at every same-name position). This must agree with
        // GO TO / GO TO DEPENDING / fall-through, which all resolve the bound ParagraphSymbol's index
        // (ControlFlowLowerer.TryResolveParagraphIndex, ParagraphSymbolIndices). Name fallback for
        // safety; a paragraph always resolves, so every paragraph is added and list position == index.
        int firstNonDeclarative = -1;
        int idx = 0;
        foreach (var para in boundProgram.Paragraphs)
        {
            if (firstNonDeclarative < 0 && !para.IsDeclarative)
                firstNonDeclarative = idx;
            if (_ctx.ParagraphSymbolMethods.TryGetValue(para.Symbol, out var m)
                || _ctx.ParagraphMethods.TryGetValue(para.Symbol.Name, out m))
                module.ParagraphDispatchOrder.Add(m);
            idx++;
        }
        module.EntryParagraphIndex = firstNonDeclarative < 0 ? 0 : firstNonDeclarative;

        // Finalize the RegisterFiles method (the per-file loop above filled `block` = register_files).
        // CilEmitter calls it from Entry, guarded by the per-program _filesRegistered flag.
        regFiles.Blocks.Add(block);
        module.Methods.Add(regFiles);
        module.RegisterFilesMethod = regFiles;

        // GLOBAL USE declaratives this program exposes to its contained programs (ISO §14.9.49.4 GR4):
        // resolve each declarative section to its inclusive paragraph-index range so CilEmitter can emit
        // and register a cross-program handler that runs the section via the shared Dispatch helper.
        foreach (var g in _semantic.GlobalUseDeclaratives)
        {
            var paras = _semantic.GetSectionParagraphs(g.SectionName);
            if (paras is not { Count: > 0 }) continue;
            if (!_ctx.ParagraphIndices.TryGetValue(paras[0], out int start)) continue;
            if (!_ctx.ParagraphIndices.TryGetValue(paras[^1], out int end)) continue;
            module.GlobalUseHandlers.Add((g.Scope, g.FileName, start, end));
        }

        // Main calls Entry(Array.Empty<ManagedPointer>()) — dispatch loop is in Entry
        mainBlock.Instructions.Add(new IrRuntimeCall(null, "Self.Entry", Array.Empty<IrValue>()));

        main.Blocks.Add(mainBlock);
        module.Methods.Insert(0, main);
    }

    // ── Statement dispatch — routes directly to lowerers via _ctx ──

    private IrBasicBlock LowerStatement(BoundStatement stmt, IrMethod method, IrBasicBlock block)
    {
        switch (stmt)
        {
            case BoundCompoundStatement compound:
                foreach (var s in compound.Statements)
                    block = LowerStatement(s, method, block);
                return block;

            // ── Inline (stays in Binder) ──
            case BoundDisplayStatement disp:
                LowerDisplay(disp, block);
                break;
            case BoundAcceptStatement acc:
                LowerAccept(acc, block);
                break;
            case BoundCallStatement call:
                return LowerCall(call, method, block);
            case BoundInvokeStatement inv:
                LowerInvoke(inv, block);
                break;
            case BoundCancelStatement cancel:
                foreach (var target in cancel.Targets)
                {
                    IrLocation? targetLoc = null;
                    if (target.IsDynamic)
                    {
                        // CANCEL identifier: read the program-name from the data item at runtime.
                        var targetSym = _semantic.ResolveData(target.Name);
                        if (targetSym != null)
                            targetLoc = _ctx.Location.ResolveLocation(targetSym);
                    }
                    block.Instructions.Add(new IrCancelProgram(target.Name, target.IsDynamic, targetLoc));
                }
                break;
            case BoundStopStatement:
                block.Instructions.Add(new IrStopRun());
                break;
            case BoundExitProgramStatement:
                block.Instructions.Add(new IrExitProgram());
                break;
            case BoundGoBackStatement gb:
                // GOBACK RETURNING x (ISO §14.9.16) ≡ MOVE x INTO the PROCEDURE DIVISION RETURNING item,
                // then return. The CALL … RETURNING wiring (DEVLOG 365) carries it to the caller.
                if (gb.Returning != null && _semantic.ProcedureReturningItem is { } retItem)
                {
                    var dest = new BoundIdentifierExpression(
                        retItem, retItem.ResolvedType?.Category ?? CobolCategory.Unknown);
                    _ctx.DataMovement.LowerMove(
                        new BoundMoveStatement(gb.Returning, new[] { dest }, isRounded: false), block);
                }
                block.Instructions.Add(new IrGoBack());
                break;
            case BoundEntryStatement:
            case BoundExitStatement:
            case BoundUseStatement:
                break;
            case BoundSetSwitchStatement setSwitch:
                foreach (var (implName, setToOn) in setSwitch.Switches)
                    block.Instructions.Add(new IrSetSwitch(implName, setToOn));
                break;

            // ── Data movement → _ctx.DataMovement ──
            case BoundMoveStatement mv:
                _ctx.DataMovement.LowerMove(mv, block);
                break;
            case BoundCorrespondingStatement corr:
                return _ctx.DataMovement.LowerCorresponding(corr, method, block);
            case BoundInitializeStatement init:
                _ctx.DataMovement.LowerInitialize(init, block);
                break;
            case BoundSetConditionStatement setCond:
                _ctx.DataMovement.LowerSetCondition(setCond, block);
                break;
            case BoundSetIndexStatement setIdx:
                _ctx.DataMovement.LowerSetIndex(setIdx, block);
                break;
            case BoundSetPointerStatement setPtr:
                LowerSetPointer(setPtr, block);
                break;
            case BoundPointerArithStatement ptrArith:
                LowerPointerArith(ptrArith, block);
                break;
            case BoundAllocateStatement alloc:
                LowerAllocate(alloc, block);
                break;

            // ── Arithmetic → _ctx.Arithmetic ──
            case BoundArithmeticStatement arith:
                return _ctx.Arithmetic.LowerArithmetic(arith, method, block);

            // ── Control flow → _ctx.ControlFlow ──
            case BoundPerformStatement perf:
                return _ctx.ControlFlow.LowerPerform(perf, method, block);
            case BoundIfStatement iff:
                return _ctx.ControlFlow.LowerIf(iff, method, block);
            case BoundEvaluateStatement eval:
                return _ctx.ControlFlow.LowerEvaluate(eval, method, block);
            case BoundGoToStatement gt:
                _ctx.ControlFlow.LowerGoTo(gt, block);
                break;
            case BoundAlterStatement alter:
                _ctx.ControlFlow.LowerAlter(alter, block);
                break;
            case BoundExitPerformStatement exitPerf:
                return _ctx.ControlFlow.LowerExitPerform(exitPerf, method, block);
            case BoundExitParagraphStatement:
                return _ctx.ControlFlow.LowerExitParagraph(method, block);
            case BoundExitSectionStatement:
                return _ctx.ControlFlow.LowerExitSection(method, block);
            case BoundNextSentenceStatement:
                return _ctx.ControlFlow.LowerNextSentence(method, block);
            case BoundSearchStatement search:
                return _ctx.ControlFlow.LowerSearch(search, method, block);
            case BoundSearchAllStatement searchAll:
                return _ctx.ControlFlow.LowerSearchAll(searchAll, method, block);

            // ── File I/O → _ctx.FileIo ──
            case BoundWriteStatement wr:
                return _ctx.FileIo.LowerWrite(wr, method, block);
            case BoundOpenStatement open:
                return _ctx.FileIo.LowerOpen(open, method, block);
            case BoundCloseStatement close:
                return _ctx.FileIo.LowerClose(close, method, block);
            case BoundInitiateStatement initRpt:
                return _ctx.FileIo.LowerInitiate(initRpt, method, block);
            case BoundGenerateStatement genRpt:
                return _ctx.FileIo.LowerGenerate(genRpt, method, block);
            case BoundTerminateStatement termRpt:
                return _ctx.FileIo.LowerTerminate(termRpt, method, block);
            case BoundReadStatement read:
                return _ctx.FileIo.LowerRead(read, method, block);
            case BoundRewriteStatement rw:
                return _ctx.FileIo.LowerRewrite(rw, method, block);
            case BoundDeleteStatement del:
                return _ctx.FileIo.LowerDelete(del, method, block);
            case BoundDeleteFileStatement delFile:
                return _ctx.FileIo.LowerDeleteFile(delFile, method, block);
            case BoundStartStatement start:
                return _ctx.FileIo.LowerStart(start, method, block);
            case BoundReturnStatement ret:
                return _ctx.FileIo.LowerReturn(ret, method, block);
            case BoundSortStatement sort:
                return _ctx.FileIo.LowerSort(sort, method, block);
            case BoundTableSortStatement tableSort:
                return _ctx.FileIo.LowerTableSort(tableSort, method, block);
            case BoundMergeStatement merge:
                return _ctx.FileIo.LowerMerge(merge, method, block);
            case BoundReleaseStatement release:
                return _ctx.FileIo.LowerRelease(release, method, block);

            // ── String operations → _ctx.String ──
            case BoundInspectStatement insp:
                _ctx.String.LowerInspect(insp, block);
                break;
            case BoundStringStatement str:
                return _ctx.String.LowerString(str, method, block);
            case BoundUnstringStatement unstr:
                return _ctx.String.LowerUnstring(unstr, method, block);
        }
        return block;
    }

    // ── DISPLAY (inline — too simple to extract) ──

    private void LowerDisplay(BoundDisplayStatement disp, IrBasicBlock block)
    {
        var operands = new List<IR.DisplayOperand>();
        foreach (var op in disp.Operands)
        {
            if (op is BoundFigurativeExpression fig)
            {
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
                var loc = _ctx.Location.ResolveExpressionLocation(op);
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

        block.Instructions.Add(new IR.IrPicDisplay(operands, disp.NoAdvancing));
    }

    // ── ACCEPT (inline — 4 lines) ──

    private void LowerAccept(BoundAcceptStatement stmt, IrBasicBlock block)
    {
        var loc = _ctx.Location.ResolveLocation(stmt.Target);
        if (loc == null) return;
        block.Instructions.Add(new IrAccept(loc, stmt.Source));
    }

    // ── CALL (inline — cross-cutting, uses location + condition) ──

    private IrBasicBlock LowerCall(BoundCallStatement call, IrMethod method, IrBasicBlock block)
    {
        var args = new List<IrCallArgument>();
        foreach (var arg in call.Arguments)
        {
            var loc = _ctx.Location.ResolveExpressionLocation(arg.Expression);
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

        IrLocation? returningLoc = null;
        if (call.ReturningTarget != null)
            returningLoc = _ctx.Location.ResolveLocation(call.ReturningTarget);

        IrLocation? targetLoc = null;
        if (call.IsDynamic)
        {
            var targetSym = _semantic.ResolveData(call.TargetName);
            if (targetSym != null)
                targetLoc = _ctx.Location.ResolveLocation(targetSym);
        }

        block.Instructions.Add(new IrCallProgram(
            call.TargetName, call.IsDynamic, args, returningLoc, targetLoc));

        if (call.OnException.Count > 0 || call.NotOnException.Count > 0)
        {
            var callResult = _valueFactory.Next(IrPrimitiveType.Bool);
            block.Instructions.Add(new IrCheckCallException(call.TargetName, callResult));
            return _ctx.Condition.LowerConditionalBranch(
                call.OnException, call.NotOnException, callResult, method, block, "call");
        }

        return block;
    }
}
