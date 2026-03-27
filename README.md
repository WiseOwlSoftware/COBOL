# CobolSharp

A COBOL-85 compiler targeting .NET, built from the ISO/IEC 1989:1985 specification.
Compiles standard COBOL source to .NET assemblies via CIL emission.

## Quick Start

```bash
# Build from source
git clone https://github.com/BrentRector/CobolSharp.git
cd CobolSharp
dotnet build

# Compile a COBOL program
dotnet run --project src/CobolSharp.CLI -- compile hello.cob -o hello.dll

# Run the compiled program
dotnet hello.dll
```

## Current Status

- **216 unit tests**, **183 integration tests** passing
- **60 NIST CCVS85 Nucleus tests** at 100% (of 95 total programs)
- COBOL-85 Nucleus module substantially complete
- Active development targeting remaining NIST compliance gaps

## Implemented Features

### Data Division
- **PICTURE clause**: all symbols (9, X, A, S, V, P, Z, *, +, -, CR, DB, B, 0, /)
- **USAGE**: DISPLAY, BINARY/COMP/COMP-4, PACKED-DECIMAL/COMP-3, COMP-5 (native binary extension), INDEX
- **Data hierarchy**: groups, elementary items, OCCURS (up to 7 levels), REDEFINES, RENAMES (level 66), level 77/88
- **OCCURS**: ASCENDING/DESCENDING KEY, INDEXED BY, OCCURS DEPENDING ON
- **VALUE clause**: literals, figurative constants (ZERO, SPACE, HIGH-VALUE, LOW-VALUE, QUOTE, ALL literal)
- **VALUE THRU** in level-88 condition names with range checking
- **BLANK WHEN ZERO**, JUSTIFIED RIGHT, SIGN IS LEADING/TRAILING SEPARATE

### Procedure Division
- **Arithmetic**: ADD, SUBTRACT, MULTIPLY, DIVIDE (with REMAINDER), COMPUTE with full operator precedence, ON SIZE ERROR
- **Control flow**: IF/ELSE/END-IF, EVALUATE/WHEN/OTHER, PERFORM (inline, out-of-line, THRU, TIMES, UNTIL, VARYING), GO TO, ALTER
- **Data movement**: MOVE (with category compatibility matrix), MOVE CORRESPONDING, INITIALIZE
- **String operations**: STRING, UNSTRING (with DELIMITED BY, POINTER, TALLYING, OVERFLOW), INSPECT (TALLYING, REPLACING, CONVERTING)
- **Conditions**: relational, sign (POSITIVE/NEGATIVE/ZERO), class (NUMERIC/ALPHABETIC/ALPHABETIC-LOWER/UPPER), condition-name (level 88), switch-status, abbreviated combined (COBOL-85 &#167;6.3.4.2)
- **File I/O**: OPEN, CLOSE, READ (sequential + keyed), WRITE (BEFORE/AFTER ADVANCING), REWRITE, DELETE, START
- **File organizations**: sequential, indexed (with ALTERNATE KEY + secondary indices), relative
- **Inter-program**: CALL (static + dynamic), USING (BY REFERENCE, BY CONTENT, BY VALUE), RETURNING, ENTRY statement, CANCEL, ON EXCEPTION / NOT ON EXCEPTION, INITIAL program support
- **Other**: DISPLAY, ACCEPT, EXIT PROGRAM, EXIT PARAGRAPH, EXIT SECTION, GOBACK, STOP RUN, NEXT SENTENCE, CONTINUE

### Environment Division
- **SPECIAL-NAMES**: implementor switches (ON/OFF STATUS), ALPHABET (STANDARD-1, STANDARD-2, NATIVE, literal THRU/ALSO), CLASS definitions, DECIMAL-POINT IS COMMA, CURRENCY SIGN
- **FILE-CONTROL**: SELECT, ASSIGN, ORGANIZATION, ACCESS MODE, FILE STATUS, RECORD KEY, ALTERNATE KEY
- **LABEL RECORDS**: parsed and accepted (obsolete clause)

### Intrinsic Functions
- ~70 functions: math (SQRT, LOG, MOD, FACTORIAL, etc.), string (LENGTH, REVERSE, UPPER-CASE, LOWER-CASE, TRIM, etc.), date/time (CURRENT-DATE, INTEGER-OF-DATE, etc.), financial (ANNUITY, PRESENT-VALUE), aggregates (MAX, MIN, SUM, MEAN, etc.)

### Compiler Infrastructure
- **Parser**: ANTLR4 lexer + parser with grammar split across 7 imported fragments
- **SUBSCRIPT lexer mode**: dedicated ANTLR4 mode preserving sign adjacency for spec-true COBOL-85 subscript parsing (&#167;5.3)
- **Preprocessor**: reference-format normalization, COPY with REPLACING, REPLACE, NIST test fixups
- **Semantic analysis**: symbol table, type system, storage layout computation, category compatibility
- **Bound tree**: typed expression tree with abbreviated condition expansion
- **IR**: basic block SSA-style intermediate representation
- **CIL emission**: Mono.Cecil-based .NET assembly generation
- **Diagnostics**: 175+ diagnostic descriptors (COBOL0001-COBOL0600, CBL0601-CBL3606) with file/line/column positions
- **Validation**: flow-sensitive file state analysis (CBL0702), FILE STATUS checking (CBL3206), 7 wired semantic validators
- **Runtime**: byte-array storage model with PIC-driven encode/decode, decimal arithmetic, sequential + indexed file handlers

### Version Targeting
- **COBOL-85**: full support (primary target)
- **COBOL-2002**: ALTER statement produces error (removed from standard); BY VALUE parameters accepted

## Architecture

```
COBOL Source
  -> Preprocessor (reference-format, COPY/REPLACE, NIST fixups)
  -> Lexer (ANTLR4, with SUBSCRIPT mode for data-name parentheses)
  -> Parser (ANTLR4, 7 imported grammar fragments)
  -> Semantic Analysis (symbol table, type resolution, storage layout)
  -> Bound Tree (expression binding, abbreviated condition expansion)
  -> IR Lowering (basic blocks, SSA values, control flow)
  -> CIL Emission (Mono.Cecil -> .NET assembly)
  -> Runtime (CobolProgram base, byte[] storage, PicRuntime, file handlers)
```

### Solution Structure

```
CobolSharp.sln
  src/
    CobolSharp.CLI/              Command-line driver
    CobolSharp.Compiler/         All compiler phases (lexer through emitter)
    CobolSharp.Runtime/          Runtime library linked into compiled programs
  tests/
    CobolSharp.Tests.Unit/       216 unit tests
    CobolSharp.Tests.Integration/ 184 integration tests (183 pass, 1 skip)
    nist/                        NIST CCVS85 test programs + expected output
  scripts/
    guard.sh                     Regression gate (unit + integration + 60 NIST)
```

## Building

Requires .NET 9.0 SDK and Java (for ANTLR4 parser generation).

```bash
dotnet build                    # Build all projects
dotnet test                     # Run unit + integration tests
bash scripts/guard.sh           # Full regression gate including NIST
```

After `dotnet clean`, the build automatically regenerates ANTLR4 parser files from the grammar.

## Known Gaps

- **SORT/MERGE**: parsed but not lowered to IR
- **Report Writer**: parsed but not implemented
- **Collating sequence**: ALPHABET clause parsed but not applied to runtime comparisons
- **OCCURS DEPENDING ON**: parsed and stored but runtime truncation not enforced
- **Recursive CALL**: not supported (COBOL-85 does not require it)

## License

Business Source License 1.1 -- Copyright (c) 2026 Brent Rector. See [LICENSE](LICENSE) for details.
