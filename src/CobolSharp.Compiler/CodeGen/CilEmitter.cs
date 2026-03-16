// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Mono.Cecil;
using Mono.Cecil.Cil;
using CobolSharp.Compiler.IR;
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
    private MethodDefinition? _currentMethodDef;
    private VariableDefinition? _arithmeticStatusLocal;

    private CilEmitter(ModuleDefinition module)
    {
        _module = module;
        SeedPrimitiveTypes();
    }

    private Semantics.SemanticModel? _semanticModel;

    public static AssemblyDefinition EmitAssembly(IrModule ir, string assemblyName,
        Semantics.SemanticModel? semanticModel = null)
    {
        var asmName = new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0));
        var asm = AssemblyDefinition.CreateAssembly(asmName, assemblyName, ModuleKind.Console);
        var emitter = new CilEmitter(asm.MainModule);
        emitter._semanticModel = semanticModel;
        emitter.EmitModule(ir);

        // Set entry point to Main method
        if (emitter._methodMap.Count > 0)
        {
            var mainMethod = emitter._methodMap.Values
                .FirstOrDefault(m => m.Name == "Main");
            if (mainMethod != null)
                asm.EntryPoint = mainMethod;
        }

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

        // 4. Method bodies
        Console.Error.WriteLine($"[CIL] IR module '{ir.Name}' has {ir.Methods.Count} methods:");
        foreach (var m in ir.Methods)
            Console.Error.WriteLine($"[CIL]   {m.Name}: {m.Blocks.Count} blocks, {m.Blocks.Sum(b => b.Instructions.Count)} instr");

        foreach (var m in ir.Methods)
            EmitMethodBody(m);
    }

    /// <summary>
    /// Create a static ProgramState field and static constructor that allocates it.
    /// </summary>
    private void EmitProgramState(IrModule ir)
    {
        // Use sizes computed by ComputeStorageLayout
        int wsSize = _semanticModel?.WorkingStorageSize ?? 4096;
        int fileSize = _semanticModel?.FileSectionSize ?? 1024;
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
        il.Append(il.Create(OpCodes.Ldc_I4, wsSize));
        il.Append(il.Create(OpCodes.Ldc_I4, fileSize));

        var ctor = _module.ImportReference(
            typeof(CobolSharp.Runtime.ProgramState)
                .GetConstructor(new[] { typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Newobj, ctor));
        il.Append(il.Create(OpCodes.Stsfld, _programStateField));

        // Apply VALUE clauses: write initial values into backing storage
        if (_semanticModel != null)
        {
            var moveStringMethod = _module.ImportReference(
                typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                    "MoveStringToField",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);

            foreach (var kvp in _semanticModel.InitialValues)
            {
                var loc = _semanticModel.GetStorageLocation(kvp.Key);
                if (!loc.HasValue) continue;

                var init = kvp.Value;

                // Load backing array
                il.Append(il.Create(OpCodes.Ldsfld, _programStateField));
                var getter = _module.ImportReference(
                    typeof(CobolSharp.Runtime.ProgramState).GetProperty(
                        loc.Value.Area == StorageAreaKind.WorkingStorage
                            ? "WorkingStorage" : "FileSection")!.GetGetMethod()!);
                il.Append(il.Create(OpCodes.Callvirt, getter));

                // offset + size
                il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Offset));
                il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));

                if (init.Value is decimal d && loc.Value.Pic.IsNumeric)
                {
                    // Numeric VALUE → PicRuntime.MoveNumericLiteral
                    // Stack already has: byte[] area, int offset, int length
                    EmitLoadPicDescriptor(il, loc.Value.Pic);
                    EmitLoadDecimal(il, d);
                    il.Append(il.Create(OpCodes.Ldc_I4_0)); // rounding = truncate

                    var numMethod = _module.ImportReference(
                        typeof(Runtime.PicRuntime).GetMethod(
                            "MoveNumericLiteral",
                            new[] { typeof(byte[]), typeof(int), typeof(int),
                                    typeof(Runtime.PicDescriptor),
                                    typeof(decimal), typeof(int) })!);
                    il.Append(il.Create(OpCodes.Call, numMethod));
                }
                else if (init.Value is string s)
                {
                    // String VALUE → MoveStringToField
                    il.Append(il.Create(OpCodes.Ldstr, s));
                    il.Append(il.Create(OpCodes.Call, moveStringMethod));
                }
                else if (init.Value is decimal d2)
                {
                    // Numeric VALUE on non-numeric field → treat as string
                    string numStr = d2.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    il.Append(il.Create(OpCodes.Ldstr, numStr));
                    il.Append(il.Create(OpCodes.Call, moveStringMethod));
                }
            }
        }

        il.Append(il.Create(OpCodes.Ret));
    }

    private void SeedPrimitiveTypes()
    {
        _typeMap[IrPrimitiveType.Int32] = _module.TypeSystem.Int32;
        _typeMap[IrPrimitiveType.Int64] = _module.TypeSystem.Int64;
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

            case IrInitArithmeticStatus:
            {
                EmitInitArithmeticStatus(il, _currentMethodDef!);
                break;
            }

            case IrLoadSizeError lse:
            {
                var statusLocal = EnsureArithmeticStatusLocal(_currentMethodDef!);
                il.Append(il.Create(OpCodes.Ldloc, statusLocal));
                var sizeErrorField = _module.ImportReference(
                    typeof(ArithmeticStatus).GetField("SizeError")!);
                il.Append(il.Create(OpCodes.Ldfld, sizeErrorField));
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

            case IrPerformThru thru:
                EmitPerformThru(il, thru, _currentMethodDef!);
                break;

            case IrMoveStringToField ms:
                EmitMoveStringToField(il, ms, getLocal);
                break;

            case IrWriteRecordFromStorage wr:
                EmitWriteRecordFromStorage(il, wr);
                break;

            case IrPicMove pm:
                EmitPicMoveFieldToField(il, pm);
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
                EmitLoadBackingArray(il, accField.Source.Area);
                il.Append(il.Create(OpCodes.Ldc_I4, accField.Source.Offset));
                il.Append(il.Create(OpCodes.Ldc_I4, accField.Source.Length));
                EmitLoadPicDescriptor(il, accField.Source.Pic);
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

            case IrSubtractAccumulatedFromTarget subAcc:
                EmitSubtractAccumulatedFromTarget(il, subAcc, getLocal);
                break;

            case IrComputeStore compStore:
                EmitComputeStore(il, compStore);
                break;

            case IrPicDivide divInst:
                EmitPicDivide(il, divInst);
                break;

            case IrPicDivideLiteral divLit:
                EmitPicDivideLiteral(il, divLit);
                break;

            case IrPicCompare cmp:
                EmitPicCompare(il, cmp, getLocal);
                break;

            case IrPicCompareLiteral cmpLit:
                EmitPicCompareLiteral(il, cmpLit, getLocal);
                break;

            case IrStringCompareLiteral strCmp:
                EmitStringCompareLiteral(il, strCmp, getLocal);
                break;

            case IrPicMoveLiteralNumeric movLit:
                EmitPicMoveLiteralNumeric(il, movLit);
                break;

            case IrRuntimeCall rtc:
                EmitRuntimeCall(il, rtc, getLocal);
                break;

            case IrPicDisplay disp:
                EmitPicDisplay(il, disp);
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

    private void EmitPerform(ILProcessor il, IrPerform perf)
    {
        var target = _methodMap[perf.Target];
        il.Append(il.Create(OpCodes.Call, target));
        // Paragraph methods return int (next PC); discard in PERFORM context
        if (target.ReturnType != _module.TypeSystem.Void)
            il.Append(il.Create(OpCodes.Pop));
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
        // Call ProgramState.MoveStringToField(byte[] area, int offset, int size, string value)
        // All args emitted inline — no stack ordering issues.
        EmitLoadBackingArray(il, ms.Target.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, ms.Target.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, ms.Target.Length));
        il.Append(il.Create(OpCodes.Ldstr, ms.Value));

        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "MoveStringToField",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(string) })!);
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

        // Load backing byte array
        EmitLoadBackingArray(il, wr.Record.Area);

        // offset + size
        il.Append(il.Create(OpCodes.Ldc_I4, wr.Record.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, wr.Record.Length));

        // Call ProgramState.WriteRecordToFile(string, byte[], int, int)
        var method = _module.ImportReference(
            typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                "WriteRecordToFile",
                new[] { typeof(string), typeof(byte[]), typeof(int), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// MOVE field TO field: routes numeric→numeric through PicRuntime.MoveNumeric,
    /// alpha→alpha through StorageHelpers.MoveFieldToField.
    /// </summary>
    private void EmitPicMoveFieldToField(ILProcessor il, IrPicMove pm)
    {
        var srcCat = pm.Source.Pic.Category;
        var dstCat = pm.Destination.Pic.Category;

        if (srcCat.IsNumericLike() && dstCat == CobolCategory.NumericEdited)
        {
            // Numeric → NumericEdited: format with editing pattern
            EmitLoadBackingArray(il, pm.Source.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Length));
            EmitLoadPicDescriptor(il, pm.Source.Pic);

            EmitLoadBackingArray(il, pm.Destination.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Length));
            EmitLoadPicDescriptor(il, pm.Destination.Pic);

            il.Append(il.Create(OpCodes.Ldc_I4, pm.Rounding));

            var method = _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericToNumericEdited",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else if (srcCat.IsNumericLike() && dstCat.IsNumericLike())
        {
            // Numeric → Numeric: PIC-aware move via PicRuntime.MoveNumeric
            EmitLoadBackingArray(il, pm.Destination.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Length));
            EmitLoadPicDescriptor(il, pm.Destination.Pic);

            EmitLoadBackingArray(il, pm.Source.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Length));
            EmitLoadPicDescriptor(il, pm.Source.Pic);

            il.Append(il.Create(OpCodes.Ldc_I4, pm.Rounding));

            var method = _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else if (srcCat.IsNumericLike() && dstCat.IsAlphanumericLike())
        {
            // Numeric → Alphanumeric: format as string
            EmitLoadBackingArray(il, pm.Source.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Length));
            EmitLoadPicDescriptor(il, pm.Source.Pic);

            EmitLoadBackingArray(il, pm.Destination.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Length));
            EmitLoadPicDescriptor(il, pm.Destination.Pic);

            il.Append(il.Create(OpCodes.Ldc_I4, pm.Rounding));

            var method = _module.ImportReference(
                typeof(Runtime.PicRuntime).GetMethod("MoveNumericToAlphanumeric",
                    new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                            typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
        else
        {
            // Alpha/group MOVE: raw byte copy
            EmitLoadBackingArray(il, pm.Destination.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Destination.Length));

            EmitLoadBackingArray(il, pm.Source.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, pm.Source.Length));

            var method = _module.ImportReference(
                typeof(CobolSharp.Runtime.StorageHelpers).GetMethod(
                    "MoveFieldToField",
                    new[] { typeof(byte[]), typeof(int), typeof(int),
                            typeof(byte[]), typeof(int), typeof(int) })!);
            il.Append(il.Create(OpCodes.Call, method));
        }
    }

    private void EmitPicMultiply(ILProcessor il, IrPicMultiply mul)
    {
        EmitLoadBackingArray(il, mul.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Destination.Length));
        EmitLoadPicDescriptor(il, mul.Destination.Pic);

        EmitLoadBackingArray(il, mul.Left.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Left.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Left.Length));
        EmitLoadPicDescriptor(il, mul.Left.Pic);

        EmitLoadBackingArray(il, mul.Right.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Right.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Right.Length));
        EmitLoadPicDescriptor(il, mul.Right.Pic);

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

    private void EmitPicCompare(ILProcessor il, IrPicCompare cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLoadBackingArray(il, cmp.Left.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Left.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Left.Length));
        EmitLoadPicDescriptor(il, cmp.Left.Pic);

        EmitLoadBackingArray(il, cmp.Right.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Right.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Right.Length));
        EmitLoadPicDescriptor(il, cmp.Right.Pic);

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
    private void EmitLoadPicDescriptor(ILProcessor il, Runtime.PicDescriptor pic)
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
        il.Append(il.Create(pic.BlankWhenZero ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.LeadingScaleDigits));
        il.Append(il.Create(OpCodes.Ldc_I4, pic.TrailingScaleDigits));

        var ctor = _module.ImportReference(
            typeof(Runtime.PicDescriptor).GetConstructor(
                new[] { typeof(int), typeof(int), typeof(bool), typeof(bool),
                        typeof(bool), typeof(bool), typeof(int), typeof(Runtime.UsageKind),
                        typeof(Runtime.CobolCategory),
                        typeof(Runtime.SignStorageKind), typeof(Runtime.EditingKind),
                        typeof(bool), typeof(int), typeof(int) })!);
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
        EmitLoadBackingArray(il, mv.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, mv.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, mv.Destination.Length));
        EmitLoadPicDescriptor(il, mv.Destination.Pic);
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
        EmitLoadBackingArray(il, mul.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Destination.Length));
        EmitLoadPicDescriptor(il, mul.Destination.Pic);
        EmitLoadDecimal(il, mul.Value);
        EmitLoadBackingArray(il, mul.Other.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Other.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, mul.Other.Length));
        EmitLoadPicDescriptor(il, mul.Other.Pic);
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
        EmitLoadBackingArray(il, add.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, add.Destination.Length));
        EmitLoadPicDescriptor(il, add.Destination.Pic);
        EmitLoadBackingArray(il, add.Source.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Source.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, add.Source.Length));
        EmitLoadPicDescriptor(il, add.Source.Pic);
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
        EmitLoadBackingArray(il, add.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, add.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, add.Destination.Length));
        EmitLoadPicDescriptor(il, add.Destination.Pic);
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
        EmitLoadBackingArray(il, sub.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Destination.Length));
        EmitLoadPicDescriptor(il, sub.Destination.Pic);
        EmitLoadBackingArray(il, sub.Source.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Source.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Source.Length));
        EmitLoadPicDescriptor(il, sub.Source.Pic);
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
        EmitLoadBackingArray(il, sub.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, sub.Destination.Length));
        EmitLoadPicDescriptor(il, sub.Destination.Pic);
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
        EmitLoadBackingArray(il, inst.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Destination.Length));
        EmitLoadPicDescriptor(il, inst.Destination.Pic);
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

    private void EmitSubtractAccumulatedFromTarget(ILProcessor il, IrSubtractAccumulatedFromTarget inst,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLoadBackingArray(il, inst.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, inst.Destination.Length));
        EmitLoadPicDescriptor(il, inst.Destination.Pic);
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
        EmitLoadBackingArray(il, div.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, div.Destination.Length));
        EmitLoadPicDescriptor(il, div.Destination.Pic);

        EmitLoadBackingArray(il, div.Left.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Left.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, div.Left.Length));
        EmitLoadPicDescriptor(il, div.Left.Pic);

        EmitLoadBackingArray(il, div.Right.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Right.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, div.Right.Length));
        EmitLoadPicDescriptor(il, div.Right.Pic);

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
        EmitLoadBackingArray(il, div.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, div.Destination.Length));
        EmitLoadPicDescriptor(il, div.Destination.Pic);
        EmitLoadDecimal(il, div.Value);
        EmitLoadBackingArray(il, div.Other.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, div.Other.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, div.Other.Length));
        EmitLoadPicDescriptor(il, div.Other.Pic);
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
    /// COMPUTE: evaluate expression tree to decimal on stack, then store to target.
    /// Emits: area, offset, length, pic, [expression result as decimal], rounding, ref status
    /// Calls MoveNumericLiteral which handles scaling, rounding, and overflow.
    /// </summary>
    private void EmitComputeStore(ILProcessor il, IrComputeStore cs)
    {
        // Push target storage args
        EmitLoadBackingArray(il, cs.Destination.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, cs.Destination.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, cs.Destination.Length));
        EmitLoadPicDescriptor(il, cs.Destination.Pic);

        // Evaluate expression — pushes decimal onto stack
        EmitExpression(il, cs.Expression);

        // Rounding mode
        il.Append(il.Create(OpCodes.Ldc_I4, cs.Rounding));

        // Call MoveNumericLiteral(area, offset, length, pic, value, rounding)
        var method = _module.ImportReference(
            typeof(Runtime.PicRuntime).GetMethod("MoveNumericLiteral",
                new[] { typeof(byte[]), typeof(int), typeof(int), typeof(Runtime.PicDescriptor),
                        typeof(decimal), typeof(int) })!);
        il.Append(il.Create(OpCodes.Call, method));
    }

    /// <summary>
    /// Recursively emit a bound expression tree, leaving a decimal on the IL stack.
    /// </summary>
    private void EmitExpression(ILProcessor il, Semantics.Bound.BoundExpression expr)
    {
        switch (expr)
        {
            case Semantics.Bound.BoundLiteralExpression lit when lit.Value is decimal d:
                EmitLoadDecimal(il, d);
                break;

            case Semantics.Bound.BoundIdentifierExpression id:
            {
                // Decode field to decimal: PicRuntime.DecodeNumeric(area, offset, len, pic)
                var loc = _semanticModel?.GetStorageLocation(id.Symbol);
                if (loc.HasValue)
                {
                    EmitLoadBackingArray(il, loc.Value.Area);
                    il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Offset));
                    il.Append(il.Create(OpCodes.Ldc_I4, loc.Value.Length));
                    EmitLoadPicDescriptor(il, loc.Value.Pic);
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
                break;
            }

            case Semantics.Bound.BoundBinaryExpression bin:
            {
                EmitExpression(il, bin.Left);
                EmitExpression(il, bin.Right);

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
                        il.Append(il.Create(OpCodes.Call,
                            _module.ImportReference(typeof(decimal).GetMethod("op_Division",
                                new[] { typeof(decimal), typeof(decimal) })!)));
                        break;
                    case (Semantics.Bound.BoundBinaryOperatorKind)99: // Power (**)
                    {
                        // Convert both to double, call Math.Pow, convert back to decimal
                        var toDouble = _module.ImportReference(
                            typeof(decimal).GetMethod("op_Explicit",
                                new[] { typeof(decimal) },
                                null)!);
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

            default:
                EmitLoadDecimal(il, 0m);
                break;
        }
    }

    private void EmitPicCompareLiteral(ILProcessor il, IrPicCompareLiteral cmp,
        Func<IrValue, VariableDefinition> getLocal)
    {
        EmitLoadBackingArray(il, cmp.Left.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Left.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Left.Length));
        EmitLoadPicDescriptor(il, cmp.Left.Pic);
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

    /// <summary>
    /// Convert CompareNumeric result (-1/0/1) to bool based on operator kind.
    /// BoundBinaryOperatorKind: Equal=4, NotEqual=5, Less=6, LessOrEqual=7, Greater=8, GreaterOrEqual=9
    /// </summary>
    private void EmitCompareResultToBool(ILProcessor il, int operatorKind)
    {
        switch (operatorKind)
        {
            case 4: // Equal: result == 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case 5: // NotEqual: NOT (result == 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case 6: // Less: result < 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Clt));
                break;
            case 7: // LessOrEqual: NOT (result > 0)
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Cgt));
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Ceq));
                break;
            case 8: // Greater: result > 0
                il.Append(il.Create(OpCodes.Ldc_I4_0));
                il.Append(il.Create(OpCodes.Cgt));
                break;
            case 9: // GreaterOrEqual: NOT (result < 0)
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
        EmitLoadBackingArray(il, cmp.Left.Area);
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Left.Offset));
        il.Append(il.Create(OpCodes.Ldc_I4, cmp.Left.Length));
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

    /// <summary>
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

        // EXIT
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

    private void EmitLoadBackingArray(ILProcessor il, StorageAreaKind area)
    {
        il.Append(il.Create(OpCodes.Ldsfld, _programStateField!));

        var propertyName = area == StorageAreaKind.WorkingStorage
            ? "WorkingStorage"
            : "FileSection";

        // Get the property as a field (it's an auto-property, so use getter)
        var getter = _module.ImportReference(
            typeof(CobolSharp.Runtime.ProgramState).GetProperty(propertyName)!.GetGetMethod()!);
        il.Append(il.Create(OpCodes.Callvirt, getter));
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
            EmitLoadBackingArray(il, field.Location.Area);
            il.Append(il.Create(OpCodes.Ldc_I4, field.Location.Offset));
            il.Append(il.Create(OpCodes.Ldc_I4, field.Location.Length));
            EmitLoadPicDescriptor(il, field.Location.Pic);

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
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("WriteText",
                    new[] { typeof(string), typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, writeText));
        }
        else if (rtc.MethodName == "CobolRuntime.OpenOutput")
        {
            // One arg on stack: fileName
            var openOutput = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("OpenOutput",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, openOutput));
        }
        else if (rtc.MethodName == "CobolRuntime.CloseFile")
        {
            // One arg on stack: fileName
            var closeFile = _module.ImportReference(
                typeof(CobolSharp.Runtime.FileRuntime).GetMethod("CloseFile",
                    new[] { typeof(string) })!);
            il.Append(il.Create(OpCodes.Call, closeFile));
        }
        // Other runtime calls: NOP for now
    }
}
