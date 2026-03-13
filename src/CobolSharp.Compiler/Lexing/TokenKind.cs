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
    FunctionPointerKeyword,  // FUNCTION-POINTER
    ProcedurePointerKeyword, // PROCEDURE-POINTER
    GlobalKeyword,           // GLOBAL
    ExternalKeyword,         // EXTERNAL
    WhenKeyword2,            // (placeholder, WHEN is already defined)
    AscendingKeyword,        // ASCENDING
    DescendingKeyword,       // DESCENDING
    KeyKeyword,              // KEY

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
}
