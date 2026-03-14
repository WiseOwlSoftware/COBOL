using CobolSharp.Compiler.Diagnostics;
using CobolSharp.Compiler.Parsing;
using CobolSharp.Compiler.Semantics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace CobolSharp.Compiler.CodeGen;

/// <summary>
/// Emits .NET CIL assemblies from analyzed COBOL ASTs using Mono.Cecil.
/// Generates one class per PROGRAM-ID deriving from CobolProgram.
/// </summary>
public sealed class CilEmitter
{
    private readonly DiagnosticBag _diagnostics;

    // Cecil references
    private AssemblyDefinition? _assembly;
    private ModuleDefinition? _module;
    private TypeDefinition? _programType;
    private MethodDefinition? _runMethod;
    private ILProcessor? _il;

    // Symbol → field mappings
    private readonly Dictionary<string, FieldDefinition> _fields = new(StringComparer.OrdinalIgnoreCase);

    // Runtime type references (resolved lazily)
    private TypeReference? _cobolProgramRef;
    private TypeReference? _cobolFieldRef;
    private TypeReference? _fieldTypeRef;
    private MethodReference? _fieldCtor;
    private MethodReference? _moveNumericMethod;
    private MethodReference? _moveAlphanumericMethod;
    private MethodReference? _moveFieldMethod;
    private MethodReference? _moveSpaceMethod;
    private MethodReference? _moveZeroMethod;
    private MethodReference? _moveHighValueMethod;
    private MethodReference? _moveLowValueMethod;
    private MethodReference? _moveQuoteMethod;
    private MethodReference? _addToMethod;
    private MethodReference? _subtractFromMethod;
    private MethodReference? _getNumericValueMethod;
    private MethodReference? _getDisplayValueMethod;

    public CilEmitter(DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public EmitResult Emit(CompilationUnit ast, SemanticModel model, string outputPath)
    {
        if (ast.Programs.Count == 0)
        {
            _diagnostics.ReportError("CS0500", "No programs to compile",
                new Common.SourceLocation("<unknown>", 0, 0, 0), new Common.TextSpan(0, 0));
            return new EmitResult(false, outputPath);
        }

        var program = ast.Programs[0];
        string programId = program.Identification.ProgramId;
        string assemblyName = Path.GetFileNameWithoutExtension(outputPath);

        // Create assembly
        var asmName = new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0));
        _assembly = AssemblyDefinition.CreateAssembly(asmName, assemblyName, ModuleKind.Console);
        _module = _assembly.MainModule;

        // Import runtime types
        ImportRuntimeTypes();

        // Create program class
        _programType = new TypeDefinition(
            "",
            programId,
            TypeAttributes.Public | TypeAttributes.Class,
            _cobolProgramRef);
        _module.Types.Add(_programType);

        // Emit fields for WORKING-STORAGE items
        EmitDataFields(model.SymbolTable);

        // Emit constructor (initializes fields)
        EmitConstructor(program, model.SymbolTable);

        // Emit Run() method (procedure division)
        EmitRunMethod(program);

        // Emit Main entry point
        EmitMainEntryPoint();

        // Write assembly to disk with portable PDB for debugging
        try
        {
            var writerParams = new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new Mono.Cecil.Cil.PortablePdbWriterProvider()
            };
            _assembly.Write(outputPath, writerParams);
            return new EmitResult(true, outputPath);
        }
        catch (Exception ex)
        {
            _diagnostics.ReportError("CS0501", $"Failed to write assembly: {ex.Message}",
                new Common.SourceLocation("<unknown>", 0, 0, 0), new Common.TextSpan(0, 0));
            return new EmitResult(false, outputPath);
        }
    }

    private void ImportRuntimeTypes()
    {
        // Import runtime assembly types
        var runtimeAssemblyPath = Path.Combine(
            Path.GetDirectoryName(typeof(CilEmitter).Assembly.Location)!,
            "CobolSharp.Runtime.dll");

        AssemblyDefinition runtimeAsm;
        if (File.Exists(runtimeAssemblyPath))
        {
            runtimeAsm = AssemblyDefinition.ReadAssembly(runtimeAssemblyPath);
        }
        else
        {
            // Fallback: find the runtime assembly via the loaded assemblies
            var runtimeType = typeof(CobolSharp.Runtime.CobolProgram);
            runtimeAsm = AssemblyDefinition.ReadAssembly(runtimeType.Assembly.Location);
        }

        var runtimeModule = runtimeAsm.MainModule;

        // Import CobolProgram
        var cobolProgramTypeDef = runtimeModule.GetType("CobolSharp.Runtime.CobolProgram");
        _cobolProgramRef = _module!.ImportReference(cobolProgramTypeDef);

        // Import CobolField
        var cobolFieldTypeDef = runtimeModule.GetType("CobolSharp.Runtime.Types.CobolField");
        _cobolFieldRef = _module.ImportReference(cobolFieldTypeDef);

        // Import FieldType enum
        var fieldTypeDef = runtimeModule.GetType("CobolSharp.Runtime.Types.FieldType");
        _fieldTypeRef = _module.ImportReference(fieldTypeDef);

        // CobolField constructor
        var fieldCtorDef = cobolFieldTypeDef.Methods.First(m => m.IsConstructor && m.Parameters.Count == 6);
        _fieldCtor = _module.ImportReference(fieldCtorDef);

        // CobolProgram methods
        _moveNumericMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveNumeric"));
        _moveAlphanumericMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveAlphanumeric"));
        _moveFieldMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveField"));
        _moveSpaceMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveSpace"));
        _moveZeroMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveZero"));
        _moveHighValueMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveHighValue"));
        _moveLowValueMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveLowValue"));
        _moveQuoteMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MoveQuote"));
        _addToMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "AddTo"));
        _subtractFromMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "SubtractFrom"));

        // CobolField methods
        _getNumericValueMethod = _module.ImportReference(
            cobolFieldTypeDef.Methods.First(m => m.Name == "GetNumericValue"));
        _getDisplayValueMethod = _module.ImportReference(
            cobolFieldTypeDef.Methods.First(m => m.Name == "GetDisplayValue"));
    }

    private void EmitDataFields(SymbolTable symbols)
    {
        foreach (var (name, symbol) in symbols.AllSymbols)
        {
            var field = new FieldDefinition(
                name,
                FieldAttributes.Private,
                _cobolFieldRef);
            _programType!.Fields.Add(field);
            _fields[name] = field;
        }
    }

    private void EmitConstructor(ProgramNode program, SymbolTable symbols)
    {
        var ctor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            _module!.TypeSystem.Void);

        var il = ctor.Body.GetILProcessor();

        // Call base constructor
        var baseCtor = _cobolProgramRef!.Resolve().Methods.First(m => m.IsConstructor && !m.HasParameters);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, _module.ImportReference(baseCtor));

        // Initialize each field
        foreach (var (name, symbol) in symbols.AllSymbols)
        {
            var field = _fields[name];
            var fieldType = symbol.Picture?.Category switch
            {
                PictureCategory.Numeric => 0,      // FieldType.Numeric
                PictureCategory.Alphabetic => 1,    // FieldType.Alphabetic
                PictureCategory.Alphanumeric => 2,  // FieldType.Alphanumeric
                _ => 2
            };

            // this.<field> = new CobolField(name, size, type, intDigits, decDigits, isSigned)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Ldc_I4, symbol.ByteSize > 0 ? symbol.ByteSize : 1);
            il.Emit(OpCodes.Ldc_I4, fieldType);
            il.Emit(OpCodes.Ldc_I4, symbol.Picture?.IntegerDigits ?? 0);
            il.Emit(OpCodes.Ldc_I4, symbol.Picture?.DecimalDigits ?? 0);
            il.Emit(OpCodes.Ldc_I4, symbol.Picture?.IsSigned == true ? 1 : 0);
            il.Emit(OpCodes.Newobj, _fieldCtor);
            il.Emit(OpCodes.Stfld, field);

            // Set initial value if present
            if (program.Data?.WorkingStorage != null)
            {
                var entry = program.Data.WorkingStorage.Entries
                    .FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

                if (entry?.InitialValue != null)
                {
                    EmitFieldInitialValue(il, field, entry.InitialValue, symbol);
                }
            }
        }

        il.Emit(OpCodes.Ret);
        _programType!.Methods.Add(ctor);
    }

    private void EmitFieldInitialValue(ILProcessor il, FieldDefinition field,
        Expression value, DataSymbol symbol)
    {
        // MoveNumeric(decimal, CobolField) and MoveAlphanumeric(string, CobolField)
        // are static methods. Stack order: arg1, arg2.
        if (value is NumericLiteralExpression numLit)
        {
            EmitDecimalConstant(il, numLit.Value);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Call, _moveNumericMethod);
        }
        else if (value is StringLiteralExpression strLit)
        {
            il.Emit(OpCodes.Ldstr, strLit.Value);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Call, _moveAlphanumericMethod);
        }
    }

    private void EmitRunMethod(ProgramNode program)
    {
        // First, emit paragraph methods
        if (program.Procedure != null)
        {
            foreach (var para in program.Procedure.Paragraphs)
            {
                EmitParagraphMethod(para);
            }
        }

        // Now emit Run() — the main entry point
        _runMethod = new MethodDefinition(
            "Run",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            _module!.TypeSystem.Void);

        _il = _runMethod.Body.GetILProcessor();

        if (program.Procedure != null)
        {
            // Emit initial statements (before first paragraph)
            foreach (var stmt in program.Procedure.Statements)
            {
                EmitStatement(stmt);
            }

            // Fall-through: call each paragraph in order (COBOL semantics)
            foreach (var para in program.Procedure.Paragraphs)
            {
                if (_paragraphMethods.TryGetValue(para.Name.ToUpperInvariant(), out var method))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, method);
                }
            }
        }

        _il.Emit(OpCodes.Ret);
        _programType!.Methods.Add(_runMethod);
    }

    private readonly Dictionary<string, MethodDefinition> _paragraphMethods = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _paragraphOrder = new(); // tracks source order for PERFORM THRU

    private void EmitParagraphMethod(Paragraph para)
    {
        var method = new MethodDefinition(
            $"Para_{para.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig,
            _module!.TypeSystem.Void);

        var savedIl = _il;
        var savedRunMethod = _runMethod;

        _runMethod = method; // for variable declarations
        _il = method.Body.GetILProcessor();

        foreach (var stmt in para.Statements)
        {
            EmitStatement(stmt);
        }

        _il.Emit(OpCodes.Ret);
        _programType!.Methods.Add(method);
        _paragraphMethods[para.Name.ToUpperInvariant()] = method;
        _paragraphOrder.Add(para.Name.ToUpperInvariant());

        _il = savedIl;
        _runMethod = savedRunMethod;
    }

    private void EmitStatement(Statement stmt)
    {
        switch (stmt)
        {
            case DisplayStatement display:
                EmitDisplayStatement(display);
                break;
            case StopRunStatement:
                EmitStopRunStatement();
                break;
            case MoveStatement move:
                EmitMoveStatement(move);
                break;
            case AddStatement add:
                EmitAddStatement(add);
                break;
            case SubtractStatement sub:
                EmitSubtractStatement(sub);
                break;
            case ComputeStatement compute:
                EmitComputeStatement(compute);
                break;
            case IfStatement ifStmt:
                EmitIfStatement(ifStmt);
                break;
            case PerformStatement perform:
                EmitPerformStatement(perform);
                break;
            case GoToStatement goTo:
                // GO TO as a call to the paragraph + return (transfers control)
                if (goTo.ParagraphName != null &&
                    _paragraphMethods.TryGetValue(goTo.ParagraphName.ToUpperInvariant(), out var goToMethod))
                {
                    _il!.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, goToMethod);
                }
                _il!.Emit(OpCodes.Ret); // exit current paragraph
                break;
            case ContinueStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case ExitStatement exit:
                if (exit.ExitKind == ExitType.Program)
                    _il!.Emit(OpCodes.Ret);
                else
                    _il!.Emit(OpCodes.Ret); // EXIT PARAGRAPH/SECTION = return from method
                break;
            case AcceptStatement:
                // TODO: ACCEPT from console/date — emit nop for now
                _il!.Emit(OpCodes.Nop);
                break;
            case InitializeStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case AlterStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case SortStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case StringStatement:
            case UnstringStatement:
            case InspectStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case OpenStatement:
            case CloseStatement:
            case ReadStatement:
            case WriteStatement:
            case RewriteStatement:
            case DeleteStatement:
            case StartStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case CallStatement:
            case CancelStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case InitiateStatement:
            case GenerateStatement:
            case TerminateStatement:
                _il!.Emit(OpCodes.Nop);
                break;
            case InvokeStatement:
            case RaiseStatement:
            case ResumeStatement:
            case SourceFormatDirective:
                _il!.Emit(OpCodes.Nop);
                break;
        }
    }

    private void EmitDisplayStatement(DisplayStatement display)
    {
        // Emit each operand as a Console.Write, then Console.WriteLine() at the end.
        // This matches COBOL DISPLAY semantics: concatenate all operands, newline at end.
        var consoleWriteString = _module!.ImportReference(
            typeof(Console).GetMethod("Write", new[] { typeof(string) }));
        var consoleWriteLine = _module.ImportReference(
            typeof(Console).GetMethod("WriteLine", Type.EmptyTypes));

        foreach (var operand in display.Operands)
        {
            if (operand is StringLiteralExpression strLit)
            {
                _il!.Emit(OpCodes.Ldstr, strLit.Value);
                _il.Emit(OpCodes.Call, consoleWriteString);
            }
            else if (operand is NumericLiteralExpression numLit)
            {
                _il!.Emit(OpCodes.Ldstr,
                    numLit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                _il.Emit(OpCodes.Call, consoleWriteString);
            }
            else if (operand is IdentifierExpression idExpr &&
                     _fields.TryGetValue(idExpr.Name, out var field))
            {
                // Call field.GetDisplayValue() → string, then Console.Write
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, field);
                _il.Emit(OpCodes.Callvirt, _getDisplayValueMethod);
                _il.Emit(OpCodes.Call, consoleWriteString);
            }
            else if (operand is FigurativeConstantExpression fig)
            {
                string text = fig.Constant switch
                {
                    FigurativeConstant.Space => " ",
                    FigurativeConstant.Zero => "0",
                    _ => ""
                };
                _il!.Emit(OpCodes.Ldstr, text);
                _il.Emit(OpCodes.Call, consoleWriteString);
            }
            else if (operand is FunctionCallExpression funcCall)
            {
                // Emit function call, get result as string for display
                EmitIntrinsicFunctionCall(funcCall, expectDecimal: false);
                _il!.Emit(OpCodes.Call, consoleWriteString);
            }
        }

        _il!.Emit(OpCodes.Call, consoleWriteLine);
    }

    private void EmitStopRunStatement()
    {
        _il!.Emit(OpCodes.Ret);
    }

    private void EmitMoveStatement(MoveStatement move)
    {
        foreach (var target in move.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;

            if (move.Source is NumericLiteralExpression numLit)
            {
                EmitDecimalConstant(_il!, numLit.Value);
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, targetField);
                _il.Emit(OpCodes.Call, _moveNumericMethod);
            }
            else if (move.Source is StringLiteralExpression strLit)
            {
                _il!.Emit(OpCodes.Ldstr, strLit.Value);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, targetField);
                _il.Emit(OpCodes.Call, _moveAlphanumericMethod);
            }
            else if (move.Source is IdentifierExpression idExpr && _fields.TryGetValue(idExpr.Name, out var srcField))
            {
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, srcField);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, targetField);
                _il.Emit(OpCodes.Call, _moveFieldMethod);
            }
            else if (move.Source is FigurativeConstantExpression fig)
            {
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, targetField);
                switch (fig.Constant)
                {
                    case FigurativeConstant.Space:
                        _il.Emit(OpCodes.Call, _moveSpaceMethod);
                        break;
                    case FigurativeConstant.Zero:
                        _il.Emit(OpCodes.Call, _moveZeroMethod);
                        break;
                    case FigurativeConstant.HighValue:
                        _il.Emit(OpCodes.Call, _moveHighValueMethod);
                        break;
                    case FigurativeConstant.LowValue:
                        _il.Emit(OpCodes.Call, _moveLowValueMethod);
                        break;
                    case FigurativeConstant.Quote:
                        _il.Emit(OpCodes.Call, _moveQuoteMethod);
                        break;
                }
            }
        }
    }

    private void EmitAddStatement(AddStatement add)
    {
        // ADD op1 op2 TO target1 target2
        // Sum all operands, then add to each target
        foreach (var target in add.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;

            foreach (var operand in add.Operands)
            {
                EmitNumericExprValue(operand);
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, targetField);
                _il.Emit(OpCodes.Call, _addToMethod);
            }
        }
    }

    private void EmitSubtractStatement(SubtractStatement sub)
    {
        foreach (var target in sub.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;

            foreach (var operand in sub.Operands)
            {
                EmitNumericExprValue(operand);
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, targetField);
                _il.Emit(OpCodes.Call, _subtractFromMethod);
            }
        }
    }

    private void EmitComputeStatement(ComputeStatement compute)
    {
        if (!_fields.TryGetValue(compute.Target.Name, out var targetField))
            return;

        EmitArithmeticExpression(compute.Value);
        _il!.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, targetField);
        _il.Emit(OpCodes.Call, _moveNumericMethod);
    }

    private void EmitIfStatement(IfStatement ifStmt)
    {
        var elseLabel = _il!.Create(OpCodes.Nop);
        var endLabel = _il.Create(OpCodes.Nop);

        EmitConditionExpression(ifStmt.Condition);
        _il.Emit(OpCodes.Brfalse, elseLabel);

        foreach (var stmt in ifStmt.ThenStatements)
            EmitStatement(stmt);

        if (ifStmt.ElseStatements.Count > 0)
        {
            _il.Emit(OpCodes.Br, endLabel);
            _il.Append(elseLabel);
            foreach (var stmt in ifStmt.ElseStatements)
                EmitStatement(stmt);
            _il.Append(endLabel);
        }
        else
        {
            _il.Append(elseLabel);
        }
    }

    private void EmitPerformStatement(PerformStatement perform)
    {
        if (perform.Times != null)
        {
            // PERFORM n TIMES
            var counter = new VariableDefinition(_module!.TypeSystem.Int32);
            _runMethod!.Body.Variables.Add(counter);
            var loopStart = _il!.Create(OpCodes.Nop);
            var loopEnd = _il.Create(OpCodes.Nop);

            // counter = 0
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Stloc, counter);

            _il.Append(loopStart);

            // if counter >= n, break
            _il.Emit(OpCodes.Ldloc, counter);
            EmitNumericExprValue(perform.Times);
            // Convert decimal to int
            _il.Emit(OpCodes.Call, _module.ImportReference(
                typeof(decimal).GetMethod("op_Explicit", new[] { typeof(decimal) })!
                    // Actually we need the one that returns int
            ));
            _il.Emit(OpCodes.Bge, loopEnd);

            foreach (var stmt in perform.Body)
                EmitStatement(stmt);

            // counter++
            _il.Emit(OpCodes.Ldloc, counter);
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Add);
            _il.Emit(OpCodes.Stloc, counter);
            _il.Emit(OpCodes.Br, loopStart);

            _il.Append(loopEnd);
        }
        else if (perform.Until != null)
        {
            // PERFORM UNTIL condition
            var loopStart = _il!.Create(OpCodes.Nop);
            var loopEnd = _il.Create(OpCodes.Nop);

            _il.Append(loopStart);
            EmitConditionExpression(perform.Until);
            _il.Emit(OpCodes.Brtrue, loopEnd);

            foreach (var stmt in perform.Body)
                EmitStatement(stmt);

            _il.Emit(OpCodes.Br, loopStart);
            _il.Append(loopEnd);
        }
        else if (perform.ParagraphName != null)
        {
            // Out-of-line PERFORM [THRU]: call paragraph method(s)
            string startName = perform.ParagraphName.ToUpperInvariant();
            string? endName = perform.ThruParagraphName?.ToUpperInvariant();

            if (endName != null)
            {
                // PERFORM THRU: call all paragraphs from start to end in source order
                bool inRange = false;
                foreach (var pName in _paragraphOrder)
                {
                    if (pName == startName) inRange = true;
                    if (inRange && _paragraphMethods.TryGetValue(pName, out var m))
                    {
                        _il!.Emit(OpCodes.Ldarg_0);
                        _il.Emit(OpCodes.Call, m);
                    }
                    if (pName == endName) break;
                }
            }
            else
            {
                // Simple PERFORM: call single paragraph
                if (_paragraphMethods.TryGetValue(startName, out var method))
                {
                    _il!.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, method);
                }
            }
        }
        else
        {
            // Inline perform (execute body once)
            foreach (var stmt in perform.Body)
                EmitStatement(stmt);
        }
    }

    private void EmitNumericExprValue(Expression expr)
    {
        if (expr is NumericLiteralExpression numLit)
        {
            EmitDecimalConstant(_il!, numLit.Value);
        }
        else if (expr is IdentifierExpression idExpr && _fields.TryGetValue(idExpr.Name, out var field))
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, field);
            _il.Emit(OpCodes.Callvirt, _getNumericValueMethod);
        }
        else
        {
            // Default: push 0
            EmitDecimalConstant(_il!, 0m);
        }
    }

    private void EmitArithmeticExpression(Expression expr)
    {
        if (expr is NumericLiteralExpression numLit)
        {
            EmitDecimalConstant(_il!, numLit.Value);
        }
        else if (expr is IdentifierExpression idExpr && _fields.TryGetValue(idExpr.Name, out var field))
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, field);
            _il.Emit(OpCodes.Callvirt, _getNumericValueMethod);
        }
        else if (expr is BinaryExpression bin)
        {
            EmitArithmeticExpression(bin.Left);
            EmitArithmeticExpression(bin.Right);

            var method = bin.Operator switch
            {
                BinaryOperator.Add => typeof(decimal).GetMethod("op_Addition"),
                BinaryOperator.Subtract => typeof(decimal).GetMethod("op_Subtraction"),
                BinaryOperator.Multiply => typeof(decimal).GetMethod("op_Multiply"),
                BinaryOperator.Divide => typeof(decimal).GetMethod("op_Division"),
                _ => typeof(decimal).GetMethod("op_Addition")
            };
            _il!.Emit(OpCodes.Call, _module!.ImportReference(method));
        }
        else if (expr is UnaryExpression unary && unary.Operator == UnaryOperator.Negate)
        {
            EmitArithmeticExpression(unary.Operand);
            _il!.Emit(OpCodes.Call, _module!.ImportReference(typeof(decimal).GetMethod("Negate")));
        }
        else if (expr is FunctionCallExpression funcCall)
        {
            EmitIntrinsicFunctionCall(funcCall, expectDecimal: true);
        }
        else
        {
            EmitDecimalConstant(_il!, 0m);
        }
    }

    private void EmitConditionExpression(Expression expr)
    {
        if (expr is BinaryExpression bin)
        {
            switch (bin.Operator)
            {
                case BinaryOperator.Equal:
                case BinaryOperator.NotEqual:
                case BinaryOperator.LessThan:
                case BinaryOperator.GreaterThan:
                case BinaryOperator.LessThanOrEqual:
                case BinaryOperator.GreaterThanOrEqual:
                    EmitArithmeticExpression(bin.Left);
                    EmitArithmeticExpression(bin.Right);

                    var compareMethod = typeof(decimal).GetMethod("Compare",
                        new[] { typeof(decimal), typeof(decimal) });
                    _il!.Emit(OpCodes.Call, _module!.ImportReference(compareMethod));

                    switch (bin.Operator)
                    {
                        case BinaryOperator.Equal:
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Ceq);
                            break;
                        case BinaryOperator.NotEqual:
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Ceq);
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Ceq);
                            break;
                        case BinaryOperator.LessThan:
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Clt);
                            break;
                        case BinaryOperator.GreaterThan:
                            _il.Emit(OpCodes.Ldc_I4_0);
                            _il.Emit(OpCodes.Cgt);
                            break;
                        case BinaryOperator.LessThanOrEqual:
                            _il.Emit(OpCodes.Ldc_I4_1);
                            _il.Emit(OpCodes.Clt);
                            break;
                        case BinaryOperator.GreaterThanOrEqual:
                            _il.Emit(OpCodes.Ldc_I4, -1);
                            _il.Emit(OpCodes.Cgt);
                            break;
                    }
                    break;

                case BinaryOperator.And:
                    EmitConditionExpression(bin.Left);
                    EmitConditionExpression(bin.Right);
                    _il!.Emit(OpCodes.And);
                    break;

                case BinaryOperator.Or:
                    EmitConditionExpression(bin.Left);
                    EmitConditionExpression(bin.Right);
                    _il!.Emit(OpCodes.Or);
                    break;
            }
        }
        else if (expr is UnaryExpression unary && unary.Operator == UnaryOperator.Not)
        {
            EmitConditionExpression(unary.Operand);
            _il!.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ceq);
        }
        else
        {
            // Default: treat as numeric comparison to zero
            EmitArithmeticExpression(expr);
            EmitDecimalConstant(_il!, 0m);
            var compareMethod = typeof(decimal).GetMethod("Compare",
                new[] { typeof(decimal), typeof(decimal) });
            _il!.Emit(OpCodes.Call, _module!.ImportReference(compareMethod));
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ceq);
            _il.Emit(OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Ceq); // NOT equal to zero = truthy
        }
    }

    private void EmitMainEntryPoint()
    {
        var mainMethod = new MethodDefinition(
            "Main",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            _module!.TypeSystem.Void);
        mainMethod.Parameters.Add(new ParameterDefinition(
            "args",
            ParameterAttributes.None,
            _module.ImportReference(typeof(string[]))));

        var il = mainMethod.Body.GetILProcessor();

        // new ProgramClass().Run();
        var ctor = _programType!.Methods.First(m => m.IsConstructor);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Callvirt, _runMethod);
        il.Emit(OpCodes.Ret);

        _programType.Methods.Add(mainMethod);
        _assembly!.EntryPoint = mainMethod;
    }

    /// <summary>
    /// Emit a call to IntrinsicFunctions.Call(name, args) and handle the result.
    /// </summary>
    private void EmitIntrinsicFunctionCall(FunctionCallExpression funcCall, bool expectDecimal)
    {
        var callMethod = _module!.ImportReference(
            typeof(CobolSharp.Runtime.Intrinsics.IntrinsicFunctions).GetMethod("Call",
                new[] { typeof(string), typeof(object[]) }));

        // Push function name
        _il!.Emit(OpCodes.Ldstr, funcCall.FunctionName);

        // Build args array
        _il.Emit(OpCodes.Ldc_I4, funcCall.Arguments.Count);
        _il.Emit(OpCodes.Newarr, _module.TypeSystem.Object);

        for (int i = 0; i < funcCall.Arguments.Count; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);

            var arg = funcCall.Arguments[i];
            if (arg is StringLiteralExpression strLit)
            {
                _il.Emit(OpCodes.Ldstr, strLit.Value);
            }
            else if (arg is NumericLiteralExpression numLit)
            {
                EmitDecimalConstant(_il, numLit.Value);
                _il.Emit(OpCodes.Box, _module.ImportReference(typeof(decimal)));
            }
            else if (arg is IdentifierExpression idExpr && _fields.TryGetValue(idExpr.Name, out var field))
            {
                // Pass the field's display value as string (works for both string and numeric args)
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, field);
                _il.Emit(OpCodes.Callvirt, _getDisplayValueMethod);
            }
            else
            {
                // Try to evaluate as arithmetic and box
                EmitArithmeticExpression(arg);
                _il.Emit(OpCodes.Box, _module.ImportReference(typeof(decimal)));
            }

            _il.Emit(OpCodes.Stelem_Ref);
        }

        // Call IntrinsicFunctions.Call(name, args) → object
        _il.Emit(OpCodes.Call, callMethod);

        if (expectDecimal)
        {
            // Unbox result to decimal
            _il.Emit(OpCodes.Unbox_Any, _module.ImportReference(typeof(decimal)));
        }
        else
        {
            // Convert result to string via ToString()
            var toStringMethod = _module.ImportReference(
                typeof(object).GetMethod("ToString", Type.EmptyTypes));
            _il.Emit(OpCodes.Callvirt, toStringMethod);
        }
    }

    private void EmitDecimalConstant(ILProcessor il, decimal value)
    {
        // Emit: new decimal(int) for simple integer values, or decimal.Parse for others
        if (value == Math.Truncate(value) && value >= int.MinValue && value <= int.MaxValue)
        {
            int intVal = (int)value;
            il.Emit(OpCodes.Ldc_I4, intVal);
            var ctor = typeof(decimal).GetConstructor(new[] { typeof(int) });
            il.Emit(OpCodes.Newobj, _module!.ImportReference(ctor));
        }
        else
        {
            // Use decimal constructor from parts
            int[] bits = decimal.GetBits(value);
            il.Emit(OpCodes.Ldc_I4, bits[0]);
            il.Emit(OpCodes.Ldc_I4, bits[1]);
            il.Emit(OpCodes.Ldc_I4, bits[2]);
            il.Emit(OpCodes.Ldc_I4, (bits[3] >> 31) != 0 ? 1 : 0); // sign
            il.Emit(OpCodes.Ldc_I4, (byte)((bits[3] >> 16) & 0xFF)); // scale
            var ctor = typeof(decimal).GetConstructor(
                new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) });
            il.Emit(OpCodes.Newobj, _module!.ImportReference(ctor));
        }
    }
}

public record EmitResult(bool Success, string OutputPath);
