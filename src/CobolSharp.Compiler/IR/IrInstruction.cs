// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Runtime;

namespace CobolSharp.Compiler.IR;

/// <summary>
/// Base class for all IR instructions. CIL-friendly, COBOL-aware.
/// </summary>
public abstract class IrInstruction
{
    public IrValue? Result { get; protected set; }
}

// ── Compiler temporaries ──

/// <summary>
/// A compiler-generated temporary variable. Not addressable from COBOL.
/// Scoped to the containing method. Lowered to a CIL local by the emitter.
/// </summary>
public sealed record IrTemp(string Name, IrPrimitiveType Type, int Id);

/// <summary>
/// Inline PERFORM N TIMES: execute BodyStatements exactly CountExpression times.
/// CountExpression is evaluated once at entry into a compiler temp (IrTemp).
/// The emitter manages the CIL local counter. EXIT PERFORM exits this scope.
/// </summary>
public sealed class IrPerformInlineTimes : IrInstruction
{
    public IrExpression CountExpression { get; }
    public IReadOnlyList<IrInstruction> BodyInstructions { get; }

    public IrPerformInlineTimes(IrExpression countExpression, IReadOnlyList<IrInstruction> bodyInstructions)
    {
        CountExpression = countExpression;
        BodyInstructions = bodyInstructions;
    }
}

// ── Data movement ──

public sealed class IrLoadField : IrInstruction
{
    public IrValue Record { get; }
    public IrField Field { get; }

    public IrLoadField(IrValue result, IrValue record, IrField field)
    {
        Result = result;
        Record = record;
        Field = field;
    }
}

public sealed class IrStoreField : IrInstruction
{
    public IrValue Record { get; }
    public IrField Field { get; }
    public IrValue Value { get; }

    public IrStoreField(IrValue record, IrField field, IrValue value)
    {
        Record = record;
        Field = field;
        Value = value;
    }
}

public sealed class IrMove : IrInstruction
{
    public IrValue Source { get; }
    public IrValue Target { get; }

    public IrMove(IrValue source, IrValue target)
    {
        Source = source;
        Target = target;
    }
}

public sealed class IrLoadConst : IrInstruction
{
    public object Value { get; }

    public IrLoadConst(IrValue result, object value)
    {
        Result = result;
        Value = value;
    }
}

// ── Arithmetic and comparisons ──

public enum IrBinaryOp
{
    Add, Sub, Mul, Div,
    And, Or,
    Eq, Ne, Lt, Le, Gt, Ge
}

public sealed class IrBinary : IrInstruction
{
    public IrBinaryOp Op { get; }
    public IrValue Left { get; }
    public IrValue Right { get; }

    public IrBinary(IrValue result, IrBinaryOp op, IrValue left, IrValue right)
    {
        Result = result;
        Op = op;
        Left = left;
        Right = right;
    }
}

// ── Control flow ──

public sealed class IrBranch : IrInstruction
{
    public IrValue Condition { get; }
    public IrBasicBlock TrueTarget { get; }
    public IrBasicBlock FalseTarget { get; }

    public IrBranch(IrValue condition, IrBasicBlock trueTarget, IrBasicBlock falseTarget)
    {
        Condition = condition;
        TrueTarget = trueTarget;
        FalseTarget = falseTarget;
    }
}

public sealed class IrJump : IrInstruction
{
    public IrBasicBlock Target { get; }
    public IrJump(IrBasicBlock target) => Target = target;
}

/// <summary>
/// Branch to Target if Condition is false; otherwise fall through.
/// </summary>
public sealed class IrBranchIfFalse : IrInstruction
{
    public IrValue Condition { get; }
    public IrBasicBlock Target { get; }

    public IrBranchIfFalse(IrValue condition, IrBasicBlock target)
    {
        Condition = condition;
        Target = target;
    }
}

/// <summary>
/// Store a boolean constant into an IrValue (used as fallback condition).
/// </summary>
public sealed class IrSetBool : IrInstruction
{
    public bool Value { get; }

    public IrSetBool(IrValue result, bool value)
    {
        Result = result;
        Value = value;
    }
}

/// <summary>
/// Tests an implementor switch state: result = SwitchRuntime.GetSwitchState(name) == testOn.
/// </summary>
public sealed class IrTestSwitch : IrInstruction
{
    public string ImplementorName { get; }
    public bool TestOnState { get; }

    public IrTestSwitch(IrValue result, string implementorName, bool testOnState)
    {
        Result = result;
        ImplementorName = implementorName;
        TestOnState = testOnState;
    }
}

/// <summary>SET mnemonic-name TO {ON | OFF} — sets implementor switch state.</summary>
public sealed class IrSetSwitch : IrInstruction
{
    public string ImplementorName { get; }
    public bool SetToOn { get; }

    public IrSetSwitch(string implementorName, bool setToOn)
    {
        ImplementorName = implementorName;
        SetToOn = setToOn;
    }
}

public enum IrLogicalOp { And, Or, Not, Xor }

/// <summary>
/// Logical AND/OR/NOT on boolean values.
/// For NOT, Left and Right are the same value (only Left is used).
/// </summary>
public sealed class IrBinaryLogical : IrInstruction
{
    public IrValue Left { get; }
    public IrValue Right { get; }
    public IrLogicalOp Op { get; }

    public IrBinaryLogical(IrValue result, IrValue left, IrValue right, IrLogicalOp op)
    {
        Result = result;
        Left = left;
        Right = right;
        Op = op;
    }
}

/// <summary>
/// Initialize (clear) the method's ArithmeticStatus local.
/// Emitted once per arithmetic statement, before any operations.
/// </summary>
public sealed class IrInitArithmeticStatus : IrInstruction { }

/// <summary>
/// Load the SizeError flag from the method's ArithmeticStatus local into a bool.
/// </summary>
public sealed class IrLoadSizeError : IrInstruction
{
    public IrLoadSizeError(IrValue result) => Result = result;
}

public sealed class IrReturn : IrInstruction
{
    public IrValue? Value { get; }
    public IrReturn(IrValue? value) => Value = value;
}

/// <summary>
/// Return a constant int from a paragraph method.
/// Fall-through: myIndex+1, GO TO: targetIndex, STOP RUN: -1.
/// </summary>
public sealed class IrReturnConst : IrInstruction
{
    public int Value { get; }
    public IrReturnConst(int value) => Value = value;
}

/// <summary>
/// PC-driven dispatch loop over paragraph methods (emitted in Main).
/// while (pc >= 0 && pc &lt; N) pc = paragraphs[pc]();
/// </summary>
public sealed class IrParagraphDispatch : IrInstruction
{
    public IReadOnlyList<IrMethod> Paragraphs { get; }
    public IrParagraphDispatch(IReadOnlyList<IrMethod> paragraphs) => Paragraphs = paragraphs;
}

/// <summary>
/// ALTER: writes a new target paragraph index into the alter indirection table.
/// </summary>
public sealed class IrAlter : IrInstruction
{
    /// <summary>Index into the alter table (identifies which alterable GO TO).</summary>
    public int AlterSlot { get; }
    /// <summary>New paragraph index to store in the alter table.</summary>
    public int NewTargetIndex { get; }

    public IrAlter(int alterSlot, int newTargetIndex)
    {
        AlterSlot = alterSlot;
        NewTargetIndex = newTargetIndex;
    }
}

/// <summary>
/// Return the value from the alter indirection table at the given slot.
/// Replaces IrReturnConst for GO TO statements inside ALTER-targeted paragraphs.
/// </summary>
public sealed class IrReturnAlterable : IrInstruction
{
    /// <summary>Index into the alter table.</summary>
    public int AlterSlot { get; }

    public IrReturnAlterable(int alterSlot) => AlterSlot = alterSlot;
}

// ── Inter-program CALL ──

/// <summary>
/// CALL another COBOL program via the program registry.
/// Builds a CobolDataPointer[] from the arguments, resolves the target,
/// and invokes its Entry method.
/// </summary>
public sealed class IrCallProgram : IrInstruction
{
    /// <summary>Static CALL: the literal program name. Dynamic CALL: the variable name (for diagnostics).</summary>
    public string TargetName { get; }
    public bool IsDynamic { get; }
    /// <summary>For dynamic CALL: the storage location holding the program name string. Null for static CALL.</summary>
    public IrLocation? TargetLocation { get; }
    public IReadOnlyList<IrCallArgument> CallArguments { get; }
    public IrLocation? ReturningTarget { get; }

    public IrCallProgram(string targetName, bool isDynamic,
        IReadOnlyList<IrCallArgument> args, IrLocation? returningTarget = null,
        IrLocation? targetLocation = null)
    {
        TargetName = targetName;
        IsDynamic = isDynamic;
        TargetLocation = targetLocation;
        CallArguments = args;
        ReturningTarget = returningTarget;
    }
}

/// <summary>
/// A single argument in an IrCallProgram instruction.
/// Carries the parameter passing mode and the source storage location.
/// </summary>
public sealed class IrCallArgument
{
    public int Mode { get; } // 0=ByReference, 1=ByContent, 2=ByValue
    public IrLocation Source { get; }

    public IrCallArgument(int mode, IrLocation source)
    {
        Mode = mode;
        Source = source;
    }
}

/// <summary>EXIT PROGRAM — return from a called program's Entry method.</summary>
public sealed class IrExitProgram : IrInstruction { }

/// <summary>GOBACK — return from called program, or terminate if in main.</summary>
public sealed class IrGoBack : IrInstruction { }

/// <summary>STOP RUN — terminate the entire run unit by throwing StopRunException.</summary>
public sealed class IrStopRun : IrInstruction { }

/// <summary>
/// CANCEL: mark a program to return to its initial state on the next CALL (ISO §14.9.5).
/// The static form (CANCEL "literal") carries the literal program-name. The dynamic form
/// (CANCEL identifier) carries the storage location from which the program-name is read at runtime.
/// </summary>
public sealed class IrCancelProgram : IrInstruction
{
    public string ProgramName { get; }
    public bool IsDynamic { get; }
    public IrLocation? TargetLocation { get; }
    public IrCancelProgram(string programName, bool isDynamic = false, IrLocation? targetLocation = null)
    {
        ProgramName = programName;
        IsDynamic = isDynamic;
        TargetLocation = targetLocation;
    }
}

/// <summary>Check whether the last CALL raised an exception (target not found, etc.).</summary>
public sealed class IrCheckCallException : IrInstruction
{
    public string TargetName { get; }
    public IrCheckCallException(string targetName, IrValue result)
    {
        TargetName = targetName;
        Result = result;
    }
}

// ── Internal calls and PERFORM ──

public sealed class IrCall : IrInstruction
{
    public IrMethod Target { get; }
    public IReadOnlyList<IrValue> Arguments { get; }

    public IrCall(IrValue? result, IrMethod target, IReadOnlyList<IrValue> args)
    {
        Result = result;
        Target = target;
        Arguments = args;
    }
}

/// <summary>
/// PERFORM paragraph → call to generated method.
/// Each COBOL paragraph becomes its own IrMethod.
/// </summary>
public sealed class IrPerform : IrInstruction
{
    public IrMethod Target { get; }
    public IrPerform(IrMethod target) => Target = target;
}

/// <summary>
/// PERFORM para N TIMES: calls Target method Count times using a CIL local counter.
/// Count is an IrExpression (literal or identifier) evaluated once at entry.
/// The emitter manages the loop counter as a CIL local int.
/// </summary>
public sealed class IrPerformTimes : IrInstruction
{
    public IrMethod Target { get; }
    public int StartIdx { get; }
    public int EndIdx { get; }
    public IReadOnlyList<IrMethod> ThruMethods { get; }
    public IrExpression CountExpression { get; }

    public IrPerformTimes(IrMethod target, int startIdx, int endIdx,
        IReadOnlyList<IrMethod> thruMethods, IrExpression countExpression)
    {
        Target = target;
        StartIdx = startIdx;
        EndIdx = endIdx;
        ThruMethods = thruMethods;
        CountExpression = countExpression;
    }
}

/// <summary>
/// PERFORM para-a THRU para-b: dynamic dispatch loop that respects GO TO returns.
/// Calls paragraphs startIdx..endIdx, but if a paragraph returns a PC within the
/// range, skips forward to that PC. If it returns outside the range or negative, exits.
/// </summary>
public sealed class IrPerformThru : IrInstruction
{
    public int StartIndex { get; }
    public int EndIndex { get; }
    public IReadOnlyList<IrMethod> Paragraphs { get; }

    public IrPerformThru(int startIndex, int endIndex, IReadOnlyList<IrMethod> paragraphs)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
        Paragraphs = paragraphs;
    }
}

// ── Storage-backed data movement ──

/// <summary>
/// MOVE "literal" TO field — writes string bytes into ProgramState backing array.
/// </summary>
/// <summary>
/// MOVE string literal TO field. Uses PIC-aware MOVE semantics:
/// plain alphanumeric fields get left-justified space-padded copy,
/// alphanumeric-edited fields get edit pattern applied (B→space, 0→zero, etc.).
/// The emitter passes the destination PIC to the runtime so the correct
/// MOVE method is selected.
/// </summary>
public sealed class IrMoveStringToField : IrInstruction
{
    public IrLocation Target { get; }
    public string Value { get; }

    public IrMoveStringToField(IrLocation target, string value)
    {
        Target = target;
        Value = value;
    }
}

/// <summary>
/// Intrinsic function call: evaluates FUNCTION name(args) and stores the result
/// into a destination field. The function name and bound arguments are carried
/// to the emitter, which dispatches to IntrinsicFunctions.Call().
/// </summary>
public sealed class IrFunctionCall : IrInstruction
{
    public string FunctionName { get; }
    public IReadOnlyList<IrFunctionArg> Arguments { get; }
    public IrLocation Destination { get; }

    /// <summary>
    /// Whether the function's result is a string (alphanumeric) rather than a decimal.
    /// Derived from the binder-computed result category, which already accounts for
    /// category-polymorphic functions (MAX/MIN over all-alphanumeric arguments) — so the
    /// emitter must use this flag rather than a static function-name list.
    /// </summary>
    public bool ReturnsString { get; }

    /// <summary>
    /// Program collating sequence (256-byte code→weight table) for collating-sensitive functions
    /// (CHAR, ORD); null = native ordinal order. Resolved at lowering time (ISO §15.15, §15.36).
    /// </summary>
    public byte[]? CollatingSequence { get; init; }

    public IrFunctionCall(string functionName, IReadOnlyList<IrFunctionArg> arguments,
        IrLocation destination, bool returnsString)
    {
        FunctionName = functionName;
        Arguments = arguments;
        Destination = destination;
        ReturnsString = returnsString;
    }
}

/// <summary>
/// MOVE figurative-constant TO field — fills entire field with figurative byte value.
/// </summary>
public sealed class IrMoveFigurative : IrInstruction
{
    public IrLocation Destination { get; }
    public FigurativeKind FigurativeKind { get; }

    public IrMoveFigurative(IrLocation dest, FigurativeKind figurativeKind)
    {
        Destination = dest;
        FigurativeKind = figurativeKind;
    }
}

/// <summary>
/// MOVE ALL "pattern" TO field — repeats pattern to fill entire field.
/// </summary>
public sealed class IrMoveAllLiteral : IrInstruction
{
    public IrLocation Destination { get; }
    public string Pattern { get; }

    public IrMoveAllLiteral(IrLocation dest, string pattern)
    {
        Destination = dest;
        Pattern = pattern;
    }
}

/// <summary>
/// WRITE record — outputs record bytes from ProgramState to file.
/// </summary>
public sealed class IrWriteRecordFromStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrWriteRecordFromStorage(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// Report Writer SOURCE placement (ISO §14.9.19): copy a SOURCE field's bytes into the active report line
/// buffer at COLUMN, truncated to the report field's width. Emitted as a call to
/// ReportWriterRuntime.PlaceField with the source storage location (area/offset/size).
/// </summary>
public sealed class IrReportPlaceField : IrInstruction
{
    public string ReportName { get; }
    public int Column { get; }
    public int FieldWidth { get; }
    public IrLocation Source { get; }

    public IrReportPlaceField(string reportName, int column, int fieldWidth, IrLocation source)
    {
        ReportName = reportName;
        Column = column;
        FieldWidth = fieldWidth;
        Source = source;
    }
}

/// <summary>
/// Report Writer VALUE-literal placement (ISO §13.18.63): place a constant literal string into the active
/// report line buffer at COLUMN, truncated to the field width — a body-group field whose value is a VALUE
/// clause rather than a SOURCE. Emitted as ReportWriterRuntime.PlaceLiteralField.
/// </summary>
public sealed class IrReportPlaceLiteral : IrInstruction
{
    public string ReportName { get; }
    public int Column { get; }
    public int FieldWidth { get; }
    public string Text { get; }

    public IrReportPlaceLiteral(string reportName, int column, int fieldWidth, string text)
    {
        ReportName = reportName;
        Column = column;
        FieldWidth = fieldWidth;
        Text = text;
    }
}

/// <summary>
/// Register a data-SOURCE field of an auto-presented report group (PAGE/REPORT HEADING/FOOTING, later
/// CONTROL HEADING/FOOTING): the runtime keeps the field's storage location (area/offset/size) and reads the
/// live bytes at presentation time (ISO §13.18.53). Emitted at INITIATE as ReportWriterRuntime.RegisterAutoDataField.
/// </summary>
public sealed class IrReportRegisterDataField : IrInstruction
{
    public string ReportName { get; }
    public int Slot { get; }
    public int Column { get; }
    public int FieldWidth { get; }
    public IrLocation Source { get; }

    public IrReportRegisterDataField(string reportName, int slot, int column, int fieldWidth, IrLocation source)
    {
        ReportName = reportName;
        Slot = slot;
        Column = column;
        FieldWidth = fieldWidth;
        Source = source;
    }
}

/// <summary>
/// Register a CONTROL item (or FINAL) of a report's control hierarchy at INITIATE (ISO §13.18.16): the runtime
/// reads the control item's live storage each GENERATE to detect a control break. Source is null for FINAL.
/// Emitted as ReportWriterRuntime.RegisterControl.
/// </summary>
public sealed class IrReportRegisterControl : IrInstruction
{
    public string ReportName { get; }
    public bool IsFinal { get; }
    public IrLocation? Source { get; }

    public IrReportRegisterControl(string reportName, bool isFinal, IrLocation? source)
    {
        ReportName = reportName;
        IsFinal = isFinal;
        Source = source;
    }
}

/// <summary>
/// Register a SUM accumulator and its DISPLAY-numeric addend storage at INITIATE (ISO §13.18.54): the runtime
/// adds the addend into the counter at each detail GENERATE. Emitted as ReportWriterRuntime.RegisterSum.
/// </summary>
public sealed class IrReportRegisterSum : IrInstruction
{
    public string ReportName { get; }
    public string CounterId { get; }
    public IrLocation Addend { get; }
    public int Scale { get; }

    public IrReportRegisterSum(string reportName, string counterId, IrLocation addend, int scale)
    {
        ReportName = reportName;
        CounterId = counterId;
        Addend = addend;
        Scale = scale;
    }
}

/// <summary>
/// Variable-length WRITE (RECORD IS VARYING … DEPENDING ON): write the record area for the number of
/// bytes given by the DEPENDING data item (read at runtime from <see cref="LengthLocation"/>), without
/// trailing-space trimming. ISO §13.18.43 / §14.9.51.
/// </summary>
public sealed class IrWriteRecordVariable : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    /// <summary>
    /// The DEPENDING data item supplying the byte count at runtime, or null for a VARYING file
    /// without DEPENDING — in which case the record's own declared length is written (no trimming).
    /// </summary>
    public IrLocation? LengthLocation { get; }

    public IrWriteRecordVariable(string fileName, IrLocation record, IrLocation? lengthLocation)
    {
        FileName = fileName;
        Record = record;
        LengthLocation = lengthLocation;
    }
}

/// <summary>
/// After a READ of a RECORD IS VARYING … DEPENDING ON file, store the actual record length
/// (FileRuntime.GetLastRecordLength) into the DEPENDING data item. ISO §13.18.43.
/// </summary>
public sealed class IrStoreRecordLength : IrInstruction
{
    public string CobolFileName { get; }
    public IrLocation LengthVariable { get; }

    public IrStoreRecordLength(string cobolFileName, IrLocation lengthVariable)
    {
        CobolFileName = cobolFileName;
        LengthVariable = lengthVariable;
    }
}

/// <summary>
/// Before a random/dynamic WRITE/REWRITE/DELETE on a RELATIVE file, convey the program's RELATIVE
/// KEY value to the runtime so the operation positions to that slot (FileRuntime.SetRelativeKey).
/// ISO §14.9.51 / §14.9.35 / §14.9.12.
/// </summary>
public sealed class IrSetRelativeKey : IrInstruction
{
    public string CobolFileName { get; }
    public IrLocation KeyVariable { get; }

    public IrSetRelativeKey(string cobolFileName, IrLocation keyVariable)
    {
        CobolFileName = cobolFileName;
        KeyVariable = keyVariable;
    }
}

/// <summary>
/// Before a RANDOM/DYNAMIC INDEXED DELETE, convey the prime RECORD KEY value (read from the record-key
/// data item in the record area) to the handler so it deletes the identified record (ISO §14.9.10 GR —
/// the record to delete is the one whose prime key equals the RECORD KEY data item). A keyed READ passes
/// the key in its call and a REWRITE carries it in the record content, but a DELETE writes no record, so
/// the key must be set explicitly.
/// </summary>
public sealed class IrSetIndexedKey : IrInstruction
{
    public string CobolFileName { get; }
    public IrLocation KeyVariable { get; }

    public IrSetIndexedKey(string cobolFileName, IrLocation keyVariable)
    {
        CobolFileName = cobolFileName;
        KeyVariable = keyVariable;
    }
}

/// <summary>
/// After a sequential WRITE or a READ on a RELATIVE file, move the relative record number of the
/// record released/made-available (FileRuntime.GetRelativeSlot) into the RELATIVE KEY data item.
/// ISO §14.9.51 GR (sequential WRITE) / §14.9.30 GR 25 (READ).
/// </summary>
public sealed class IrStoreRelativeKey : IrInstruction
{
    public string CobolFileName { get; }
    public IrLocation KeyVariable { get; }

    public IrStoreRelativeKey(string cobolFileName, IrLocation keyVariable)
    {
        CobolFileName = cobolFileName;
        KeyVariable = keyVariable;
    }
}

/// <summary>
/// REWRITE record — replaces the last-read record in a file.
/// </summary>
public sealed class IrRewriteRecordFromStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    /// <summary>For a RECORD VARYING file, the DEPENDING ON data item supplying the rewrite length at
    /// runtime (the number of bytes being rewritten). Null for a fixed-length record (use the record's
    /// declared size). Lets §14.9.35 GR16 compare the true rewrite length against the replaced record's.</summary>
    public IrLocation? LengthLocation { get; }

    public IrRewriteRecordFromStorage(string fileName, IrLocation record, IrLocation? lengthLocation = null)
    {
        FileName = fileName;
        Record = record;
        LengthLocation = lengthLocation;
    }
}

/// <summary>
/// WRITE BEFORE/AFTER ADVANCING: print-control write with line advance or page eject.
/// AdvanceLines = -1 means PAGE advancing (form-feed).
/// When AdvancingLocation is non-null, the advancing count is read from a data field
/// at runtime instead of using the compile-time AdvanceLines constant.
/// </summary>
public sealed class IrWriteAdvancing : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    public int AdvanceLines { get; }
    public bool IsBefore { get; }
    /// <summary>When non-null, advancing lines are read from this field at runtime.</summary>
    public IrLocation? AdvancingLocation { get; }

    public IrWriteAdvancing(string fileName, IrLocation record, int advanceLines, bool isBefore = false,
        IrLocation? advancingLocation = null)
    {
        FileName = fileName;
        Record = record;
        AdvanceLines = advanceLines;
        IsBefore = isBefore;
        AdvancingLocation = advancingLocation;
    }
}

/// <summary>
/// READ: read next record from file into storage location.
/// </summary>
public sealed class IrReadRecordToStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrReadRecordToStorage(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// READ PREVIOUS: read previous record from file into storage location (reverse sequential).
/// </summary>
public sealed class IrReadPreviousToStorage : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrReadPreviousToStorage(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// READ by key: read a specific record from an indexed/relative file using the key value.
/// Used for RANDOM and DYNAMIC access modes (non-NEXT reads).
/// </summary>
public sealed class IrReadByKey : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    public IrLocation Key { get; }
    // Key of reference: -1 = prime record key, 0+ = alternate record key index. A random/dynamic READ may
    // name an alternate key (READ … KEY IS alt-key, ISO §14.9.30); the chosen key becomes the key of
    // reference for a subsequent READ NEXT.
    public int KeyIndex { get; }

    public IrReadByKey(string fileName, IrLocation record, IrLocation key, int keyIndex = -1)
    {
        FileName = fileName;
        Record = record;
        Key = key;
        KeyIndex = keyIndex;
    }
}

/// <summary>
/// DELETE: delete the current record from an indexed/relative file.
/// </summary>
public sealed class IrDeleteRecord : IrInstruction
{
    public string FileName { get; }
    public IrDeleteRecord(string fileName) { FileName = fileName; }
}

/// <summary>DELETE FILE (COBOL-2023, ISO §14.9.10): delete the physical file. Carries the COBOL file name (for
/// FILE STATUS) and the ASSIGN target (to resolve the host path).</summary>
public sealed class IrDeleteFile : IrInstruction
{
    public string FileName { get; }
    public string AssignTarget { get; }
    public IrDeleteFile(string fileName, string assignTarget)
    {
        FileName = fileName;
        AssignTarget = assignTarget;
    }
}

/// <summary>
/// START: position an indexed file for subsequent READ NEXT.
/// Condition maps to Runtime.IO.StartCondition enum.
/// </summary>
public sealed class IrStartFile : IrInstruction
{
    public string FileName { get; }
    public IrLocation KeyLocation { get; }
    public int Condition { get; }
    // Key of reference: -1 = prime record key, 0+ = alternate record key index. START may position on the
    // prime key or any alternate key (ISO §14.9.41); the chosen key becomes the key of reference for the
    // subsequent sequential READ NEXT ordering.
    public int KeyIndex { get; }

    public IrStartFile(string fileName, IrLocation keyLocation, int condition, int keyIndex = -1)
    {
        FileName = fileName;
        KeyLocation = keyLocation;
        Condition = condition;
        KeyIndex = keyIndex;
    }
}

/// <summary>
/// Check if the last file operation was successful (status == "00").
/// Sets result bool to true if the operation failed (invalid key / error).
/// </summary>
public sealed class IrCheckFileInvalidKey : IrInstruction
{
    public string FileName { get; }

    public IrCheckFileInvalidKey(string fileName, IrValue result)
    {
        FileName = fileName;
        Result = result;
    }
}

/// <summary>
/// Decide whether a USE AFTER ERROR/EXCEPTION declarative of the given scope should run after the last
/// I/O on the file (ISO §14.9.49). Sets <see cref="IrInstruction.Result"/> bool via
/// FileRuntime.ShouldRunUseDeclarative(fileName, scope). Scope: -1 = file-name-scoped; 0/1/2/3 =
/// open-mode-scoped INPUT/OUTPUT/I-O/EXTEND.
/// </summary>
public sealed class IrCheckUseDeclarative : IrInstruction
{
    public string FileName { get; }
    public int Scope { get; }
    // When the originating I/O statement carries a handling phrase, that phrase — not the declarative —
    // services its own condition (ISO §14.6.6). ExcludeAtEnd suppresses the declarative for the at-end
    // condition (status 10) when an AT END phrase is present; ExcludeInvalidKey suppresses it for the
    // invalid-key conditions (21/22/23/24) when an INVALID KEY phrase is present. The declarative still
    // fires for every other exception (e.g. 47/48/49 not-open), which the phrase does not handle.
    public bool ExcludeAtEnd { get; }
    public bool ExcludeInvalidKey { get; }

    public IrCheckUseDeclarative(string fileName, int scope, IrValue result,
        bool excludeAtEnd = false, bool excludeInvalidKey = false)
    {
        FileName = fileName;
        Scope = scope;
        Result = result;
        ExcludeAtEnd = excludeAtEnd;
        ExcludeInvalidKey = excludeInvalidKey;
    }
}

/// <summary>
/// Dispatch a CONTAINING program's GLOBAL USE AFTER ERROR declarative after an I/O operation in this
/// (contained) program that this program has no USE declarative for (ISO §14.9.49.4 GR4 /
/// §8.4.6.2.2). Emits a call to GlobalUseDeclarativeRegistry.Dispatch(fileName, scope, excludeAtEnd,
/// excludeInvalidKey), which applies the same ShouldRunUseDeclarative gate as a local declarative and,
/// when it fires, invokes the containing program's registered handler. Scope: -1 file-name; 0/1/2/3
/// open-mode INPUT/OUTPUT/I-O/EXTEND.
/// </summary>
public sealed class IrDispatchGlobalUse : IrInstruction
{
    public string FileName { get; }
    public int Scope { get; }
    public bool ExcludeAtEnd { get; }
    public bool ExcludeInvalidKey { get; }

    public IrDispatchGlobalUse(string fileName, int scope, bool excludeAtEnd, bool excludeInvalidKey)
    {
        FileName = fileName;
        Scope = scope;
        ExcludeAtEnd = excludeAtEnd;
        ExcludeInvalidKey = excludeInvalidKey;
    }
}

/// <summary>
/// Check a file's read status after a READ. Sets result bool. By default this is the AT END
/// CONDITION (status "10" only) driving an AT END / NOT AT END branch. When
/// <see cref="TreatErrorsAsEnd"/> is set, it is a loop-exhaustion check (EOF OR any terminal
/// unreadable status) used by compiler-generated read loops so they terminate on error too.
/// </summary>
public sealed class IrCheckFileAtEnd : IrInstruction
{
    public string FileName { get; }
    public bool TreatErrorsAsEnd { get; }

    public IrCheckFileAtEnd(string fileName, IrValue result, bool treatErrorsAsEnd = false)
    {
        FileName = fileName;
        Result = result;
        TreatErrorsAsEnd = treatErrorsAsEnd;
    }
}

/// <summary>Set Result to whether the most recent WRITE to a LINAGE file raised the end-of-page
/// condition (ISO §14.9.51 GR26) — used to branch on the AT END-OF-PAGE / NOT AT END-OF-PAGE phrase.</summary>
public sealed class IrCheckEndOfPage : IrInstruction
{
    public string FileName { get; }

    public IrCheckEndOfPage(string fileName, IrValue result)
    {
        FileName = fileName;
        Result = result;
    }
}

/// <summary>Evaluate the LINAGE clause's page parameters at OPEN OUTPUT and apply them to the file's
/// runtime linage state (ISO §13.18.34 GR6b: data-name values are read at OPEN OUTPUT; GR7d: the
/// LINAGE-COUNTER is reset to one). Each phrase is either a data-name (a non-null IrLocation, decoded to
/// an integer at runtime) or an integer literal (the *Const fallback when the location is null).</summary>
public sealed class IrInitLinage : IrInstruction
{
    public string FileName { get; }
    public IrLocation? BodyLoc { get; }
    public int BodyConst { get; }
    public IrLocation? FootingLoc { get; }
    public int FootingConst { get; }
    public IrLocation? TopLoc { get; }
    public int TopConst { get; }
    public IrLocation? BottomLoc { get; }
    public int BottomConst { get; }

    public IrInitLinage(string fileName,
        IrLocation? bodyLoc, int bodyConst, IrLocation? footingLoc, int footingConst,
        IrLocation? topLoc, int topConst, IrLocation? bottomLoc, int bottomConst)
    {
        FileName = fileName;
        BodyLoc = bodyLoc; BodyConst = bodyConst;
        FootingLoc = footingLoc; FootingConst = footingConst;
        TopLoc = topLoc; TopConst = topConst;
        BottomLoc = bottomLoc; BottomConst = bottomConst;
    }
}

/// <summary>
/// Store the most recent file status code into a FILE STATUS variable.
/// </summary>
public sealed class IrStoreFileStatus : IrInstruction
{
    public string CobolFileName { get; }
    public IrLocation StatusVariable { get; }

    public IrStoreFileStatus(string cobolFileName, IrLocation statusVariable)
    {
        CobolFileName = cobolFileName;
        StatusVariable = statusVariable;
    }
}

// ── Sort/Merge ──

/// <summary>
/// Initialize a sort file: SortRuntime.InitSortFile(fileName, recordLength).
/// </summary>
public sealed class IrSortInit : IrInstruction
{
    public string FileName { get; }
    public int RecordLength { get; }

    public IrSortInit(string fileName, int recordLength)
    {
        FileName = fileName;
        RecordLength = recordLength;
    }
}

/// <summary>
/// Release a record to the sort file: SortRuntime.ReleaseRecord(fileName, area, offset, length).
/// </summary>
public sealed class IrSortRelease : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrSortRelease(string fileName, IrLocation record)
    {
        FileName = fileName;
        Record = record;
    }
}

/// <summary>
/// Release a VARIABLE-length record to the sort file: SortRuntime.ReleaseRecord(fileName, area, offset,
/// length) where the byte count is the actual length of the record just read from <see cref="LengthFileName"/>
/// (FileRuntime.GetLastRecordLength), not the record's declared max size — so each record keeps its own
/// length through the sort (a SORT … USING over a variable-length file).
/// </summary>
public sealed class IrSortReleaseVariable : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    public string LengthFileName { get; }

    public IrSortReleaseVariable(string fileName, IrLocation record, string lengthFileName)
    {
        FileName = fileName;
        Record = record;
        LengthFileName = lengthFileName;
    }
}

/// <summary>
/// Explicit RELEASE of a VARIABLE-length record to the sort file (Format-1 SORT with an INPUT PROCEDURE):
/// SortRuntime.ReleaseRecord(fileName, area, offset, length) where the byte count is read at runtime from the
/// SD's RECORD VARYING DEPENDING ON data item (StorageHelpers.ReadFieldAsInt of <see cref="LengthLocation"/>),
/// not the record's declared max — so each released record keeps its own length through the sort. ISO §13.18.43.
/// </summary>
public sealed class IrSortReleaseFromDepending : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    public IrLocation LengthLocation { get; }

    public IrSortReleaseFromDepending(string fileName, IrLocation record, IrLocation lengthLocation)
    {
        FileName = fileName;
        Record = record;
        LengthLocation = lengthLocation;
    }
}

/// <summary>
/// After an explicit RETURN of a VARIABLE-length record (Format-1 SORT with an OUTPUT PROCEDURE), store the
/// returned record's actual length (SortRuntime.GetLastReturnedLength) into the SD's RECORD VARYING DEPENDING
/// ON data item via StorageHelpers.MoveIntToField — so the program sees each record at its own length
/// (ISO §13.18.43 GR15). Mirrors <see cref="IrStoreRecordLength"/> for the READ path.
/// </summary>
public sealed class IrSortReturnStoreLength : IrInstruction
{
    public string FileName { get; }
    public IrLocation LengthVariable { get; }

    public IrSortReturnStoreLength(string fileName, IrLocation lengthVariable)
    {
        FileName = fileName;
        LengthVariable = lengthVariable;
    }
}

/// <summary>
/// Write a VARIABLE-length record returned from the sort to a GIVING output file:
/// StorageHelpers.WriteRecordVariableToFile(outputFileName, area, offset, length) where the byte count is the
/// actual length of the record produced by the most recent RETURN (SortRuntime.GetLastReturnedLength of
/// <see cref="SortFileName"/>) — so a variable-length SORT … GIVING re-emits each record at its own length.
/// </summary>
public sealed class IrSortGivingWriteVariable : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }
    public string SortFileName { get; }

    public IrSortGivingWriteVariable(string fileName, IrLocation record, string sortFileName)
    {
        FileName = fileName;
        Record = record;
        SortFileName = sortFileName;
    }
}

/// <summary>
/// Rewind the sort/merge return cursor to the first sorted record (SortRuntime.RewindReturn). Emitted
/// before each GIVING file's write loop so a multi-file SORT/MERGE … GIVING writes the full result to each
/// output file (ISO §14.9.24/§14.9.45).
/// </summary>
public sealed class IrSortRewind : IrInstruction
{
    public string FileName { get; }
    public IrSortRewind(string fileName) => FileName = fileName;
}

/// <summary>
/// Sort the collected records: SortRuntime.SortRecords(fileName, keysSpec).
/// keysSpec is "offset,length,asc;..." encoded string.
/// </summary>
public sealed class IrSortSort : IrInstruction
{
    public string FileName { get; }
    public string KeysSpec { get; }
    /// <summary>Collating table (256-byte code→weight) for alphanumeric keys; null = native byte order.</summary>
    public byte[]? CollatingSequence { get; }

    public IrSortSort(string fileName, string keysSpec, byte[]? collatingSequence)
    {
        FileName = fileName;
        KeysSpec = keysSpec;
        CollatingSequence = collatingSequence;
    }
}

/// <summary>
/// Return the next sorted record: SortRuntime.ReturnRecord(fileName, area, offset, length) → bool.
/// Result is true if a record was returned, false if at end.
/// </summary>
public sealed class IrSortReturn : IrInstruction
{
    public string FileName { get; }
    public IrLocation Record { get; }

    public IrSortReturn(string fileName, IrLocation record, IrValue result)
    {
        FileName = fileName;
        Record = record;
        Result = result;
    }
}

/// <summary>
/// Close/clean up a sort file: SortRuntime.CloseSortFile(fileName).
/// </summary>
public sealed class IrSortClose : IrInstruction
{
    public string FileName { get; }

    public IrSortClose(string fileName)
    {
        FileName = fileName;
    }
}

/// <summary>
/// In-place table sort (SORT Format 2): SortRuntime.SortTable(storageArea, offset, entrySize, count, keysSpec).
/// </summary>
public sealed class IrTableSort : IrInstruction
{
    public IrLocation TableLocation { get; }
    public int EntrySize { get; }
    public int EntryCount { get; }
    public string KeysSpec { get; }
    /// <summary>Collating table (256-byte code→weight) for alphanumeric keys; null = native byte order.</summary>
    public byte[]? CollatingSequence { get; }

    public IrTableSort(IrLocation tableLocation, int entrySize, int entryCount, string keysSpec,
        byte[]? collatingSequence)
    {
        TableLocation = tableLocation;
        EntrySize = entrySize;
        EntryCount = entryCount;
        KeysSpec = keysSpec;
        CollatingSequence = collatingSequence;
    }
}

/// <summary>
/// Merge records from multiple input files: SortRuntime.MergeRecords(mergeFile, inputFiles, keysSpec).
/// </summary>
public sealed class IrSortMerge : IrInstruction
{
    public string MergeFileName { get; }
    public string InputFileNames { get; } // semicolon-delimited
    public string KeysSpec { get; }
    /// <summary>Collating table (256-byte code→weight) for alphanumeric keys; null = native byte order.</summary>
    public byte[]? CollatingSequence { get; }

    public IrSortMerge(string mergeFileName, string inputFileNames, string keysSpec,
        byte[]? collatingSequence)
    {
        MergeFileName = mergeFileName;
        InputFileNames = inputFileNames;
        KeysSpec = keysSpec;
        CollatingSequence = collatingSequence;
    }
}

// ── Location abstraction ──

/// <summary>
/// Base type for "where a value lives": either a compile-time-known static
/// location or a runtime-computed element within an OCCURS array.
/// All IR instructions that operate on data items use IrLocation instead of
/// raw StorageLocation, making subscript handling uniform.
/// </summary>
public abstract class IrLocation { }

/// <summary>
/// A compile-time-known storage location (non-subscripted, or constant-subscript
/// already folded to a fixed offset).
/// </summary>
public sealed class IrStaticLocation : IrLocation
{
    public CodeGen.StorageLocation Location { get; }

    public IrStaticLocation(CodeGen.StorageLocation location)
    {
        Location = location;
    }
}

/// <summary>
/// A typed-native field location (data-model migration, <c>docs/RECORD_STRUCT_STORAGE_DESIGN.md</c> S3): the
/// operand is a native .NET <see cref="string"/> field, not a byte window. Carries the emitted field's name
/// (resolved to a <c>FieldDefinition</c> via <c>EmissionContext.TypedFields</c>) and the COBOL character width.
/// Produced by <c>LocationResolver</c> only when <c>CompilationOptions.EnableTypedFields</c> is on and the
/// classifier marks the item typed; the byte path (<see cref="IrStaticLocation"/> etc.) is otherwise unchanged.
/// </summary>
public sealed class IrTypedFieldLocation : IrLocation
{
    public string FieldName { get; }
    public int Width { get; }
    /// <summary>The item's original (alphanumeric) descriptor — so <c>GetPic()</c> works and the existing
    /// lowering routes a literal/field move through the right branch before the typed emit cell takes over.</summary>
    public Runtime.PicDescriptor Pic { get; }
    /// <summary>S3a (a standalone elementary item): null → <see cref="FieldName"/> is a flat static field. S3b
    /// (a flipped <c>01</c> group → a <c>record struct</c>): the name of the static struct-instance field, and
    /// <see cref="FieldName"/> is the member within it (accessed <c>ldsflda instance; ldfld/stfld member</c>).</summary>
    public string? InstanceName { get; }

    public IrTypedFieldLocation(string fieldName, int width, Runtime.PicDescriptor pic, string? instanceName = null)
    {
        FieldName = fieldName;
        Width = width;
        Pic = pic;
        InstanceName = instanceName;
    }

    /// <summary>S4: a typed NUMERIC field is represented as a .NET <c>decimal</c> (vs <c>long</c>) exactly when it
    /// is signed or scaled (a fraction or P-scale) — the complement of the unsigned-integer slice that flips to
    /// <c>long</c>. This single predicate keeps the Binder's field-type choice and every emit cell in agreement.</summary>
    public static bool IsDecimalRepresented(Runtime.PicDescriptor pic) =>
        pic.IsSigned || pic.FractionDigits != 0 || pic.LeadingScaleDigits != 0 || pic.TrailingScaleDigits != 0;

    /// <summary>True when this is a numeric field stored as a .NET <c>decimal</c> (signed/scaled); false for the
    /// unsigned-integer <c>long</c> slice and for character fields.</summary>
    public bool IsDecimalNumeric => Pic.Category == Runtime.CobolCategory.Numeric && IsDecimalRepresented(Pic);
}

/// <summary>
/// A reference to an element within an OCCURS array (1D, 2D, or 3D).
/// The effective offset is computed at runtime using the general formula:
///   offset = base + sum_i((subscript_i - 1) * multiplier_i)
/// where multiplier_i is the product of all inner dimension sizes * element size.
/// Subscripts are carried as IrExpressions — the emitter evaluates each one
/// via EmitIrExpression (handles literals, loads, arithmetic, and function calls).
/// </summary>
public sealed class IrElementRef : IrLocation
{
    public CodeGen.StorageLocation BaseLocation { get; }
    public IReadOnlyList<IrExpression> Subscripts { get; }
    public IReadOnlyList<int> Multipliers { get; }
    public int ElementSize { get; }
    public Runtime.PicDescriptor ElementPic { get; }

    public IrElementRef(CodeGen.StorageLocation baseLocation,
        IReadOnlyList<IrExpression> subscripts,
        IReadOnlyList<int> multipliers,
        int elementSize, Runtime.PicDescriptor elementPic)
    {
        BaseLocation = baseLocation;
        Subscripts = subscripts;
        Multipliers = multipliers;
        ElementSize = elementSize;
        ElementPic = elementPic;
    }
}

/// <summary>
/// A reference modification: base location + runtime start:length substring.
/// Composes with IrStaticLocation or IrElementRef as the base.
/// The effective storage is: (base_area, base_offset + start - 1, length).
/// </summary>
public sealed class IrRefModLocation : IrLocation
{
    public IrLocation Base { get; }
    public IrExpression Start { get; }
    public IrExpression? Length { get; }
    public int BaseFieldLength { get; }

    public IrRefModLocation(IrLocation @base, IrExpression start,
        IrExpression? length, int baseFieldLength)
    {
        Base = @base;
        Start = start;
        Length = length;
        BaseFieldLength = baseFieldLength;
    }
}

/// <summary>
/// A whole-item reference to a group (or table) whose length varies at runtime
/// because it contains a trailing OCCURS DEPENDING ON table. The effective byte
/// length is computed at runtime as:
///   length = maxLength - (maxOccurs - dependingOnValue) * elementSize
/// (equivalently fixedPart + dependingOnValue * elementSize for a trailing ODO),
/// where maxLength is the compile-time layout size. Used wherever such a group is
/// an operand of a group MOVE, comparison, INSPECT, STRING, or UNSTRING so the
/// inactive trailing occurrences are excluded (ISO 1989:1985 13.18.36.3).
/// </summary>
public sealed class IrOdoGroupLocation : IrLocation
{
    public CodeGen.StorageLocation Base { get; }   // compile-time max-length location
    public int MaxOccurs { get; }
    public int ElementSize { get; }
    public IrLocation DependingOnLocation { get; }  // numeric field holding the active count

    public IrOdoGroupLocation(CodeGen.StorageLocation @base, int maxOccurs, int elementSize,
        IrLocation dependingOnLocation)
    {
        Base = @base;
        MaxOccurs = maxOccurs;
        ElementSize = elementSize;
        DependingOnLocation = dependingOnLocation;
    }
}

/// <summary>
/// Wraps an IrLocation with a cache key so that the CIL emitter computes
/// (area, offset, length) once into locals and reuses them on subsequent
/// encounters with the same key.  Used by MOVE when the source has subscripts
/// and there are multiple targets — the spec says the source is evaluated ONCE.
/// </summary>
public sealed class IrCachedLocation : IrLocation
{
    public IrLocation Inner { get; }
    public int CacheKey { get; }

    public IrCachedLocation(IrLocation inner, int cacheKey)
    {
        Inner = inner;
        CacheKey = cacheKey;
    }
}

// ── GO TO DEPENDING ──

/// <summary>
/// GO TO para1 para2 ... DEPENDING ON selector.
/// Evaluates selector as integer N (1-based). If 1 ≤ N ≤ targets.Count,
/// returns targets[N-1] as the next PC. Otherwise falls through.
/// </summary>
public sealed class IrGoToDepending : IrInstruction
{
    public IrLocation Selector { get; }
    public IReadOnlyList<int> TargetParagraphIndices { get; }

    public IrGoToDepending(IrLocation selector, IReadOnlyList<int> targetParagraphIndices)
    {
        Selector = selector;
        TargetParagraphIndices = targetParagraphIndices;
    }
}

// ── ACCEPT ──

public sealed class IrAccept : IrInstruction
{
    public IrLocation Target { get; }
    public AcceptSourceKind Source { get; }

    public IrAccept(IrLocation target, AcceptSourceKind source)
    {
        Target = target;
        Source = source;
    }
}

// ── INSPECT ──

/// <summary>
/// IR-level INSPECT pattern: either a compile-time literal string or a pre-resolved
/// runtime location. The Binder resolves BoundInspectPatternValue → IrInspectPatternValue
/// during lowering, converting data-ref patterns to IrLocations.
/// </summary>
public sealed class IrInspectPatternValue
{
    public string? Literal { get; }
    public IrLocation? Location { get; }

    public bool IsLiteral => Literal != null;
    public bool IsLocation => Location != null;

    private IrInspectPatternValue(string? literal, IrLocation? location)
    {
        Literal = literal;
        Location = location;
    }

    public static IrInspectPatternValue FromLiteral(string value) => new(value, null);
    public static IrInspectPatternValue FromLocation(IrLocation loc) => new(null, loc);
}

/// <summary>One TALLYING operand: counter, kind, pattern, and BEFORE/AFTER region delimiters.</summary>
public sealed class IrInspectTallyOp
{
    public IrLocation Counter { get; }
    public InspectTallyKind Kind { get; }
    public IrInspectPatternValue? Pattern { get; }       // null for CHARACTERS
    public IrInspectPatternValue? BeforePattern { get; }
    public IrInspectPatternValue? AfterPattern { get; }

    public IrInspectTallyOp(IrLocation counter, InspectTallyKind kind, IrInspectPatternValue? pattern,
        IrInspectPatternValue? beforePattern, IrInspectPatternValue? afterPattern)
    {
        Counter = counter; Kind = kind; Pattern = pattern;
        BeforePattern = beforePattern; AfterPattern = afterPattern;
    }
}

/// <summary>
/// All TALLYING operands of a single INSPECT statement, executed as one comparison cycle
/// (ISO 6.17.3 GR 8). Operands are tried in source order at each position; first match wins.
/// </summary>
public sealed class IrInspectTallying : IrInstruction
{
    public IrLocation Target { get; }
    public IReadOnlyList<IrInspectTallyOp> Ops { get; }
    /// <summary>BACKWARD phrase (ISO §14.9.21): tally right-to-left.</summary>
    public bool Backward { get; }

    public IrInspectTallying(IrLocation target, IReadOnlyList<IrInspectTallyOp> ops, bool backward = false)
    {
        Target = target; Ops = ops; Backward = backward;
    }
}

/// <summary>One REPLACING operand: kind, pattern, replacement, and BEFORE/AFTER region delimiters.</summary>
public sealed class IrInspectReplaceOp
{
    public InspectReplaceKind Kind { get; }
    public IrInspectPatternValue? Pattern { get; }       // null for CHARACTERS
    public IrInspectPatternValue Replacement { get; }
    public IrInspectPatternValue? BeforePattern { get; }
    public IrInspectPatternValue? AfterPattern { get; }

    public IrInspectReplaceOp(InspectReplaceKind kind, IrInspectPatternValue? pattern,
        IrInspectPatternValue replacement,
        IrInspectPatternValue? beforePattern, IrInspectPatternValue? afterPattern)
    {
        Kind = kind; Pattern = pattern; Replacement = replacement;
        BeforePattern = beforePattern; AfterPattern = afterPattern;
    }
}

/// <summary>
/// All REPLACING operands of a single INSPECT statement, executed as one comparison cycle
/// (ISO 6.17.3 GR 8). Operands are tried in source order at each position; first match wins.
/// </summary>
public sealed class IrInspectReplacing : IrInstruction
{
    public IrLocation Target { get; }
    public IReadOnlyList<IrInspectReplaceOp> Ops { get; }
    /// <summary>BACKWARD phrase (ISO §14.9.21): replace right-to-left.</summary>
    public bool Backward { get; }

    public IrInspectReplacing(IrLocation target, IReadOnlyList<IrInspectReplaceOp> ops, bool backward = false)
    {
        Target = target; Ops = ops; Backward = backward;
    }
}

public sealed class IrInspectConvert : IrInstruction
{
    public IrLocation Target { get; }
    public IrInspectPatternValue FromSet { get; }
    public IrInspectPatternValue ToSet { get; }
    public IrInspectPatternValue? BeforePattern { get; }
    public bool BeforeInitial { get; }
    public IrInspectPatternValue? AfterPattern { get; }
    public bool AfterInitial { get; }
    /// <summary>BACKWARD phrase (ISO §14.9.21): convert scanning right-to-left.</summary>
    public bool Backward { get; }

    public IrInspectConvert(IrLocation target,
        IrInspectPatternValue fromSet, IrInspectPatternValue toSet,
        IrInspectPatternValue? beforePattern, bool beforeInitial,
        IrInspectPatternValue? afterPattern, bool afterInitial,
        bool backward = false)
    {
        Target = target; FromSet = fromSet; ToSet = toSet;
        BeforePattern = beforePattern; BeforeInitial = beforeInitial;
        AfterPattern = afterPattern; AfterInitial = afterInitial;
        Backward = backward;
    }
}

// ── PIC-aware arithmetic ──

public sealed class IrPicMultiply : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicMultiply(IrLocation left, IrLocation right,
        IrLocation dest, int rounding = 0)
    {
        Left = left; Right = right; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicMultiplyLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Other { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicMultiplyLiteral(decimal value, IrLocation other,
        IrLocation dest, int rounding = 0)
    {
        Value = value; Other = other; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicAdd : IrInstruction
{
    public IrLocation Source { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicAdd(IrLocation src, IrLocation dest, int rounding = 0)
    {
        Source = src; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicAddLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicAddLiteral(IrLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

public sealed class IrPicSubtract : IrInstruction
{
    public IrLocation Source { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicSubtract(IrLocation src, IrLocation dest, int rounding = 0)
    {
        Source = src; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicSubtractLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicSubtractLiteral(IrLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

// ── Accumulator pattern for multi-operand ADD/SUBTRACT ──
// COBOL spec: "All operands preceding TO/FROM are summed, then the sum is applied to each target."

/// <summary>
/// Initialize a decimal accumulator local to zero.
/// </summary>
public sealed class IrInitAccumulator : IrInstruction
{
    public IrInitAccumulator(IrValue result)
    {
        Result = result;
    }
}

/// <summary>
/// Decode a field to decimal and add it to the accumulator.
/// </summary>
public sealed class IrAccumulateField : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Source { get; }

    public IrAccumulateField(IrValue accumulator, IrLocation source)
    {
        Accumulator = accumulator;
        Source = source;
    }
}

/// <summary>
/// Add a literal decimal to the accumulator.
/// </summary>
public sealed class IrAccumulateLiteral : IrInstruction
{
    public IrValue Accumulator { get; }
    public decimal Value { get; }

    public IrAccumulateLiteral(IrValue accumulator, decimal value)
    {
        Accumulator = accumulator;
        Value = value;
    }
}

/// <summary>
/// Evaluate an IrExpression and store the decimal result into an accumulator.
/// Used for DIVIDE BY GIVING with multiple targets: evaluate quotient once,
/// then store from the accumulator to each target.
/// </summary>
public sealed class IrComputeIntoAccumulator : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrExpression Expression { get; }

    public IrComputeIntoAccumulator(IrValue accumulator, IrExpression expression)
    {
        Accumulator = accumulator;
        Expression = expression;
    }
}

/// <summary>
/// target = target + accumulator, with rounding and overflow detection.
/// </summary>
public sealed class IrAddAccumulatedToTarget : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrAddAccumulatedToTarget(IrValue accumulator, IrLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

/// <summary>
/// target = accumulator (GIVING form: store sum directly, don't add to current value).
/// </summary>
public sealed class IrMoveAccumulatedToTarget : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrMoveAccumulatedToTarget(IrValue accumulator, IrLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

/// <summary>
/// target = target - accumulator, with rounding and overflow detection.
/// </summary>
public sealed class IrSubtractAccumulatedFromTarget : IrInstruction
{
    public IrValue Accumulator { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrSubtractAccumulatedFromTarget(IrValue accumulator, IrLocation dest, int rounding = 0)
    {
        Accumulator = accumulator;
        Destination = dest;
        Rounding = rounding;
    }
}

public sealed class IrPicDivide : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicDivide(IrLocation left, IrLocation right,
        IrLocation dest, int rounding = 0)
    {
        Left = left; Right = right; Destination = dest; Rounding = rounding;
    }
}

public sealed class IrPicDivideLiteral : IrInstruction
{
    public decimal Value { get; }
    public IrLocation Other { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrPicDivideLiteral(decimal value, IrLocation other,
        IrLocation dest, int rounding = 0)
    {
        Value = value; Other = other; Destination = dest; Rounding = rounding;
    }
}

/// <summary>
/// COMPUTE: evaluate an IrExpression tree and store the decimal result
/// into a target field with optional rounding and overflow detection.
/// </summary>
public sealed class IrComputeStore : IrInstruction
{
    public IrExpression Expression { get; }
    public IrLocation Destination { get; }
    public int Rounding { get; }

    public IrComputeStore(IrExpression expression, IrLocation dest, int rounding = 0)
    {
        Expression = expression;
        Destination = dest;
        Rounding = rounding;
    }
}

/// <summary>
/// COBOL DIVIDE REMAINDER: R = dividend - truncatedQuotient × divisor.
/// Uses the quotient accumulator value truncated to the GIVING field's precision.
/// </summary>
public sealed class IrCobolRemainder : IrInstruction
{
    public IrExpression Dividend { get; }
    public IrExpression Divisor { get; }
    public IrValue QuotientAccumulator { get; }
    public int GivingFractionDigits { get; }
    public IrLocation Destination { get; }

    public IrCobolRemainder(
        IrExpression dividend, IrExpression divisor,
        IrValue quotientAccumulator, int givingFractionDigits, IrLocation destination)
    {
        Dividend = dividend;
        Divisor = divisor;
        QuotientAccumulator = quotientAccumulator;
        GivingFractionDigits = givingFractionDigits;
        Destination = destination;
    }
}

/// <summary>
/// Class condition: IS NUMERIC, IS ALPHABETIC, etc.
/// Calls PicRuntime.IsNumericClass / IsAlphabeticClass / etc.
/// </summary>
public sealed class IrClassCondition : IrInstruction
{
    public IrLocation Subject { get; }
    public int ClassKind { get; }  // ClassConditionKind enum value

    public IrClassCondition(IrLocation subject, int classKind, IrValue result)
    {
        Subject = subject;
        ClassKind = classKind;
        Result = result;
    }
}

/// <summary>
/// User-defined CLASS condition from SPECIAL-NAMES.
/// Calls PicRuntime.IsInUserClass with the pre-computed valid byte set.
/// </summary>
public sealed class IrUserClassCondition : IrInstruction
{
    public IrLocation Subject { get; }
    public byte[] ValidBytes { get; }

    public IrUserClassCondition(IrLocation subject, byte[] validBytes, IrValue result)
    {
        Subject = subject;
        ValidBytes = validBytes;
        Result = result;
    }
}

public sealed class IrPicCompare : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public int OperatorKind { get; }

    public IrPicCompare(IrLocation left, IrLocation right,
        IrValue result, int operatorKind)
    {
        Left = left; Right = right; Result = result; OperatorKind = operatorKind;
    }
}

public sealed class IrPicCompareLiteral : IrInstruction
{
    public IrLocation Left { get; }
    public decimal Value { get; }
    public int OperatorKind { get; }

    public IrPicCompareLiteral(IrLocation left, decimal value,
        IrValue result, int operatorKind)
    {
        Left = left; Value = value; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Compare a PIC field to a computed decimal value (from arithmetic expression).
/// The accumulator holds the pre-evaluated result of the arithmetic expression.
/// </summary>
public sealed class IrPicCompareAccumulator : IrInstruction
{
    public IrLocation Left { get; }
    public IrValue Accumulator { get; }
    public int OperatorKind { get; }

    public IrPicCompareAccumulator(IrLocation left, IrValue accumulator,
        IrValue result, int operatorKind)
    {
        Left = left; Accumulator = accumulator; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Compare two decimal accumulators. Result is bool.
/// Used for ArithmeticExpression vs ArithmeticExpression comparisons.
/// </summary>
public sealed class IrDecimalCompare : IrInstruction
{
    public IrValue Left { get; }
    public IrValue Right { get; }
    public int OperatorKind { get; }

    public IrDecimalCompare(IrValue left, IrValue right, IrValue result, int operatorKind)
    {
        Left = left; Right = right; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Compare a decimal accumulator to a literal. Result is bool.
/// Used for ArithmeticExpression vs NumericLiteral comparisons.
/// </summary>
public sealed class IrDecimalCompareLiteral : IrInstruction
{
    public IrValue Accumulator { get; }
    public decimal LiteralValue { get; }
    public int OperatorKind { get; }

    public IrDecimalCompareLiteral(IrValue accumulator, decimal literalValue, IrValue result, int operatorKind)
    {
        Accumulator = accumulator; LiteralValue = literalValue; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Compare an alphanumeric field to a string literal. Result is bool.
/// </summary>
public sealed class IrStringCompareLiteral : IrInstruction
{
    public IrLocation Left { get; }
    public string Value { get; }
    public int OperatorKind { get; }

    public IrStringCompareLiteral(IrLocation left, string value,
        IrValue result, int operatorKind)
    {
        Left = left; Value = value; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Alphanumeric field-to-field comparison. Uses StorageHelpers.CompareFieldToField.
/// </summary>
public sealed class IrStringCompare : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public int OperatorKind { get; }

    public IrStringCompare(IrLocation left, IrLocation right,
        IrValue result, int operatorKind)
    {
        Left = left; Right = right; Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Alphanumeric field-to-field comparison with a custom collating sequence.
/// Uses PicRuntime.CompareAlphanumericWithSequence.
/// </summary>
public sealed class IrStringCompareWithSequence : IrInstruction
{
    public IrLocation Left { get; }
    public IrLocation Right { get; }
    public byte[] CollatingSequence { get; }
    public int OperatorKind { get; }

    public IrStringCompareWithSequence(IrLocation left, IrLocation right,
        byte[] collatingSequence, IrValue result, int operatorKind)
    {
        Left = left; Right = right; CollatingSequence = collatingSequence;
        Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Alphanumeric field-to-string comparison with a custom collating sequence.
/// Uses PicRuntime.CompareAlphanumericWithSequence after encoding the string.
/// </summary>
public sealed class IrStringCompareLiteralWithSequence : IrInstruction
{
    public IrLocation Left { get; }
    public string Value { get; }
    public byte[] CollatingSequence { get; }
    public int OperatorKind { get; }

    public IrStringCompareLiteralWithSequence(IrLocation left, string value,
        byte[] collatingSequence, IrValue result, int operatorKind)
    {
        Left = left; Value = value; CollatingSequence = collatingSequence;
        Result = result; OperatorKind = operatorKind;
    }
}

/// <summary>
/// Compare a string-VALUED expression (e.g. an alphanumeric intrinsic-function result such as
/// FUNCTION UPPER-CASE(x)) against another operand given as a field location or a literal.
/// The left expression is evaluated to a System.String; the right is read as a string; the two
/// are compared with StorageHelpers.CompareStringValues (trailing-space-insensitive).
/// </summary>
public sealed class IrStringExprCompare : IrInstruction
{
    public IrExpression LeftStringExpr { get; }
    public IrLocation? RightLocation { get; }
    public string? RightLiteral { get; }
    public int OperatorKind { get; }

    public IrStringExprCompare(IrExpression leftStringExpr, IrLocation? rightLocation,
        string? rightLiteral, IrValue result, int operatorKind)
    {
        LeftStringExpr = leftStringExpr;
        RightLocation = rightLocation;
        RightLiteral = rightLiteral;
        Result = result;
        OperatorKind = operatorKind;
    }
}

/// <summary>
/// One sending item in a STRING statement.
/// </summary>
public sealed class IrStringSending
{
    /// <summary>Literal value (non-null for literal sendings).</summary>
    public string? LiteralValue { get; }
    /// <summary>Field location (non-null for field sendings).</summary>
    public IrLocation? SourceLocation { get; }
    public string? Delimiter { get; }
    public IrLocation? DelimiterLocation { get; }
    public bool DelimitedBySize { get; }

    public IrStringSending(string? literalValue, IrLocation? sourceLocation,
        string? delimiter, IrLocation? delimiterLocation, bool delimitedBySize)
    {
        LiteralValue = literalValue;
        SourceLocation = sourceLocation;
        Delimiter = delimiter;
        DelimiterLocation = delimiterLocation;
        DelimitedBySize = delimitedBySize;
    }
}

/// <summary>
/// STRING statement: concatenate multiple sending items into a destination.
/// The emitter manages a single pointer local, initializes from PointerLocation
/// (or 1 if null), calls StringConcatLiteral/StringConcat per sending, and
/// writes the pointer back to PointerLocation (if non-null).
/// </summary>
public sealed class IrStringStatement : IrInstruction
{
    public IrLocation Destination { get; }
    public IReadOnlyList<IrStringSending> Sendings { get; }
    /// <summary>Null if no WITH POINTER clause.</summary>
    public IrLocation? PointerLocation { get; }

    public IrStringStatement(IrLocation dest, IReadOnlyList<IrStringSending> sendings,
        IrLocation? pointerLocation, IrValue overflowResult)
    {
        Destination = dest;
        Sendings = sendings;
        PointerLocation = pointerLocation;
        Result = overflowResult;
    }
}

/// <summary>
/// One INTO target in an UNSTRING statement.
/// </summary>
public sealed class IrUnstringInto
{
    public IrLocation Target { get; }
    public IrLocation? CountIn { get; }
    public IrLocation? DelimiterIn { get; }

    public IrUnstringInto(IrLocation target, IrLocation? countIn, IrLocation? delimiterIn)
    {
        Target = target;
        CountIn = countIn;
        DelimiterIn = delimiterIn;
    }
}

/// <summary>
/// UNSTRING statement: split a source string into multiple destination fields.
/// The emitter manages a shared pointer local, calls UnstringExtract per INTO,
/// handles COUNT IN / DELIMITER IN write-back, and writes pointer/tallying back.
/// </summary>
/// <summary>A resolved UNSTRING delimiter: either a literal string or a field location, with ALL flag.</summary>
public sealed record IrUnstringDelimiter(string? LiteralValue, IrLocation? Location, bool IsAll);

public sealed class IrUnstringStatement : IrInstruction
{
    public IrLocation Source { get; }
    /// <summary>All OR-separated delimiters. Empty list means no delimiter phrase.</summary>
    public IReadOnlyList<IrUnstringDelimiter> Delimiters { get; }
    public IReadOnlyList<IrUnstringInto> Intos { get; }
    public IrLocation? PointerLocation { get; }
    public IrLocation? TallyingLocation { get; }

    public IrUnstringStatement(IrLocation source, IReadOnlyList<IrUnstringDelimiter> delimiters,
        IReadOnlyList<IrUnstringInto> intos, IrLocation? pointerLocation, IrLocation? tallyingLocation,
        IrValue overflowResult)
    {
        Source = source;
        Delimiters = delimiters;
        Intos = intos;
        PointerLocation = pointerLocation;
        TallyingLocation = tallyingLocation;
        Result = overflowResult;
    }
}

public sealed class IrPicMoveLiteralNumeric : IrInstruction
{
    public IrLocation Destination { get; }
    public decimal Value { get; }
    public int Rounding { get; }

    public IrPicMoveLiteralNumeric(IrLocation dest, decimal value, int rounding = 0)
    {
        Destination = dest; Value = value; Rounding = rounding;
    }
}

// ── PIC-aware data movement ──

/// <summary>
/// PIC-aware field-to-field MOVE. Canonical primitive for all identifier→identifier
/// moves: regular MOVE, MOVE CORRESPONDING pairs, and SET TRUE/FALSE.
/// Carries resolved PIC descriptors — the emitter dispatches to the appropriate
/// PicRuntime helper based on source/destination categories.
/// </summary>
public sealed class IrMoveFieldToField : IrInstruction
{
    public IrLocation Source { get; }
    public IrLocation Destination { get; }
    public Runtime.PicDescriptor SourcePic { get; }
    public Runtime.PicDescriptor DestinationPic { get; }
    public bool IsRounded { get; }

    public IrMoveFieldToField(
        IrLocation source, IrLocation destination,
        Runtime.PicDescriptor sourcePic, Runtime.PicDescriptor destinationPic,
        bool isRounded = false)
    {
        Source = source;
        Destination = destination;
        SourcePic = sourcePic;
        DestinationPic = destinationPic;
        IsRounded = isRounded;
    }
}

// ── DISPLAY ──

/// <summary>
/// Represents a single DISPLAY operand: either a string literal or a field reference.
/// </summary>
public abstract class DisplayOperand { }

public sealed class DisplayLiteralOperand : DisplayOperand
{
    public string Value { get; }
    public DisplayLiteralOperand(string value) => Value = value;
}

public sealed class DisplayFieldOperand : DisplayOperand
{
    public IrLocation Location { get; }
    public DisplayFieldOperand(IrLocation location) => Location = location;
}

/// <summary>
/// DISPLAY statement: outputs concatenated operands (literals + field values) to console.
/// </summary>
public sealed class IrPicDisplay : IrInstruction
{
    public IReadOnlyList<DisplayOperand> Operands { get; }

    /// <summary>True for DISPLAY … WITH NO ADVANCING — emit Console.Write (no trailing newline).</summary>
    public bool NoAdvancing { get; }

    public IrPicDisplay(IReadOnlyList<DisplayOperand> operands, bool noAdvancing = false)
    {
        Operands = operands;
        NoAdvancing = noAdvancing;
    }
}

// ── I/O and runtime calls ──

public sealed class IrRuntimeCall : IrInstruction
{
    public string MethodName { get; }
    public IReadOnlyList<IrValue> Arguments { get; }

    public IrRuntimeCall(IrValue? result, string methodName, IReadOnlyList<IrValue> args)
    {
        Result = result;
        MethodName = methodName;
        Arguments = args;
    }
}
