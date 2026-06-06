// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
using CobolSharp.Compiler.Semantics;
using CobolSharp.Compiler.CodeGen.Emission;
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
    private MethodDefinition? _dispatchMethod;
    /// <summary>
    /// Per-program static bool: true once this program's file connectors have been registered for the
    /// current activation. Entry tests-and-sets it so RegisterFiles runs exactly once per activation —
    /// the run-unit main registers on its first (only) Entry; a CALLed subprogram registers on its own
    /// first Entry and SKIPS on subsequent CALLs so its open file/position is preserved. Reset to false
    /// on the re-initialization path (INITIAL program activation, or first CALL after CANCEL) so a
    /// canceled-and-re-CALLed program re-registers fresh connectors.
    /// </summary>
    private FieldDefinition? _filesRegisteredField;
    /// <summary>The program type's static constructor — GLOBAL USE handler registration is inserted
    /// before its terminal Ret in a late phase (after the Dispatch helper and handler bodies exist).</summary>
    private MethodDefinition? _cctorMethod;
    private VariableDefinition? _arithmeticStatusLocal;
    /// <summary>
    /// Cache for IrCachedLocation: maps cache key → (area, offset, length) locals.
    /// Cleared per method via <see cref="ClearCachedLocationLocals"/>.
    /// </summary>
    private readonly Dictionary<int, (VariableDefinition area, VariableDefinition offset, VariableDefinition length)>
        _cachedLocationLocals = new();

    // ── M003: Emission context and emitter instances ──
    // Created in constructor; methods will move to these classes in Stages 2-5.
    internal readonly EmissionContext _ctx;

    private CilEmitter(ModuleDefinition module)
    {
        _module = module;
        SeedPrimitiveTypes();

        // M003: Build emission context with shared state
        _ctx = new EmissionContext(module);

        // M003: Create emitter instances (empty shells — methods move in Stages 2-5)
        _ctx.ModuleSetup = new CilModuleSetup(_ctx);
        _ctx.ProgramState = new CilProgramStateEmitter(_ctx);
        _ctx.Location = new CilLocationEmitter(_ctx);
        _ctx.Expression = new CilExpressionEmitter(_ctx);
        _ctx.Data = new CilDataEmitter(_ctx);
        _ctx.Arithmetic = new CilArithmeticEmitter(_ctx);
        _ctx.Comparison = new CilComparisonEmitter(_ctx);
        _ctx.ControlFlow = new CilControlFlowEmitter(_ctx);
        _ctx.String = new CilStringEmitter(_ctx);
        _ctx.FileIo = new CilFileIoEmitter(_ctx);
        _ctx.EmitInstruction = EmitInstruction;
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

    /// <summary>
    /// Sync CilEmitter's local fields into EmissionContext so extracted emitters can access them.
    /// Called at key synchronization points during emission.
    /// </summary>
    private void SyncToContext()
    {
        _ctx.ProgramType = _programType;
        _ctx.ProgramStateField = _programStateField;
        _ctx.InitializeStateMethod = _initializeStateMethod;
        _ctx.AlterTableField = _alterTableField;
        _ctx.CurrentMethodDef = _currentMethodDef;
        _ctx.ArithmeticStatusLocal = _arithmeticStatusLocal;
        _ctx.SemanticModel = _semanticModel;
        _ctx.EntryMethod = _entryMethod;
        _ctx.DispatchMethod = _dispatchMethod;
        _ctx.LastCallResultField = _lastCallResultField;
        // Sync collection contents: copy CilEmitter's collections into _ctx's collections.
        // MethodMap (needed by CilControlFlowEmitter for PERFORM/THRU dispatch)
        _ctx.MethodMap.Clear();
        foreach (var kvp in _methodMap) _ctx.MethodMap[kvp.Key] = kvp.Value;
        // FieldMap (needed by CilDataEmitter for load/store)
        _ctx.FieldMap.Clear();
        foreach (var kvp in _fieldMap) _ctx.FieldMap[kvp.Key] = kvp.Value;
        // TypeMap (needed by CilModuleSetup)
        _ctx.TypeMap.Clear();
        foreach (var kvp in _typeMap) _ctx.TypeMap[kvp.Key] = kvp.Value;
        // LinkageFields
        _ctx.LinkageFields.Clear();
        foreach (var kvp in _linkageFields) _ctx.LinkageFields[kvp.Key] = kvp.Value;
        // ExternalFields
        _ctx.ExternalFields.Clear();
        foreach (var kvp in _externalFields) _ctx.ExternalFields[kvp.Key] = kvp.Value;
        // ExternalRanges
        _ctx.ExternalRanges.Clear();
        _ctx.ExternalRanges.AddRange(_externalRanges);
    }

    /// <summary>
    /// Sync mutable fields back from EmissionContext to CilEmitter's local fields.
    /// Called after emitter methods that may modify shared state.
    /// </summary>
    private void SyncFromContext()
    {
        _arithmeticStatusLocal = _ctx.ArithmeticStatusLocal;
        _lastCallResultField = _ctx.LastCallResultField;
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

        // M003: Sync after ProgramState setup so emitters called during VALUE clause init can access fields
        SyncToContext();

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

        // 4b. Emit the shared paragraph-dispatch helper before any body (PERFORM…THRU calls it).
        EmitDispatchHelper(ir);

        // M003: Sync all CilEmitter fields to EmissionContext before method body emission
        SyncToContext();

        // 5. Method bodies
        foreach (var m in ir.Methods)
            EmitMethodBody(m);

        // 6. Generate Entry method body (paragraph dispatch loop)
        EmitEntryMethodBody(ir);

        // 7. Generate alternate Entry methods for ENTRY statements
        foreach (var ep in ir.EntryPoints)
            EmitAlternateEntryMethod(ir, ep.Name, ep.UsingParams);

        // 8. GLOBAL USE declarative handlers: emit a public static handler per entry that runs the
        // declarative section via the Dispatch helper, and register it in GlobalUseDeclarativeRegistry
        // from the static constructor (so a contained program can dispatch it). Done last, after the
        // Dispatch helper and all paragraph bodies exist.
        EmitGlobalUseHandlers(ir);
    }

    /// <summary>
    /// Emit and register this program's GLOBAL USE AFTER ERROR declarative handlers (ISO §14.9.49.4 GR4).
    /// For each handler: a public static <c>__GlobalUse_i()</c> method runs the declarative section's
    /// paragraph range through the shared <c>Dispatch(start, end)</c> helper; the static constructor then
    /// registers a delegate to it in <c>GlobalUseDeclarativeRegistry</c> keyed by scope (or file name).
    /// </summary>
    private void EmitGlobalUseHandlers(IrModule ir)
    {
        if (ir.GlobalUseHandlers.Count == 0 || _dispatchMethod == null || _cctorMethod == null)
            return;

        // Registration is inserted before the cctor's terminal Ret so it runs at type init.
        var cctorIl = _cctorMethod.Body.GetILProcessor();
        var cctorRet = _cctorMethod.Body.Instructions[_cctorMethod.Body.Instructions.Count - 1];

        var actionCtor = _module.ImportReference(
            typeof(Action).GetConstructor(new[] { typeof(object), typeof(IntPtr) })!);
        var regForMode = _module.ImportReference(
            typeof(GlobalUseDeclarativeRegistry).GetMethod("RegisterForMode",
                new[] { typeof(int), typeof(Action) })!);
        var regForFile = _module.ImportReference(
            typeof(GlobalUseDeclarativeRegistry).GetMethod("RegisterForFile",
                new[] { typeof(string), typeof(Action) })!);

        for (int i = 0; i < ir.GlobalUseHandlers.Count; i++)
        {
            var (scope, fileName, start, end) = ir.GlobalUseHandlers[i];

            // public static void __GlobalUse_i() { Dispatch(start, end); }
            var handler = new MethodDefinition(
                $"__GlobalUse_{i}",
                MethodAttributes.Public | MethodAttributes.Static,
                _module.TypeSystem.Void);
            _programType!.Methods.Add(handler);
            var hIl = handler.Body.GetILProcessor();
            hIl.Append(hIl.Create(OpCodes.Ldc_I4, start));
            hIl.Append(hIl.Create(OpCodes.Ldc_I4, end));
            hIl.Append(hIl.Create(OpCodes.Call, _dispatchMethod));
            hIl.Append(hIl.Create(OpCodes.Pop)); // discard the returned next-pc
            hIl.Append(hIl.Create(OpCodes.Ret));

            // Registration in the static constructor: new Action(__GlobalUse_i) → Register*.
            if (scope < 0 && fileName != null)
            {
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Ldstr, fileName));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Ldnull));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Ldftn, handler));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Newobj, actionCtor));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Call, regForFile));
            }
            else
            {
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Ldc_I4, scope));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Ldnull));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Ldftn, handler));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Newobj, actionCtor));
                cctorIl.InsertBefore(cctorRet, cctorIl.Create(OpCodes.Call, regForMode));
            }
        }
    }

    /// <summary>
    /// Emit a contained program's cross-program GLOBAL USE declarative dispatch (ISO §14.9.49.4 GR4):
    /// GlobalUseDeclarativeRegistry.Dispatch(fileName, scope, excludeAtEnd, excludeInvalidKey). The
    /// registry applies the same exception gate as a local declarative and runs the containing program's
    /// registered handler when it fires.
    /// </summary>
    private void EmitDispatchGlobalUse(ILProcessor il, IrDispatchGlobalUse dgu)
    {
        il.Append(il.Create(OpCodes.Ldstr, dgu.FileName));
        il.Append(il.Create(OpCodes.Ldc_I4, dgu.Scope));
        il.Append(il.Create(dgu.ExcludeAtEnd ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(dgu.ExcludeInvalidKey ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        var m = _module.ImportReference(
            typeof(GlobalUseDeclarativeRegistry).GetMethod("Dispatch",
                new[] { typeof(string), typeof(int), typeof(bool), typeof(bool) })!);
        il.Append(il.Create(OpCodes.Call, m));
        il.Append(il.Create(OpCodes.Pop)); // discard the bool result
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

        // Return the program to its initial state when required (ISO §14.6.2.3.2; §14.9.5 GR3):
        //  - an INITIAL program: on every activation;
        //  - any program: on the first CALL after it was CANCELed (the registry flag is consumed).
        // Otherwise WORKING-STORAGE keeps its last-used state across calls (static items persist).
        // When the program IS re-initialized, also clear _filesRegistered so its file connectors are
        // re-registered below (a canceled-and-re-CALLed program, or each activation of an INITIAL
        // program, gets fresh connectors — ISO §14.9.5 GR3 returns the program to its initial state,
        // which includes its file connectors).
        if (ir.IsInitial)
        {
            il.Append(il.Create(OpCodes.Call, _initializeStateMethod!));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Stsfld, _filesRegisteredField!));
        }
        else
        {
            var skipReinit = il.Create(OpCodes.Nop);
            il.Append(il.Create(OpCodes.Ldstr, ir.Name));
            il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                typeof(CobolProgramRegistry).GetMethod("ConsumeReinitFlag", new[] { typeof(string) })!)));
            il.Append(il.Create(OpCodes.Brfalse, skipReinit));
            il.Append(il.Create(OpCodes.Call, _initializeStateMethod!));
            il.Append(il.Create(OpCodes.Ldc_I4_0));
            il.Append(il.Create(OpCodes.Stsfld, _filesRegisteredField!));
            il.Append(skipReinit);
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

        // Register this program's file connectors once per activation, before any paragraph runs.
        //   if (!_filesRegistered) { RegisterFiles(); _filesRegistered = true; }
        // This is the ONLY place a CALLed subprogram registers its internal files (its Main is dead
        // code — the run unit is entered through the run-unit main's Main, every subprogram through
        // Entry). The test-and-set means a re-CALL without an intervening CANCEL keeps the existing,
        // possibly-open connectors (so an OPEN file and its position survive across CALLs, ISO §14.6),
        // while the re-init path above cleared the flag for a fresh activation. FileRuntime.Init is NOT
        // called here — that belongs to the run-unit main's Main alone, so a subprogram never disposes
        // the caller's open files.
        if (ir.RegisterFilesMethod != null && _methodMap.TryGetValue(ir.RegisterFilesMethod, out var regFilesMd))
        {
            var skipRegister = il.Create(OpCodes.Nop);
            il.Append(il.Create(OpCodes.Ldsfld, _filesRegisteredField!));
            il.Append(il.Create(OpCodes.Brtrue, skipRegister));
            il.Append(il.Create(OpCodes.Call, regFilesMd));
            il.Append(il.Create(OpCodes.Ldc_I4_1));
            il.Append(il.Create(OpCodes.Stsfld, _filesRegisteredField!));
            il.Append(skipRegister);
        }

        // Paragraph dispatch loop — delegate to the shared Dispatch helper, starting at the first
        // non-declarative paragraph (EntryParagraphIndex) with no exit paragraph (exitPc = -1, so the
        // exit-paragraph check never fires; the loop runs until STOP RUN/off-end).
        if (ir.ParagraphDispatchOrder.Count > 0 && _dispatchMethod != null)
        {
            il.Append(il.Create(OpCodes.Ldc_I4, ir.EntryParagraphIndex));
            il.Append(il.Create(OpCodes.Ldc_I4_M1));
            il.Append(il.Create(OpCodes.Call, _dispatchMethod));
            il.Append(il.Create(OpCodes.Pop));
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
    /// Emit the shared paragraph-dispatch helper: <c>int Dispatch(int startPc, int exitPc)</c>.
    ///
    /// This is the single engine for ALL paragraph control flow — the main program loop and every
    /// PERFORM…THRU both call it. Each paragraph method returns the next pc (fall-through = myIndex+1,
    /// GO TO = target index, STOP RUN/EXIT PROGRAM/GOBACK = −1). The helper runs:
    /// <code>
    ///   pc = startPc;
    ///   while (pc &gt;= 0 &amp;&amp; pc &lt; N) {
    ///       bool atExit = (pc == exitPc);
    ///       pc = paragraphs[pc]();           // execute paragraph, follow its returned next-pc
    ///       if (atExit &amp;&amp; pc == exitPc + 1) break;   // exit paragraph fell through → return
    ///   }
    ///   return pc;
    /// </code>
    /// This is a RETURN-ADDRESS model, not a physical-range model: it follows control flow ANYWHERE
    /// (GO TOs that leave and re-enter the range, and inverted THRU ranges where the exit paragraph
    /// physically precedes the entry), returning only when the named exit paragraph (exitPc) completes
    /// by falling off its end. The main loop passes exitPc = −1 so that check never fires and it runs
    /// until STOP RUN (pc &lt; 0) or off the end (pc &gt;= N).
    /// </summary>
    private void EmitDispatchHelper(IrModule ir)
    {
        var paragraphs = ir.ParagraphDispatchOrder;
        int count = paragraphs.Count;
        if (count == 0) return;

        var md = new MethodDefinition(
            "Dispatch",
            MethodAttributes.Private | MethodAttributes.Static,
            _module.TypeSystem.Int32);
        md.Parameters.Add(new ParameterDefinition("startPc", ParameterAttributes.None, _module.TypeSystem.Int32));
        md.Parameters.Add(new ParameterDefinition("exitPc", ParameterAttributes.None, _module.TypeSystem.Int32));
        _programType!.Methods.Add(md);
        _dispatchMethod = md;

        md.Body.InitLocals = true;
        var il = md.Body.GetILProcessor();

        var pcLocal = new VariableDefinition(_module.TypeSystem.Int32);    // current pc
        var atExitLocal = new VariableDefinition(_module.TypeSystem.Int32); // (pc == exitPc) before the call
        md.Body.Variables.Add(pcLocal);
        md.Body.Variables.Add(atExitLocal);

        // pc = startPc
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Stloc, pcLocal));

        var loopLabel = il.Create(OpCodes.Nop);
        var checkLabel = il.Create(OpCodes.Nop);
        var exitLabel = il.Create(OpCodes.Nop);

        il.Append(loopLabel);

        // if (pc < 0) goto EXIT          — STOP RUN / EXIT PROGRAM / GOBACK
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Blt, exitLabel));
        // if (pc >= count) goto EXIT     — fell off the end of the program
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldc_I4, count));
        il.Append(il.Create(OpCodes.Bge, exitLabel));

        // atExit = (pc == exitPc) ? 1 : 0   — capture BEFORE the call (the call overwrites pc)
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldarg_1));
        il.Append(il.Create(OpCodes.Ceq));
        il.Append(il.Create(OpCodes.Stloc, atExitLocal));

        // switch (pc) { case i: pc = Para_i(); goto CHECK; }
        var caseLabels = new Instruction[count];
        for (int i = 0; i < count; i++)
            caseLabels[i] = il.Create(OpCodes.Nop);
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Switch, caseLabels));
        il.Append(il.Create(OpCodes.Br, exitLabel)); // default (unreachable: pc in [0,count) here)

        for (int i = 0; i < count; i++)
        {
            il.Append(caseLabels[i]);
            il.Append(il.Create(OpCodes.Call, _methodMap[paragraphs[i]])); // pc = paragraph(): next pc
            il.Append(il.Create(OpCodes.Stloc, pcLocal));
            il.Append(il.Create(OpCodes.Br, checkLabel));
        }

        // CHECK: if (atExit && pc == exitPc + 1) return; else loop
        il.Append(checkLabel);
        il.Append(il.Create(OpCodes.Ldloc, atExitLocal));
        il.Append(il.Create(OpCodes.Brfalse, loopLabel));        // not the exit paragraph → keep going
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ldarg_1));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Add));
        il.Append(il.Create(OpCodes.Bne_Un, loopLabel));         // exit paragraph GO TO'd elsewhere → keep going
        // else: exit paragraph fell through to exitPc+1 → return from PERFORM range
        il.Append(il.Create(OpCodes.Br, exitLabel));

        // EXIT: return pc
        il.Append(exitLabel);
        il.Append(il.Create(OpCodes.Ldloc, pcLocal));
        il.Append(il.Create(OpCodes.Ret));
    }

    /// <summary>
    /// Create the static ProgramState field, an InitializeState() method that puts the program into
    /// its initial state, and a static constructor that performs the first initialization.
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

        // Data-model migration S3 (docs/RECORD_STRUCT_STORAGE_DESIGN.md): one static .NET string field per item
        // flipped out of the byte areas. Defined here (before method bodies are emitted, EmitModule order) so
        // IrTypedFieldLocation resolves; initialized in InitializeState below. Empty unless EnableTypedFields is on.
        foreach (var tf in ir.TypedFieldDefs)
        {
            var typedFd = new FieldDefinition(tf.Name,
                FieldAttributes.Public | FieldAttributes.Static, _module.TypeSystem.String);
            _programType.Fields.Add(typedFd);
            _ctx.TypedFields[tf.Name] = typedFd;
        }

        // Static bool: have this program's file connectors been registered for the current activation?
        // Entry tests-and-sets it; the re-init path (INITIAL / post-CANCEL) clears it. (See field doc.)
        _filesRegisteredField = new FieldDefinition(
            "_filesRegistered",
            FieldAttributes.Private | FieldAttributes.Static,
            _module.TypeSystem.Boolean);
        _programType.Fields.Add(_filesRegisteredField);

        // InitializeState(): (re)allocate ProgramState and apply every initial-state action —
        // ProgramState allocation, EXTERNAL binding, VALUE clauses, ALTER defaults, LOCAL-STORAGE
        // snapshot. Called once from .cctor and again from Entry to return the program to its
        // initial state — on every activation of an INITIAL program, and on the first CALL after a
        // CANCEL (ISO §14.6.2.3.2 Initial state; §14.9.5 GR3). Re-running it does not disturb
        // EXTERNAL data (its storage is the shared array; EXTERNAL items cannot carry VALUE), so
        // §14.9.5 GR8 is preserved.
        _initializeStateMethod = new MethodDefinition(
            "InitializeState",
            MethodAttributes.Private | MethodAttributes.Static,
            _module.TypeSystem.Void);
        _programType.Methods.Add(_initializeStateMethod);

        var initIl = _initializeStateMethod.Body.GetILProcessor();
        EmitProgramStateAllocation(initIl, wsSize, fileSize, lsSize);
        EmitExternalStorageInitialization(initIl);
        // M003: Sync after EXTERNAL init so VALUE clause init can use location emitters
        SyncToContext();
        EmitValueClauseInitialization(initIl);
        // S3: COBOL-correct init of each typed-native field (never default(null) — ADR §1.7). InitValue is
        // already padded/truncated to the field width by Binder.CollectTypedFields.
        foreach (var tf in ir.TypedFieldDefs)
        {
            initIl.Append(initIl.Create(OpCodes.Ldstr, tf.InitValue));
            initIl.Append(initIl.Create(OpCodes.Stsfld, _ctx.TypedFields[tf.Name]));
        }
        EmitAlterTableInitialization(initIl, ir);
        // Snapshot LOCAL-STORAGE defaults after VALUE clause init (used by ReinitializeLocalStorage).
        if (lsSize > 0)
            EmitLocalStorageDefaultsSnapshot(initIl, lsSize);
        initIl.Append(initIl.Create(OpCodes.Ret));

        // Static constructor: .cctor — first initialization.
        var cctor = new MethodDefinition(
            ".cctor",
            MethodAttributes.Private | MethodAttributes.Static |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            _module.TypeSystem.Void);
        _programType.Methods.Add(cctor);
        _cctorMethod = cctor;
        var cctorIl = cctor.Body.GetILProcessor();
        cctorIl.Append(cctorIl.Create(OpCodes.Call, _initializeStateMethod));
        cctorIl.Append(cctorIl.Create(OpCodes.Ret));
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

            _ctx.Location.EmitLoadBackingArrayOrExternal(il, loc.Value.Area, loc.Value.Offset, out var figAdjOffset);
            il.Append(il.Create(OpCodes.Ldc_I4, figAdjOffset));
            il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));
            _ctx.Expression.EmitLoadPicDescriptor(il, loc.Value.Pic);
            il.Append(il.Create(OpCodes.Ldc_I4, (int)kvp.Value));
            il.Append(il.Create(OpCodes.Call, moveFigMethod));
        }

        // Literal/numeric VALUE clauses
        var moveStringToOccursMethod = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "MoveStringToOccursField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(string) })!);

        // National items store VALUE literals UTF-16-encoded, not as 1-byte ASCII fill.
        var moveNationalToOccursMethod = _module.ImportReference(
            typeof(CobolSharp.Runtime.PicRuntime).GetMethod(
                "MoveNationalLiteralToOccursField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(string) })!);

        // Boolean items store VALUE literals as '0'/'1' bytes with '0' fill (not space).
        var moveBooleanToOccursMethod = _module.ImportReference(
            typeof(CobolSharp.Runtime.PicRuntime).GetMethod(
                "MoveBooleanLiteralToOccursField",
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

            _ctx.Location.EmitLoadBackingArrayOrExternal(il, loc.Value.Area, loc.Value.Offset, out var valAdjOffset);

            if (init.Value is decimal d && loc.Value.Pic.IsNumeric)
            {
                il.Append(il.Create(OpCodes.Ldc_I4, valAdjOffset));
                il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));
                _ctx.Expression.EmitLoadPicDescriptor(il, loc.Value.Pic, suppressBlankWhenZero: true);
                _ctx.Expression.EmitLoadDecimal(il, d);
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
                il.Append(il.Create(OpCodes.Call,
                    loc.Value.Pic.Category.IsNationalLike() ? moveNationalToOccursMethod
                    : loc.Value.Pic.Category.IsBooleanLike() ? moveBooleanToOccursMethod
                    : moveStringToOccursMethod));
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
            // EXTERNAL level-01 records share storage across the run unit (§13.18.22). Two cases:
            //   • WORKING-STORAGE 01 ... IS EXTERNAL (IC226A)
            //   • the 01 record of an FD ... IS EXTERNAL file — Area=FileSection (IC227A); the record AREA
            //     is the externalized entity, keyed by the record name.
            // Both are keyed by the record NAME, so the same name in a separately-compiled program resolves
            // to the same ExternalStorage byte[]. The Area is carried into _externalRanges so the redirect
            // only fires for references in the SAME area (a FileSection range never redirects a WS offset).
            if (!data.IsExternal || data.LevelNumber != 1
                || data.Area is not (StorageAreaKind.WorkingStorage or StorageAreaKind.FileSection))
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

            _externalRanges.Add((data.Area, loc.Value.Offset, loc.Value.Length, extField));

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

    /// <summary>EXTERNAL record offset ranges: (area, offset, length) → shared byte[] field. The Area
    /// discriminator keeps WorkingStorage and FileSection offset namespaces from cross-redirecting (see
    /// <see cref="Emission.EmissionContext.ExternalRanges"/>).</summary>
    private readonly List<(StorageAreaKind Area, int Offset, int Length, FieldDefinition ExtField)> _externalRanges = [];


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
        // M003: Sync per-method state to EmissionContext
        _ctx.CurrentMethodDef = md;
        _ctx.ArithmeticStatusLocal = null;
        _ctx.CachedLocationLocals.Clear();

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

        // M003: Sync back from EmissionContext (emitters may have modified shared state)
        SyncFromContext();
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
            // ── Data movement ──
            case IrLoadConst lc: _ctx.Data.EmitLoadConst(il, lc, getLocal); break;
            case IrLoadField lf: _ctx.Data.EmitLoadField(il, lf, getLocal); break;
            case IrStoreField sf: _ctx.Data.EmitStoreField(il, sf, getLocal); break;
            case IrMove mv: _ctx.Data.EmitMove(il, mv, getLocal); break;
            case IrMoveStringToField ms: _ctx.Data.EmitMoveStringToField(il, ms, getLocal); break;
            case IrMoveFigurative mf: _ctx.Data.EmitMoveFigurative(il, mf); break;
            case IrMoveAllLiteral mal: _ctx.Data.EmitMoveAllLiteral(il, mal); break;
            case IrMoveFieldToField mf: _ctx.Data.EmitMoveFieldToField(il, mf); break;
            case IrPicMoveLiteralNumeric movLit: _ctx.Data.EmitPicMoveLiteralNumeric(il, movLit); break;
            case IrAccept acc: _ctx.Data.EmitAccept(il, acc); break;
            case IrPicDisplay disp: _ctx.Data.EmitPicDisplay(il, disp); break;

            // ── Control flow ──
            case IrBranch br: _ctx.ControlFlow.EmitBranch(il, br, getLocal, blockLabels); break;
            case IrJump j: _ctx.ControlFlow.EmitJump(il, j, blockLabels); break;
            case IrBranchIfFalse bif: _ctx.ControlFlow.EmitBranchIfFalse(il, bif, getLocal, blockLabels); break;
            case IrReturn ret: _ctx.ControlFlow.EmitReturn(il, ret, getLocal); break;
            case IrReturnConst rc: _ctx.ControlFlow.EmitReturnConst(il, rc); break;
            case IrReturnAlterable ra: _ctx.ControlFlow.EmitReturnAlterable(il, ra); break;
            case IrAlter alt: _ctx.ControlFlow.EmitAlter(il, alt); break;
            case IrGoToDepending gtd: _ctx.ControlFlow.EmitGoToDepending(il, gtd, getLocal); break;
            case IrStopRun: _ctx.ControlFlow.EmitStopRun(il); break;
            case IrExitProgram: _ctx.ControlFlow.EmitExitProgram(il); break;
            case IrGoBack: _ctx.ControlFlow.EmitGoBack(il); break;
            case IrSetSwitch ss: _ctx.ControlFlow.EmitSetSwitch(il, ss); break;
            case IrTestSwitch ts: _ctx.ControlFlow.EmitTestSwitch(il, ts, getLocal); break;
            case IrPerform perf: _ctx.ControlFlow.EmitPerform(il, perf); break;
            case IrPerformTimes perfTimes: _ctx.ControlFlow.EmitPerformTimes(il, perfTimes, _currentMethodDef!); break;
            case IrPerformInlineTimes pit: _ctx.ControlFlow.EmitPerformInlineTimes(il, pit, _currentMethodDef!, getLocal, blockLabels); break;
            case IrPerformThru thru: _ctx.ControlFlow.EmitPerformThru(il, thru, _currentMethodDef!); break;

            // ── Arithmetic ──
            case IrBinary bin: _ctx.Arithmetic.EmitBinary(il, bin, getLocal); break;
            case IrPicMultiply mul: _ctx.Arithmetic.EmitPicMultiply(il, mul); break;
            case IrPicMultiplyLiteral mulLit: _ctx.Arithmetic.EmitPicMultiplyLiteral(il, mulLit); break;
            case IrPicAdd addInst: _ctx.Arithmetic.EmitPicAdd(il, addInst); break;
            case IrPicAddLiteral addLit: _ctx.Arithmetic.EmitPicAddLiteral(il, addLit); break;
            case IrPicSubtract subInst: _ctx.Arithmetic.EmitPicSubtract(il, subInst); break;
            case IrPicSubtractLiteral subLit: _ctx.Arithmetic.EmitPicSubtractLiteral(il, subLit); break;
            case IrPicDivide divInst: _ctx.Arithmetic.EmitPicDivide(il, divInst); break;
            case IrPicDivideLiteral divLit: _ctx.Arithmetic.EmitPicDivideLiteral(il, divLit); break;
            case IrAddAccumulatedToTarget addAcc: _ctx.Arithmetic.EmitAddAccumulatedToTarget(il, addAcc, getLocal); break;
            case IrMoveAccumulatedToTarget moveAcc: _ctx.Arithmetic.EmitMoveAccumulatedToTarget(il, moveAcc, getLocal); break;
            case IrSubtractAccumulatedFromTarget subAcc: _ctx.Arithmetic.EmitSubtractAccumulatedFromTarget(il, subAcc, getLocal); break;
            case IrComputeStore compStore: _ctx.Arithmetic.EmitComputeStore(il, compStore); break;
            case IrCobolRemainder rem: _ctx.Arithmetic.EmitCobolRemainder(il, rem, getLocal); break;
            case IrInitArithmeticStatus: _ctx.Arithmetic.EmitInitArithmeticStatus(il, _currentMethodDef!); break;

            case IrLoadSizeError lse:
            {
                var statusLocal = _ctx.Arithmetic.EnsureArithmeticStatusLocal(_currentMethodDef!);
                il.Append(il.Create(OpCodes.Ldloca, statusLocal));
                var sizeErrorGetter = _module.ImportReference(
                    typeof(ArithmeticStatus).GetProperty("SizeError")!.GetGetMethod()!);
                il.Append(il.Create(OpCodes.Call, sizeErrorGetter));
                if (lse.Result.HasValue)
                    il.Append(il.Create(OpCodes.Stloc, getLocal(lse.Result.Value)));
                break;
            }

            case IrInitAccumulator initAcc:
            {
                var local = getLocal(initAcc.Result!.Value);
                _ctx.Expression.EmitLoadDecimal(il, 0m);
                il.Append(il.Create(OpCodes.Stloc, local));
                break;
            }

            case IrAccumulateField accField:
            {
                var accLocal = getLocal(accField.Accumulator);
                il.Append(il.Create(OpCodes.Ldloc, accLocal));
                _ctx.Location.EmitLocationArgsWithPic(il, accField.Source);
                il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                    typeof(Runtime.PicRuntime).GetMethod("DecodeNumeric",
                        new[] { typeof(byte[]), typeof(int), typeof(int),
                                typeof(Runtime.PicDescriptor) })!)));
                il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                    typeof(decimal).GetMethod("op_Addition",
                        new[] { typeof(decimal), typeof(decimal) })!)));
                il.Append(il.Create(OpCodes.Stloc, accLocal));
                break;
            }

            case IrAccumulateLiteral accLit:
            {
                var accLocal = getLocal(accLit.Accumulator);
                il.Append(il.Create(OpCodes.Ldloc, accLocal));
                _ctx.Expression.EmitLoadDecimal(il, accLit.Value);
                il.Append(il.Create(OpCodes.Call, _module.ImportReference(
                    typeof(decimal).GetMethod("op_Addition",
                        new[] { typeof(decimal), typeof(decimal) })!)));
                il.Append(il.Create(OpCodes.Stloc, accLocal));
                break;
            }

            case IrComputeIntoAccumulator compAccum:
                _ctx.Expression.EmitIrExpression(il, compAccum.Expression);
                il.Append(il.Create(OpCodes.Stloc, getLocal(compAccum.Accumulator)));
                break;

            // ── Comparison ──
            case IrClassCondition classInst: _ctx.Comparison.EmitClassCondition(il, classInst, getLocal); break;
            case IrUserClassCondition userClassInst: _ctx.Comparison.EmitUserClassCondition(il, userClassInst, getLocal); break;
            case IrPicCompare cmp: _ctx.Comparison.EmitPicCompare(il, cmp, getLocal); break;
            case IrPicCompareLiteral cmpLit: _ctx.Comparison.EmitPicCompareLiteral(il, cmpLit, getLocal); break;
            case IrPicCompareAccumulator cmpAccum: _ctx.Comparison.EmitPicCompareAccumulator(il, cmpAccum, getLocal); break;
            case IrDecimalCompare decCmp: _ctx.Comparison.EmitDecimalCompare(il, decCmp, getLocal); break;
            case IrDecimalCompareLiteral decLitCmp: _ctx.Comparison.EmitDecimalCompareLiteral(il, decLitCmp, getLocal); break;
            case IrStringCompareLiteral strCmp: _ctx.Comparison.EmitStringCompareLiteral(il, strCmp, getLocal); break;
            case IrStringExprCompare strExprCmp: _ctx.Comparison.EmitStringExprCompare(il, strExprCmp, getLocal); break;
            case IrStringCompare strFldCmp: _ctx.Comparison.EmitStringCompare(il, strFldCmp, getLocal); break;
            case IrStringCompareWithSequence seqCmp: _ctx.Comparison.EmitStringCompareWithSequence(il, seqCmp, getLocal); break;
            case IrStringCompareLiteralWithSequence seqLitCmp: _ctx.Comparison.EmitStringCompareLiteralWithSequence(il, seqLitCmp, getLocal); break;

            // ── String operations ──
            case IrStringStatement strStmt: _ctx.String.EmitStringStatement(il, strStmt, getLocal); break;
            case IrUnstringStatement unstrStmt: _ctx.String.EmitUnstringStatement(il, unstrStmt, getLocal); break;
            case IrInspectTallying it: _ctx.String.EmitInspectTallying(il, it); break;
            case IrInspectReplacing ir: _ctx.String.EmitInspectReplacing(il, ir); break;
            case IrInspectConvert ic: _ctx.String.EmitInspectConvert(il, ic); break;

            // ── File I/O ──
            case IrWriteRecordFromStorage wr: _ctx.FileIo.EmitWriteRecordFromStorage(il, wr); break;
            case IrReportPlaceField rpf: _ctx.FileIo.EmitReportPlaceField(il, rpf); break;
            case IrReportPlaceLiteral rpl: _ctx.FileIo.EmitReportPlaceLiteral(il, rpl); break;
            case IrReportRegisterDataField rrd: _ctx.FileIo.EmitReportRegisterDataField(il, rrd); break;
            case IrReportRegisterControl rrc: _ctx.FileIo.EmitReportRegisterControl(il, rrc); break;
            case IrReportRegisterSum rrs: _ctx.FileIo.EmitReportRegisterSum(il, rrs); break;
            case IrWriteRecordVariable wrv: _ctx.FileIo.EmitWriteRecordVariable(il, wrv); break;
            case IrStoreRecordLength srl: _ctx.FileIo.EmitStoreRecordLength(il, srl); break;
            case IrSetRelativeKey srk: _ctx.FileIo.EmitSetRelativeKey(il, srk); break;
            case IrSetIndexedKey sik: _ctx.FileIo.EmitSetIndexedKey(il, sik); break;
            case IrStoreRelativeKey strk: _ctx.FileIo.EmitStoreRelativeKey(il, strk); break;
            case IrRewriteRecordFromStorage rw: _ctx.FileIo.EmitRewriteRecordFromStorage(il, rw); break;
            case IrWriteAdvancing waa: _ctx.FileIo.EmitWriteAdvancing(il, waa); break;
            case IrReadRecordToStorage rd: _ctx.FileIo.EmitReadRecordToStorage(il, rd); break;
            case IrReadPreviousToStorage rdp: _ctx.FileIo.EmitReadPreviousToStorage(il, rdp); break;
            case IrReadByKey rbk: _ctx.FileIo.EmitReadByKey(il, rbk); break;
            case IrStoreFileStatus sfs: _ctx.FileIo.EmitStoreFileStatus(il, sfs); break;
            case IrCheckFileAtEnd chk: _ctx.FileIo.EmitCheckFileAtEnd(il, chk, getLocal); break;
            case IrCheckEndOfPage eop: _ctx.FileIo.EmitCheckEndOfPage(il, eop, getLocal); break;
            case IrInitLinage initLin: _ctx.FileIo.EmitInitLinage(il, initLin); break;
            case IrDeleteRecord del: _ctx.FileIo.EmitDeleteRecord(il, del); break;
            case IrDeleteFile delFile: _ctx.FileIo.EmitDeleteFile(il, delFile); break;
            case IrStartFile sf: _ctx.FileIo.EmitStartFile(il, sf); break;
            case IrCheckFileInvalidKey cik: _ctx.FileIo.EmitCheckFileInvalidKey(il, cik, getLocal); break;
            case IrCheckUseDeclarative cud: _ctx.FileIo.EmitCheckUseDeclarative(il, cud, getLocal); break;
            case IrDispatchGlobalUse dgu: EmitDispatchGlobalUse(il, dgu); break;
            case IrSortInit sortInit: _ctx.FileIo.EmitSortInit(il, sortInit); break;
            case IrSortRelease sortRel: _ctx.FileIo.EmitSortRelease(il, sortRel); break;
            case IrSortReleaseVariable sortRelV: _ctx.FileIo.EmitSortReleaseVariable(il, sortRelV); break;
            case IrSortReleaseFromDepending sortRelD: _ctx.FileIo.EmitSortReleaseFromDepending(il, sortRelD); break;
            case IrSortRewind sortRew: _ctx.FileIo.EmitSortRewind(il, sortRew); break;
            case IrSortSort sortSort: _ctx.FileIo.EmitSortSort(il, sortSort); break;
            case IrSortReturn sortRet: _ctx.FileIo.EmitSortReturn(il, sortRet, getLocal); break;
            case IrSortReturnStoreLength sortRetLen: _ctx.FileIo.EmitSortReturnStoreLength(il, sortRetLen); break;
            case IrSortGivingWriteVariable sortGwV: _ctx.FileIo.EmitSortGivingWriteVariable(il, sortGwV); break;
            case IrSortClose sortClose: _ctx.FileIo.EmitSortClose(il, sortClose); break;
            case IrSortMerge sortMerge: _ctx.FileIo.EmitSortMerge(il, sortMerge); break;
            case IrTableSort tableSort: _ctx.FileIo.EmitTableSort(il, tableSort); break;

            // ── Expression / intrinsics ──
            case IrFunctionCall funcCall: _ctx.Expression.EmitFunctionCall(il, funcCall); break;

            // ── Orchestration (stays in CilEmitter) ──
            case IrCallProgram callProg: EmitCallProgram(il, callProg, _currentMethodDef!); break;
            case IrCheckCallException checkExc: EmitCheckCallException(il, checkExc, getLocal); break;
            case IrCall call: EmitCall(il, call, getLocal); break;
            case IrParagraphDispatch dispatch: EmitParagraphDispatch(il, dispatch, _currentMethodDef!); break;
            case IrRuntimeCall rtc: EmitRuntimeCall(il, rtc, getLocal); break;

            case IrCancelProgram cancelProg:
                if (cancelProg.IsDynamic && cancelProg.TargetLocation != null)
                {
                    // CANCEL identifier: read the program-name from storage at runtime.
                    _ctx.Location.EmitLocationArgsWithPic(il, cancelProg.TargetLocation);
                    il.Append(il.Create(OpCodes.Call,
                        _module.ImportReference(typeof(PicRuntime).GetMethod("GetDisplayString",
                            new[] { typeof(byte[]), typeof(int), typeof(int), typeof(PicDescriptor) })!)));
                }
                else
                {
                    il.Append(il.Create(OpCodes.Ldstr, cancelProg.ProgramName));
                }
                il.Append(il.Create(OpCodes.Call,
                    _module.ImportReference(typeof(CobolProgramRegistry).GetMethod("Cancel",
                        new[] { typeof(string) })!)));
                break;

            case IrSetBool sb:
            {
                il.Append(il.Create(sb.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
                if (sb.Result.HasValue)
                    il.Append(il.Create(OpCodes.Stloc, getLocal(sb.Result.Value)));
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
                    il.Append(il.Create(log.Op switch
                    {
                        IrLogicalOp.And => OpCodes.And,
                        IrLogicalOp.Xor => OpCodes.Xor, // boolean operands are 0/1, so xor == exactly-one-true
                        _ => OpCodes.Or,
                    }));
                }
                if (log.Result.HasValue)
                    il.Append(il.Create(OpCodes.Stloc, getLocal(log.Result.Value)));
                break;
            }

            default:
                throw new NotSupportedException(
                    $"IR instruction {inst.GetType().Name} not supported in CIL emission.");
        }
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
            _ctx.Location.EmitLocationArgs(il, arg.Source);

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
            _ctx.Location.EmitLocationArgs(il, callProg.ReturningTarget!);
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
            _ctx.Location.EmitLocationArgsWithPic(il, callProg.TargetLocation);
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


    // ── ACCEPT ──


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
            il.Append(il.Create(OpCodes.Call, target));   // pc = paragraph(): next pc
            il.Append(il.Create(OpCodes.Stloc, pcLocal));
            il.Append(il.Create(OpCodes.Br, loopLabel));
        }

        // EXIT — wrap dispatch loop exit with StopRunException catch
        // StopRunException may be thrown by STOP RUN from any paragraph
        il.Append(exitLabel);
    }


    private void EmitRuntimeCall(ILProcessor il, IrRuntimeCall rtc,
        Func<IrValue, VariableDefinition> getLocal)
    {
        // Dispatch an IrRuntimeCall to its runtime helper by method name. (Real DISPLAY lowers via
        // its own emitter, not here; the legacy "CobolRuntime.Display" branch had no IR producer
        // and was removed — DEVLOG 232.)
        if (rtc.MethodName == "CobolRuntime.WriteText")
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
            // ISO §14.6.11 / §14.4: at normal run-unit termination the runtime executes an implicit CLOSE
            // (no phrases) for every file still in the open mode, persisting buffered I-O changes. STOP RUN
            // just exits the dispatch loop (returns from Entry) without closing, so without this a file the
            // program never explicitly CLOSEs loses its writes/rewrites (RL208A — RL207A's REWRITEs were
            // discarded). CloseAll disposes each handler, which persists it; an already-closed handler is a
            // guarded no-op. Scoped to Main (the run unit), so a CALLed subprogram's EXIT PROGRAM/GOBACK is
            // unaffected (those return to the caller, not the run unit).
            var closeAll = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("CloseAll", Type.EmptyTypes)!);
            il.Append(il.Create(OpCodes.Call, closeAll));
        }
        else if (rtc.MethodName == "FileRuntime.Init")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("Init",
                    Type.EmptyTypes)!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.OpenOutput")
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
        else if (rtc.MethodName == "FileRuntime.CloseReelUnit")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("CloseReelUnit",
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
        else if (rtc.MethodName == "FileRuntime.SetRelativeAccess")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetRelativeAccess",
                    new[] { typeof(string), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.SetIndexedAccess")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetIndexedAccess",
                    new[] { typeof(string), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.EnterUseDeclarative")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("EnterUseDeclarative",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.ExitUseDeclarative")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("ExitUseDeclarative",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.SetIndexedVarying")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetIndexedVarying",
                    new[] { typeof(string), typeof(bool), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.SetRelativeVarying")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetRelativeVarying",
                    new[] { typeof(string), typeof(bool), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "FileRuntime.SetSequentialVarying")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("SetSequentialVarying",
                    new[] { typeof(string), typeof(bool), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        // ── Report Writer (ISO §14.9) ──
        else if (rtc.MethodName == "ReportWriterRuntime.InitiateReport")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("InitiateReport",
                    new[] { typeof(string), typeof(string), typeof(int), typeof(int), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.RegisterAutoGroup")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("RegisterAutoGroup",
                    new[] { typeof(string), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.RegisterAutoField")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("RegisterAutoField",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.RegisterAutoSumField")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("RegisterAutoSumField",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.RegisterGroupIndicateField")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("RegisterGroupIndicateField",
                    new[] { typeof(string), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.BeginLine")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("BeginLine",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.EmitGroup")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("EmitGroup",
                    new[] { typeof(string), typeof(int), typeof(bool) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        else if (rtc.MethodName == "ReportWriterRuntime.TerminateReport")
        {
            var m = _module.ImportReference(
                typeof(CobolSharp.Runtime.ReportWriterRuntime).GetMethod("TerminateReport",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, m));
        }
        // Other runtime calls: NOP for now
    }

}
