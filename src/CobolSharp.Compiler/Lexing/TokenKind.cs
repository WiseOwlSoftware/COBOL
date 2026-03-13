namespace CobolSharp.Compiler.Lexing;

/// <summary>
/// All token types recognized by the COBOL lexer.
/// Phase 1 includes the minimal set; additional keywords will be added in later phases.
/// </summary>
public enum TokenKind
{
    // Special
    EndOfFile,
    Bad,

    // Literals
    IntegerLiteral,
    DecimalLiteral,
    StringLiteral,

    // Identifiers (user-defined words)
    Identifier,

    // Punctuation / operators
    Period,              // .
    LeftParen,           // (
    RightParen,          // )
    Plus,                // +
    Minus,               // -
    Multiply,            // *
    Divide,              // /
    Power,               // **
    Equals,              // =
    LessThan,            // <
    GreaterThan,         // >
    LessThanOrEqual,     // <=
    GreaterThanOrEqual,  // >=
    Comma,               // ,
    Colon,               // :

    // ── Division / section / paragraph keywords ──
    IdentificationKeyword,   // IDENTIFICATION
    ProgramIdKeyword,        // PROGRAM-ID
    DivisionKeyword,         // DIVISION
    SectionKeyword,          // SECTION
    EnvironmentKeyword,      // ENVIRONMENT
    DataKeyword,             // DATA
    ProcedureKeyword,        // PROCEDURE
    WorkingStorageKeyword,   // WORKING-STORAGE

    // ── Data description keywords ──
    PicKeyword,              // PIC / PICTURE
    UsageKeyword,            // USAGE
    ValueKeyword,            // VALUE
    ValuesKeyword,           // VALUES
    DisplayUsageKeyword,     // DISPLAY (as USAGE)
    CompKeyword,             // COMP / COMPUTATIONAL / BINARY
    Comp3Keyword,            // COMP-3 / PACKED-DECIMAL
    FillerKeyword,           // FILLER
    RedefinesKeyword,        // REDEFINES
    OccursKeyword,           // OCCURS
    DependingKeyword,        // DEPENDING
    RenamesKeyword,          // RENAMES
    BlankKeyword,            // BLANK
    JustifiedKeyword,        // JUSTIFIED / JUST
    SynchronizedKeyword,     // SYNCHRONIZED / SYNC
    IndexKeyword,            // INDEX
    IndexedKeyword,          // INDEXED
    PointerKeyword,          // POINTER
    FunctionKeyword,         // FUNCTION
    FunctionPointerKeyword,  // FUNCTION-POINTER
    ProcedurePointerKeyword, // PROCEDURE-POINTER
    GlobalKeyword,           // GLOBAL
    ExternalKeyword,         // EXTERNAL
    WhenKeyword2,            // (placeholder, WHEN is already defined)
    AscendingKeyword,        // ASCENDING
    DescendingKeyword,       // DESCENDING
    KeyKeyword,              // KEY

    // ── File I/O keywords ──
    SelectKeyword,           // SELECT
    AssignKeyword,           // ASSIGN
    OrganizationKeyword,     // ORGANIZATION
    AccessKeyword,           // ACCESS
    ModeKeyword,             // MODE
    RecordKeyword,           // RECORD
    AlternateKeyword,        // ALTERNATE
    StatusKeyword,           // STATUS
    FileKeyword,             // FILE
    FdKeyword,               // FD
    SdKeyword,               // SD
    BlockKeyword,            // BLOCK
    ContainsKeyword,         // CONTAINS
    LabelKeyword,            // LABEL
    RecordsKeyword,          // RECORDS
    LinageKeyword,           // LINAGE
    InputKeyword,            // INPUT
    OutputKeyword,           // OUTPUT
    ExtendKeyword,           // EXTEND
    PageKeyword,             // PAGE
    AdvancingKeyword,        // ADVANCING
    DuplicatesKeyword,       // DUPLICATES
    WithKeyword,             // WITH
    LinkageKeyword,          // LINKAGE
    LineKeyword,             // LINE
    SequentialKeyword,       // SEQUENTIAL
    RandomKeyword,           // RANDOM
    DynamicKeyword,          // DYNAMIC
    RelativeKeyword,         // RELATIVE
    InvalidKeyword,          // INVALID
    EndReadKeyword,          // END-READ
    EndWriteKeyword,         // END-WRITE
    EndDeleteKeyword,        // END-DELETE
    EndStartKeyword,         // END-START
    AtKeyword,               // AT
    EndKeyword,              // END
    NextKeyword,             // NEXT
    I_OKeyword,              // I-O

    // ── Procedure division statements ──
    DisplayKeyword,          // DISPLAY
    StopKeyword,             // STOP
    RunKeyword,              // RUN
    MoveKeyword,             // MOVE
    ToKeyword,               // TO
    AddKeyword,              // ADD
    GivingKeyword,           // GIVING
    SubtractKeyword,         // SUBTRACT
    FromKeyword,             // FROM
    MultiplyKeyword,         // MULTIPLY
    ByKeyword,               // BY
    DivideKeyword,           // DIVIDE
    IntoKeyword,             // INTO
    RemainderKeyword,        // REMAINDER
    ComputeKeyword,          // COMPUTE
    IfKeyword,               // IF
    ElseKeyword,             // ELSE
    EndIfKeyword,            // END-IF
    PerformKeyword,          // PERFORM
    EndPerformKeyword,       // END-PERFORM
    UntilKeyword,            // UNTIL
    VaryingKeyword,          // VARYING
    TimesKeyword,            // TIMES
    ThruKeyword,             // THRU / THROUGH
    EvaluateKeyword,         // EVALUATE
    WhenKeyword,             // WHEN
    OtherKeyword,            // OTHER
    EndEvaluateKeyword,      // END-EVALUATE
    GoKeyword,               // GO
    NotKeyword,              // NOT
    AndKeyword,              // AND
    OrKeyword,               // OR
    TrueKeyword,             // TRUE
    FalseKeyword,            // FALSE
    SpaceKeyword,            // SPACE / SPACES
    SpacesKeyword,           // (alias, maps to SpaceKeyword)
    ZeroKeyword,             // ZERO / ZEROS / ZEROES
    HighValueKeyword,        // HIGH-VALUE / HIGH-VALUES
    LowValueKeyword,        // LOW-VALUE / LOW-VALUES
    QuoteKeyword,            // QUOTE / QUOTES
    AllKeyword,              // ALL

    // Level numbers (treated as tokens for simplicity)
    LevelNumber,

    // ── Additional keywords for later phases (reserved now for the lexer) ──
    AcceptKeyword,
    CallKeyword,
    CancelKeyword,
    ContinueKeyword,
    CopyKeyword,
    DeleteKeyword,
    ExitKeyword,
    InitializeKeyword,
    InspectKeyword,
    MergeKeyword,
    OpenKeyword,
    ReadKeyword,
    ReleaseKeyword,
    ReplaceKeyword,
    ReturnKeyword,
    RewriteKeyword,
    SearchKeyword,
    SetKeyword,
    SortKeyword,
    StartKeyword,
    StringKeyword,
    UnstringKeyword,
    WriteKeyword,
    CloseKeyword,
    AlterKeyword,

    // CALL/CANCEL keywords
    UsingKeyword,            // USING
    ReturningKeyword,        // RETURNING
    ReferenceKeyword,        // REFERENCE
    ContentKeyword,          // CONTENT

    // Arithmetic/conditional modifiers
    RoundedKeyword,          // ROUNDED
    SizeKeyword,             // SIZE
    ErrorKeyword,            // ERROR
    OnKeyword,               // ON
    CorrespondingKeyword,    // CORRESPONDING / CORR

    // IS keyword (used in conditions, VALUE IS, etc.)
    IsKeyword,

    // THAN keyword (GREATER THAN, LESS THAN)
    ThanKeyword,

    // EQUAL keyword
    EqualKeyword,

    // GREATER / LESS keywords
    GreaterKeyword,
    LessKeyword,

    // POSITIVE / NEGATIVE
    PositiveKeyword,
    NegativeKeyword,

    // NUMERIC / ALPHABETIC
    NumericKeyword,
    AlphabeticKeyword,

    // TEST / BEFORE / AFTER
    TestKeyword,
    BeforeKeyword,
    AfterKeyword,

    // ── Phase 5.2: Report Writer ──
    ReportKeyword,           // REPORT
    ReportSectionKeyword,    // REPORT SECTION (treated as two tokens, REPORT + SECTION)
    RdKeyword,               // RD
    InitiateKeyword,         // INITIATE
    GenerateKeyword,         // GENERATE
    TerminateKeyword,        // TERMINATE

    // ── Phase 5.3: Screen Section ──
    ScreenKeyword,           // SCREEN

    // ── Phase 5.4: OO COBOL ──
    ClassIdKeyword,          // CLASS-ID
    MethodIdKeyword,         // METHOD-ID
    InterfaceIdKeyword,      // INTERFACE-ID
    InvokeKeyword,           // INVOKE
    FactoryKeyword,          // FACTORY
    ObjectKeyword,           // OBJECT

    // ── Phase 5.5: Exception Handling ──
    RaiseKeyword,            // RAISE
    ResumeKeyword,           // RESUME

    // ── Phase 5.6–5.10: Compiler directives ──
    SourceFormatKeyword,     // SOURCE-FORMAT (compiler directive keyword)
    FreeKeyword,             // FREE (source format value)
    FixedKeyword,            // FIXED (source format value)
    NationalKeyword,         // NATIONAL (PIC N usage / type)

    // ─── Compiler directive delimiter >>>
    CompilerDirective,       // >>SOURCE FORMAT IS FREE/FIXED
}
