// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// Emits .NET CIL assemblies from IrModule using Mono.Cecil.
/// Maps IR types → Cecil TypeDefinitions, IR methods → MethodDefinitions,
/// IR instructions → IL opcodes.
/// </summary>
public sealed class CilEmitter
{
    private readonly ModuleDefinition _module;
    private readonly Dictionary<IrType, TypeReference> _typeMap = new();
    private readonly Dictionary<IrField, FieldDefinition> _fieldMap = new();
    private readonly Dictionary<IrMethod, MethodDefinition> _methodMap = new();
    private TypeDefinition? _programType;
    private FieldDefinition? _programStateField;
    private MethodDefinition? _initializeStateMethod;
    private FieldDefinition? _alterTableField;
    /// <summary>Static fields for LINKAGE SECTION parameters, keyed by USING parameter name (case-insensitive).</summary>
    private readonly Dictionary<string, FieldDefinition> _linkageFields = new(StringComparer.OrdinalIgnoreCase);
    private MethodDefinition? _currentMethodDef;
    private VariableDefinition? _arithmeticStatusLocal;
    /// <summary>
    /// Cache for IrCachedLocation: maps cache key → (area, offset, length) locals.
    /// Cleared per method via <see cref="ClearCachedLocationLocals"/>.
    /// </summary>
    private readonly Dictionary<int, (VariableDefinition area, VariableDefinition offset, VariableDefinition length)>
        _cachedLocationLocals = new();

    private CilEmitter(ModuleDefinition module)
    {
        _module = module;
        SeedPrimitiveTypes();
    }

    private Semantics.SemanticModel? _semanticModel;

    public static AssemblyDefinition EmitAssembly(IrModule ir, string assemblyName,
        Semantics.SemanticModel? semanticModel = null)
    {
        return EmitAssembly([(ir, semanticModel)], assemblyName);
    }

    /// <summary>
    /// Emit an assembly containing multiple COBOL programs.
    /// Each program becomes a separate public type in the assembly.
    /// The first program's Main method is used as the assembly entry point.
    /// </summary>
    public static AssemblyDefinition EmitAssembly(
        List<(IrModule Module, SemanticModel? Model)> programs,
        string assemblyName)
    {
        var asmName = new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0));
        var asm = AssemblyDefinition.CreateAssembly(asmName, assemblyName, ModuleKind.Console);
        MethodDefinition? entryPoint = null;

        foreach (var (module, model) in programs)
        {
            var emitter = new CilEmitter(asm.MainModule);
            emitter._semanticModel = model;
            emitter.EmitModule(module);

            // Only the first program gets the entry point
            if (entryPoint == null && emitter._methodMap.Count > 0)
            {
                var mainMethod = emitter._methodMap.Values
                    .FirstOrDefault(m => m.Name == "Main");
                if (mainMethod != null)
                    entryPoint = mainMethod;
            }
        }

        if (entryPoint != null)
            asm.EntryPoint = entryPoint;

        return asm;
    }

    private void EmitModule(IrModule ir)
    {
        // Create the program type that holds globals and methods
        _programType = new TypeDefinition(
            @namespace: "",
            name: ir.Name,
            attributes: TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
            baseType: _module.TypeSystem.Object);
        _module.Types.Add(_programType);

        // 0. ProgramState static field + static constructor
        EmitProgramState(ir);

        // 1. Record types
        foreach (var t in ir.Types)
            DefineType(t);

        // 2. Globals (static fields on program type)
        foreach (var g in ir.Globals)
            DefineGlobal(g);

        // 3. Method signatures
        foreach (var m in ir.Methods)
            DefineMethodSignature(m);

        // 4. Create Entry method signature and LINKAGE fields (before emitting bodies)
        CreateEntryMethodSignature(ir);

        // 5. Method bodies
        Console.Error.WriteLine($"[CIL] IR module '{ir.Name}' has {ir.Methods.Count} methods:");
        foreach (var m in ir.Methods)
            Console.Error.WriteLine($"[CIL]   {m.Name}: {m.Blocks.Count} blocks, {m.Blocks.Sum(b => b.Instructions.Count)} instr");

        foreach (var m in ir.Methods)
            EmitMethodBody(m);

        // 6. Generate Entry method body (paragraph dispatch loop)
        EmitEntryMethodBody(ir);

        // 7. Generate alternate Entry methods for ENTRY statements
        foreach (var ep in ir.EntryPoints)
            EmitAlternateEntryMethod(ir, ep.Name, ep.UsingParams);
    }

    private MethodDefinition? _entryMethod;

    /// <summary>
    /// Create the Entry method signature and LINKAGE parameter fields.
    /// Must run before emitting paragraph method bodies so LINKAGE access works.
    /// </summary>
    private void CreateEntryMethodSignature(IrModule ir)
    {
        _entryMethod = new MethodDefinition(
            "Entry",
            MethodAttributes.Public | MethodAttributes.Static,
            _module.TypeSystem.Int32);
        _entryMethod.Parameters.Add(new ParameterDefinition(
            "args", ParameterAttributes.None,
            _module.ImportReference(typeof(CobolDataPointer[]))));
        _programType!.Methods.Add(_entryMethod);

        // Create static CobolDataPointer fields for each LINKAGE parameter
        // (so paragraph methods can access LINKAGE items via these fields)
        foreach (var paramName in ir.UsingParameterNames)
        {
            var field = new FieldDefinition(
                $"_linkage_{paramName}",
                FieldAttributes.Private | FieldAttributes.Static,
                _module.ImportReference(typeof(CobolDataPointer)));
            _programType.Fields.Add(field);
            _linkageFields[paramName] = field;
        }
    }

    /// <summary>
    /// Emit the Entry method body: maps args to LINKAGE items, runs paragraph dispatch.
    /// </summary>
    private void EmitEntryMethodBody(IrModule ir)
    {
        var il = _entryMethod!.Body.GetILProcessor();

        // INITIAL programs: re-initialize ProgramState at each Entry call
        if (ir.IsInitial && _initializeStateMethod != null)
        {
            il.Append(il.Create(OpCodes.Call, _initializeStateMethod));
        }

        // LOCAL-STORAGE: re-initialize to defaults on every invocation (§13.8)
        if ((_semanticModel?.LocalStorageSize ?? 0) > 0)
        {
            il.Append(il.Create(OpCodes.Ldsfld, _programStateField!));
            var reinitMethod = _module.ImportReference(
                typeof(ProgramState).GetMethod("ReinitializeLocalStorage")!);
            il.Append(il.Create(OpCodes.Callvirt, reinitMethod));
        }

        // Map args[i] → static CobolDataPointer fields (created in CreateEntryMethodSignature)
        for (int i = 0; i < ir.UsingParameterNames.Count; i++)
        {
            string paramName = ir.UsingParameterNames[i];
            if (!_linkageFields.TryGetValue(paramName, out var field))
                continue;

            // _linkage_X = args.Length > i ? args[i] : default
            var defaultLabel = il.Create(OpCodes.Nop);
            var afterLabel = il.Create(OpCodes.Nop);

            il.Append(il.Create(OpCodes.Ldarg_0)); // args
            il.Append(il.Create(OpCodes.Ldlen));
            il.Append(il.Create(OpCodes.Conv_I4));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ble, defaultLabel));

            // args[i]
            il.Append(il.Create(OpCodes.Ldarg_0)); // args
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldelem_Any, _module.ImportReference(typeof(CobolDataPointer))));
            il.Append(il.Create(OpCodes.Stsfld, field));
            il.Append(il.Create(OpCodes.Br, afterLabel));

            // default
            il.Append(defaultLabel);
            var tempLocal = new VariableDefinition(_module.ImportReference(typeof(CobolDataPointer)));
            _entryMethod.Body.Variables.Add(tempLocal);
            il.Append(il.Create(OpCodes.Ldloca, tempLocal));
            il.Append(il.Create(OpCodes.Initobj, _module.ImportReference(typeof(CobolDataPointer))));
            il.Append(il.Create(OpCodes.Ldloc, tempLocal));
            il.Append(il.Create(OpCodes.Stsfld, field));

            il.Append(afterLabel);
        }

        // Paragraph dispatch loop
        if (ir.ParagraphDispatchOrder.Count > 0)
        {
            EmitParagraphDispatchInline(il, ir.ParagraphDispatchOrder, _entryMethod);
        }

        // Normal return: 0
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ret));
    }

    /// <summary>
    /// Generate an alternate entry method (for ENTRY statement) that delegates to the main Entry.
    /// Each ENTRY name is registered in CobolProgramRegistry.
    /// </summary>
    private void EmitAlternateEntryMethod(IrModule ir, string entryName, IReadOnlyList<string> usingParams)
    {
        var method = new MethodDefinition(
            $"Entry_{entryName}",
            MethodAttributes.Public | MethodAttributes.Static,
            _module.TypeSystem.Int32);
        method.Parameters.Add(new ParameterDefinition(
            "args", ParameterAttributes.None,
            _module.ImportReference(typeof(CobolDataPointer[]))));
        _programType!.Methods.Add(method);

        var il = method.Body.GetILProcessor();

        // Delegate to the main Entry method (ENTRY shares the same paragraph dispatch)
        // TODO: map ENTRY-specific USING parameters to LINKAGE fields
        il.Append(il.Create(OpCodes.Ldarg_0)); // args
        il.Append(il.Create(OpCodes.Call, _entryMethod!));
        il.Append(il.Create(OpCodes.Ret));
    }

    /// <summary>
    /// Emit the paragraph dispatch loop inline into a method (Entry or legacy Main).
    /// while (pc >= 0 && pc < N) { pc = paragraphs[pc](); }
    /// </summary>
    private void EmitParagraphDispatchInline(ILProcessor il,
        IReadOnlyList<IrMethod> paragraphs, MethodDefinition md)
    {
        int count = paragraphs.Count;

        var pcLocal = new VariableDefinition(_module.TypeSystem.Int32);
        md.Body.Variables.Add(pcLocal);

        // pc = 0
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, pcLocal));

        var loopLabel = il.Create(OpCodes.Nop);
        il.Append(loopLabel);

        var exitLabel = il.Create(OpCodes.Nop);

        // if pc < 0, goto EXIT
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Blt, exitLabel));

        // switch (pc)
        var caseLabels = new Instruction[count];
        for (int i = 0; i < count; i++)
            caseLabels[i] = il.Create(OpCodes.Nop);

        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Switch, caseLabels));

        // Default: goto EXIT
        il.Append(il.Create(OpCodes.Br, exitLabel));

        // Case bodies
        for (int i = 0; i < count; i++)
        {
            il.Append(caseLabels[i]);
            var target = _methodMap[paragraphs[i]];
            il.Append(il.Create(OpCodes.Call, target));
            il.Append(il.Create(OpCodes.Stloc, pcLocal));
            il.Append(il.Create(OpCodes.Br, loopLabel));
        }

        il.Append(exitLabel);
    }

    /// <summary>
    /// Create a static ProgramState field and static constructor that allocates it.
    /// </summary>
    private void EmitProgramState(IrModule ir)
    {
        int wsSize = _semanticModel?.WorkingStorageSize ?? 4096;
        int fileSize = _semanticModel?.FileSectionSize ?? 1024;
        int lsSize = _semanticModel?.LocalStorageSize ?? 0;
        if (wsSize == 0) wsSize = 4096;
        if (fileSize == 0) fileSize = 1024;

        // Static field: ProgramState State
        _programStateField = new FieldDefinition(
            "State",
            FieldAttributes.Public | FieldAttributes.Static,
            _module.ImportReference(typeof(CobolSharp.Runtime.ProgramState)));
        _programType!.Fields.Add(_programStateField);

        // Static constructor: .cctor
        var cctor = new MethodDefinition(
            ".cctor",
            MethodAttributes.Private | MethodAttributes.Static |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            _module.TypeSystem.Void);
        _programType.Methods.Add(cctor);

        var il = cctor.Body.GetILProcessor();
        var ctor = EmitProgramStateAllocation(il, wsSize, fileSize, lsSize);
        EmitExternalStorageInitialization(il);
        EmitValueClauseInitialization(il);
        EmitAlterTableInitialization(il, ir);

        // Snapshot LOCAL-STORAGE defaults after VALUE clause initialization.
        // This snapshot is used by ReinitializeLocalStorage() on each program invocation.
        if (lsSize > 0)
            EmitLocalStorageDefaultsSnapshot(il, lsSize);

        il.Append(il.Create(OpCodes.Ret));

        EmitResetStateMethod(ir, ctor, wsSize, fileSize, lsSize);
    }

    /// <summary>Emit ProgramState allocation: new ProgramState(wsSize, fileSize, lsSize) → static field.</summary>
    private MethodReference EmitProgramStateAllocation(ILProcessor il, int wsSize, int fileSize, int lsSize)
    {
        il.Append(il.Create(OpCodes.Ldc_I4, wsSize));
        il.Append(il.Create(OpCodes.Ldc_I4, fileSize));
        il.Append(il.Create(OpCodes.Ldc_I4, lsSize));
        var ctor = _module.ImportReference(
            typeof(CobolSharp.Runtime.ProgramState)
                .GetConstructor(new[] { typeof(int), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Newobj, ctor));
        il.Append(il.Create(OpCodes.Stsfld, _programStateField!));
        return ctor;
    }

    /// <summary>Emit VALUE clause initialization: figurative fills + literal/numeric values.</summary>
    private void EmitValueClauseInitialization(ILProcessor il)
    {
        if (_semanticModel == null) return;

        // Figurative VALUE clauses (SPACE, ZERO, HIGH-VALUE, etc.)
        var moveFigMethod = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod(
                "MoveFigurativeToField",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(Runtime.PicDescriptor), typeof(int) })!);

        foreach (var kvp in _semanticModel.FigurativeInitValues)
        {
            var loc = _semanticModel.GetStorageLocation(kvp.Key);
            if (!loc.HasValue) continue;

            EmitLoadBackingArrayOrExternal(il, loc.Value.Area, loc.Value.Offset, out var figAdjOffset);
            il.Append(il.Create(OpCodes.Ldc_I4, figAdjOffset));
            il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));
            EmitLoadPicDescriptor(il, loc.Value.Pic);
            il.Append(il.Create(OpCodes.Ldc_I4, (int)kvp.Value));
            il.Append(il.Create(OpCodes.Call, moveFigMethod));
        }

        // Literal/numeric VALUE clauses
        var moveStringToOccursMethod = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "MoveStringToOccursField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(string) })!);

        foreach (var kvp in _semanticModel.InitialValues)
        {
            if (_semanticModel.FigurativeInitValues.ContainsKey(kvp.Key))
                continue;

            var data = kvp.Key;
            var loc = _semanticModel.GetStorageLocation(data);
            if (!loc.HasValue) continue;

            var init = kvp.Value;
            var (elementSize, totalOccurs) = ComputeOccursExtent(data, loc.Value);

            EmitLoadBackingArrayOrExternal(il, loc.Value.Area, loc.Value.Offset, out var valAdjOffset);

            if (init.Value is decimal d && loc.Value.Pic.IsNumeric)
            {
                il.Append(il.Create(OpCodes.Ldc_I4, valAdjOffset));
                il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));
                EmitLoadPicDescriptor(il, loc.Value.Pic, suppressBlankWhenZero: true);
                EmitLoadDecimal(il, d);
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                var numMethod = _module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod(
                        "MoveNumericLiteral",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(Runtime.PicDescriptor),
                                typeof(decimal), typeof(int) })!);
                il.Append(il.Create(OpCodes.Call, numMethod));
            }
            else
            {
                string strValue = init.Value is string s ? s
                    : ((decimal)init.Value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                il.Append(il.Create(OpCodes.Ldc_I4, valAdjOffset));
                il.Append(il.Create(OpCodes.Ldc_I4, elementSize));
                il.Append(il.Create(OpCodes.Ldc_I4, totalOccurs));
                il.Append(il.Create(OpCodes.Ldstr, strValue));
                il.Append(il.Create(OpCodes.Call, moveStringToOccursMethod));
            }
        }
    }

    /// <summary>Compute OCCURS-aware element size and total occurrences across nested dimensions.</summary>
    private (int elementSize, int totalOccurs) ComputeOccursExtent(
        Semantics.DataSymbol data, StorageLocation loc)
    {
        int directOccurs = data.Occurs?.MaxOccurs ?? 1;
        int totalSize = loc.Length;
        int elementSize = directOccurs > 1 ? totalSize / directOccurs : totalSize;
        int totalOccurs = directOccurs;

        for (var parent = data.Parent; parent != null; parent = parent.Parent)
        {
            int parentMaxOccurs = parent.Occurs?.MaxOccurs ?? 1;
            if (parentMaxOccurs <= 1) continue;
            var parentLoc = _semanticModel?.GetStorageLocation(parent);
            if (!parentLoc.HasValue) break;
            int parentPerOccurrence = parentLoc.Value.Length / parentMaxOccurs;
            if (totalOccurs * elementSize == parentPerOccurrence)
                totalOccurs *= parentMaxOccurs;
            else
                break;
        }

        return (elementSize, totalOccurs);
    }

    /// <summary>Emit ALTER indirection table initialization into .cctor.</summary>
    private void EmitAlterTableInitialization(ILProcessor il, IrModule ir)
    {
        if (ir.AlterDefaults.Count == 0) return;

        _alterTableField = new FieldDefinition(
            "_alterTable",
            FieldAttributes.Private | FieldAttributes.Static,
            _module.ImportReference(typeof(int[])));
        _programType!.Fields.Add(_alterTableField);

        il.Append(il.Create(OpCodes.Ldc_I4, ir.AlterDefaults.Count));
        il.Append(il.Create(OpCodes.Newarr, _module.TypeSystem.Int32));
        for (int i = 0; i < ir.AlterDefaults.Count; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldc_I4, ir.AlterDefaults[i]));
            il.Append(il.Create(OpCodes.Stelem_I4));
        }
        il.Append(il.Create(OpCodes.Stsfld, _alterTableField));
    }

    /// <summary>For INITIAL programs, generate ResetState that re-creates ProgramState.</summary>
    private void EmitResetStateMethod(IrModule ir, MethodReference ctor, int wsSize, int fileSize, int lsSize)
    {
        if (!ir.IsInitial) return;

        _initializeStateMethod = new MethodDefinition(
            "ResetState",
            MethodAttributes.Private | MethodAttributes.Static,
            _module.TypeSystem.Void);
        _programType!.Methods.Add(_initializeStateMethod);

        var resetIl = _initializeStateMethod.Body.GetILProcessor();
        resetIl.Append(resetIl.Create(OpCodes.Ldc_I4, wsSize));
        resetIl.Append(resetIl.Create(OpCodes.Ldc_I4, fileSize));
        resetIl.Append(resetIl.Create(OpCodes.Ldc_I4, lsSize));
        resetIl.Append(resetIl.Create(OpCodes.Newobj, ctor));
        resetIl.Append(resetIl.Create(OpCodes.Stsfld, _programStateField!));
        resetIl.Append(resetIl.Create(OpCodes.Ret));
    }

    /// <summary>
    /// Snapshot LOCAL-STORAGE after VALUE clause initialization into LocalStorageDefaults.
    /// Called from .cctor so per-invocation re-initialization can restore these defaults.
    /// Emits: State.SnapshotLocalStorageDefaults()
    /// </summary>
    private void EmitLocalStorageDefaultsSnapshot(ILProcessor il, int lsSize)
    {
        il.Append(il.Create(OpCodes.Ldsfld, _programStateField!));
        var snapshotMethod = _module.ImportReference(
            typeof(ProgramState).GetMethod("SnapshotLocalStorageDefaults")!);
        il.Append(il.Create(OpCodes.Callvirt, snapshotMethod));
    }

    /// <summary>
    /// Emit EXTERNAL storage initialization: for each EXTERNAL level-01 item,
    /// call ExternalStorage.GetOrCreate() and store the shared byte[] in a static field.
    /// </summary>
    private void EmitExternalStorageInitialization(ILProcessor il)
    {
        if (_semanticModel == null) return;

        foreach (var data in _semanticModel.DataItemsInOrder)
        {
            if (!data.IsExternal || data.LevelNumber != 1 || data.Area != StorageAreaKind.WorkingStorage)
                continue;
            var loc = _semanticModel.GetStorageLocation(data);
            if (!loc.HasValue) continue;

            string externalName = data.Name.ToUpperInvariant();

            var extField = new FieldDefinition(
                $"_ext_{externalName}",
                FieldAttributes.Private | FieldAttributes.Static,
                _module.ImportReference(typeof(byte[])));
            _programType!.Fields.Add(extField);
            _externalFields[data.Name] = extField;

            _externalRanges.Add((loc.Value.Offset, loc.Value.Length, extField));

            il.Append(il.Create(OpCodes.Ldstr, externalName));
            il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));
            var getOrCreate = _module.ImportReference(
                typeof(ExternalStorage).GetMethod("GetOrCreate",
                    new[] { typeof(string), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, getOrCreate));
            il.Append(il.Create(OpCodes.Stsfld, extField));
        }
    }

    /// <summary>Static fields for EXTERNAL data items, keyed by data name (case-insensitive).</summary>
    private readonly Dictionary<string, FieldDefinition> _externalFields = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>EXTERNAL record offset ranges in WorkingStorage: (wsOffset, wsLength) → (extField, baseOffset).</summary>
    private readonly List<(int WsOffset, int WsLength, FieldDefinition ExtField)> _externalRanges = [];

    /// <summary>
    /// Try to find the EXTERNAL byte[] field for a WorkingStorage offset.
    /// Returns true if the offset falls within an EXTERNAL record's range.
    /// adjustedOffset is the offset relative to the EXTERNAL array (always starts at 0).
    /// </summary>
    private bool TryGetExternalField(int wsOffset, out FieldDefinition? extField, out int adjustedOffset)
    {
        foreach (var (rangeOffset, rangeLength, field) in _externalRanges)
        {
            if (wsOffset >= rangeOffset && wsOffset < rangeOffset + rangeLength)
            {
                extField = field;
                adjustedOffset = wsOffset - rangeOffset;
                return true;
            }
        }
        extField = null;
        adjustedOffset = 0;
        return false;
    }

    private void SeedPrimitiveTypes()
    {
        _typeMap[IrPrimitiveType.Int32] = _module.TypeSystem.Int32;
        _typeMap[IrPrimitiveType.Int64] = _module.TypeSystem.Int64;
        _typeMap[IrPrimitiveType.Float32] = _module.TypeSystem.Single;
        _typeMap[IrPrimitiveType.Float64] = _module.TypeSystem.Double;
        _typeMap[IrPrimitiveType.Decimal] = _module.ImportReference(typeof(decimal));
        _typeMap[IrPrimitiveType.String] = _module.TypeSystem.String;
        _typeMap[IrPrimitiveType.Bool] = _module.TypeSystem.Boolean;
        _typeMap[IrPrimitiveType.Void] = _module.TypeSystem.Void;
        _typeMap[IrPrimitiveType.ByteArray] = _module.ImportReference(typeof(byte[]));
    }

    // ── Type definitions ──

    private void DefineType(IrType irType)
    {
        if (irType is IrRecordType rec)
        {
            var td = new TypeDefinition(
                @namespace: "",
                name: rec.Name,
                attributes: TypeAttributes.Public | TypeAttributes.SequentialLayout |
                            TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: _module.ImportReference(typeof(System.ValueType)));

            _module.Types.Add(td);

            foreach (var f in rec.Fields)
            {
                var fieldType = GetTypeRef(f.FieldType);
                var fd = new FieldDefinition(f.Name, FieldAttributes.Public, fieldType);
                td.Fields.Add(fd);
                _fieldMap[f] = fd;
            }

            _typeMap[irType] = td;
        }
    }

    private void DefineGlobal(IrGlobal g)
    {
        var typeRef = GetTypeRef(g.Type);
        var fd = new FieldDefinition(
            g.Name,
            FieldAttributes.Public | FieldAttributes.Static,
            typeRef);

        _programType!.Fields.Add(fd);
    }

    private TypeReference GetTypeRef(IrType irType)
    {
        if (_typeMap.TryGetValue(irType, out var tr))
            return tr;

        throw new InvalidOperationException($"No TypeReference for IR type '{irType.Name}'");
    }

    // ── Method signatures ──

    private void DefineMethodSignature(IrMethod irMethod)
    {
        var returnType = irMethod.ReturnType != null
            ? GetTypeRef(irMethod.ReturnType)
            : _module.TypeSystem.Void;

        var md = new MethodDefinition(
            irMethod.Name,
            MethodAttributes.Public | MethodAttributes.Static,
            returnType);

        foreach (var p in irMethod.Parameters)
        {
            var paramType = GetTypeRef(p.Type);
            md.Parameters.Add(new ParameterDefinition(p.Name, ParameterAttributes.None, paramType));
        }

        _programType!.Methods.Add(md);
        _methodMap[irMethod] = md;
    }

    // ── Method body emission ──

    private void EmitMethodBody(IrMethod irMethod)
    {
        var md = _methodMap[irMethod];
        _currentMethodDef = md;
        _arithmeticStatusLocal = null; // reset per method (lazy allocation)
        _cachedLocationLocals.Clear(); // reset per method

        md.Body.InitLocals = true;

        var il = md.Body.GetILProcessor();

        // Allocate locals for IR values on demand
        var valueLocalMap = new Dictionary<int, VariableDefinition>();

        VariableDefinition GetLocalForValue(IrValue v)
        {
            if (!valueLocalMap.TryGetValue(v.Id, out var vd))
            {
                vd = new VariableDefinition(GetTypeRef(v.Type));
                md.Body.Variables.Add(vd);
                valueLocalMap[v.Id] = vd;
            }
            return vd;
        }

        // Pass 1: Create labels for all blocks (enables forward branches)
        var blockLabels = new Dictionary<IrBasicBlock, Instruction>();
        foreach (var block in irMethod.Blocks)
            blockLabels[block] = il.Create(OpCodes.Nop);

        // Pass 2: Emit blocks with labels and instructions
        foreach (var block in irMethod.Blocks)
        {
            il.Append(blockLabels[block]);

            foreach (var inst in block.Instructions)
                EmitInstruction(il, inst, GetLocalForValue, blockLabels);
        }

        // Ensure method ends with ret if not already
        if (md.Body.Instructions.Count == 0 ||
            md.Body.Instructions[^1].OpCode != OpCodes.Ret)
        {
            il.Append(il.Create(OpCodes.Ret));
        }

        // DIAG: dump IL with computed offsets
        int diagOffset = 0;
        Console.Error.WriteLine($"[IL] {md.FullName}: {md.Body.Instructions.Count} IL instructions, {md.Body.Variables.Count} locals");
        foreach (var instr in md.Body.Instructions)
        {
            instr.Offset = diagOffset;
            diagOffset += instr.GetSize();
        }
        foreach (var instr in md.Body.Instructions)
            Console.Error.WriteLine($"[IL]   {instr}");
    }

    // ── Instruction emission ──

    private void EmitInstruction(
        ILProcessor il,
        IrInstruction inst,
        Func<IrValue, VariableDefinition> getLocal,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        switch (inst)
        {
            case IrLoadConst lc:
                EmitLoadConst(il, lc, getLocal);
                break;

            case IrLoadField lf:
                EmitLoadField(il, lf, getLocal);
                break;

            case IrStoreField sf:
                EmitStoreField(il, sf, getLocal);
                break;

            case IrMove mv:
                EmitMove(il, mv, getLocal);
                break;

            case IrBinary bin:
                EmitBinary(il, bin, getLocal);
                break;

            case IrBranch br:
                EmitBranch(il, br, getLocal, blockLabels);
                break;

            case IrJump j:
                il.Append(il.Create(OpCodes.Br, blockLabels[j.Target]));
                break;

            case IrBranchIfFalse bif:
            {
                var condLocal = getLocal(bif.Condition);
                il.Append(il.Create(OpCodes.Ldloc, condLocal));
                il.Append(il.Create(OpCodes.Brfalse, blockLabels[bif.Target]));
                break;
            }

            case IrSetBool sb:
            {
                il.Append(il.Create(sb.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                if (sb.Result.HasValue)
                {
                    var local = getLocal(sb.Result.Value);
                    il.Append(il.Create(OpCodes.Stloc, local));
                }
                break;
            }

            case IrTestSwitch ts:
            {
                // Call SwitchRuntime.GetSwitchState(implementorName)
                il.Append(il.Create(OpCodes.Ldstr, ts.ImplementorName));
                var getSwitchMethod = _module.ImportReference(
                    typeof(CobolSharp.Runtime.SwitchRuntime).GetMethod("GetSwitchState")!);
                il.Append(il.Create(OpCodes.Call, getSwitchMethod));
                // If testing OFF state, negate the result
                if (!ts.TestOnState)
                {
                    il.Append(il.Create(OpCodes.Ldc_I4_0));
                    il.Append(il.Create(OpCodes.Ceq));
                }
                if (ts.Result.HasValue)
                {
                    var local = getLocal(ts.Result.Value);
                    il.Append(il.Create(OpCodes.Stloc, local));
                }
                break;
            }

            case IrBinaryLogical log:
            {
                var leftLocal = getLocal(log.Left);
                if (log.Op == IrLogicalOp.Not)
                {
                    il.Append(il.Create(OpCodes.Ldloc, leftLocal));
                    il.Append(il.Create(OpCodes.Ldc_I4_0));
                    il.Append(il.Create(OpCodes.Ceq));
                }
                else
                {
                    var rightLocal = getLocal(log.Right);
                    il.Append(il.Create(OpCodes.Ldloc, leftLocal));
                    il.Append(il.Create(OpCodes.Ldloc, rightLocal));
                    if (log.Op == IrLogicalOp.And)
                        il.Append(il.Create(OpCodes.And));
                    else // Or
                        il.Append(il.Create(OpCodes.Or));
                }
                if (log.Result.HasValue)
                {
                    var local = getLocal(log.Result.Value);
                    il.Append(il.Create(OpCodes.Stloc, local));
                }
                break;
            }

            case IrInitArithmeticStatus:
            {
                EmitInitArithmeticStatus(il, _currentMethodDef!);
                break;
            }

            case IrLoadSizeError lse:
            {
                var statusLocal = EnsureArithmeticStatusLocal(_currentMethodDef!);
                il.Append(il.Create(OpCodes.Ldloca, statusLocal));
                var sizeErrorGetter = _module.ImportReference(
                    typeof(ArithmeticStatus).GetProperty("SizeError")!.GetGetMethod()!);
                il.Append(il.Create(OpCodes.Call, sizeErrorGetter));
                if (lse.Result.HasValue)
                {
                    var local = getLocal(lse.Result.Value);
                    il.Append(il.Create(OpCodes.Stloc, local));
                }
                break;
            }

            case IrReturnConst rc:
                il.Append(il.Create(OpCodes.Ldc_I4, rc.Value));
                il.Append(il.Create(OpCodes.Ret));
                break;

            case IrReturnAlterable ra:
                // return _alterTable[slot]
                il.Append(il.Create(OpCodes.Ldsfld, _alterTableField!));
                il.Append(il.Create(OpCodes.Ldc_I4, ra.AlterSlot));
                il.Append(il.Create(OpCodes.Ldelem_I4));
                il.Append(il.Create(OpCodes.Ret));
                break;

            case IrAlter alt:
                // _alterTable[slot] = newTargetIndex
                il.Append(il.Create(OpCodes.Ldsfld, _alterTableField!));
                il.Append(il.Create(OpCodes.Ldc_I4, alt.AlterSlot));
                il.Append(il.Create(OpCodes.Ldc_I4, alt.NewTargetIndex));
                il.Append(il.Create(OpCodes.Stelem_I4));
                break;

            case IrGoToDepending gtd:
                EmitGoToDepending(il, gtd, getLocal);
                break;

            case IrStopRun:
                // STOP RUN: exit the paragraph dispatch loop by returning -1.
                // In a called program, this exits the Entry method.
                // TODO: When nested CALL is exercised, upgrade to throw StopRunException
                // to unwind across call boundaries.
                il.Append(il.Create(OpCodes.Ldc_I4_M1));
                il.Append(il.Create(OpCodes.Ret));
                break;

            case IrExitProgram:
                // EXIT PROGRAM: return -1 from paragraph method (exit dispatch loop)
                // In Phase 3 full implementation, this returns from the Entry method.
                // For now, same as IrReturnConst(-1) to exit the paragraph dispatch.
                il.Append(il.Create(OpCodes.Ldc_I4_M1));
                il.Append(il.Create(OpCodes.Ret));
                break;

            case IrGoBack:
                // GOBACK: same as EXIT PROGRAM (return from dispatch loop)
                // In a main program, STOP RUN semantics apply, but since we don't
                // yet distinguish main vs called at emit time, exit the dispatch loop.
                il.Append(il.Create(OpCodes.Ldc_I4_M1));
                il.Append(il.Create(OpCodes.Ret));
                break;

            case IrCallProgram callProg:
                EmitCallProgram(il, callProg, _currentMethodDef!);
                break;

            case IrCancelProgram cancelProg:
                il.Append(il.Create(OpCodes.Ldstr, cancelProg.ProgramName));
                il.Append(il.Create(OpCodes.Call,
                    _module.ImportReference(typeof(CobolProgramRegistry).GetMethod("Cancel",
                        new[] { typeof(string) })!)));
                break;

            case IrCheckCallException checkExc:
                EmitCheckCallException(il, checkExc, getLocal);
                break;

            case IrReturn ret:
                EmitReturn(il, ret, getLocal);
                break;

            case IrParagraphDispatch dispatch:
                EmitParagraphDispatch(il, dispatch, _currentMethodDef!);
                break;

            case IrCall call:
                EmitCall(il, call, getLocal);
                break;

            case IrPerform perf:
                EmitPerform(il, perf);
                break;

            case IrPerformTimes perfTimes:
                EmitPerformTimes(il, perfTimes, _currentMethodDef!);
                break;

            case IrPerformInlineTimes perfInlineTimes:
                EmitPerformInlineTimes(il, perfInlineTimes, _currentMethodDef!, getLocal, blockLabels);
                break;

            case IrPerformThru thru:
                EmitPerformThru(il, thru, _currentMethodDef!);
                break;

            case IrMoveStringToField ms:
                EmitMoveStringToField(il, ms, getLocal);
                break;

            case IrMoveFigurative mf:
                EmitMoveFigurative(il, mf);
                break;

            case IrMoveAllLiteral mal:
                EmitMoveAllLiteral(il, mal);
                break;

            case IrWriteRecordFromStorage wr:
                EmitWriteRecordFromStorage(il, wr);
                break;

            case IrRewriteRecordFromStorage rw:
                EmitRewriteRecordFromStorage(il, rw);
                break;

            case IrWriteAdvancing waa:
                EmitWriteAdvancing(il, waa);
                break;

            case IrReadRecordToStorage rd:
                EmitReadRecordToStorage(il, rd);
                break;

            case IrReadPreviousToStorage rdp:
                EmitReadPreviousToStorage(il, rdp);
                break;

            case IrReadByKey rbk:
                EmitReadByKey(il, rbk);
                break;

            case IrCheckFileAtEnd chk:
                EmitCheckFileAtEnd(il, chk, getLocal);
                break;

            case IrStoreFileStatus sfs:
                EmitStoreFileStatus(il, sfs);
                break;

            case IrAccept acc:
                EmitAccept(il, acc);
                break;

            case IrInspectTally it:
                EmitInspectTally(il, it);
                break;

            case IrInspectReplace ir:
                EmitInspectReplace(il, ir);
                break;

            case IrInspectConvert ic:
                EmitInspectConvert(il, ic);
                break;

            case IrDeleteRecord del:
                EmitDeleteRecord(il, del);
                break;

            case IrStartFile sf:
                EmitStartFile(il, sf);
                break;

            case IrCheckFileInvalidKey cik:
                EmitCheckFileInvalidKey(il, cik, getLocal);
                break;

            case IrMoveFieldToField mf:
                EmitMoveFieldToField(il, mf);
                break;

            case IrPicMultiply mul:
                EmitPicMultiply(il, mul);
                break;

            case IrPicMultiplyLiteral mulLit:
                EmitPicMultiplyLiteral(il, mulLit);
                break;

            case IrPicAdd addInst:
                EmitPicAdd(il, addInst);
                break;

            case IrPicAddLiteral addLit:
                EmitPicAddLiteral(il, addLit);
                break;

            case IrPicSubtract subInst:
                EmitPicSubtract(il, subInst);
                break;

            case IrPicSubtractLiteral subLit:
                EmitPicSubtractLiteral(il, subLit);
                break;

            case IrInitAccumulator initAcc:
            {
                var local = getLocal(initAcc.Result!.Value);
                EmitLoadDecimal(il, 0m);
                il.Append(il.Create(OpCodes.Stloc, local));
                break;
            }

            case IrAccumulateField accField:
            {
                var accLocal = getLocal(accField.Accumulator);
                il.Append(il.Create(OpCodes.Ldloc, accLocal));
                // DecodeNumeric(area, offset, length, pic) → decimal
                EmitLocationArgsWithPic(il, accField.Source);
                var decode = _module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(Runtime.PicDescriptor) })!);
                il.Append(il.Create(OpCodes.Call, decode));
                // accumulator += decoded
                il.Append(il.Create(OpCodes.Call,
                    _module.ImportReference(typeof(decimal).GetMethod("op_Addition",
                        new[] { typeof(decimal), typeof(decimal) })!)));
                il.Append(il.Create(OpCodes.Stloc, accLocal));
                break;
            }

            case IrAccumulateLiteral accLit:
            {
                var accLocal = getLocal(accLit.Accumulator);
                il.Append(il.Create(OpCodes.Ldloc, accLocal));
                EmitLoadDecimal(il, accLit.Value);
                il.Append(il.Create(OpCodes.Call,
                    _module.ImportReference(typeof(decimal).GetMethod("op_Addition",
                        new[] { typeof(decimal), typeof(decimal) })!)));
                il.Append(il.Create(OpCodes.Stloc, accLocal));
                break;
            }

            case IrAddAccumulatedToTarget addAcc:
                EmitAddAccumulatedToTarget(il, addAcc, getLocal);
                break;

            case IrMoveAccumulatedToTarget moveAcc:
                EmitMoveAccumulatedToTarget(il, moveAcc, getLocal);
                break;

            case IrSubtractAccumulatedFromTarget subAcc:
                EmitSubtractAccumulatedFromTarget(il, subAcc, getLocal);
                break;

            case IrComputeStore compStore:
                EmitComputeStore(il, compStore);
                break;

            case IrCobolRemainder rem:
                EmitCobolRemainder(il, rem, getLocal);
                break;

            case IrComputeIntoAccumulator compAccum:
                EmitExpression(il, compAccum.Expression, compAccum.ResolvedLocations);
                il.Append(il.Create(OpCodes.Stloc, getLocal(compAccum.Accumulator)));
                break;

            case IrPicDivide divInst:
                EmitPicDivide(il, divInst);
                break;

            case IrPicDivideLiteral divLit:
                EmitPicDivideLiteral(il, divLit);
                break;

            case IrClassCondition classInst:
                EmitClassCondition(il, classInst, getLocal);
                break;

            case IrUserClassCondition userClassInst:
                EmitUserClassCondition(il, userClassInst, getLocal);
                break;

            case IrPicCompare cmp:
                EmitPicCompare(il, cmp, getLocal);
                break;

            case IrPicCompareLiteral cmpLit:
                EmitPicCompareLiteral(il, cmpLit, getLocal);
                break;

            case IrPicCompareAccumulator cmpAccum:
                EmitPicCompareAccumulator(il, cmpAccum, getLocal);
                break;

            case IrStringCompareLiteral strCmp:
                EmitStringCompareLiteral(il, strCmp, getLocal);
                break;

            case IrStringCompare strFldCmp:
                EmitStringCompare(il, strFldCmp, getLocal);
                break;

            case IrStringCompareWithSequence seqCmp:
                EmitStringCompareWithSequence(il, seqCmp, getLocal);
                break;

            case IrStringCompareLiteralWithSequence seqLitCmp:
                EmitStringCompareLiteralWithSequence(il, seqLitCmp, getLocal);
                break;

            case IrPicMoveLiteralNumeric movLit:
                EmitPicMoveLiteralNumeric(il, movLit);
                break;

            case IrFunctionCall funcCall:
                EmitFunctionCall(il, funcCall);
                break;

            case IrRuntimeCall rtc:
                EmitRuntimeCall(il, rtc, getLocal);
                break;

            case IrPicDisplay disp:
                EmitPicDisplay(il, disp);
                break;

            case IrStringStatement strStmt:
                EmitStringStatement(il, strStmt, getLocal);
                break;

            case IrUnstringStatement unstrStmt:
                EmitUnstringStatement(il, unstrStmt, getLocal);
                break;

            case IrSortInit sortInit:
                EmitSortInit(il, sortInit);
                break;

            case IrSortRelease sortRel:
                EmitSortRelease(il, sortRel);
                break;

            case IrSortSort sortSort:
                EmitSortSort(il, sortSort);
                break;

            case IrSortReturn sortRet:
                EmitSortReturn(il, sortRet, getLocal);
                break;

            case IrSortClose sortClose:
                EmitSortClose(il, sortClose);
                break;

            case IrSortMerge sortMerge:
                EmitSortMerge(il, sortMerge);
                break;

            default:
                throw new NotSupportedException(
                    $"IR instruction {inst.GetType().Name} not supported in CIL emission.");
        }
    }

    private void EmitLoadConst(ILProcessor il, IrLoadConst lc,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Push constant onto stack — no stloc.
        // Consumer (next instruction) reads from stack directly.
        switch (lc.Value)
        {
            case int i:
                il.Append(il.Create(OpCodes.Ldc_I4, i));
                break;
            case long l:
                il.Append(il.Create(OpCodes.Ldc_I8, l));
                break;
            case string s:
                il.Append(il.Create(OpCodes.Ldstr, s));
                break;
            case bool b:
                il.Append(il.Create(b ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                break;
            default:
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                break;
        }
        // No stloc — value stays on stack for the next instruction to consume
    }

    private void EmitLoadField(ILProcessor il, IrLoadField lf,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var fieldRef = _fieldMap[lf.Field];
        il.Append(il.Create(OpCodes.Ldsfld, fieldRef));

        if (lf.Result is { } res)
        {
            var local = getLocal(res);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }

    private void EmitStoreField(ILProcessor il, IrStoreField sf,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var fieldRef = _fieldMap[sf.Field];
        var valueLocal = getLocal(sf.Value);

        il.Append(il.Create(OpCodes.Ldloc, valueLocal));
        il.Append(il.Create(OpCodes.Stsfld, fieldRef));
    }

    private void EmitMove(ILProcessor il, IrMove mv,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var srcLocal = getLocal(mv.Source);
        var dstLocal = getLocal(mv.Target);

        il.Append(il.Create(OpCodes.Ldloc, srcLocal));
        il.Append(il.Create(OpCodes.Stloc, dstLocal));
    }

    private void EmitBinary(ILProcessor il, IrBinary bin,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var leftLocal = getLocal(bin.Left);
        var rightLocal = getLocal(bin.Right);

        il.Append(il.Create(OpCodes.Ldloc, leftLocal));
        il.Append(il.Create(OpCodes.Ldloc, rightLocal));

        var op = bin.Op switch
        {
            IrBinaryOp.Add => OpCodes.Add,
            IrBinaryOp.Sub => OpCodes.Sub,
            IrBinaryOp.Mul => OpCodes.Mul,
            IrBinaryOp.Div => OpCodes.Div,
            IrBinaryOp.Eq => OpCodes.Ceq,
            IrBinaryOp.Lt => OpCodes.Clt,
            IrBinaryOp.Gt => OpCodes.Cgt,
            IrBinaryOp.And => OpCodes.And,
            IrBinaryOp.Or => OpCodes.Or,
            _ => throw new NotSupportedException($"Binary op {bin.Op}")
        };

        il.Append(il.Create(op));

        if (bin.Result is { } res)
        {
            var local = getLocal(res);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }

    private void EmitBranch(ILProcessor il, IrBranch br,
        Func<IrValue, VariableDefinition> getLocal,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        var condLocal = getLocal(br.Condition);
        il.Append(il.Create(OpCodes.Ldloc, condLocal));
        il.Append(il.Create(OpCodes.Brtrue, blockLabels[br.TrueTarget]));
        il.Append(il.Create(OpCodes.Br, blockLabels[br.FalseTarget]));
    }

    private void EmitReturn(ILProcessor il, IrReturn ret,
        Func<IrValue, VariableDefinition> getLocal)
    {
        if (ret.Value is { } val)
        {
            var local = getLocal(val);
            il.Append(il.Create(OpCodes.Ldloc, local));
        }
        il.Append(il.Create(OpCodes.Ret));
    }

    private void EmitCall(ILProcessor il, IrCall call,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var target = _methodMap[call.Target];

        foreach (var arg in call.Arguments)
        {
            var argLocal = getLocal(arg);
            il.Append(il.Create(OpCodes.Ldloc, argLocal));
        }

        il.Append(il.Create(OpCodes.Call, target));

        if (call.Result is { } res)
        {
            var local = getLocal(res);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }

    /// <summary>
    /// Field to track the last CALL result (0=success, non-zero=exception).
    /// Allocated lazily when the first IrCallProgram is emitted.
    /// </summary>
    private FieldDefinition? _lastCallResultField;

    private void EmitCallProgram(ILProcessor il, IrCallProgram callProg, MethodDefinition method)
    {
        // Build CobolDataPointer[] args (+ 1 extra for RETURNING if present)
        int argCount = callProg.CallArguments.Count;
        bool hasReturning = callProg.ReturningTarget != null;
        int totalArgs = hasReturning ? argCount + 1 : argCount;
        il.Append(il.Create(OpCodes.Ldc_I4, totalArgs));
        il.Append(il.Create(OpCodes.Newarr,
            _module.ImportReference(typeof(CobolDataPointer))));

        for (int i = 0; i < argCount; i++)
        {
            var arg = callProg.CallArguments[i];
            il.Append(il.Create(OpCodes.Dup)); // array ref
            il.Append(il.Create(OpCodes.Ldc_I4, i)); // index

            // Push (area, offset, length) for the source location
            EmitLocationArgs(il, arg.Source);

            if (arg.Mode == 1 || arg.Mode == 2) // BY CONTENT or BY VALUE — copy
            {
                // BY CONTENT: callee gets private copy, modifications don't propagate
                // BY VALUE: same copy semantics (value is encoded in source location)
                var createByContent = _module.ImportReference(
                    typeof(CobolDataPointer).GetMethod("CreateByContent",
                        new[] { typeof(byte[]), typeof(int), typeof(int) })!);
                il.Append(il.Create(OpCodes.Call, createByContent));
            }
            else // BY REFERENCE — share caller's storage
            {
                var createByRef = _module.ImportReference(
                    typeof(CobolDataPointer).GetMethod("CreateByReference",
                        new[] { typeof(byte[]), typeof(int), typeof(int) })!);
                il.Append(il.Create(OpCodes.Call, createByRef));
            }

            il.Append(il.Create(OpCodes.Stelem_Any, _module.ImportReference(typeof(CobolDataPointer))));
        }

        // If RETURNING specified, add it as an extra BY REFERENCE arg at the end
        if (hasReturning)
        {
            il.Append(il.Create(OpCodes.Dup)); // array ref
            il.Append(il.Create(OpCodes.Ldc_I4, argCount)); // last index
            EmitLocationArgs(il, callProg.ReturningTarget!);
            var createByRef = _module.ImportReference(
                typeof(CobolDataPointer).GetMethod("CreateByReference",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, createByRef));
            il.Append(il.Create(OpCodes.Stelem_Any, _module.ImportReference(typeof(CobolDataPointer))));
        }

        // Store args array in a local
        var argsLocal = new VariableDefinition(_module.ImportReference(typeof(CobolDataPointer[])));
        method.Body.Variables.Add(argsLocal);
        il.Append(il.Create(OpCodes.Stloc, argsLocal));

        // Resolve the target program name
        if (callProg.IsDynamic && callProg.TargetLocation != null)
        {
            // Dynamic CALL: read program name from storage at runtime
            // GetDisplayString returns the trimmed string value of the data item
            EmitLocationArgsWithPic(il, callProg.TargetLocation);
            var getDisplay = _module.ImportReference(
                typeof(PicRuntime).GetMethod("GetDisplayString",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
            il.Append(il.Create(OpCodes.Call, getDisplay));
        }
        else
        {
            // Static CALL: program name is a compile-time literal
            il.Append(il.Create(OpCodes.Ldstr, callProg.TargetName));
        }
        il.Append(il.Create(OpCodes.Call,
            _module.ImportReference(typeof(CobolProgramRegistry).GetMethod("Resolve",
                new[] { typeof(string) })!)));

        // Check if resolve returned null (program not found)
        var entryLocal = new VariableDefinition(
            _module.ImportReference(typeof(CobolProgramEntry)));
        method.Body.Variables.Add(entryLocal);
        il.Append(il.Create(OpCodes.Stloc, entryLocal));

        // Ensure _lastCallResultField exists
        if (_lastCallResultField == null)
        {
            _lastCallResultField = new FieldDefinition(
                "_lastCallResult",
                FieldAttributes.Private | FieldAttributes.Static,
                _module.TypeSystem.Int32);
            _programType!.Fields.Add(_lastCallResultField);
        }

        // If entry is null, set result = -1 (exception)
        var callLabel = il.Create(OpCodes.Nop);
        var afterLabel = il.Create(OpCodes.Nop);

        il.Append(il.Create(OpCodes.Ldloc, entryLocal));
        il.Append(il.Create(OpCodes.Brtrue, callLabel));

        // Not found: set result = -1
        il.Append(il.Create(OpCodes.Ldc_I4_M1));
        il.Append(il.Create(OpCodes.Stsfld, _lastCallResultField));
        il.Append(il.Create(OpCodes.Br, afterLabel));

        // Found: invoke entry(args)
        il.Append(callLabel);
        il.Append(il.Create(OpCodes.Ldloc, entryLocal));
        il.Append(il.Create(OpCodes.Ldloc, argsLocal));
        il.Append(il.Create(OpCodes.Callvirt,
            _module.ImportReference(typeof(CobolProgramEntry).GetMethod("Invoke")!)));
        il.Append(il.Create(OpCodes.Stsfld, _lastCallResultField));

        il.Append(afterLabel);
    }

    private void EmitCheckCallException(ILProcessor il, IrCheckCallException checkExc,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Load _lastCallResult, check if < 0 (exception)
        il.Append(il.Create(OpCodes.Ldsfld, _lastCallResultField!));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Clt));
        if (checkExc.Result.HasValue)
        {
            var local = getLocal(checkExc.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, local));
        }
    }

    private void EmitLocationLength(ILProcessor il, IrLocation loc)
    {
        if (loc is IrStaticLocation sl)
            il.Append(il.Create(OpCodes.Ldc_I4, sl.Location.Length));
        else
            il.Append(il.Create(OpCodes.Ldc_I4_0)); // fallback
    }

    private void EmitDefaultPicDescriptor(ILProcessor il)
    {
        // Push a default PicDescriptor (alphanumeric, for parameter passing)
        // This is a simplified version — the actual PicDescriptor comes from
        // the caller's StorageLocation.Pic in a full implementation
        var defaultPicCtor = _module.ImportReference(
            typeof(PicDescriptor).GetConstructors()
                .First(c => c.GetParameters().Length > 10));
        // Push all constructor args for a basic alphanumeric descriptor
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // totalDigits
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // fractionDigits
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // isSigned
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // isNumeric
        il.Append(il.Create(OpCodes.Ldc_I4_1)); // isAlphanumeric
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // hasEditing
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // storageLength (will be set from Length)
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // usage = Display
        il.Append(il.Create(OpCodes.Ldc_I4_1)); // category = Alphanumeric
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // signStorage
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // editing
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // blankWhenZero
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // leadingScaleDigits
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // trailingScaleDigits
        il.Append(il.Create(OpCodes.Ldnull));    // editPattern
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // isGroup
        il.Append(il.Create(OpCodes.Ldc_I4, 36)); // currencySign '$'
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // decimalPointIsComma
        var picEnvCtor = _module.ImportReference(
            typeof(PicEnvironment).GetConstructor(new[] { typeof(char), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Newobj, picEnvCtor));
        il.Append(il.Create(OpCodes.Newobj, defaultPicCtor));
    }

    private MethodReference? _cobolDataPointerCtor;
    private MethodReference GetCobolDataPointerCtor()
    {
        _cobolDataPointerCtor ??= _module.ImportReference(
            typeof(CobolDataPointer).GetConstructor(
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
        return _cobolDataPointerCtor;
    }

    private void EmitPerform(ILProcessor il, IrPerform perf)
    {
        var target = _methodMap[perf.Target];
        il.Append(il.Create(OpCodes.Call, target));
        // Paragraph methods return int (next PC); discard in PERFORM context
        if (target.ReturnType != _module.TypeSystem.Void)
            il.Append(il.Create(OpCodes.Pop));
    }

    /// <summary>
    /// PERFORM N TIMES: evaluates count expression into a CIL local int,
    /// then loops calling the paragraph method(s) that many times.
    /// </summary>
    private void EmitPerformTimes(ILProcessor il, IrPerformTimes pt, MethodDefinition md)
    {
        // Evaluate count expression → decimal → int → store in local
        var counterLocal = new VariableDefinition(_module.TypeSystem.Int32);
        md.Body.Variables.Add(counterLocal);

        EmitExpression(il, pt.CountExpression, pt.ResolvedLocations);
        var toInt32 = _module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, toInt32));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop: while counter > 0
        var loopStart = il.Create(OpCodes.Nop);
        var loopEnd = il.Create(OpCodes.Nop);

        il.Append(loopStart);

        // Check: counter > 0
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ble, loopEnd));

        // Body: call paragraph(s)
        if (pt.StartIdx == pt.EndIdx)
        {
            var target = _methodMap[pt.Target];
            il.Append(il.Create(OpCodes.Call, target));
            if (target.ReturnType != _module.TypeSystem.Void)
                il.Append(il.Create(OpCodes.Pop));
        }
        else
        {
            // THRU: reuse EmitPerformThru with a synthetic IrPerformThru
            var syntheticThru = new IrPerformThru(pt.StartIdx, pt.EndIdx, pt.ThruMethods);
            EmitPerformThru(il, syntheticThru, md);
        }

        // Decrement counter
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop back
        il.Append(il.Create(OpCodes.Br, loopStart));

        il.Append(loopEnd);
    }

    /// <summary>
    /// Inline PERFORM N TIMES: evaluates count expression into a CIL local int,
    /// then loops over the body instructions that many times.
    /// The body instructions are emitted inline (no paragraph call).
    /// </summary>
    private void EmitPerformInlineTimes(ILProcessor il, IrPerformInlineTimes pit,
        MethodDefinition md, Func<IrValue, VariableDefinition> getLocal,
        Dictionary<IrBasicBlock, Instruction> blockLabels)
    {
        // Create counter local
        var counterLocal = new VariableDefinition(_module.TypeSystem.Int32);
        md.Body.Variables.Add(counterLocal);

        // Evaluate count expression → decimal → int → store in counter
        EmitExpression(il, pit.CountExpression, pit.ResolvedLocations);
        var toInt32 = _module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, toInt32));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop: while counter > 0
        var loopStart = il.Create(OpCodes.Nop);
        var loopEnd = il.Create(OpCodes.Nop);

        il.Append(loopStart);

        // Check: counter > 0
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ble, loopEnd));

        // Emit body instructions inline
        foreach (var bodyInst in pit.BodyInstructions)
            EmitInstruction(il, bodyInst, getLocal, blockLabels);

        // Decrement counter
        il.Append(il.Create(OpCodes.Ldloc, counterLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));
        il.Append(il.Create(OpCodes.Stloc, counterLocal));

        // Loop back
        il.Append(il.Create(OpCodes.Br, loopStart));

        il.Append(loopEnd);
    }

    /// <summary>
    /// PERFORM THRU: dynamic dispatch loop respecting GO TO returns.
    /// Generated IL:
    ///   int pc = startIndex;
    ///   LOOP: if (pc &lt; startIndex || pc &gt; endIndex) goto EXIT;
    ///         switch (pc - startIndex) { case 0: pc = Para_A(); break; case 1: pc = Para_B(); ... }
    ///         goto LOOP;
    ///   EXIT:
    /// </summary>
    private void EmitPerformThru(ILProcessor il, IrPerformThru thru, MethodDefinition md)
    {
        int rangeSize = thru.EndIndex - thru.StartIndex + 1;

        // Local: int pc
        var pcLocal = new VariableDefinition(_module.TypeSystem.Int32);
        md.Body.Variables.Add(pcLocal);

        // pc = startIndex
        il.Append(il.Create(OpCodes.Ldc_I4, thru.StartIndex));
        il.Append(il.Create(OpCodes.Stloc, pcLocal));

        // LOOP:
        var loopLabel = il.Create(OpCodes.Nop);
        il.Append(loopLabel);

        // EXIT label (appended later)
        var exitLabel = il.Create(OpCodes.Nop);

        // if (pc < startIndex) goto EXIT
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, thru.StartIndex));
        il.Append(il.Create(OpCodes.Blt, exitLabel));

        // if (pc > endIndex) goto EXIT
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, thru.EndIndex));
        il.Append(il.Create(OpCodes.Bgt, exitLabel));

        // switch (pc - startIndex)
        var caseLabels = new Instruction[rangeSize];
        for (int i = 0; i < rangeSize; i++)
            caseLabels[i] = il.Create(OpCodes.Nop);

        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        if (thru.StartIndex != 0)
        {
            il.Append(il.Create(OpCodes.Ldc_I4, thru.StartIndex));
            il.Append(il.Create(OpCodes.Sub));
        }
        il.Append(il.Create(OpCodes.Switch, caseLabels));

        // Default: goto EXIT (shouldn't happen but safety)
        il.Append(il.Create(OpCodes.Br, exitLabel));

        // Case bodies
        for (int i = 0; i < rangeSize; i++)
        {
            il.Append(caseLabels[i]);
            var para = thru.Paragraphs[i];
            if (para != null)
            {
                var target = _methodMap[para];
                il.Append(il.Create(OpCodes.Call, target));
                il.Append(il.Create(OpCodes.Stloc, pcLocal)); // pc = returned value
            }
            else
            {
                // Unresolved paragraph: advance pc by 1
                il.Append(il.Create(OpCodes.Ldloc, pcLocal));
                il.Append(il.Create(OpCodes.Ldc_I4_1));
                il.Append(il.Create(OpCodes.Add));
                il.Append(il.Create(OpCodes.Stloc, pcLocal));
            }
            il.Append(il.Create(OpCodes.Br, loopLabel));
        }

        // EXIT:
        il.Append(exitLabel);
    }

    /// <summary>
    /// MOVE "literal" TO field:
    /// IL: ldsfld State → ldfld WorkingStorage → ldc.i4 offset → ldc.i4 size → ldstr value → call MoveStringToField
    /// </summary>
    private void EmitMoveStringToField(ILProcessor il, IrMoveStringToField ms,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var pic = ms.Target.GetPic();

        // Numeric targets: right-justified numeric MOVE (rightmost digits taken)
        if (pic.Category == Runtime.CobolCategory.Numeric)
        {
            EmitLocationArgsWithPic(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod(
                    "MoveStringLiteralToNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(Runtime.PicDescriptor), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        // Alphanumeric-edited targets: apply edit pattern (B→space, 0→zero, etc.)
        else if (pic.Category == Runtime.CobolCategory.AlphanumericEdited && pic.EditPattern != null)
        {
            EmitLocationArgs(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));
            il.Append(il.Create(OpCodes.Ldstr, pic.EditPattern));

            var method = _module.ImportReference(
                typeof(Runtime.StorageHelpers).GetMethod(
                    "MoveStringToEditedField",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(string), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else if (pic.IsJustifiedRight)
        {
            // JUSTIFIED RIGHT alphanumeric: right-justified, left-padded/left-truncated
            EmitLocationArgs(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _module.ImportReference(
                typeof(Runtime.StorageHelpers).GetMethod(
                    "MoveStringToJustifiedField",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else
        {
            // Plain alphanumeric: left-justified, space-padded
            EmitLocationArgs(il, ms.Target);
            il.Append(il.Create(OpCodes.Ldstr, ms.Value));

            var method = _module.ImportReference(
                typeof(Runtime.StorageHelpers).GetMethod(
                    "MoveStringToField",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    /// <summary>
    /// MOVE figurative-constant TO field: calls PicRuntime.MoveFigurativeToField.
    /// </summary>
    /// <summary>
    /// Emit a MOVE call with the standard (src, dst, rounding) signature used by most PicRuntime MOVE methods.
    /// </summary>
    private void EmitMoveWithStandardSignature(
        ILProcessor il, IrLocation source, IrLocation destination, int rounding, string methodName)
    {
        EmitLocationArgsWithPic(il, source);
        EmitLocationArgsWithPic(il, destination);

        il.Append(il.Create(OpCodes.Ldc_I4, rounding));

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod(methodName,
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitMoveFigurative(ILProcessor il, IrMoveFigurative mf)
    {
        EmitLocationArgsWithPic(il, mf.Destination);
        il.Append(il.Create(OpCodes.Ldc_I4, (int)mf.FigurativeKind));

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod(
                "MoveFigurativeToField",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(Runtime.PicDescriptor), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// MOVE ALL "pattern" TO field: calls PicRuntime.MoveAllLiteralToField.
    /// </summary>
    private void EmitMoveAllLiteral(ILProcessor il, IrMoveAllLiteral mal)
    {
        EmitLocationArgs(il, mal.Destination);

        // Emit pattern as byte[]: new byte[] { b0, b1, ... }
        var patternBytes = System.Text.Encoding.ASCII.GetBytes(mal.Pattern);
        il.Append(il.Create(OpCodes.Ldc_I4, patternBytes.Length));
        il.Append(il.Create(OpCodes.Newarr, _module.TypeSystem.Byte));
        for (int i = 0; i < patternBytes.Length; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldc_I4, (int)patternBytes[i]));
            il.Append(il.Create(OpCodes.Stelem_I1));
        }

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod(
                "MoveAllLiteralToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// WRITE record from ProgramState storage:
    /// IL: ldstr fileName → ldsfld State → ldfld area → ldc.i4 offset → ldc.i4 size → call WriteRecordToFile
    /// </summary>
    private void EmitWriteRecordFromStorage(ILProcessor il, IrWriteRecordFromStorage wr)
    {
        // fileName
        il.Append(il.Create(OpCodes.Ldstr, wr.FileName));

        // Load area, offset, size
        EmitLocationArgs(il, wr.Record);

        // Call ProgramState.WriteRecordToFile(string, byte[], int, int)
        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "WriteRecordToFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// WRITE AFTER ADVANCING: calls FileRuntime.WriteAfterAdvancing(string, byte[], int, int, int).
    /// </summary>
    /// <summary>
    /// REWRITE record: calls FileRuntime.Rewrite(string, byte[], int, int).
    /// </summary>
    private void EmitRewriteRecordFromStorage(ILProcessor il, IrRewriteRecordFromStorage rw)
    {
        il.Append(il.Create(OpCodes.Ldstr, rw.FileName));
        EmitLocationArgs(il, rw.Record);

        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.FileRuntime).GetMethod(
                "Rewrite",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitWriteAdvancing(ILProcessor il, IrWriteAdvancing waa)
    {
        // fileName
        il.Append(il.Create(OpCodes.Ldstr, waa.FileName));
        // Load area, offset, size
        EmitLocationArgs(il, waa.Record);
        // advanceLines: from data field or compile-time constant
        if (waa.AdvancingLocation != null)
        {
            // Read advancing count from data field at runtime
            EmitLocationArgs(il, waa.AdvancingLocation);
            var readInt = _module.ImportReference(
                typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsInt",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, readInt));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4, waa.AdvanceLines));
        }
        // isBefore
        il.Append(il.Create(waa.IsBefore ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.FileRuntime).GetMethod(
                "WriteAdvancing",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitReadRecordToStorage(ILProcessor il, IrReadRecordToStorage rd)
    {
        // StorageHelpers.ReadRecordFromFile(string fileName, byte[] area, int offset, int size)
        il.Append(il.Create(OpCodes.Ldstr, rd.FileName));
        EmitLocationArgs(il, rd.Record);

        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "ReadRecordFromFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
        il.Append(il.Create(OpCodes.Pop)); // Discard bool return (AT END checked separately)
    }

    private void EmitReadPreviousToStorage(ILProcessor il, IrReadPreviousToStorage rdp)
    {
        // StorageHelpers.ReadPreviousRecordFromFile(string fileName, byte[] area, int offset, int size)
        il.Append(il.Create(OpCodes.Ldstr, rdp.FileName));
        EmitLocationArgs(il, rdp.Record);

        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "ReadPreviousRecordFromFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
        il.Append(il.Create(OpCodes.Pop)); // Discard bool return (AT END checked separately)
    }

    private void EmitReadByKey(ILProcessor il, IrReadByKey rbk)
    {
        // FileRuntime.ReadByKey(string fileName, byte[] recArea, int recOff, int recSize,
        //                       byte[] keyArea, int keyOff, int keySize)
        il.Append(il.Create(OpCodes.Ldstr, rbk.FileName));
        EmitLocationArgs(il, rbk.Record);
        EmitLocationArgs(il, rbk.Key);

        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.FileRuntime).GetMethod(
                "ReadByKey",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Store FILE STATUS: call FileRuntime.GetLastStatus(cobolName) → MoveStringToField.
    /// </summary>
    private void EmitStoreFileStatus(ILProcessor il, IrStoreFileStatus sfs)
    {
        // Push args for MoveStringToField(byte[] area, int offset, int length, string value)
        EmitLocationArgs(il, sfs.StatusVariable);

        // Call FileRuntime.GetLastStatus(cobolName) to get the status string
        il.Append(il.Create(OpCodes.Ldstr, sfs.CobolFileName));
        var getStatus = _module.ImportReference(
            typeof(CobolSharp.Runtime.FileRuntime).GetMethod(
                "GetLastStatus", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, getStatus));

        // Call StorageHelpers.MoveStringToField(area, offset, length, value)
        var moveString = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "MoveStringToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, moveString));
    }

    private void EmitCheckFileAtEnd(
        ILProcessor il,
        IrCheckFileAtEnd chk,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // FileRuntime.IsAtEnd(string fileName)
        il.Append(il.Create(OpCodes.Ldstr, chk.FileName));
        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.FileRuntime).GetMethod(
                "IsAtEnd",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
        il.Append(il.Create(OpCodes.Stloc, getLocal(chk.Result)));
    }

    // ── DELETE / START / INVALID KEY ──

    private void EmitDeleteRecord(ILProcessor il, IrDeleteRecord del)
    {
        il.Append(il.Create(OpCodes.Ldstr, del.FileName));
        var method = _module.ImportReference(
            typeof(Runtime.FileRuntime).GetMethod("DeleteRecord",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitStartFile(ILProcessor il, IrStartFile sf)
    {
        il.Append(il.Create(OpCodes.Ldstr, sf.FileName));
        EmitLocationArgs(il, sf.KeyLocation);
        il.Append(il.Create(OpCodes.Ldc_I4, sf.Condition));
        var method = _module.ImportReference(
            typeof(Runtime.FileRuntime).GetMethod("StartFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitCheckFileInvalidKey(ILProcessor il, IrCheckFileInvalidKey cik,
        Func<IrValue, VariableDefinition> getLocal)
    {
        il.Append(il.Create(OpCodes.Ldstr, cik.FileName));
        var method = _module.ImportReference(
            typeof(Runtime.FileRuntime).GetMethod("IsInvalidKey",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));
        if (cik.Result.HasValue)
            il.Append(il.Create(OpCodes.Stloc, getLocal(cik.Result.Value)));
    }

    // ── GO TO DEPENDING ──

    private void EmitGoToDepending(ILProcessor il, IrGoToDepending gtd,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Decode selector to int: PicRuntime.DecodeNumeric(area, offset, length, pic) → decimal
        EmitLocationArgsWithPic(il, gtd.Selector);

        var decodeMethod = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor) })!);
        il.Append(il.Create(OpCodes.Call, decodeMethod));

        // Convert decimal → int32
        var toInt = _module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, toInt));

        // Store in a local
        var selectorLocal = new VariableDefinition(_module.TypeSystem.Int32);
        _currentMethodDef!.Body.Variables.Add(selectorLocal);
        il.Append(il.Create(OpCodes.Stloc, selectorLocal));

        // Emit cascaded: if (selector == 1) return target[0]; if (selector == 2) return target[1]; ...
        for (int i = 0; i < gtd.TargetParagraphIndices.Count; i++)
        {
            int value = i + 1; // 1-based
            int targetPc = gtd.TargetParagraphIndices[i];

            var nextCheck = il.Create(OpCodes.Nop);

            il.Append(il.Create(OpCodes.Ldloc, selectorLocal));
            il.Append(il.Create(OpCodes.Ldc_I4, value));
            il.Append(il.Create(OpCodes.Bne_Un, nextCheck));

            // Match: return the target PC
            il.Append(il.Create(OpCodes.Ldc_I4, targetPc));
            il.Append(il.Create(OpCodes.Ret));

            il.Append(nextCheck);
        }

        // No match: fall through (don't return, let execution continue)
    }

    // ── ACCEPT ──

    private void EmitAccept(ILProcessor il, IrAccept acc)
    {
        EmitLocationArgs(il, acc.Target);
        il.Append(il.Create(OpCodes.Ldc_I4, (int)acc.Source));

        var method = _module.ImportReference(
            typeof(Runtime.AcceptRuntime).GetMethod("Accept",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.AcceptSourceKind) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    // ── INSPECT emit helpers ──

    /// <summary>
    /// Emit an IrInspectPatternValue onto the CIL stack as a string.
    /// Literals: Ldstr. Locations: ReadFieldAsRawString(area, offset, length).
    /// </summary>
    private void EmitIrInspectPatternValue(ILProcessor il, IR.IrInspectPatternValue? pv)
    {
        if (pv == null || pv.IsLiteral)
        {
            il.Append(il.Create(OpCodes.Ldstr, pv?.Literal ?? ""));
        }
        else if (pv.IsLocation)
        {
            EmitLocationArgs(il, pv.Location!);
            var readMethod = _module.ImportReference(
                typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsRawString",
                    new[] { typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, readMethod));
        }
    }

    /// <summary>
    /// Emit an InspectPatternValue as a nullable string for BEFORE/AFTER/CONVERTING args.
    /// Literals use Ldstr (compile-time). Data refs use ReadFieldAsRawString (runtime).
    /// Null patterns emit Ldnull.
    /// </summary>
    private void EmitIrInspectPatternValueAsOptionalString(ILProcessor il,
        IR.IrInspectPatternValue? pv)
    {
        if (pv == null)
            il.Append(il.Create(OpCodes.Ldnull));
        else
            EmitIrInspectPatternValue(il, pv);
    }

    private void EmitInspectTally(ILProcessor il, IrInspectTally it)
    {
        // Target area/offset/length
        EmitLocationArgs(il, it.Target);

        string methodName;

        if (it.Kind == Semantics.Bound.InspectTallyKind.Characters)
        {
            methodName = "TallyCharactersAndStore";
        }
        else
        {
            methodName = it.Kind == Semantics.Bound.InspectTallyKind.Leading
                ? "TallyLeadingAndStore" : "TallyAllAndStore";
            EmitIrInspectPatternValue(il, it.Pattern);
        }

        // Counter area/offset/length/pic
        EmitLocationArgsWithPic(il, it.Counter);

        // Region args
        EmitIrInspectPatternValueAsOptionalString(il, it.BeforePattern);
        il.Append(il.Create(it.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        EmitIrInspectPatternValueAsOptionalString(il, it.AfterPattern);
        il.Append(il.Create(it.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        System.Type[] paramTypes;
        if (it.Kind == Semantics.Bound.InspectTallyKind.Characters)
        {
            paramTypes = new[] { typeof(byte[]), typeof(int), typeof(int),
                typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                typeof(string), typeof(bool), typeof(string), typeof(bool) };
        }
        else
        {
            paramTypes = new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string),
                typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                typeof(string), typeof(bool), typeof(string), typeof(bool) };
        }

        var method = _module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod(methodName, paramTypes)!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitInspectReplace(ILProcessor il, IrInspectReplace ir)
    {
        EmitLocationArgs(il, ir.Target);

        if (ir.Kind == Semantics.Bound.InspectReplaceKind.Characters)
        {
            // REPLACING CHARACTERS BY x — no pattern, just replacement
            EmitIrInspectPatternValue(il, ir.Replacement);
            EmitIrInspectPatternValueAsOptionalString(il, ir.BeforePattern);
            il.Append(il.Create(ir.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            EmitIrInspectPatternValueAsOptionalString(il, ir.AfterPattern);
            il.Append(il.Create(ir.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            var charsMethod = _module.ImportReference(
                typeof(Runtime.InspectRuntime).GetMethod("ReplaceCharacters",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(string),
                        typeof(string), typeof(bool), typeof(string), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, charsMethod));
        }
        else
        {
            string methodName = ir.Kind switch
            {
                Semantics.Bound.InspectReplaceKind.First => "ReplaceFirst",
                Semantics.Bound.InspectReplaceKind.Leading => "ReplaceLeading",
                _ => "ReplaceAll"
            };

            EmitIrInspectPatternValue(il, ir.Pattern);
            EmitIrInspectPatternValue(il, ir.Replacement);
            EmitIrInspectPatternValueAsOptionalString(il, ir.BeforePattern);
            il.Append(il.Create(ir.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
            EmitIrInspectPatternValueAsOptionalString(il, ir.AfterPattern);
            il.Append(il.Create(ir.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            var method = _module.ImportReference(
                typeof(Runtime.InspectRuntime).GetMethod(methodName,
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(string), typeof(string),
                        typeof(string), typeof(bool), typeof(string), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    private void EmitInspectConvert(ILProcessor il, IrInspectConvert ic)
    {
        EmitLocationArgs(il, ic.Target);
        EmitIrInspectPatternValue(il, ic.FromSet);
        EmitIrInspectPatternValue(il, ic.ToSet);
        EmitIrInspectPatternValueAsOptionalString(il, ic.BeforePattern);
        il.Append(il.Create(ic.BeforeInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        EmitIrInspectPatternValueAsOptionalString(il, ic.AfterPattern);
        il.Append(il.Create(ic.AfterInitial ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        var method = _module.ImportReference(
            typeof(Runtime.InspectRuntime).GetMethod("Convert",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                    typeof(string), typeof(string),
                    typeof(string), typeof(bool), typeof(string), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitOptionalString(ILProcessor il, string? value)
    {
        if (value != null)
            il.Append(il.Create(OpCodes.Ldstr, value));
        else
            il.Append(il.Create(OpCodes.Ldnull));
    }

    /// <summary>
    /// MOVE field TO field: routes numeric→numeric through PicRuntime.MoveNumeric,
    /// alpha→alpha through StorageHelpers.MoveFieldToField.
    /// </summary>
    private void EmitMoveFieldToField(ILProcessor il, IrMoveFieldToField mf)
    {
        var srcPic = mf.SourcePic;
        var dstPic = mf.DestinationPic;
        var srcCat = srcPic.Category;
        var dstCat = dstPic.Category;
        int rounding = mf.IsRounded ? 1 : 0;

        // Group items are always alphanumeric for MOVE: raw byte copy, no formatting/editing.
        if (srcPic.IsGroup || dstPic.IsGroup)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumeric");
            return;
        }

        // Destination AlphanumericEdited: must be checked before generic IsNumericLike() rules.
        if (dstCat == CobolCategory.AlphanumericEdited)
        {
            if (srcCat == CobolCategory.Numeric)
                EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToAlphanumericEdited");
            else
                EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumericEdited");
            return;
        }
        // NumericEdited source: specific handling before generic IsNumericLike() rules.
        else if (srcCat == CobolCategory.NumericEdited && dstCat == CobolCategory.NumericEdited)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericEditedToNumericEdited");
        }
        else if (srcCat == CobolCategory.NumericEdited && dstCat == CobolCategory.Numeric)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericEditedToNumeric");
        }
        else if (srcCat == CobolCategory.NumericEdited && dstCat.IsAlphanumericLike())
        {
            // NumericEdited → Alphanumeric: COBOL treats source as alphanumeric (raw byte copy)
            EmitLocationArgs(il, mf.Destination);
            EmitLocationArgs(il, mf.Source);
            var method = _module.ImportReference(
                typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                    "MoveFieldToField",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, method));
            return;
        }
        // Generic numeric source rules.
        else if (srcCat.IsNumericLike() && dstCat == CobolCategory.NumericEdited)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToNumericEdited");
        }
        else if (srcCat.IsNumericLike() && dstCat.IsNumericLike())
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToNumeric");
        }
        else if (srcCat.IsNumericLike() && dstCat.IsAlphanumericLike())
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveNumericToAlphanumeric");
        }
        // Alphanumeric source rules.
        else if (srcCat.IsAlphanumericLike() && dstCat == CobolCategory.Numeric)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToNumeric");
        }
        else if (srcCat.IsAlphanumericLike() && dstCat == CobolCategory.NumericEdited)
        {
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToNumericEdited");
        }
        else
        {
            // Alphanumeric MOVE: left-justified, space-padded (handles JUSTIFIED RIGHT)
            EmitMoveWithStandardSignature(il, mf.Source, mf.Destination, rounding, "MoveAlphanumericToAlphanumeric");
        }
    }

    private void EmitPicMultiply(ILProcessor il, IrPicMultiply mul)
    {
        EmitLocationArgsWithPic(il, mul.Destination);
        EmitLocationArgsWithPic(il, mul.Left);
        EmitLocationArgsWithPic(il, mul.Right);

        il.Append(il.Create(OpCodes.Ldc_I4, mul.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("MultiplyNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitClassCondition(ILProcessor il, IrClassCondition inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        var kind = (Semantics.Bound.ClassConditionKind)inst.ClassKind;
        string methodName = kind switch
        {
            Semantics.Bound.ClassConditionKind.Numeric => "IsNumericClass",
            Semantics.Bound.ClassConditionKind.Alphabetic => "IsAlphabeticClass",
            Semantics.Bound.ClassConditionKind.AlphabeticLower => "IsAlphabeticLowerClass",
            Semantics.Bound.ClassConditionKind.AlphabeticUpper => "IsAlphabeticUpperClass",
            _ => throw new InvalidOperationException($"Unknown class condition: {kind}")
        };

        // IsNumericClass takes PicDescriptor; others don't
        System.Reflection.MethodInfo method;
        if (kind == Semantics.Bound.ClassConditionKind.Numeric)
        {
            EmitLocationArgsWithPic(il, inst.Subject);
            method = typeof(Runtime.PicRuntime).GetMethod(methodName,
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor) })!;
        }
        else
        {
            EmitLocationArgs(il, inst.Subject);
            method = typeof(Runtime.PicRuntime).GetMethod(methodName,
                new[] { typeof(byte[]), typeof(int), typeof(int) })!;
        }

        il.Append(il.Create(OpCodes.Call, _module.ImportReference(method)));

        if (inst.Result.HasValue)
        {
            var resLocal = getLocal(inst.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    private void EmitUserClassCondition(ILProcessor il, IrUserClassCondition inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgs(il, inst.Subject);

        // Emit the valid bytes array as a static field or inline byte array
        EmitByteArrayLiteral(il, inst.ValidBytes);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("IsInUserClass",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));

        if (inst.Result.HasValue)
        {
            var resLocal = getLocal(inst.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    /// <summary>
    /// Emit a byte[] literal onto the CIL stack. Creates a new array and fills it.
    /// </summary>
    private void EmitByteArrayLiteral(ILProcessor il, byte[] data)
    {
        il.Append(il.Create(OpCodes.Ldc_I4, data.Length));
        il.Append(il.Create(OpCodes.Newarr, _module.ImportReference(typeof(byte))));
        for (int i = 0; i < data.Length; i++)
        {
            il.Append(il.Create(OpCodes.Dup));
            il.Append(il.Create(OpCodes.Ldc_I4, i));
            il.Append(il.Create(OpCodes.Ldc_I4, (int)data[i]));
            il.Append(il.Create(OpCodes.Stelem_I1));
        }
    }

    private void EmitPicCompare(ILProcessor il, IrPicCompare cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgsWithPic(il, cmp.Left);
        EmitLocationArgsWithPic(il, cmp.Right);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    /// <summary>
    /// Construct a PicDescriptor on the CIL stack.
    /// </summary>
    private void EmitLoadPicDescriptor(ILProcessor il, Runtime.PicDescriptor pic,
        bool suppressBlankWhenZero = false)
    {
        // Must match the CIL-emitted constructor parameter order in PicDescriptor
        il.Append(il.Create(OpCodes.Ldc_I4, pic.TotalDigits));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.FractionDigits));
        il.Append(il.Create(pic.IsSigned ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(pic.IsNumeric ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(pic.IsAlphanumeric ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(pic.HasEditing ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.StorageLength));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.Usage));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.Category));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.SignStorage));
        il.Append(il.Create(OpCodes.Ldc_I4, (int)pic.Editing));
        bool emitBlank = pic.BlankWhenZero && !suppressBlankWhenZero;
        il.Append(il.Create(emitBlank ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.LeadingScaleDigits));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.TrailingScaleDigits));

        if (pic.EditPattern != null)
            il.Append(il.Create(OpCodes.Ldstr, pic.EditPattern));
        else
            il.Append(il.Create(OpCodes.Ldnull));

        il.Append(il.Create(pic.IsJustifiedRight ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

        // Emit PicEnvironment: new PicEnvironment(currencySign, decimalPointIsComma)
        var env = pic.Environment;
        il.Append(il.Create(OpCodes.Ldc_I4, (int)env.CurrencySign));
        il.Append(il.Create(env.DecimalPointIsComma ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        var envCtor = _module.ImportReference(
            typeof(Runtime.PicEnvironment).GetConstructor(
                new[] { typeof(char), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Newobj, envCtor));

        var ctor = _module.ImportReference(
            typeof(Runtime.PicDescriptor).GetConstructor(
                new[] { typeof(int), typeof(int), typeof(bool), typeof(bool),
                        typeof(bool), typeof(bool), typeof(int), typeof(Runtime.UsageKind),
                        typeof(Runtime.CobolCategory),
                        typeof(Runtime.SignStorageKind), typeof(Runtime.EditingKind),
                        typeof(bool), typeof(int), typeof(int), typeof(string),
                        typeof(bool), typeof(Runtime.PicEnvironment) })!);
        il.Append(il.Create(OpCodes.Newobj, ctor));
    }

    /// <summary>
    /// Emit a decimal literal onto the CIL stack.
    /// </summary>
    /// <summary>
    /// Emit a decimal literal with exact precision (no double round-trip).
    /// Uses decimal(lo, mid, hi, isNegative, scale) constructor.
    /// </summary>
    private void EmitLoadDecimal(ILProcessor il, decimal value)
    {
        var bits = decimal.GetBits(value);
        il.Append(il.Create(OpCodes.Ldc_I4, bits[0]));  // lo
        il.Append(il.Create(OpCodes.Ldc_I4, bits[1]));  // mid
        il.Append(il.Create(OpCodes.Ldc_I4, bits[2]));  // hi
        il.Append(il.Create(value < 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));  // isNegative
        // Scale is in bits 16-23 of bits[3]
        byte scale = (byte)((bits[3] >> 16) & 0xFF);
        il.Append(il.Create(OpCodes.Ldc_I4, (int)scale));  // scale

        var decCtor = _module.ImportReference(
            typeof(decimal).GetConstructor(new[] {
                typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) })!);
        il.Append(il.Create(OpCodes.Newobj, decCtor));
    }

    // ── New PIC-aware emitters ──

    private void EmitPicMoveLiteralNumeric(ILProcessor il, IrPicMoveLiteralNumeric mv)
    {
        EmitLocationArgsWithPic(il, mv.Destination);
        EmitLoadDecimal(il, mv.Value);
        il.Append(il.Create(OpCodes.Ldc_I4, mv.Rounding));

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicMultiplyLiteral(ILProcessor il, IrPicMultiplyLiteral mul)
    {
        EmitLocationArgsWithPic(il, mul.Destination);
        EmitLoadDecimal(il, mul.Value);
        EmitLocationArgsWithPic(il, mul.Other);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("MultiplyNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicAdd(ILProcessor il, IrPicAdd add)
    {
        EmitLocationArgsWithPic(il, add.Destination);
        EmitLocationArgsWithPic(il, add.Source);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("AddNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicAddLiteral(ILProcessor il, IrPicAddLiteral add)
    {
        EmitLocationArgsWithPic(il, add.Destination);
        EmitLoadDecimal(il, add.Value);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("AddNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicSubtract(ILProcessor il, IrPicSubtract sub)
    {
        EmitLocationArgsWithPic(il, sub.Destination);
        EmitLocationArgsWithPic(il, sub.Source);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("SubtractNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicSubtractLiteral(ILProcessor il, IrPicSubtractLiteral sub)
    {
        EmitLocationArgsWithPic(il, sub.Destination);
        EmitLoadDecimal(il, sub.Value);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("SubtractNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitAddAccumulatedToTarget(ILProcessor il, IrAddAccumulatedToTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgsWithPic(il, inst.Destination);
        var accLocal = getLocal(inst.Accumulator);
        il.Append(il.Create(OpCodes.Ldloc, accLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("AddAccumulatedToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitMoveAccumulatedToTarget(ILProcessor il, IrMoveAccumulatedToTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgsWithPic(il, inst.Destination);
        var accLocal = getLocal(inst.Accumulator);
        il.Append(il.Create(OpCodes.Ldloc, accLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("MoveAccumulatedToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitSubtractAccumulatedFromTarget(ILProcessor il, IrSubtractAccumulatedFromTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgsWithPic(il, inst.Destination);
        var accLocal = getLocal(inst.Accumulator);
        il.Append(il.Create(OpCodes.Ldloc, accLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("SubtractAccumulatedFromField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicDivide(ILProcessor il, IrPicDivide div)
    {
        EmitLocationArgsWithPic(il, div.Destination);
        EmitLocationArgsWithPic(il, div.Left);
        EmitLocationArgsWithPic(il, div.Right);

        il.Append(il.Create(OpCodes.Ldc_I4, div.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("DivideNumeric",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    private void EmitPicDivideLiteral(ILProcessor il, IrPicDivideLiteral div)
    {
        EmitLocationArgsWithPic(il, div.Destination);
        EmitLoadDecimal(il, div.Value);
        EmitLocationArgsWithPic(il, div.Other);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("DivideNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal),
                        typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(int), typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// COMPUTE/GIVING store: evaluate expression tree to decimal, then store to target.
    /// Routes through MoveAccumulatedToField — the unified "store decimal with overflow
    /// detection" path shared by all arithmetic operations.
    /// </summary>
    private void EmitComputeStore(ILProcessor il, IrComputeStore cs)
    {
        EmitLocationArgsWithPic(il, cs.Destination);
        EmitExpression(il, cs.Expression, cs.ResolvedLocations);
        il.Append(il.Create(OpCodes.Ldc_I4, cs.Rounding));
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("MoveAccumulatedToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int),
                        typeof(Runtime.ArithmeticStatus).MakeByRefType() })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Emit CIL for IrFunctionCall: call IntrinsicFunctions.Call() and store result into destination.
    /// Dispatches to numeric (MoveAccumulatedToField) or string (MoveAlphanumericToField) path
    /// based on the function's result category.
    /// </summary>
    private void EmitFunctionCall(ILProcessor il, IR.IrFunctionCall funcCall)
    {
        // Determine if this function returns a string result
        bool isStringFunction = funcCall.FunctionName.ToUpperInvariant() switch
        {
            "LOWER-CASE" or "UPPER-CASE" or "REVERSE" or "TRIM" or "CONCATENATE"
                or "SUBSTITUTE" or "CHAR" or "CURRENT-DATE" or "WHEN-COMPILED" => true,
            _ => false
        };

        if (isStringFunction)
        {
            // String-returning function: push dest args first, then call function, store to field.
            // Stack order: area, offset, length, stringValue → MoveStringToField
            EmitLocationArgs(il, funcCall.Destination);
            EmitIntrinsicCall(il, funcCall.FunctionName, funcCall.Arguments, funcCall.ResolvedLocations);
            // Result is object on stack; cast to string
            il.Append(il.Create(OpCodes.Castclass,
                _module.ImportReference(typeof(string))));
            il.Append(il.Create(OpCodes.Call,
                _module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("MoveStringToField",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(string) })!)));
        }
        else
        {
            // Numeric-returning function: call, unbox to decimal, store via MoveAccumulatedToField
            EmitLocationArgsWithPic(il, funcCall.Destination);
            EmitIntrinsicCall(il, funcCall.FunctionName, funcCall.Arguments, funcCall.ResolvedLocations);
            il.Append(il.Create(OpCodes.Unbox_Any,
                _module.ImportReference(typeof(decimal))));
            il.Append(il.Create(OpCodes.Ldc_I4_0)); // no rounding
            EmitLoadArithmeticStatusRef(il, _currentMethodDef!);
            il.Append(il.Create(OpCodes.Call,
                _module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("MoveAccumulatedToField",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                                typeof(decimal), typeof(int),
                                typeof(Runtime.ArithmeticStatus).MakeByRefType() })!)));
        }
    }

    /// <summary>
    /// Emit CIL to call IntrinsicFunctions.Call(functionName, args).
    /// Pushes the result (object) onto the evaluation stack.
    /// Each argument is evaluated and boxed into an object[] array.
    /// </summary>
    private void EmitIntrinsicCall(ILProcessor il, string functionName,
        IReadOnlyList<Semantics.Bound.BoundExpression> arguments,
        IReadOnlyDictionary<Semantics.Bound.BoundExpression, IR.IrLocation>? resolvedLocations)
    {
        // Push function name
        il.Append(il.Create(OpCodes.Ldstr, functionName.ToUpperInvariant()));

        // Build object[] args array
        il.Append(il.Create(OpCodes.Ldc_I4, arguments.Count));
        il.Append(il.Create(OpCodes.Newarr, _module.ImportReference(typeof(object))));

        for (int i = 0; i < arguments.Count; i++)
        {
            il.Append(il.Create(OpCodes.Dup)); // duplicate array ref
            il.Append(il.Create(OpCodes.Ldc_I4, i)); // index

            var arg = arguments[i];
            if (arg is Semantics.Bound.BoundLiteralExpression lit && lit.Value is string s)
            {
                // String literal argument
                il.Append(il.Create(OpCodes.Ldstr, s));
            }
            else if (arg.Category == Runtime.CobolCategory.Alphanumeric
                     || arg.Category == Runtime.CobolCategory.AlphanumericEdited)
            {
                // Alphanumeric field: read as string via StorageArea.ReadFieldAsString
                if (resolvedLocations != null && resolvedLocations.TryGetValue(arg, out var loc))
                {
                    EmitLocationArgs(il, loc);
                    il.Append(il.Create(OpCodes.Call,
                        _module.ImportReference(
                            typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsString",
                                new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
                }
                else if (arg is Semantics.Bound.BoundIdentifierExpression fallbackId
                         && !fallbackId.IsSubscripted)
                {
                    var storageLoc = _semanticModel?.GetStorageLocation(fallbackId.Symbol);
                    if (storageLoc.HasValue)
                    {
                        EmitLoadBackingArrayOrExternal(il, storageLoc.Value.Area,
                            storageLoc.Value.Offset, out var adjOff);
                        il.Append(il.Create(OpCodes.Ldc_I4, adjOff));
                        il.Append(il.Create(OpCodes.Ldc_I4, storageLoc.Value.Length));
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(
                                typeof(Runtime.StorageHelpers).GetMethod("ReadFieldAsString",
                                    new[] { typeof(byte[]), typeof(int), typeof(int) })!)));
                    }
                    else
                    {
                        il.Append(il.Create(OpCodes.Ldstr, ""));
                    }
                }
                else
                {
                    il.Append(il.Create(OpCodes.Ldstr, ""));
                }
            }
            else
            {
                // Numeric argument: evaluate expression to decimal, box it
                EmitExpression(il, arg, resolvedLocations);
                il.Append(il.Create(OpCodes.Box, _module.ImportReference(typeof(decimal))));
            }

            il.Append(il.Create(OpCodes.Stelem_Ref)); // store into array
        }

        // Call IntrinsicFunctions.Call(string, object[])
        il.Append(il.Create(OpCodes.Call,
            _module.ImportReference(
                typeof(Runtime.Intrinsics.IntrinsicFunctions).GetMethod("Call",
                    new[] { typeof(string), typeof(object[]) })!)));
    }

    private void EmitCobolRemainder(ILProcessor il, IrCobolRemainder rem,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Push: dividend(decimal), divisor(decimal), rawQuotient(decimal),
        //        givingFractionDigits(int), dest(area,off,len,pic), ref status
        EmitExpression(il, rem.Dividend, rem.DividendLocations);
        EmitExpression(il, rem.Divisor, rem.DivisorLocations);
        il.Append(il.Create(OpCodes.Ldloc, getLocal(rem.QuotientAccumulator)));
        il.Append(il.Create(OpCodes.Ldc_I4, rem.GivingFractionDigits));
        EmitLocationArgsWithPic(il, rem.Destination);
        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("ComputeCobolRemainder",
                new[] {
                    typeof(decimal), typeof(decimal), typeof(decimal),
                    typeof(int),
                    typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                    typeof(Runtime.ArithmeticStatus).MakeByRefType()
                })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Recursively emit a bound expression tree, leaving a decimal on the IL stack.
    /// Data-reference leaves (identifiers, ref-mod) are resolved via the pre-resolved
    /// location dictionary populated by the Binder — no direct GetStorageLocation here.
    /// When resolvedLocations is null (subscript/ref-mod sub-expression context),
    /// falls back to GetStorageLocation for simple non-subscripted identifiers only.
    /// </summary>
    private void EmitExpression(ILProcessor il, Semantics.Bound.BoundExpression expr,
        IReadOnlyDictionary<Semantics.Bound.BoundExpression, IR.IrLocation>? resolvedLocations = null)
    {
        switch (expr)
        {
            case Semantics.Bound.BoundLiteralExpression lit when lit.Value is decimal d:
                EmitLoadDecimal(il, d);
                break;

            case Semantics.Bound.BoundIdentifierExpression:
            case Semantics.Bound.BoundReferenceModificationExpression:
            {
                // Data-reference leaf: load numeric value via pre-resolved IrLocation.
                // EmitLocationArgsWithPic pushes (area, offset, length, pic) for any
                // IrLocation type (static, element ref, ref-mod) — then DecodeNumeric
                // converts the raw bytes to a decimal.
                if (resolvedLocations != null && resolvedLocations.TryGetValue(expr, out var loc))
                {
                    EmitLocationArgsWithPic(il, loc);
                    var decode = _module.ImportReference(
                        typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                            new[] { typeof(byte[]), typeof(int), typeof(int),
                                    typeof(Runtime.PicDescriptor) })!);
                    il.Append(il.Create(OpCodes.Call, decode));
                }
                else if (expr is Semantics.Bound.BoundIdentifierExpression fallbackId
                         && !fallbackId.IsSubscripted)
                {
                    // Fallback for simple identifiers in subscript/ref-mod sub-expressions.
                    // COBOL subscript expressions are always simple elementary items or literals,
                    // never themselves subscripted or ref-mod'd.
                    var storageLoc = _semanticModel?.GetStorageLocation(fallbackId.Symbol);
                    if (storageLoc.HasValue)
                    {
                        EmitLoadBackingArrayOrExternal(il, storageLoc.Value.Area, storageLoc.Value.Offset, out var fbAdjOffset);
                        il.Append(il.Create(OpCodes.Ldc_I4, fbAdjOffset));
                        il.Append(il.Create(OpCodes.Ldc_I4, storageLoc.Value.Length));
                        EmitLoadPicDescriptor(il, storageLoc.Value.Pic);
                        var decode = _module.ImportReference(
                            typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                                new[] { typeof(byte[]), typeof(int), typeof(int),
                                        typeof(Runtime.PicDescriptor) })!);
                        il.Append(il.Create(OpCodes.Call, decode));
                    }
                    else
                    {
                        EmitLoadDecimal(il, 0m);
                    }
                }
                else
                {
                    EmitLoadDecimal(il, 0m);
                }
                break;
            }

            case Semantics.Bound.BoundBinaryExpression bin:
            {
                EmitExpression(il, bin.Left, resolvedLocations);
                EmitExpression(il, bin.Right, resolvedLocations);

                switch (bin.OperatorKind)
                {
                    case Semantics.Bound.BoundBinaryOperatorKind.Add:
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(decimal).GetMethod("op_Addition",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case Semantics.Bound.BoundBinaryOperatorKind.Subtract:
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(decimal).GetMethod("op_Subtraction",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case Semantics.Bound.BoundBinaryOperatorKind.Multiply:
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(decimal).GetMethod("op_Multiply",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case Semantics.Bound.BoundBinaryOperatorKind.Divide:
                        // Use SafeDivide instead of op_Division to handle divide-by-zero
                        // as SIZE ERROR instead of crashing with DivideByZeroException.
                        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(Runtime.PicRuntime).GetMethod("SafeDivide",
                                new[] { typeof(decimal), typeof(decimal),
                                        typeof(Runtime.ArithmeticStatus).MakeByRefType() })!)));
                        break;
                    case Semantics.Bound.BoundBinaryOperatorKind.Remainder:
                        EmitLoadArithmeticStatusRef(il, _currentMethodDef!);
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(Runtime.PicRuntime).GetMethod("SafeRemainder",
                                new[] { typeof(decimal), typeof(decimal),
                                        typeof(Runtime.ArithmeticStatus).MakeByRefType() })!)));
                        break;
                    case Semantics.Bound.BoundBinaryOperatorKind.Power:
                    {
                        // Convert both to double, call Math.Pow, convert back to decimal
                        // Stack has: decimal left, decimal right
                        // Need to convert: store right, convert left, load right, convert right
                        var tempRight = new VariableDefinition(_module.ImportReference(typeof(decimal)));
                        il.Body.Variables.Add(tempRight);
                        il.Append(il.Create(OpCodes.Stloc, tempRight));
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(decimal).GetMethod("ToDouble",
                                new[] { typeof(decimal) })!)));
                        il.Append(il.Create(OpCodes.Ldloc, tempRight));
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(decimal).GetMethod("ToDouble",
                                new[] { typeof(decimal) })!)));
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(Math).GetMethod("Pow",
                                new[] { typeof(double), typeof(double) })!)));
                        // Convert double result back to decimal
                        il.Append(il.Create(OpCodes.Newobj,
                            _module.ImportReference(typeof(decimal).GetConstructor(
                                new[] { typeof(double) })!)));
                        break;
                    }
                }
                break;
            }

            case Semantics.Bound.BoundFunctionCallExpression func:
            {
                // Emit intrinsic function call that returns decimal.
                // Build object[] args, call IntrinsicFunctions.Call(), cast result to decimal.
                EmitIntrinsicCall(il, func.FunctionName, func.Arguments, resolvedLocations);
                // The Call() returns object; unbox to decimal for arithmetic context
                il.Append(il.Create(OpCodes.Unbox_Any,
                    _module.ImportReference(typeof(decimal))));
                break;
            }

            default:
                EmitLoadDecimal(il, 0m);
                break;
        }
    }

    private void EmitPicCompareLiteral(ILProcessor il, IrPicCompareLiteral cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgsWithPic(il, cmp.Left);
        EmitLoadDecimal(il, cmp.Value);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareNumericToLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    private void EmitPicCompareAccumulator(ILProcessor il, IrPicCompareAccumulator cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgsWithPic(il, cmp.Left);
        // Load the pre-evaluated accumulator (decimal)
        il.Append(il.Create(OpCodes.Ldloc, getLocal(cmp.Accumulator)));

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareNumericToLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    /// <summary>
    /// Convert CompareNumeric result (-1/0/1) to bool based on operator kind.
    /// Uses enum values directly — never hardcode integer constants for enum members.
    /// </summary>
    private void EmitCompareResultToBool(ILProcessor il, int operatorKind)
    {
        var op = (Semantics.Bound.BoundBinaryOperatorKind)operatorKind;
        switch (op)
        {
            case Semantics.Bound.BoundBinaryOperatorKind.Equal: // result == 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case Semantics.Bound.BoundBinaryOperatorKind.NotEqual: // NOT (result == 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case Semantics.Bound.BoundBinaryOperatorKind.Less: // result < 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Clt));
                break;
            case Semantics.Bound.BoundBinaryOperatorKind.LessOrEqual: // NOT (result > 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Cgt));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case Semantics.Bound.BoundBinaryOperatorKind.Greater: // result > 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Cgt));
                break;
            case Semantics.Bound.BoundBinaryOperatorKind.GreaterOrEqual: // NOT (result < 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Clt));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            default: // Fallback: treat as equal
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
        }
    }

    private void EmitStringCompareLiteral(ILProcessor il, IrStringCompareLiteral cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgs(il, cmp.Left);
        il.Append(il.Create(OpCodes.Ldstr, cmp.Value));

        var method = _module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("CompareFieldToString",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    private void EmitStringCompare(ILProcessor il, IrStringCompare cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgs(il, cmp.Left);
        EmitLocationArgs(il, cmp.Right);

        var method = _module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("CompareFieldToField",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    private void EmitStringCompareWithSequence(ILProcessor il, IrStringCompareWithSequence cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLocationArgs(il, cmp.Left);
        EmitLocationArgs(il, cmp.Right);
        EmitByteArrayLiteral(il, cmp.CollatingSequence);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareAlphanumericWithSequence",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    private void EmitStringCompareLiteralWithSequence(ILProcessor il, IrStringCompareLiteralWithSequence cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Convert the string literal to a byte array and compare using CompareAlphanumericWithSequence
        EmitLocationArgs(il, cmp.Left);

        // Create a temp byte array from the string literal
        var bytes = System.Text.Encoding.ASCII.GetBytes(cmp.Value);
        EmitByteArrayLiteral(il, bytes);
        il.Append(il.Create(OpCodes.Ldc_I4_0)); // offset = 0
        il.Append(il.Create(OpCodes.Ldc_I4, bytes.Length)); // length

        EmitByteArrayLiteral(il, cmp.CollatingSequence);

        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("CompareAlphanumericWithSequence",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int), typeof(byte[]) })!);
        il.Append(il.Create(OpCodes.Call, method));

        EmitCompareResultToBool(il, cmp.OperatorKind);

        if (cmp.Result.HasValue)
        {
            var resLocal = getLocal(cmp.Result.Value);
            il.Append(il.Create(OpCodes.Stloc, resLocal));
        }
    }

    /// <summary>
    // ── STRING emit ──

    private void EmitStringStatement(ILProcessor il, IR.IrStringStatement strStmt,
        Func<IR.IrValue, VariableDefinition> getLocal)
    {
        // Create a shared pointer local for the entire STRING statement
        var ptrLocal = new VariableDefinition(_module.TypeSystem.Int32);
        _currentMethodDef!.Body.Variables.Add(ptrLocal);

        // Initialize pointer: from user POINTER variable or 1
        if (strStmt.PointerLocation != null)
        {
            EmitLocationArgsWithPic(il, strStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(Runtime.PicDescriptor) })!)));
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4_1));
        }
        il.Append(il.Create(OpCodes.Stloc, ptrLocal));

        // Initialize overflow result to false
        var overflowLocal = getLocal(strStmt.Result!.Value);
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, overflowLocal));

        // Emit each sending
        foreach (var sending in strStmt.Sendings)
        {
            // Push dest args
            EmitLocationArgs(il, strStmt.Destination);

            if (sending.LiteralValue != null)
            {
                // Literal sending: StringConcatLiteral(dest area/off/len, value, delim, bySize, ref ptr)
                il.Append(il.Create(OpCodes.Ldstr, sending.LiteralValue));
            }
            else
            {
                // Field sending: StringConcat(dest area/off/len, src area/off/len, delim, bySize, ref ptr)
                EmitLocationArgs(il, sending.SourceLocation!);
            }

            // Delimiter
            if (sending.Delimiter != null)
                il.Append(il.Create(OpCodes.Ldstr, sending.Delimiter));
            else
                il.Append(il.Create(OpCodes.Ldnull));

            // DelimitedBySize
            il.Append(il.Create(sending.DelimitedBySize ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            // Pass pointer by ref
            il.Append(il.Create(OpCodes.Ldloca, ptrLocal));

            // Call appropriate runtime method
            if (sending.LiteralValue != null)
            {
                il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("StringConcatLiteral",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string),
                                typeof(string), typeof(bool), typeof(int).MakeByRefType() })!)));
            }
            else
            {
                il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                    typeof(Runtime.StorageHelpers).GetMethod("StringConcat",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(byte[]), typeof(int), typeof(int),
                                typeof(string), typeof(bool), typeof(int).MakeByRefType() })!)));
            }

            // OR overflow: overflowLocal |= result
            il.Append(il.Create(OpCodes.Ldloc, overflowLocal));
            il.Append(il.Create(OpCodes.Or));
            il.Append(il.Create(OpCodes.Stloc, overflowLocal));
        }

        // Write pointer back to POINTER variable (if present)
        if (strStmt.PointerLocation != null)
        {
            EmitLocationArgsWithPic(il, strStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));
            il.Append(il.Create(OpCodes.Newobj,
                _module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(decimal), typeof(int) })!)));
        }
    }

    private void EmitUnstringStatement(ILProcessor il, IR.IrUnstringStatement unstrStmt,
        Func<IR.IrValue, VariableDefinition> getLocal)
    {
        // Create shared pointer local for the entire UNSTRING statement
        var ptrLocal = new VariableDefinition(_module.TypeSystem.Int32);
        _currentMethodDef!.Body.Variables.Add(ptrLocal);

        // Initialize pointer: from user POINTER variable or 1
        if (unstrStmt.PointerLocation != null)
        {
            EmitLocationArgsWithPic(il, unstrStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(Runtime.PicDescriptor) })!)));
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!)));
        }
        else
        {
            il.Append(il.Create(OpCodes.Ldc_I4_1));
        }
        il.Append(il.Create(OpCodes.Stloc, ptrLocal));

        // Create shared overflow local
        var overflowLocal = new VariableDefinition(_module.TypeSystem.Boolean);
        _currentMethodDef.Body.Variables.Add(overflowLocal);
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, overflowLocal));

        // Tally counter local
        var tallyLocal = new VariableDefinition(_module.TypeSystem.Int32);
        _currentMethodDef.Body.Variables.Add(tallyLocal);
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, tallyLocal));

        // Resolve the UnstringExtract method reference
        var extractMethod = _module.ImportReference(
            typeof(Runtime.StorageHelpers).GetMethod("UnstringExtract",
                new[] { typeof(byte[]), typeof(int), typeof(int),
                        typeof(byte[]), typeof(int), typeof(int),
                        typeof(string), typeof(bool),
                        typeof(byte[]), typeof(int), typeof(int),
                        typeof(int).MakeByRefType(), typeof(bool).MakeByRefType() })!);

        // Count local for COUNT IN write-back
        var countLocal = new VariableDefinition(_module.TypeSystem.Int32);
        _currentMethodDef.Body.Variables.Add(countLocal);

        // Process each INTO
        foreach (var into in unstrStmt.Intos)
        {
            // Push source args (area, offset, length)
            EmitLocationArgs(il, unstrStmt.Source);

            // Push dest args (area, offset, length)
            EmitLocationArgs(il, into.Target);

            // Push delimiter (string? or null)
            if (unstrStmt.LiteralDelimiter != null)
                il.Append(il.Create(OpCodes.Ldstr, unstrStmt.LiteralDelimiter));
            else
                il.Append(il.Create(OpCodes.Ldnull));

            // Push delimitedByAll flag
            il.Append(il.Create(unstrStmt.DelimitedByAll ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

            // Push DELIMITER IN args (area, offset, length) or nulls
            if (into.DelimiterIn != null)
            {
                EmitLocationArgs(il, into.DelimiterIn);
            }
            else
            {
                il.Append(il.Create(OpCodes.Ldnull));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
            }

            // Pass pointer by ref
            il.Append(il.Create(OpCodes.Ldloca, ptrLocal));

            // Pass overflow by ref
            il.Append(il.Create(OpCodes.Ldloca, overflowLocal));

            // Call UnstringExtract — returns int (count of extracted chars)
            il.Append(il.Create(OpCodes.Call, extractMethod));

            // Store returned count
            il.Append(il.Create(OpCodes.Stloc, countLocal));

            // Handle COUNT IN: write the count to the COUNT IN field
            if (into.CountIn != null)
            {
                EmitLocationArgsWithPic(il, into.CountIn);
                il.Append(il.Create(OpCodes.Ldloc, countLocal));
                il.Append(il.Create(OpCodes.Newobj,
                    _module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                        new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                                typeof(decimal), typeof(int) })!)));
            }

            // Increment tally counter
            il.Append(il.Create(OpCodes.Ldloc, tallyLocal));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Add));
            il.Append(il.Create(OpCodes.Stloc, tallyLocal));
        }

        // Write pointer back to POINTER variable (if present)
        if (unstrStmt.PointerLocation != null)
        {
            EmitLocationArgsWithPic(il, unstrStmt.PointerLocation);
            il.Append(il.Create(OpCodes.Ldloc, ptrLocal));
            il.Append(il.Create(OpCodes.Newobj,
                _module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(decimal), typeof(int) })!)));
        }

        // Write tally count to TALLYING variable (if present)
        if (unstrStmt.TallyingLocation != null)
        {
            EmitLocationArgsWithPic(il, unstrStmt.TallyingLocation);
            il.Append(il.Create(OpCodes.Ldloc, tallyLocal));
            il.Append(il.Create(OpCodes.Newobj,
                _module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int) })!)));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(decimal), typeof(int) })!)));
        }

        // Store overflow result for branching
        var resultLocal = getLocal(unstrStmt.Result!.Value);
        il.Append(il.Create(OpCodes.Ldloc, overflowLocal));
        il.Append(il.Create(OpCodes.Stloc, resultLocal));
    }

    /// Emit the PC-driven dispatch loop for Main:
    ///   int pc = 0;
    ///   while (pc >= 0 && pc &lt; N) pc = paragraphs[pc]();
    /// Uses CIL switch opcode for O(1) dispatch.
    /// </summary>
    private void EmitParagraphDispatch(ILProcessor il, IrParagraphDispatch dispatch,
        MethodDefinition md)
    {
        var paragraphs = dispatch.Paragraphs;
        int count = paragraphs.Count;

        // Local: int pc
        var pcLocal = new VariableDefinition(_module.TypeSystem.Int32);
        md.Body.Variables.Add(pcLocal);

        // pc = 0
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Stloc, pcLocal));

        // LOOP label
        var loopLabel = il.Create(OpCodes.Nop);
        il.Append(loopLabel);

        // EXIT label (created now, appended later)
        var exitLabel = il.Create(OpCodes.Nop);

        // if pc < 0, goto EXIT
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Blt, exitLabel));

        // Create case labels
        var caseLabels = new Instruction[count];
        for (int i = 0; i < count; i++)
            caseLabels[i] = il.Create(OpCodes.Nop);

        // switch (pc) — jumps to caseLabels[pc], falls through if pc >= count
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Switch, caseLabels));

        // Default (pc >= count): goto EXIT
        il.Append(il.Create(OpCodes.Br, exitLabel));

        // Case bodies: call paragraph, store returned pc, loop
        for (int i = 0; i < count; i++)
        {
            il.Append(caseLabels[i]);
            var target = _methodMap[paragraphs[i]];
            il.Append(il.Create(OpCodes.Call, target));
            il.Append(il.Create(OpCodes.Stloc, pcLocal));
            il.Append(il.Create(OpCodes.Br, loopLabel));
        }

        // EXIT — wrap dispatch loop exit with StopRunException catch
        // StopRunException may be thrown by STOP RUN from any paragraph
        il.Append(exitLabel);
    }

    /// <summary>
    /// Load the backing byte array from ProgramState.
    /// Pushes: State.WorkingStorage or State.FileSection (byte[]) onto the stack.
    /// </summary>
    /// <summary>
    /// Lazily allocate one ArithmeticStatus local per method.
    /// </summary>
    private VariableDefinition EnsureArithmeticStatusLocal(MethodDefinition md)
    {
        if (_arithmeticStatusLocal == null)
        {
            _arithmeticStatusLocal = new VariableDefinition(
                _module.ImportReference(typeof(ArithmeticStatus)));
            md.Body.Variables.Add(_arithmeticStatusLocal);
        }
        return _arithmeticStatusLocal;
    }

    /// <summary>
    /// Zero-initialize the ArithmeticStatus local before an arithmetic call.
    /// </summary>
    private void EmitInitArithmeticStatus(ILProcessor il, MethodDefinition md)
    {
        var statusLocal = EnsureArithmeticStatusLocal(md);
        il.Append(il.Create(OpCodes.Ldloca, statusLocal));
        il.Append(il.Create(OpCodes.Initobj,
            _module.ImportReference(typeof(ArithmeticStatus))));
    }

    /// <summary>
    /// Push address of ArithmeticStatus local onto stack (for ref parameter).
    /// </summary>
    private void EmitLoadArithmeticStatusRef(ILProcessor il, MethodDefinition md)
    {
        var statusLocal = EnsureArithmeticStatusLocal(md);
        il.Append(il.Create(OpCodes.Ldloca, statusLocal));
    }

    /// <summary>
    /// Emit (area, offset, length) for a LINKAGE SECTION item.
    /// Loads from the CobolDataPointer field, adding the relative offset.
    /// </summary>
    private void EmitLinkageLocationArgs(ILProcessor il, IR.IrStaticLocation s)
    {
        // Find which LINKAGE parameter field this item belongs to.
        // Try to match the item name directly first, then search for a parent match.
        FieldDefinition? field = null;
        string? matchedName = null;

        // Try exact match (for 01-level LINKAGE items)
        if (_semanticModel != null)
        {
            foreach (var param in _semanticModel.ProcedureUsingParameters)
            {
                if (_linkageFields.TryGetValue(param.Name, out var f))
                {
                    // Check if this storage location falls within this parameter's range
                    var paramLoc = _semanticModel.GetStorageLocation(param);
                    if (paramLoc.HasValue &&
                        s.Location.Offset >= paramLoc.Value.Offset &&
                        s.Location.Offset < paramLoc.Value.Offset + paramLoc.Value.Length)
                    {
                        field = f;
                        matchedName = param.Name;
                        break;
                    }
                }
            }
        }

        if (field != null)
        {
            // Load CobolDataPointer.Buffer
            il.Append(il.Create(OpCodes.Ldsflda, field));
            var bufferGetter = _module.ImportReference(
                typeof(CobolDataPointer).GetProperty("Buffer")!.GetGetMethod()!);
            il.Append(il.Create(OpCodes.Call, bufferGetter));

            // Offset = CobolDataPointer.Offset + relative offset within the parameter
            il.Append(il.Create(OpCodes.Ldsflda, field));
            var offsetGetter = _module.ImportReference(
                typeof(CobolDataPointer).GetProperty("Offset")!.GetGetMethod()!);
            il.Append(il.Create(OpCodes.Call, offsetGetter));
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset)); // relative offset
            il.Append(il.Create(OpCodes.Add));

            // Length
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
        }
        else
        {
            // Fallback: LINKAGE item not mapped to a USING parameter
            // (may happen for LINKAGE items not in the USING clause)
            // Push nulls that will likely cause a runtime NullReferenceException
            // if actually accessed — this is correct behavior for unmapped LINKAGE
            il.Append(il.Create(OpCodes.Ldnull));
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
        }
    }

    private void EmitLoadBackingArray(ILProcessor il, StorageAreaKind area)
    {
        // LINKAGE SECTION items are NOT backed by ProgramState — they're backed
        // by CobolDataPointer fields populated from CALL USING args.
        // This method only handles WorkingStorage, LocalStorage, and FileSection.
        // LINKAGE access is handled separately in EmitLocationArgs.
        il.Append(il.Create(OpCodes.Ldsfld, _programStateField!));

        var propertyName = area switch
        {
            StorageAreaKind.WorkingStorage => "WorkingStorage",
            StorageAreaKind.LocalStorage   => "LocalStorage",
            StorageAreaKind.FileSection    => "FileSection",
            _ => throw new InvalidOperationException(
                $"EmitLoadBackingArray: unexpected StorageAreaKind '{area}'. " +
                "LinkageSection should be handled separately via CobolDataPointer.")
        };

        var getter = _module.ImportReference(
            typeof(CobolSharp.Runtime.ProgramState).GetProperty(propertyName)!.GetGetMethod()!);
        il.Append(il.Create(OpCodes.Callvirt, getter));
    }

    /// <summary>
    /// Load the backing array for a storage location, accounting for EXTERNAL items.
    /// For EXTERNAL WorkingStorage items, loads the shared ExternalStorage byte[] field.
    /// Returns the adjusted offset (0-based within the external array, or unchanged for non-external).
    /// </summary>
    private void EmitLoadBackingArrayOrExternal(ILProcessor il, StorageAreaKind area, int wsOffset, out int adjustedOffset)
    {
        if (area == StorageAreaKind.WorkingStorage && TryGetExternalField(wsOffset, out var extField, out adjustedOffset))
        {
            il.Append(il.Create(OpCodes.Ldsfld, extField!));
            return;
        }

        adjustedOffset = wsOffset;
        EmitLoadBackingArray(il, area);
    }

    // ── Unified IrLocation emission helpers ──


    /// <summary>
    /// Push (area, offset, length) onto the IL stack for any IrLocation.
    /// For static: pushes compile-time constants.
    /// For element ref: computes runtime offset via subscript decode.
    /// For ref mod: composes base location + runtime start:length.
    /// </summary>
    private void EmitLocationArgs(ILProcessor il, IR.IrLocation loc)
    {
        switch (loc)
        {
            case IR.IrCachedLocation cached:
                EmitCachedLocationArgs(il, cached);
                break;

            case IR.IrStaticLocation s when s.Location.Area == StorageAreaKind.LinkageSection:
                // LINKAGE item: load from CobolDataPointer static field
                EmitLinkageLocationArgs(il, s);
                break;

            case IR.IrStaticLocation s
                when s.Location.Area == StorageAreaKind.WorkingStorage
                  && TryGetExternalField(s.Location.Offset, out var extField, out var adjOffset):
                // EXTERNAL item: load from shared ExternalStorage byte[]
                il.Append(il.Create(OpCodes.Ldsfld, extField!));
                il.Append(il.Create(OpCodes.Ldc_I4, adjOffset));
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
                break;

            case IR.IrStaticLocation s:
                EmitLoadBackingArray(il, s.Location.Area);
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Length));
                break;

            case IR.IrElementRef e:
                EmitElementAddress(il, e);
                il.Append(il.Create(OpCodes.Ldc_I4, e.ElementSize));
                break;

            case IR.IrRefModLocation r:
                EmitRefModAddress(il, r);
                break;

            default:
                throw new NotSupportedException($"Unknown IrLocation type: {loc.GetType().Name}");
        }
    }

    /// <summary>
    /// Emit (area, offset, length) for a cached location. On first encounter with a
    /// given cache key, compute the inner location args, store into locals, and reload.
    /// On subsequent encounters, just load from the cached locals.
    /// </summary>
    private void EmitCachedLocationArgs(ILProcessor il, IR.IrCachedLocation cached)
    {
        if (_cachedLocationLocals.TryGetValue(cached.CacheKey, out var locals))
        {
            // Already computed — reload from locals
            il.Append(il.Create(OpCodes.Ldloc, locals.area));
            il.Append(il.Create(OpCodes.Ldloc, locals.offset));
            il.Append(il.Create(OpCodes.Ldloc, locals.length));
            return;
        }

        // First encounter — compute inner, store into locals
        EmitLocationArgs(il, cached.Inner);

        var body = _currentMethodDef!.Body;
        var lengthLocal = new VariableDefinition(_module.TypeSystem.Int32);
        body.Variables.Add(lengthLocal);
        var offsetLocal = new VariableDefinition(_module.TypeSystem.Int32);
        body.Variables.Add(offsetLocal);
        var areaLocal = new VariableDefinition(_module.ImportReference(typeof(byte[])));
        body.Variables.Add(areaLocal);

        // Stack is: area, offset, length — store in reverse order
        il.Append(il.Create(OpCodes.Stloc, lengthLocal));
        il.Append(il.Create(OpCodes.Stloc, offsetLocal));
        il.Append(il.Create(OpCodes.Stloc, areaLocal));

        _cachedLocationLocals[cached.CacheKey] = (areaLocal, offsetLocal, lengthLocal);

        // Reload onto stack
        il.Append(il.Create(OpCodes.Ldloc, areaLocal));
        il.Append(il.Create(OpCodes.Ldloc, offsetLocal));
        il.Append(il.Create(OpCodes.Ldloc, lengthLocal));
    }

    /// <summary>
    /// Push (area, offset, length, pic) onto the IL stack for any IrLocation.
    /// </summary>
    private void EmitLocationArgsWithPic(ILProcessor il, IR.IrLocation loc)
    {
        EmitLocationArgs(il, loc);
        EmitLoadPicDescriptor(il, loc.GetPic());
    }

    /// <summary>
    /// Push (area, effectiveOffset) for a multi-dimensional IrElementRef.
    /// Each subscript is a BoundExpression evaluated via EmitExpression → decimal → int32.
    /// Handles identifiers (ARR(I)), arithmetic (ARR(I+1)), and any expression uniformly.
    /// </summary>
    private void EmitElementAddress(ILProcessor il, IR.IrElementRef e)
    {
        // Push base area (EXTERNAL-aware)
        EmitLoadBackingArrayOrExternal(il, e.BaseLocation.Area, e.BaseLocation.Offset, out var elemAdjOffset);

        // Push base offset — accumulates displacement from each dimension
        il.Append(il.Create(OpCodes.Ldc_I4, elemAdjOffset));

        var toInt32 = _module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);

        for (int dim = 0; dim < e.Subscripts.Count; dim++)
        {
            int multiplier = e.Multipliers[dim];

            // Evaluate subscript expression → decimal on stack
            EmitExpression(il, e.Subscripts[dim]);

            // decimal → int32
            il.Append(il.Create(OpCodes.Call, toInt32));

            // (subscript - 1) * multiplier
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Sub));
            il.Append(il.Create(OpCodes.Ldc_I4, multiplier));
            il.Append(il.Create(OpCodes.Mul));

            // Add to running offset
            il.Append(il.Create(OpCodes.Add));
        }

        // Stack: [area, effectiveOffset]
    }

    /// <summary>
    /// Push (area, substringOffset, substringLength) for a reference modification.
    /// Composes the base location (static or element) with runtime start:length.
    /// </summary>
    private void EmitRefModAddress(ILProcessor il, IR.IrRefModLocation r)
    {
        var toInt32 = _module.ImportReference(
            typeof(Convert).GetMethod("ToInt32", new[] { typeof(decimal) })!);

        // Evaluate start and length first (into locals), before pushing base
        // start (1-based)
        EmitExpression(il, (Semantics.Bound.BoundExpression)r.Start);
        il.Append(il.Create(OpCodes.Call, toInt32));
        var startLocal = new VariableDefinition(_module.TypeSystem.Int32);
        _currentMethodDef!.Body.Variables.Add(startLocal);
        il.Append(il.Create(OpCodes.Stloc, startLocal));

        // length: expression or rest-of-field
        VariableDefinition lengthLocal;
        if (r.Length != null)
        {
            EmitExpression(il, (Semantics.Bound.BoundExpression)r.Length);
            il.Append(il.Create(OpCodes.Call, toInt32));
            lengthLocal = new VariableDefinition(_module.TypeSystem.Int32);
            _currentMethodDef!.Body.Variables.Add(lengthLocal);
            il.Append(il.Create(OpCodes.Stloc, lengthLocal));
        }
        else
        {
            // Rest-of-field: length = baseFieldLength - (start - 1)
            lengthLocal = new VariableDefinition(_module.TypeSystem.Int32);
            _currentMethodDef!.Body.Variables.Add(lengthLocal);
            il.Append(il.Create(OpCodes.Ldc_I4, r.BaseFieldLength));
            il.Append(il.Create(OpCodes.Ldloc, startLocal));
            il.Append(il.Create(OpCodes.Sub));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Add));
            il.Append(il.Create(OpCodes.Stloc, lengthLocal));
        }

        // Push base location (area, baseOffset)
        switch (r.Base)
        {
            case IR.IrStaticLocation s
                when s.Location.Area == StorageAreaKind.WorkingStorage
                  && TryGetExternalField(s.Location.Offset, out var rmExtField, out var rmAdjOffset):
                il.Append(il.Create(OpCodes.Ldsfld, rmExtField!));
                il.Append(il.Create(OpCodes.Ldc_I4, rmAdjOffset));
                break;

            case IR.IrStaticLocation s:
                EmitLoadBackingArray(il, s.Location.Area);
                il.Append(il.Create(OpCodes.Ldc_I4, s.Location.Offset));
                break;

            case IR.IrElementRef e:
                EmitElementAddress(il, e);
                break;

            default:
                throw new NotSupportedException($"Unsupported base location for ref mod: {r.Base.GetType().Name}");
        }

        // Stack: [area, baseOffset]

        // baseOffset + (start - 1)
        il.Append(il.Create(OpCodes.Ldloc, startLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Sub));
        il.Append(il.Create(OpCodes.Add));

        // Push length
        il.Append(il.Create(OpCodes.Ldloc, lengthLocal));

        // Stack: [area, substringOffset, substringLength]
    }

    private void EmitPicDisplay(ILProcessor il, IrPicDisplay disp)
    {
        // Strategy: push each operand as a string, then concat and call Console.WriteLine.
        // For a single operand, just push it directly. For multiple, use String.Concat.
        if (disp.Operands.Count == 0)
        {
            // DISPLAY with no operands: just output empty line
            il.Append(il.Create(OpCodes.Ldstr, ""));
        }
        else if (disp.Operands.Count == 1)
        {
            EmitDisplayOperand(il, disp.Operands[0]);
        }
        else
        {
            // Create a string array, populate it, then call String.Concat(string[])
            il.Append(il.Create(OpCodes.Ldc_I4, disp.Operands.Count));
            il.Append(il.Create(OpCodes.Newarr, _module.ImportReference(typeof(string))));

            for (int i = 0; i < disp.Operands.Count; i++)
            {
                il.Append(il.Create(OpCodes.Dup)); // keep array ref
                il.Append(il.Create(OpCodes.Ldc_I4, i));
                EmitDisplayOperand(il, disp.Operands[i]);
                il.Append(il.Create(OpCodes.Stelem_Ref));
            }

            var concat = _module.ImportReference(
                typeof(string).GetMethod("Concat", new[] { typeof(string[]) })!);
            il.Append(il.Create(OpCodes.Call, concat));
        }

        var consoleWriteLine = _module.ImportReference(
            typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, consoleWriteLine));
    }

    private void EmitDisplayOperand(ILProcessor il, DisplayOperand operand)
    {
        if (operand is DisplayLiteralOperand lit)
        {
            il.Append(il.Create(OpCodes.Ldstr, lit.Value));
        }
        else if (operand is DisplayFieldOperand field)
        {
            // Call PicRuntime.GetDisplayString(byte[] area, int offset, int length, PicDescriptor pic)
            EmitLocationArgsWithPic(il, field.Location);

            var method = _module.ImportReference(
                typeof(PicRuntime).GetMethod("GetDisplayString",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    private void EmitRuntimeCall(ILProcessor il, IrRuntimeCall rtc,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // For now, DISPLAY emits Console.WriteLine("statement executed")
        // Other runtime calls are NOPs
        if (rtc.MethodName == "CobolRuntime.Display")
        {
            // Argument is already on the stack (pushed by preceding IrLoadConst).
            var consoleWriteLine = _module.ImportReference(
                typeof(Console).GetMethod("WriteLine", new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, consoleWriteLine));
        }
        else if (rtc.MethodName == "CobolRuntime.WriteText")
        {
            // Two args on stack: fileName, text
            var writeText = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("WriteAfterAdvancingText",
                    new[] { typeof(string), typeof(string), typeof(int) })!);
            // Need to push advanceLines=1 for legacy WriteText calls
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Call, writeText));
        }
        else if (rtc.MethodName == "Self.Entry")
        {
            // Main calls Entry(Array.Empty<CobolDataPointer>())
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Newarr, _module.ImportReference(typeof(CobolDataPointer))));
            il.Append(il.Create(OpCodes.Call, _entryMethod!));
            il.Append(il.Create(OpCodes.Pop)); // discard int return value in Main
        }
        else if (rtc.MethodName == "FileRuntime.Init")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("Init",
                    Type.EmptyTypes)!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName is "CobolRuntime.OpenOutput" or "FileRuntime.OpenOutput")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("OpenOutput",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.OpenInput")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("OpenInput",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.OpenIO")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("OpenIO",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.OpenExtend")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("OpenExtend",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName is "CobolRuntime.CloseFile" or "FileRuntime.CloseFile")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("CloseFile",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.CloseFileWithLock")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("CloseFileWithLock",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.RegisterAlternateKey")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("RegisterAlternateKey",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.RegisterFileHandlerWithOrg")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("RegisterFileHandlerWithOrg",
                    new[] { typeof(string), typeof(string), typeof(int), typeof(bool),
                            typeof(string), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.SetFileOptional")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetFileOptional",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.SetFileLinage")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetFileLinage",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        // Other runtime calls: NOP for now
    }

    // ── Sort/Merge CIL emission ──

    private void EmitSortInit(ILProcessor il, IrSortInit inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.RecordLength));
        var m = _module.ImportReference(
            typeof(Runtime.SortRuntime).GetMethod("InitSortFile",
                new[] { typeof(string), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    private void EmitSortRelease(ILProcessor il, IrSortRelease inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        EmitLocationArgs(il, inst.Record);
        var m = _module.ImportReference(
            typeof(Runtime.SortRuntime).GetMethod("ReleaseRecord",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    private void EmitSortSort(ILProcessor il, IrSortSort inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        il.Append(il.Create(OpCodes.Ldstr, inst.KeysSpec));
        var m = _module.ImportReference(
            typeof(Runtime.SortRuntime).GetMethod("SortRecords",
                new[] { typeof(string), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    private void EmitSortReturn(ILProcessor il, IrSortReturn inst,
        Func<IR.IrValue, VariableDefinition> getLocal)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        EmitLocationArgs(il, inst.Record);
        var m = _module.ImportReference(
            typeof(Runtime.SortRuntime).GetMethod("ReturnRecord",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, m));
        // Store bool result
        var local = getLocal(inst.Result!.Value);
        il.Append(il.Create(OpCodes.Stloc, local));
    }

    private void EmitSortClose(ILProcessor il, IrSortClose inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.FileName));
        var m = _module.ImportReference(
            typeof(Runtime.SortRuntime).GetMethod("CloseSortFile",
                new[] { typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }

    private void EmitSortMerge(ILProcessor il, IrSortMerge inst)
    {
        il.Append(il.Create(OpCodes.Ldstr, inst.MergeFileName));
        il.Append(il.Create(OpCodes.Ldstr, inst.InputFileNames));
        il.Append(il.Create(OpCodes.Ldstr, inst.KeysSpec));
        var m = _module.ImportReference(
            typeof(Runtime.SortRuntime).GetMethod("MergeRecords",
                new[] { typeof(string), typeof(string), typeof(string) })!);
        il.Append(il.Create(OpCodes.Call, m));
    }
}
