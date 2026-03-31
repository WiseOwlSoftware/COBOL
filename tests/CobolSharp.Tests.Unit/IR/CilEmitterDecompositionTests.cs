// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// M003 Stage 1: Structural tests verifying that all emission infrastructure
// exists, CilEmitter still contains all methods, and EmissionContext exposes
// all required fields and emitter references.
using System.Reflection;
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.CodeGen.Emission;
using CobolSharp.Compiler.IR;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace CobolSharp.Tests.Unit.IR;

public class CilEmitterDecompositionTests
{
    private static readonly Assembly CompilerAssembly = typeof(IrInstruction).Assembly;

    // ── All 11 emission classes exist ──

    [Theory]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.EmissionContext")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilModuleSetup")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilProgramStateEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilControlFlowEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilDataEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilArithmeticEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilComparisonEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilExpressionEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilLocationEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilStringEmitter")]
    [InlineData("CobolSharp.Compiler.CodeGen.Emission.CilFileIoEmitter")]
    public void Emission_class_exists(string typeName)
    {
        var type = CompilerAssembly.GetType(typeName);
        Assert.NotNull(type);
        Assert.True(type!.IsClass);
        Assert.True(type.IsSealed, $"{typeName} should be sealed");
    }

    // ── All emission classes are internal, not public ──

    [Theory]
    [InlineData(typeof(EmissionContext))]
    [InlineData(typeof(CilModuleSetup))]
    [InlineData(typeof(CilProgramStateEmitter))]
    [InlineData(typeof(CilControlFlowEmitter))]
    [InlineData(typeof(CilDataEmitter))]
    [InlineData(typeof(CilArithmeticEmitter))]
    [InlineData(typeof(CilComparisonEmitter))]
    [InlineData(typeof(CilExpressionEmitter))]
    [InlineData(typeof(CilLocationEmitter))]
    [InlineData(typeof(CilStringEmitter))]
    [InlineData(typeof(CilFileIoEmitter))]
    public void Emission_class_is_internal(Type type)
    {
        Assert.False(type.IsPublic, $"{type.Name} should be internal, not public");
    }

    // ── EmissionContext exposes all required state properties ──

    [Theory]
    [InlineData("Module", typeof(ModuleDefinition))]
    [InlineData("ProgramType", typeof(TypeDefinition))]
    [InlineData("ProgramStateField", typeof(FieldDefinition))]
    [InlineData("InitializeStateMethod", typeof(MethodDefinition))]
    [InlineData("AlterTableField", typeof(FieldDefinition))]
    [InlineData("CurrentMethodDef", typeof(MethodDefinition))]
    [InlineData("ArithmeticStatusLocal", typeof(VariableDefinition))]
    [InlineData("SemanticModel", typeof(CobolSharp.Compiler.Semantics.SemanticModel))]
    [InlineData("EntryMethod", typeof(MethodDefinition))]
    [InlineData("LastCallResultField", typeof(FieldDefinition))]
    [InlineData("CobolDataPointerCtor", typeof(MethodReference))]
    public void EmissionContext_has_state_property(string propName, Type expectedType)
    {
        var prop = typeof(EmissionContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        // Handle nullable: unwrap Nullable<T> for value types, or check assignability
        var actualType = Nullable.GetUnderlyingType(prop!.PropertyType) ?? prop.PropertyType;
        Assert.True(expectedType.IsAssignableFrom(actualType),
            $"{propName}: expected {expectedType.Name} but got {prop.PropertyType.Name}");
    }

    // ── EmissionContext exposes all collection properties ──

    [Theory]
    [InlineData("TypeMap")]
    [InlineData("FieldMap")]
    [InlineData("MethodMap")]
    [InlineData("LinkageFields")]
    [InlineData("ExternalFields")]
    [InlineData("ExternalRanges")]
    [InlineData("CachedLocationLocals")]
    public void EmissionContext_has_collection_property(string propName)
    {
        var prop = typeof(EmissionContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
    }

    // ── EmissionContext references all emitters ──

    [Theory]
    [InlineData("ModuleSetup", typeof(CilModuleSetup))]
    [InlineData("ProgramState", typeof(CilProgramStateEmitter))]
    [InlineData("ControlFlow", typeof(CilControlFlowEmitter))]
    [InlineData("Data", typeof(CilDataEmitter))]
    [InlineData("Arithmetic", typeof(CilArithmeticEmitter))]
    [InlineData("Comparison", typeof(CilComparisonEmitter))]
    [InlineData("Expression", typeof(CilExpressionEmitter))]
    [InlineData("Location", typeof(CilLocationEmitter))]
    [InlineData("String", typeof(CilStringEmitter))]
    [InlineData("FileIo", typeof(CilFileIoEmitter))]
    public void EmissionContext_has_emitter_reference(string propName, Type expectedType)
    {
        var prop = typeof(EmissionContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(expectedType, prop!.PropertyType);
    }

    // ── EmissionContext has EmitInstruction delegate ──

    [Fact]
    public void EmissionContext_has_EmitInstruction_delegate()
    {
        var prop = typeof(EmissionContext).GetProperty("EmitInstruction",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        // Action<ILProcessor, IrInstruction, Func<IrValue, VariableDefinition>,
        //        Dictionary<IrBasicBlock, Instruction>>
        Assert.Equal(typeof(Action<,,,>), prop.PropertyType.GetGenericTypeDefinition());
    }

    // ── CilEmitter has _ctx field ──

    [Fact]
    public void CilEmitter_has_EmissionContext_field()
    {
        var field = typeof(CilEmitter).GetField("_ctx",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(field);
        Assert.Equal(typeof(EmissionContext), field!.FieldType);
    }

    // ── Stage 5: CilEmitter contains ONLY orchestration methods ──

    [Theory]
    [InlineData("EmitModule")]
    [InlineData("EmitInstruction")]
    [InlineData("EmitMethodBody")]
    [InlineData("EmitEntryMethodBody")]
    [InlineData("EmitRuntimeCall")]
    [InlineData("EmitCallProgram")]
    [InlineData("EmitCheckCallException")]
    [InlineData("EmitCall")]
    [InlineData("EmitParagraphDispatch")]
    [InlineData("EmitParagraphDispatchInline")]
    [InlineData("EmitProgramState")]
    [InlineData("SeedPrimitiveTypes")]
    [InlineData("DefineType")]
    [InlineData("DefineMethodSignature")]
    public void CilEmitter_contains_orchestration_method(string methodName)
    {
        var methods = typeof(CilEmitter).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 5: CilEmitter has NO forwarding wrappers ──

    [Fact]
    public void CilEmitter_has_no_forwarding_wrappers()
    {
        // After Stage 5, no private methods should exist that were previously
        // forwarding wrappers (methods moved to emitter classes).
        var movedMethods = new HashSet<string>
        {
            "EmitLocationArgs", "EmitLocationArgsWithPic", "EmitLoadBackingArray",
            "EmitLoadDecimal", "EmitLoadPicDescriptor", "EmitByteArrayLiteral",
            "EmitIrExpression", "EmitIrIntrinsicCall", "EmitFunctionCall",
            "EmitBranch", "EmitReturn", "EmitPerform", "EmitPerformTimes",
            "EmitPerformThru", "EmitGoToDepending",
            "EmitPicCompare", "EmitPicAdd", "EmitMoveFieldToField",
            "EmitStringStatement", "EmitUnstringStatement",
            "EmitWriteRecordFromStorage", "EmitReadRecordToStorage",
            "EmitSortInit", "EmitInspectTally", "EmitAccept",
            "EmitPicDisplay", "EmitMoveFigurative"
        };
        var cilMethods = typeof(CilEmitter).GetMethods(
            BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var m in cilMethods)
        {
            Assert.DoesNotContain(m.Name, movedMethods);
        }
    }

    // ── Stage 2: CilLocationEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitLocationArgs")]
    [InlineData("EmitLocationArgsWithPic")]
    [InlineData("EmitCachedLocationArgs")]
    [InlineData("EmitElementAddress")]
    [InlineData("EmitRefModAddress")]
    [InlineData("EmitLoadBackingArray")]
    [InlineData("EmitLoadBackingArrayOrExternal")]
    [InlineData("EmitLinkageLocationArgs")]
    [InlineData("TryGetExternalField")]
    public void CilLocationEmitter_contains_method(string methodName)
    {
        var type = typeof(CilLocationEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 2: CilExpressionEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitIrExpression")]
    [InlineData("EmitIrIntrinsicCall")]
    [InlineData("EmitFunctionCall")]
    [InlineData("EmitLoadDecimal")]
    [InlineData("EmitLoadPicDescriptor")]
    [InlineData("EmitByteArrayLiteral")]
    public void CilExpressionEmitter_contains_method(string methodName)
    {
        var type = typeof(CilExpressionEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 3: CilComparisonEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitClassCondition")]
    [InlineData("EmitUserClassCondition")]
    [InlineData("EmitPicCompare")]
    [InlineData("EmitPicCompareLiteral")]
    [InlineData("EmitPicCompareAccumulator")]
    [InlineData("EmitDecimalCompare")]
    [InlineData("EmitDecimalCompareLiteral")]
    [InlineData("EmitCompareResultToBool")]
    [InlineData("EmitStringCompareLiteral")]
    [InlineData("EmitStringCompare")]
    [InlineData("EmitStringCompareWithSequence")]
    [InlineData("EmitStringCompareLiteralWithSequence")]
    public void CilComparisonEmitter_contains_method(string methodName)
    {
        var type = typeof(CilComparisonEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 3: CilArithmeticEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitBinary")]
    [InlineData("EmitPicMultiply")]
    [InlineData("EmitPicMultiplyLiteral")]
    [InlineData("EmitPicAdd")]
    [InlineData("EmitPicAddLiteral")]
    [InlineData("EmitPicSubtract")]
    [InlineData("EmitPicSubtractLiteral")]
    [InlineData("EmitAddAccumulatedToTarget")]
    [InlineData("EmitMoveAccumulatedToTarget")]
    [InlineData("EmitSubtractAccumulatedFromTarget")]
    [InlineData("EmitPicDivide")]
    [InlineData("EmitPicDivideLiteral")]
    [InlineData("EmitComputeStore")]
    [InlineData("EmitCobolRemainder")]
    [InlineData("EnsureArithmeticStatusLocal")]
    [InlineData("EmitInitArithmeticStatus")]
    [InlineData("EmitLoadArithmeticStatusRef")]
    public void CilArithmeticEmitter_contains_method(string methodName)
    {
        var type = typeof(CilArithmeticEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 3: CilDataEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitLoadConst")]
    [InlineData("EmitLoadField")]
    [InlineData("EmitStoreField")]
    [InlineData("EmitMove")]
    [InlineData("EmitMoveStringToField")]
    [InlineData("EmitMoveWithStandardSignature")]
    [InlineData("EmitMoveFigurative")]
    [InlineData("EmitMoveAllLiteral")]
    [InlineData("EmitMoveFieldToField")]
    [InlineData("EmitPicMoveLiteralNumeric")]
    [InlineData("EmitPicDisplay")]
    [InlineData("EmitDisplayOperand")]
    [InlineData("EmitAccept")]
    [InlineData("EmitLocationLength")]
    [InlineData("EmitDefaultPicDescriptor")]
    [InlineData("GetCobolDataPointerCtor")]
    [InlineData("EmitOptionalString")]
    public void CilDataEmitter_contains_method(string methodName)
    {
        var type = typeof(CilDataEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: CilControlFlowEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitBranch")]
    [InlineData("EmitReturn")]
    [InlineData("EmitPerform")]
    [InlineData("EmitPerformTimes")]
    [InlineData("EmitPerformInlineTimes")]
    [InlineData("EmitPerformThru")]
    [InlineData("EmitGoToDepending")]
    [InlineData("EmitJump")]
    [InlineData("EmitBranchIfFalse")]
    [InlineData("EmitReturnConst")]
    [InlineData("EmitReturnAlterable")]
    [InlineData("EmitAlter")]
    [InlineData("EmitStopRun")]
    [InlineData("EmitExitProgram")]
    [InlineData("EmitGoBack")]
    [InlineData("EmitSetSwitch")]
    [InlineData("EmitTestSwitch")]
    public void CilControlFlowEmitter_contains_method(string methodName)
    {
        var type = typeof(CilControlFlowEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: CilStringEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitStringStatement")]
    [InlineData("EmitUnstringStatement")]
    [InlineData("EmitInspectTally")]
    [InlineData("EmitInspectReplace")]
    [InlineData("EmitInspectConvert")]
    [InlineData("EmitIrInspectPatternValue")]
    [InlineData("EmitIrInspectPatternValueAsOptionalString")]
    public void CilStringEmitter_contains_method(string methodName)
    {
        var type = typeof(CilStringEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: CilFileIoEmitter contains moved methods ──

    [Theory]
    [InlineData("EmitWriteRecordFromStorage")]
    [InlineData("EmitRewriteRecordFromStorage")]
    [InlineData("EmitWriteAdvancing")]
    [InlineData("EmitReadRecordToStorage")]
    [InlineData("EmitReadPreviousToStorage")]
    [InlineData("EmitReadByKey")]
    [InlineData("EmitStoreFileStatus")]
    [InlineData("EmitCheckFileAtEnd")]
    [InlineData("EmitDeleteRecord")]
    [InlineData("EmitStartFile")]
    [InlineData("EmitCheckFileInvalidKey")]
    [InlineData("EmitSortInit")]
    [InlineData("EmitSortRelease")]
    [InlineData("EmitSortSort")]
    [InlineData("EmitSortReturn")]
    [InlineData("EmitSortClose")]
    [InlineData("EmitSortMerge")]
    public void CilFileIoEmitter_contains_method(string methodName)
    {
        var type = typeof(CilFileIoEmitter);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }
}
