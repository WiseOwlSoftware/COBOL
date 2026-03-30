// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// M002 Stage 1: Structural tests verifying that all lowering infrastructure
// exists, Binder still contains all 101 methods, and LoweringContext exposes
// all required fields.
using System.Reflection;
using CobolSharp.Compiler.CodeGen;
using CobolSharp.Compiler.CodeGen.Lowering;
using CobolSharp.Compiler.IR;
using Xunit;

namespace CobolSharp.Tests.Unit.IR;

public class BinderDecompositionTests
{
    private static readonly Assembly CompilerAssembly = typeof(IrInstruction).Assembly;

    // ── All 9 lowering classes exist ──

    [Theory]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.LoweringContext")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.LocationResolver")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.ExpressionLowerer")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.ConditionLowerer")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.ControlFlowLowerer")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.ArithmeticLowerer")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.DataMovementLowerer")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.FileIoLowerer")]
    [InlineData("CobolSharp.Compiler.CodeGen.Lowering.StringLowerer")]
    public void Lowering_class_exists(string typeName)
    {
        var type = CompilerAssembly.GetType(typeName);
        Assert.NotNull(type);
        Assert.True(type!.IsClass);
        // All lowerers except LoweringContext should be sealed
        if (!typeName.EndsWith("LoweringContext"))
            Assert.True(type.IsSealed, $"{typeName} should be sealed");
    }

    // ── All lowerers are internal, not public ──

    [Theory]
    [InlineData(typeof(LoweringContext))]
    [InlineData(typeof(LocationResolver))]
    [InlineData(typeof(ExpressionLowerer))]
    [InlineData(typeof(ConditionLowerer))]
    [InlineData(typeof(ControlFlowLowerer))]
    [InlineData(typeof(ArithmeticLowerer))]
    [InlineData(typeof(DataMovementLowerer))]
    [InlineData(typeof(FileIoLowerer))]
    [InlineData(typeof(StringLowerer))]
    public void Lowering_class_is_internal(Type type)
    {
        Assert.False(type.IsPublic, $"{type.Name} should be internal, not public");
    }

    // ── LoweringContext exposes all required fields ──

    [Theory]
    [InlineData("Semantic", typeof(CobolSharp.Compiler.Semantics.SemanticModel))]
    [InlineData("ValueFactory", typeof(IrValueFactory))]
    [InlineData("Diagnostics", typeof(CobolSharp.Compiler.Diagnostics.DiagnosticBag))]
    [InlineData("Options", typeof(CobolSharp.Compiler.Semantics.CompilationOptions))]
    public void LoweringContext_has_service_property(string propName, Type expectedType)
    {
        var prop = typeof(LoweringContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(expectedType, prop!.PropertyType);
    }

    [Theory]
    [InlineData("ParagraphMethods")]
    [InlineData("ParagraphIndices")]
    [InlineData("ParagraphsByIndex")]
    [InlineData("AlterSlots")]
    [InlineData("AlterDefaults")]
    public void LoweringContext_has_collection_property(string propName)
    {
        var prop = typeof(LoweringContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
    }

    [Theory]
    [InlineData("CurrentParagraphName")]
    [InlineData("CurrentSentenceEnd")]
    [InlineData("ParagraphEndBlock")]
    [InlineData("SectionExitReturnIndex")]
    public void LoweringContext_has_tracking_property(string propName)
    {
        var prop = typeof(LoweringContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.True(prop!.CanWrite, $"{propName} should be writable");
    }

    [Theory]
    [InlineData("PerformExitStack")]
    [InlineData("PerformContinueStack")]
    public void LoweringContext_has_stack_property(string propName)
    {
        var prop = typeof(LoweringContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
    }

    [Fact]
    public void LoweringContext_has_NextCacheKey_method()
    {
        var method = typeof(LoweringContext).GetMethod("NextCacheKey",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.Equal(typeof(int), method!.ReturnType);
    }

    // ── LoweringContext references all lowerers ──

    [Theory]
    [InlineData("Location", typeof(LocationResolver))]
    [InlineData("Expression", typeof(ExpressionLowerer))]
    [InlineData("Condition", typeof(ConditionLowerer))]
    [InlineData("ControlFlow", typeof(ControlFlowLowerer))]
    [InlineData("Arithmetic", typeof(ArithmeticLowerer))]
    [InlineData("DataMovement", typeof(DataMovementLowerer))]
    [InlineData("FileIo", typeof(FileIoLowerer))]
    [InlineData("String", typeof(StringLowerer))]
    public void LoweringContext_has_lowerer_reference(string propName, Type expectedType)
    {
        var prop = typeof(LoweringContext).GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(expectedType, prop!.PropertyType);
    }

    [Fact]
    public void LoweringContext_has_LowerStatement_delegate()
    {
        var prop = typeof(LoweringContext).GetProperty("LowerStatement",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        // Func<BoundStatement, IrMethod, IrBasicBlock, IrBasicBlock> = Func<,,,>
        Assert.Equal(typeof(Func<,,,>), prop.PropertyType.GetGenericTypeDefinition());
    }

    // ── Binder contains only orchestration + 3 inline methods ──

    [Theory]
    [InlineData("Bind")]
    [InlineData("LowerStatement")]
    [InlineData("LowerDisplay")]
    [InlineData("LowerAccept")]
    [InlineData("LowerCall")]
    public void Binder_contains_method(string methodName)
    {
        var binderType = typeof(CobolSharp.Compiler.CodeGen.Binder);
        var methods = binderType.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Binder has _ctx field ──

    [Fact]
    public void Binder_has_LoweringContext_field()
    {
        var field = typeof(CobolSharp.Compiler.CodeGen.Binder).GetField("_ctx",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(field);
        Assert.Equal(typeof(LoweringContext), field!.FieldType);
    }

    // ── Binder _ctx is wired to all lowerers ──

    [Fact]
    public void Binder_ctx_has_all_lowerers_wired()
    {
        // Create a minimal Binder to verify wiring
        var prog = new CobolSharp.Compiler.Semantics.ProgramSymbol("TEST", 1);
        var symbols = new CobolSharp.Compiler.Semantics.SymbolTable("TEST", 1);
        var diag = new CobolSharp.Compiler.Diagnostics.DiagnosticBag();
        var model = new CobolSharp.Compiler.Semantics.SemanticModel(prog, symbols, diag);
        var binder = new CobolSharp.Compiler.CodeGen.Binder(model, diag);

        Assert.NotNull(binder._ctx);
        Assert.NotNull(binder._ctx.Location);
        Assert.NotNull(binder._ctx.Expression);
        Assert.NotNull(binder._ctx.Condition);
        Assert.NotNull(binder._ctx.ControlFlow);
        Assert.NotNull(binder._ctx.Arithmetic);
        Assert.NotNull(binder._ctx.DataMovement);
        Assert.NotNull(binder._ctx.FileIo);
        Assert.NotNull(binder._ctx.String);
        Assert.NotNull(binder._ctx.LowerStatement);
    }

    // ── Stage 2: LocationResolver and ExpressionLowerer contain moved methods ──

    [Theory]
    [InlineData("ResolveLocation")]
    [InlineData("ResolveExpressionLocation")]
    [InlineData("ResolveRefModLocation")]
    [InlineData("ComputeMultipliers")]
    public void LocationResolver_contains_method(string methodName)
    {
        var type = typeof(LocationResolver);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    [Theory]
    [InlineData("LowerExpression")]
    [InlineData("TryEvalConstant")]
    [InlineData("TryExtractNegativeLiteral")]
    [InlineData("FormatLiteralForAlphanumeric")]
    public void ExpressionLowerer_contains_method(string methodName)
    {
        var type = typeof(ExpressionLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 3: ConditionLowerer, ArithmeticLowerer, DataMovementLowerer contain moved methods ──

    [Theory]
    [InlineData("LowerCondition")]
    [InlineData("LowerComparison")]
    [InlineData("NormalizeOperand")]
    [InlineData("LowerSignCondition")]
    [InlineData("LowerClassCondition")]
    [InlineData("LowerUserClassCondition")]
    [InlineData("LowerConditionName")]
    [InlineData("LowerConditionalBranch")]
    [InlineData("MakeFigurativeString")]
    [InlineData("EvaluateComparisonResult")]
    [InlineData("FlipComparisonOp")]
    [InlineData("IsNumericComparison")]
    [InlineData("IsStrictlyNumeric")]
    [InlineData("EmitLocationVsFigurative")]
    public void ConditionLowerer_contains_method(string methodName)
    {
        var type = typeof(ConditionLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    [Theory]
    [InlineData("LowerArithmetic")]
    [InlineData("LowerAdd")]
    [InlineData("LowerSubtract")]
    [InlineData("LowerMultiply")]
    [InlineData("LowerDivide")]
    [InlineData("LowerCompute")]
    [InlineData("LowerSizeError")]
    public void ArithmeticLowerer_contains_method(string methodName)
    {
        var type = typeof(ArithmeticLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    [Theory]
    [InlineData("LowerMove")]
    [InlineData("LowerCorresponding")]
    [InlineData("LowerInitialize")]
    [InlineData("LowerSetCondition")]
    [InlineData("LowerSetIndex")]
    [InlineData("FigurativeToStringHelper")]
    [InlineData("ClassifyInitializeCategory")]
    public void DataMovementLowerer_contains_method(string methodName)
    {
        var type = typeof(DataMovementLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 4: ControlFlowLowerer, FileIoLowerer, StringLowerer contain moved methods ──

    [Theory]
    [InlineData("LowerPerform")]
    [InlineData("LowerIf")]
    [InlineData("LowerEvaluate")]
    [InlineData("LowerGoTo")]
    [InlineData("LowerAlter")]
    [InlineData("LowerNextSentence")]
    [InlineData("LowerExitPerform")]
    [InlineData("LowerExitParagraph")]
    [InlineData("LowerExitSection")]
    [InlineData("LowerSearch")]
    [InlineData("LowerSearchAll")]
    [InlineData("EmitBinarySearchNode")]
    [InlineData("ExtractFirstRelationalComparison")]
    public void ControlFlowLowerer_contains_method(string methodName)
    {
        var type = typeof(ControlFlowLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    [Theory]
    [InlineData("LowerOpen")]
    [InlineData("LowerClose")]
    [InlineData("LowerRead")]
    [InlineData("LowerWrite")]
    [InlineData("LowerRewrite")]
    [InlineData("LowerDelete")]
    [InlineData("LowerStart")]
    [InlineData("LowerReturn")]
    [InlineData("LowerSort")]
    [InlineData("LowerMerge")]
    [InlineData("LowerRelease")]
    [InlineData("EmitFileStatus")]
    [InlineData("EmitUseDeclarative")]
    [InlineData("BuildKeysSpec")]
    public void FileIoLowerer_contains_method(string methodName)
    {
        var type = typeof(FileIoLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    [Theory]
    [InlineData("LowerInspect")]
    [InlineData("LowerString")]
    [InlineData("LowerUnstring")]
    [InlineData("LowerInspectPattern")]
    public void StringLowerer_contains_method(string methodName)
    {
        var type = typeof(StringLowerer);
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.Contains(methods, m => m.Name == methodName);
    }

    // ── Stage 5: Binder has no forwarding wrappers ──

    [Theory]
    [InlineData("LowerMove")]
    [InlineData("LowerCorresponding")]
    [InlineData("LowerPerform")]
    [InlineData("LowerIf")]
    [InlineData("LowerEvaluate")]
    [InlineData("LowerGoTo")]
    [InlineData("LowerAlter")]
    [InlineData("LowerArithmetic")]
    [InlineData("LowerCondition")]
    [InlineData("LowerComparison")]
    [InlineData("LowerOpen")]
    [InlineData("LowerClose")]
    [InlineData("LowerRead")]
    [InlineData("LowerWrite")]
    [InlineData("LowerSort")]
    [InlineData("LowerInspect")]
    [InlineData("LowerString")]
    [InlineData("LowerUnstring")]
    [InlineData("LowerSearch")]
    [InlineData("LowerSearchAll")]
    [InlineData("LowerInitialize")]
    [InlineData("LowerExpression")]
    [InlineData("ResolveLocation")]
    [InlineData("ResolveExpressionLocation")]
    public void Binder_does_not_contain_forwarding_wrapper(string methodName)
    {
        var binderType = typeof(CobolSharp.Compiler.CodeGen.Binder);
        var methods = binderType.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.DoesNotContain(methods, m => m.Name == methodName);
    }

    [Fact]
    public void No_lowerer_references_Binder_type()
    {
        var lowererTypes = new[]
        {
            typeof(LocationResolver), typeof(ExpressionLowerer),
            typeof(ConditionLowerer), typeof(ControlFlowLowerer),
            typeof(ArithmeticLowerer), typeof(DataMovementLowerer),
            typeof(FileIoLowerer), typeof(StringLowerer)
        };

        foreach (var type in lowererTypes)
        {
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Assert.NotEqual(typeof(CobolSharp.Compiler.CodeGen.Binder), field.FieldType);
            }
        }
    }

    // ── End-to-end: Binder still works ──

    [Fact]
    public void Binder_still_compiles_simple_program()
    {
        var (success, stdout, stderr) = new EndToEndHelper().CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. M002TEST.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(3) VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY X.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("042", stdout);
    }

    /// <summary>Helper for end-to-end compilation in unit test project.</summary>
    private sealed class EndToEndHelper : IDisposable
    {
        private readonly string _tempDir = Path.Combine(
            Path.GetTempPath(), "M002_" + Guid.NewGuid().ToString("N")[..8]);

        public EndToEndHelper() => Directory.CreateDirectory(_tempDir);

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        public (bool success, string stdout, string stderr) CompileAndRun(string source)
        {
            string srcPath = Path.Combine(_tempDir, "test.cob");
            string outPath = Path.Combine(_tempDir, "test.dll");
            File.WriteAllText(srcPath, source);

            var compilation = new CobolSharp.Compiler.Compilation();
            var result = compilation.Compile(srcPath, outPath);
            if (!result.Success)
                return (false, "", string.Join("\n", result.Diagnostics.Select(d => d.ToString())));

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet", Arguments = outPath,
                WorkingDirectory = _tempDir,
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(psi)!;
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(10000);
            return (process.ExitCode == 0, stdout.TrimEnd(), stderr.TrimEnd());
        }
    }
}
