using CobolSharp.Compiler.Common;
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
    private MethodReference? _multiplyByMethod;
    private MethodReference? _divideIntoMethod;
    private MethodReference? _divideGivingMethod;
    private MethodReference? _getNumericValueMethod;
    private MethodReference? _getDisplayValueMethod;

    // File I/O
    private FieldDefinition? _fileManagerField;
    private TypeReference? _fileManagerRef;
    private MethodReference? _fileManagerCtor;
    private MethodReference? _fileManagerRegisterMethod;
    private MethodReference? _fileManagerOpenMethod;
    private MethodReference? _fileManagerCloseMethod;
    private MethodReference? _fileManagerReadNextMethod;
    private MethodReference? _fileManagerWriteMethod;
    private MethodReference? _fileManagerRewriteMethod;
    private MethodReference? _fileManagerDeleteMethod;
    private MethodReference? _seqFileHandlerCtor;
    private TypeReference? _fileOpenModeRef;
    private MethodReference? _fileReadNextMethod;
    private MethodReference? _fileWriteMethod;
    private MethodReference? _fileRewriteMethod;
    private MethodReference? _setFromBytesMethod;
    private MethodReference? _copyToBytesMethod;

    // Maps COBOL file name → record field name (01-level under FD)
    private readonly Dictionary<string, string> _fileRecordNames = new(StringComparer.OrdinalIgnoreCase);
    // Maps COBOL file name → FILE STATUS field name
    private readonly Dictionary<string, string> _fileStatusNames = new(StringComparer.OrdinalIgnoreCase);

    // New runtime methods for all statement types
    private MethodReference? _acceptFromConsoleMethod;
    private MethodReference? _acceptDateMethod;
    private MethodReference? _acceptDayMethod;
    private MethodReference? _acceptTimeMethod;
    private MethodReference? _initializeFieldMethod;
    private MethodReference? _callProgramMethod;
    private MethodReference? _stringConcatMethod;
    private MethodReference? _unstringFieldMethod;
    private MethodReference? _inspectReplacingMethod;
    private MethodReference? _inspectTallyingMethod;

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
            _assembly.Write(outputPath);
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
        _multiplyByMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "MultiplyBy"));
        _divideIntoMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "DivideInto"));
        _divideGivingMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "DivideGiving"));

        // File I/O types
        var fileManagerTypeDef = runtimeModule.GetType("CobolSharp.Runtime.IO.CobolFileManager");
        _fileManagerRef = _module.ImportReference(fileManagerTypeDef);
        _fileManagerCtor = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.IsConstructor && !m.HasParameters));
        _fileManagerRegisterMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "RegisterFile"));
        _fileManagerOpenMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "Open"));
        _fileManagerCloseMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "Close"));
        _fileManagerReadNextMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "ReadNext"));
        _fileManagerWriteMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "Write"));
        _fileManagerRewriteMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "Rewrite"));
        _fileManagerDeleteMethod = _module.ImportReference(
            fileManagerTypeDef.Methods.First(m => m.Name == "Delete"));

        var seqHandlerTypeDef = runtimeModule.GetType("CobolSharp.Runtime.IO.SequentialFileHandler");
        _seqFileHandlerCtor = _module.ImportReference(
            seqHandlerTypeDef.Methods.First(m => m.IsConstructor));

        var fileOpenModeTypeDef = runtimeModule.GetType("CobolSharp.Runtime.IO.FileOpenMode");
        _fileOpenModeRef = _module.ImportReference(fileOpenModeTypeDef);

        // File I/O helper methods on CobolProgram
        _fileReadNextMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "FileReadNext"));
        _fileWriteMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "FileWrite"));
        _fileRewriteMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "FileRewrite"));

        // CobolField methods for file I/O
        _setFromBytesMethod = _module.ImportReference(
            cobolFieldTypeDef.Methods.First(m => m.Name == "SetFromBytes"));
        _copyToBytesMethod = _module.ImportReference(
            cobolFieldTypeDef.Methods.First(m => m.Name == "CopyToBytes"));

        // New runtime methods
        _acceptFromConsoleMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "AcceptFromConsole"));
        _acceptDateMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "AcceptDate"));
        _acceptDayMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "AcceptDay"));
        _acceptTimeMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "AcceptTime"));
        _initializeFieldMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "InitializeField"));
        _callProgramMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "CallProgram"));
        _stringConcatMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "StringConcat"));
        _unstringFieldMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "UnstringField"));
        _inspectReplacingMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "InspectReplacing"));
        _inspectTallyingMethod = _module.ImportReference(
            cobolProgramTypeDef.Methods.First(m => m.Name == "InspectTallying"));

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

        // Initialize file manager if program has file I/O
        if (program.Environment?.FileControl?.Entries.Count > 0)
        {
            EmitFileManagerInit(il, program, symbols);
        }

        il.Emit(OpCodes.Ret);
        _programType!.Methods.Add(ctor);
    }

    private void EmitFileManagerInit(ILProcessor il, ProgramNode program, SymbolTable symbols)
    {
        // Create the file manager field
        _fileManagerField = new FieldDefinition(
            "_fileManager",
            FieldAttributes.Private,
            _fileManagerRef);
        _programType!.Fields.Add(_fileManagerField);

        // this._fileManager = new CobolFileManager()
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, _fileManagerCtor);
        il.Emit(OpCodes.Stfld, _fileManagerField);

        // Register each file handler
        var fileEntries = program.Environment!.FileControl!.Entries;
        var fdEntries = program.Data?.FileSection?.Entries ?? new List<Parsing.FileDescriptionEntry>();

        foreach (var selectEntry in fileEntries)
        {
            // Find the FD entry to determine record length
            var fd = fdEntries.FirstOrDefault(f =>
                string.Equals(f.FileName, selectEntry.FileName, StringComparison.OrdinalIgnoreCase));
            int recordLength = fd?.RecordContainsMax ?? 80;
            if (recordLength == 0 && fd?.RecordDescriptions.Count > 0)
            {
                // Try to get from symbol table
                var recName = fd.RecordDescriptions[0].Name;
                if (recName != null)
                {
                    var sym = symbols.Resolve(recName);
                    if (sym != null) recordLength = sym.ByteSize;
                }
            }
            if (recordLength == 0) recordLength = 80; // default

            bool lineSeq = selectEntry.Organization == Parsing.FileOrganization.LineSequential;

            // this._fileManager.RegisterFile("FILENAME",
            //     new SequentialFileHandler("external-name", recordLength, lineSeq))
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _fileManagerField);
            il.Emit(OpCodes.Ldstr, selectEntry.FileName);
            il.Emit(OpCodes.Ldstr, selectEntry.AssignTo);
            il.Emit(OpCodes.Ldc_I4, recordLength);
            il.Emit(lineSeq ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newobj, _seqFileHandlerCtor);
            il.Emit(OpCodes.Callvirt, _fileManagerRegisterMethod);

            // Create a byte[] field for the record buffer
            var bufferField = new FieldDefinition(
                $"_buf_{selectEntry.FileName}",
                FieldAttributes.Private,
                _module!.ImportReference(typeof(byte[])));
            _programType.Fields.Add(bufferField);
            _fields[$"_buf_{selectEntry.FileName}"] = bufferField;

            // this._buf_FILENAME = new byte[recordLength]
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, recordLength);
            il.Emit(OpCodes.Newarr, _module.TypeSystem.Byte);
            il.Emit(OpCodes.Stfld, bufferField);

            // Track the record field name (01-level under FD)
            if (fd?.RecordDescriptions.Count > 0 && fd.RecordDescriptions[0].Name != null)
                _fileRecordNames[selectEntry.FileName] = fd.RecordDescriptions[0].Name!;

            // Track FILE STATUS field name
            if (selectEntry.FileStatusName != null)
                _fileStatusNames[selectEntry.FileName] = selectEntry.FileStatusName;
        }
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
            case AcceptStatement accept:
                EmitAcceptStatement(accept);
                break;
            case InitializeStatement init:
                EmitInitializeStatement(init);
                break;
            case AlterStatement:
                // ALTER is archaic (modifies GO TO targets at runtime).
                // Emit diagnostic — not a silent nop.
                EmitRuntimeWarning("ALTER statement is not supported");
                break;
            case SortStatement:
                // SORT requires file-based merge infrastructure.
                EmitRuntimeWarning("SORT statement is not yet implemented");
                break;
            case StringStatement str:
                EmitStringStatement(str);
                break;
            case UnstringStatement unstr:
                EmitUnstringStatement(unstr);
                break;
            case InspectStatement insp:
                EmitInspectStatement(insp);
                break;
            case OpenStatement open:
                EmitOpenStatement(open);
                break;
            case CloseStatement close:
                EmitCloseStatement(close);
                break;
            case ReadStatement read:
                EmitReadStatement(read);
                break;
            case WriteStatement write:
                EmitWriteStatement(write);
                break;
            case RewriteStatement rewrite:
                EmitRewriteStatement(rewrite);
                break;
            case DeleteStatement del:
                EmitDeleteStatement(del);
                break;
            case StartStatement start:
                EmitStartStatement(start);
                break;
            case CallStatement call:
                EmitCallStatement(call);
                break;
            case CancelStatement:
                // CANCEL unloads a called program — no-op in .NET (GC handles it)
                _il!.Emit(OpCodes.Nop);
                break;
            case InitiateStatement:
            case GenerateStatement:
            case TerminateStatement:
                // Report Writer — requires full report runtime, emit diagnostic
                EmitRuntimeWarning("Report Writer statements are not yet implemented");
                break;
            case InvokeStatement:
                EmitRuntimeWarning("INVOKE (OO COBOL) is not yet implemented");
                break;
            case RaiseStatement:
            case ResumeStatement:
                EmitRuntimeWarning("Exception handling statements are not yet implemented");
                break;
            case SourceFormatDirective:
                // Compiler directive — no runtime code needed (correct nop)
                break;
            case EvaluateStatement eval:
                EmitEvaluateStatement(eval);
                break;
            case MultiplyStatement mul:
                EmitMultiplyStatement(mul);
                break;
            case DivideStatement div:
                EmitDivideStatement(div);
                break;
            case SetStatement set:
                EmitSetStatement(set);
                break;
            case SearchStatement:
                EmitRuntimeWarning("SEARCH statement requires table indexing — not yet implemented");
                break;
            case GobackStatement:
                EmitStopRunStatement(); // GOBACK is equivalent to STOP RUN
                break;
            case GoToDependingStatement goToDep:
                EmitGoToDependingStatement(goToDep);
                break;
        }
    }

    private void EmitEvaluateStatement(EvaluateStatement eval)
    {
        // Emit EVALUATE as if-else chain:
        // EVALUATE subject WHEN val1 stmts WHEN val2 stmts WHEN OTHER stmts END-EVALUATE
        // becomes:
        // if (subject == val1) { stmts } else if (subject == val2) { stmts } else { stmts }
        var endLabel = _il!.Create(OpCodes.Nop);

        foreach (var whenClause in eval.WhenClauses)
        {
            var nextWhen = _il.Create(OpCodes.Nop);

            // For multiple objects in one WHEN: OR them together
            // WHEN 1 WHEN 2 → subject = 1 OR subject = 2
            if (whenClause.Objects.Count == 1)
            {
                // Simple case: single WHEN object
                EmitConditionExpression(
                    new BinaryExpression(eval.Subject, BinaryOperator.Equal,
                        whenClause.Objects[0],
                        TextSpan.FromBounds(eval.Subject.Span.Start, whenClause.Objects[0].Span.End)));
                _il.Emit(OpCodes.Brfalse, nextWhen);
            }
            else
            {
                // Multiple WHEN objects: OR them
                var matchLabel = _il.Create(OpCodes.Nop);
                foreach (var obj in whenClause.Objects)
                {
                    EmitConditionExpression(
                        new BinaryExpression(eval.Subject, BinaryOperator.Equal, obj,
                            TextSpan.FromBounds(eval.Subject.Span.Start, obj.Span.End)));
                    _il.Emit(OpCodes.Brtrue, matchLabel);
                }
                _il.Emit(OpCodes.Br, nextWhen);
                _il.Append(matchLabel);
            }

            // Emit the statements for this WHEN
            foreach (var stmt in whenClause.Statements)
                EmitStatement(stmt);

            _il.Emit(OpCodes.Br, endLabel);
            _il.Append(nextWhen);
        }

        // WHEN OTHER
        foreach (var stmt in eval.WhenOtherStatements)
            EmitStatement(stmt);

        _il.Append(endLabel);
    }

    private void EmitGoToDependingStatement(GoToDependingStatement goToDep)
    {
        // GO TO para1 para2 para3 DEPENDING ON expr
        // If expr=1, go to para1; expr=2, go to para2; etc.
        // If expr < 1 or > count, fall through (COBOL spec behavior).
        var fallThrough = _il!.Create(OpCodes.Nop);
        var labels = new List<Mono.Cecil.Cil.Instruction>();

        // Create labels for each paragraph
        foreach (var _ in goToDep.ParagraphNames)
            labels.Add(_il.Create(OpCodes.Nop));

        // Evaluate the expression → decimal on stack
        EmitNumericExprValue(goToDep.DependingOn);

        // Convert decimal to int
        var decToInt = _module!.ImportReference(
            typeof(decimal).GetMethods()
                .First(m => m.Name == "op_Explicit" && m.ReturnType == typeof(int)));
        _il.Emit(OpCodes.Call, decToInt);

        // switch (value - 1) → jump table
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Sub);
        _il.Emit(OpCodes.Switch, labels.ToArray());
        _il.Emit(OpCodes.Br, fallThrough); // out of range → fall through

        for (int i = 0; i < goToDep.ParagraphNames.Count; i++)
        {
            _il.Append(labels[i]);
            string pName = goToDep.ParagraphNames[i].ToUpperInvariant();
            if (_paragraphMethods.TryGetValue(pName, out var method))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Call, method);
            }
            _il.Emit(OpCodes.Ret);
        }

        _il.Append(fallThrough);
    }

    /// <summary>Emit a Console.Error.WriteLine with a warning message.</summary>
    private void EmitRuntimeWarning(string message)
    {
        var writeLineMethod = _module!.ImportReference(
            typeof(Console).GetProperty("Error")!.GetGetMethod());
        var textWriterWriteLine = _module.ImportReference(
            typeof(System.IO.TextWriter).GetMethod("WriteLine", new[] { typeof(string) }));
        _il!.Emit(OpCodes.Call, writeLineMethod); // Console.Error
        _il.Emit(OpCodes.Ldstr, message);
        _il.Emit(OpCodes.Callvirt, textWriterWriteLine);
    }

    private void EmitAcceptStatement(AcceptStatement accept)
    {
        if (!_fields.TryGetValue(accept.Target.Name, out var targetField))
            return;

        _il!.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, targetField);

        if (accept.FromSource == null)
        {
            _il.Emit(OpCodes.Call, _acceptFromConsoleMethod);
        }
        else
        {
            var method = accept.FromSource.ToUpperInvariant() switch
            {
                "DATE" => _acceptDateMethod,
                "DAY" => _acceptDayMethod,
                "TIME" => _acceptTimeMethod,
                "DAY-OF-WEEK" => _acceptDateMethod, // approximate
                _ => _acceptFromConsoleMethod
            };
            _il.Emit(OpCodes.Call, method);
        }
    }

    private void EmitInitializeStatement(InitializeStatement init)
    {
        foreach (var target in init.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
            _il.Emit(OpCodes.Call, _initializeFieldMethod);
        }
    }

    private void EmitCallStatement(CallStatement call)
    {
        // Emit: CallProgram(programName, new CobolField[] { param1, param2, ... })
        // Push program name as string
        if (call.ProgramName is StringLiteralExpression strLit)
        {
            _il!.Emit(OpCodes.Ldstr, strLit.Value);
        }
        else if (call.ProgramName is IdentifierExpression id &&
                 _fields.TryGetValue(id.Name, out var nameField))
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, nameField);
            _il.Emit(OpCodes.Call, _getDisplayValueMethod);
        }
        else
        {
            _il!.Emit(OpCodes.Ldstr, "UNKNOWN");
        }

        // Create parameter array
        _il.Emit(OpCodes.Ldc_I4, call.Parameters.Count);
        _il.Emit(OpCodes.Newarr, _cobolFieldRef);
        for (int i = 0; i < call.Parameters.Count; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            if (call.Parameters[i].Value is IdentifierExpression paramId &&
                _fields.TryGetValue(paramId.Name, out var paramField))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, paramField);
            }
            else
            {
                _il.Emit(OpCodes.Ldnull);
            }
            _il.Emit(OpCodes.Stelem_Ref);
        }

        _il.Emit(OpCodes.Call, _callProgramMethod);
    }

    private void EmitStringStatement(StringStatement str)
    {
        // Create sources array
        _il!.Emit(OpCodes.Ldc_I4, str.Sources.Count);
        _il.Emit(OpCodes.Newarr, _cobolFieldRef);
        for (int i = 0; i < str.Sources.Count; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            if (str.Sources[i].Value is IdentifierExpression srcId &&
                _fields.TryGetValue(srcId.Name, out var srcField))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, srcField);
            }
            else
            {
                _il.Emit(OpCodes.Ldnull);
            }
            _il.Emit(OpCodes.Stelem_Ref);
        }

        // Create delimiters array (string?[])
        _il.Emit(OpCodes.Ldc_I4, str.Sources.Count);
        _il.Emit(OpCodes.Newarr, _module!.TypeSystem.String);
        for (int i = 0; i < str.Sources.Count; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            var delim = str.Sources[i].Delimiter;
            if (delim is StringLiteralExpression delimStr)
                _il.Emit(OpCodes.Ldstr, delimStr.Value);
            else if (delim is IdentifierExpression delimId &&
                     _fields.TryGetValue(delimId.Name, out var delimField))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, delimField);
                _il.Emit(OpCodes.Call, _getDisplayValueMethod);
            }
            else
                _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        // Target field
        if (_fields.TryGetValue(str.Target.Name, out var targetField))
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
        }
        else
        {
            _il.Emit(OpCodes.Ldnull);
        }

        // Pointer field (or null)
        if (str.Pointer != null && _fields.TryGetValue(str.Pointer.Name, out var ptrField))
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, ptrField);
        }
        else
        {
            _il.Emit(OpCodes.Ldnull);
        }

        _il.Emit(OpCodes.Call, _stringConcatMethod);
    }

    private void EmitUnstringStatement(UnstringStatement unstr)
    {
        // Source field
        if (_fields.TryGetValue(unstr.Source.Name, out var srcField))
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, srcField);
        }
        else
        {
            _il!.Emit(OpCodes.Ldnull);
        }

        // Delimiter string (or null)
        if (unstr.Delimiter is StringLiteralExpression delimStr)
            _il.Emit(OpCodes.Ldstr, delimStr.Value);
        else if (unstr.Delimiter is IdentifierExpression delimId &&
                 _fields.TryGetValue(delimId.Name, out var delimField))
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, delimField);
            _il.Emit(OpCodes.Call, _getDisplayValueMethod);
        }
        else
            _il.Emit(OpCodes.Ldnull);

        // Targets array
        _il.Emit(OpCodes.Ldc_I4, unstr.Targets.Count);
        _il.Emit(OpCodes.Newarr, _cobolFieldRef);
        for (int i = 0; i < unstr.Targets.Count; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            if (_fields.TryGetValue(unstr.Targets[i].Name, out var tgtField))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, tgtField);
            }
            else
                _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Stelem_Ref);
        }

        // Tallying field (or null)
        if (unstr.Tallying != null && _fields.TryGetValue(unstr.Tallying.Name, out var tallyField))
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, tallyField);
        }
        else
            _il.Emit(OpCodes.Ldnull);

        _il.Emit(OpCodes.Call, _unstringFieldMethod);
    }

    private void EmitInspectStatement(InspectStatement insp)
    {
        if (!_fields.TryGetValue(insp.Target.Name, out var targetField))
            return;

        bool isTallying = insp.InspectKind is InspectType.TallyingAll or InspectType.TallyingLeading;
        bool allOccurrences = insp.InspectKind is InspectType.ReplacingAll or InspectType.TallyingAll;

        if (isTallying)
        {
            // InspectTallying(target, counter, searchFor, allOccurrences)
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);

            if (insp.TallyCounter != null && _fields.TryGetValue(insp.TallyCounter.Name, out var counterField))
            {
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, counterField);
            }
            else
                _il.Emit(OpCodes.Ldnull);

            EmitStringExprValue(insp.SearchFor);
            _il.Emit(allOccurrences ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Call, _inspectTallyingMethod);
        }
        else if (insp.InspectKind == InspectType.Converting)
        {
            // Converting X TO Y = replacing ALL X BY Y
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
            EmitStringExprValue(insp.SearchFor);
            EmitStringExprValue(insp.ReplaceWith);
            _il.Emit(OpCodes.Ldc_I4_1); // all occurrences
            _il.Emit(OpCodes.Call, _inspectReplacingMethod);
        }
        else
        {
            // InspectReplacing(target, searchFor, replaceWith, allOccurrences)
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
            EmitStringExprValue(insp.SearchFor);
            EmitStringExprValue(insp.ReplaceWith);
            _il.Emit(allOccurrences ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            _il.Emit(OpCodes.Call, _inspectReplacingMethod);
        }
    }

    /// <summary>Emit an expression as a string value on the stack.</summary>
    private void EmitStringExprValue(Expression? expr)
    {
        if (expr is StringLiteralExpression strLit)
            _il!.Emit(OpCodes.Ldstr, strLit.Value);
        else if (expr is IdentifierExpression id && _fields.TryGetValue(id.Name, out var field))
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, field);
            _il.Emit(OpCodes.Call, _getDisplayValueMethod);
        }
        else
            _il!.Emit(OpCodes.Ldstr, "");
    }

    // ── File I/O emission ──

    /// <summary>Helper: emit status store if FILE STATUS is declared.</summary>
    private void EmitFileStatusStore(string fileName)
    {
        // Stack has the status string on top. If FILE STATUS declared, store it.
        if (_fileStatusNames.TryGetValue(fileName, out var statusName) &&
            _fields.TryGetValue(statusName, out var statusField))
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, statusField);
            _il.Emit(OpCodes.Call, _moveAlphanumericMethod);
        }
        else
        {
            _il!.Emit(OpCodes.Pop); // discard status string
        }
    }

    private void EmitOpenStatement(OpenStatement open)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }

        foreach (var clause in open.Clauses)
        {
            int mode = clause.Mode switch
            {
                OpenMode.Input => 0,
                OpenMode.Output => 1,
                OpenMode.InputOutput => 2,
                OpenMode.Extend => 3,
                _ => 0
            };

            foreach (var fileName in clause.FileNames)
            {
                // _fileManager.Open(fileName, mode)
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, _fileManagerField);
                _il.Emit(OpCodes.Ldstr, fileName);
                _il.Emit(OpCodes.Ldc_I4, mode);
                _il.Emit(OpCodes.Callvirt, _fileManagerOpenMethod);
                EmitFileStatusStore(fileName);
            }
        }
    }

    private void EmitCloseStatement(CloseStatement close)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }

        foreach (var fileName in close.FileNames)
        {
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, _fileManagerField);
            _il.Emit(OpCodes.Ldstr, fileName);
            _il.Emit(OpCodes.Callvirt, _fileManagerCloseMethod);
            EmitFileStatusStore(fileName);
        }
    }

    private void EmitReadStatement(ReadStatement read)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }

        string fileName = read.FileName;

        // Get the record field and buffer for this file
        string? recName = _fileRecordNames.GetValueOrDefault(fileName);
        FieldDefinition? recField = recName != null ? _fields.GetValueOrDefault(recName) : null;
        FieldDefinition? bufField = _fields.GetValueOrDefault($"_buf_{fileName}");

        if (recField == null || bufField == null)
        {
            EmitRuntimeWarning($"READ {fileName}: no record field or buffer found");
            return;
        }

        // status = FileReadNext(this._fileManager, fileName, this._buf, this.recordField)
        _il!.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, _fileManagerField);
        _il.Emit(OpCodes.Ldstr, fileName);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, bufField);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, recField);
        _il.Emit(OpCodes.Call, _fileReadNextMethod);

        // Store status; also need it for AT END check
        var statusLocal = new VariableDefinition(_module!.TypeSystem.String);
        _runMethod!.Body.Variables.Add(statusLocal);
        _il.Emit(OpCodes.Dup);
        _il.Emit(OpCodes.Stloc, statusLocal);
        EmitFileStatusStore(fileName);

        // INTO clause: copy record field to INTO target
        if (read.Into != null && _fields.TryGetValue(read.Into.Name, out var intoField))
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, recField);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, intoField);
            _il.Emit(OpCodes.Call, _moveFieldMethod);
        }

        // AT END / NOT AT END branching
        if (read.AtEnd.Count > 0 || read.NotAtEnd.Count > 0)
        {
            var notAtEndLabel = _il.Create(OpCodes.Nop);
            var endLabel = _il.Create(OpCodes.Nop);

            // if (status == "10") → AT END
            _il.Emit(OpCodes.Ldloc, statusLocal);
            _il.Emit(OpCodes.Ldstr, "10");
            var stringEquals = _module.ImportReference(
                typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) }));
            _il.Emit(OpCodes.Call, stringEquals);
            _il.Emit(OpCodes.Brfalse, notAtEndLabel);

            foreach (var stmt in read.AtEnd)
                EmitStatement(stmt);
            _il.Emit(OpCodes.Br, endLabel);

            _il.Append(notAtEndLabel);
            foreach (var stmt in read.NotAtEnd)
                EmitStatement(stmt);

            _il.Append(endLabel);
        }
    }

    private void EmitWriteStatement(WriteStatement write)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }

        // WRITE record-name — find which file this record belongs to
        string recName = write.RecordName.Name;
        string? fileName = null;
        foreach (var (fn, rn) in _fileRecordNames)
        {
            if (string.Equals(rn, recName, StringComparison.OrdinalIgnoreCase))
            { fileName = fn; break; }
        }
        if (fileName == null) { EmitRuntimeWarning($"WRITE: cannot find file for record {recName}"); return; }

        FieldDefinition? recField = _fields.GetValueOrDefault(recName);
        FieldDefinition? bufField = _fields.GetValueOrDefault($"_buf_{fileName}");
        if (recField == null || bufField == null) return;

        // FROM clause: move source to record field first
        if (write.From != null)
        {
            if (write.From is IdentifierExpression fromId && _fields.TryGetValue(fromId.Name, out var fromField))
            {
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, fromField);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, recField);
                _il.Emit(OpCodes.Call, _moveFieldMethod);
            }
        }

        // FileWrite(fm, fileName, buffer, recordField)
        _il!.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, _fileManagerField);
        _il.Emit(OpCodes.Ldstr, fileName);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, bufField);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, recField);
        _il.Emit(OpCodes.Call, _fileWriteMethod);
        EmitFileStatusStore(fileName);
    }

    private void EmitRewriteStatement(RewriteStatement rewrite)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }

        string recName = rewrite.RecordName.Name;
        string? fileName = null;
        foreach (var (fn, rn) in _fileRecordNames)
        {
            if (string.Equals(rn, recName, StringComparison.OrdinalIgnoreCase))
            { fileName = fn; break; }
        }
        if (fileName == null) return;

        FieldDefinition? recField = _fields.GetValueOrDefault(recName);
        FieldDefinition? bufField = _fields.GetValueOrDefault($"_buf_{fileName}");
        if (recField == null || bufField == null) return;

        if (rewrite.From != null)
        {
            if (rewrite.From is IdentifierExpression fromId && _fields.TryGetValue(fromId.Name, out var fromField))
            {
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, fromField);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, recField);
                _il.Emit(OpCodes.Call, _moveFieldMethod);
            }
        }

        _il!.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, _fileManagerField);
        _il.Emit(OpCodes.Ldstr, fileName);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, bufField);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, recField);
        _il.Emit(OpCodes.Call, _fileRewriteMethod);
        EmitFileStatusStore(fileName);
    }

    private void EmitDeleteStatement(DeleteStatement del)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }

        _il!.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Ldfld, _fileManagerField);
        _il.Emit(OpCodes.Ldstr, del.FileName);
        _il.Emit(OpCodes.Callvirt, _fileManagerDeleteMethod);
        EmitFileStatusStore(del.FileName);
    }

    private void EmitStartStatement(StartStatement start)
    {
        if (_fileManagerField == null) { _il!.Emit(OpCodes.Nop); return; }
        // START is a positioning operation for keyed files — requires IFileHandler.Start
        // For now, emit a diagnostic for this less common operation
        EmitRuntimeWarning($"START {start.FileName} — keyed positioning not yet implemented");
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

    private void EmitMultiplyStatement(MultiplyStatement mul)
    {
        // MULTIPLY operand BY target1 [target2 ...]
        foreach (var target in mul.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;
            EmitNumericExprValue(mul.Operand);
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
            _il.Emit(OpCodes.Call, _multiplyByMethod);
        }
    }

    private void EmitDivideStatement(DivideStatement div)
    {
        // DIVIDE operand INTO target
        foreach (var target in div.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;
            EmitNumericExprValue(div.Operand);
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
            _il.Emit(OpCodes.Call, _divideIntoMethod);
        }
    }

    private void EmitSetStatement(SetStatement set)
    {
        // SET target TO value — equivalent to MOVE for code gen purposes
        foreach (var target in set.Targets)
        {
            if (!_fields.TryGetValue(target.Name, out var targetField))
                continue;
            EmitNumericExprValue(set.Value);
            _il!.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, targetField);
            _il.Emit(OpCodes.Call, _moveNumericMethod);
        }
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
        else if (perform.Varying != null && perform.Until != null)
        {
            // PERFORM VARYING identifier FROM expr BY expr UNTIL condition
            // 1. SET identifier TO from-value
            // 2. Loop: test condition → exit if true
            // 3. Execute body
            // 4. ADD by-value TO identifier
            // 5. Go to 2
            var varying = perform.Varying;
            if (_fields.TryGetValue(varying.Identifier.Name, out var varyField))
            {
                // Step 1: identifier = FROM value
                EmitNumericExprValue(varying.From);
                _il!.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, varyField);
                _il.Emit(OpCodes.Call, _moveNumericMethod);

                var loopStart = _il.Create(OpCodes.Nop);
                var loopEnd = _il.Create(OpCodes.Nop);

                _il.Append(loopStart);

                // Step 2: test UNTIL condition
                EmitConditionExpression(perform.Until);
                _il.Emit(OpCodes.Brtrue, loopEnd);

                // Step 3: execute body or call paragraph
                if (perform.ParagraphName != null)
                {
                    string pName = perform.ParagraphName.ToUpperInvariant();
                    if (_paragraphMethods.TryGetValue(pName, out var m))
                    {
                        _il.Emit(OpCodes.Ldarg_0);
                        _il.Emit(OpCodes.Call, m);
                    }
                }
                else
                {
                    foreach (var stmt in perform.Body)
                        EmitStatement(stmt);
                }

                // Step 4: ADD BY value TO identifier
                EmitNumericExprValue(varying.By);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, varyField);
                _il.Emit(OpCodes.Call, _addToMethod);

                _il.Emit(OpCodes.Br, loopStart);
                _il.Append(loopEnd);
            }
        }
        else if (perform.Until != null)
        {
            // PERFORM UNTIL condition (inline or out-of-line)
            var loopStart = _il!.Create(OpCodes.Nop);
            var loopEnd = _il.Create(OpCodes.Nop);

            _il.Append(loopStart);
            EmitConditionExpression(perform.Until);
            _il.Emit(OpCodes.Brtrue, loopEnd);

            if (perform.ParagraphName != null)
            {
                string pName = perform.ParagraphName.ToUpperInvariant();
                if (_paragraphMethods.TryGetValue(pName, out var m))
                {
                    _il.Emit(OpCodes.Ldarg_0);
                    _il.Emit(OpCodes.Call, m);
                }
            }
            else
            {
                foreach (var stmt in perform.Body)
                    EmitStatement(stmt);
            }

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
