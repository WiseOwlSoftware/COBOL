# CobolSharp

COBOL.NET — a COBOL compiler targeting .NET, built from the ISO/IEC 1989:2023 specification
with correct support for all prior editions (1985, 2002, 2014). It translates standard COBOL
source to idiomatic, typed-native C#, which Roslyn compiles into a .NET assembly.

## Quick Start

```bash
# Build from source
git clone https://github.com/BrentRector/CobolSharp.git
cd CobolSharp
dotnet build

# Compile a COBOL program (the source is a positional argument; the produced exe is `cobol`)
dotnet run --project src/Cobol.Net.Cli -- hello.cob -o hello.dll

# Run the compiled program
dotnet hello.dll
```

## Current Status

- **3166 conformance tests**, **281 unit tests**, **33 characterization tests** passing
- Differential legacy-oracle guard: **353 NIST programs MATCH** byte-for-byte
- COBOL-85 corpus complete; full ISO-2023 plus per-edition (1985/2002/2014) conformance is the standing mission
- Clean-architecture rearchitecture in progress (a selectable Roslyn / direct-CIL backend over one bound tree)

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
- **Parser**: ANTLR4 lexer + parser with a modular grammar split across 9 imported per-subsystem fragments (data, expressions, control flow, I/O, OO, report writer, screen, special-names, words) over a dedicated lexer
- **SUBSCRIPT lexer mode**: dedicated ANTLR4 mode preserving sign adjacency for spec-true subscript parsing (&#167;5.3)
- **Preprocessor**: reference-format normalization, COPY with REPLACING, REPLACE, NIST test fixups
- **Binder**: scope-aware symbol table, type system, storage-form and record-layout computation, category compatibility
- **Bound tree**: typed expression/statement tree with abbreviated condition expansion, walked by a source-generated exhaustive visitor (no lowered IR)
- **C# emission**: the bound tree is rendered to idiomatic, typed-native C# source and compiled by Roslyn (the primary backend); a direct-CIL backend is a future phase
- **Edition conformance**: a two-arm version-conformance pass gates each construct against the targeted ISO edition
- **Diagnostics**: descriptor-based diagnostics with file/line/column positions
- **Validation**: flow-sensitive file-state analysis, FILE STATUS checking, and wired semantic validators
- **Runtime**: typed-native runtime library — native scaled-integer numerics, strings, tables, and sequential/indexed/relative file handlers (no byte-array State)

### Version Targeting
- **Editions**: full support for COBOL-85, COBOL-2002, COBOL-2014, and COBOL-2023, selected with `--std 85|2002|2014|2023`
- **Default**: COBOL-2023 (or COBOL-85 under `--nist`)
- **Edition gating**: a construct the targeted edition removed (e.g. ALTER) is rejected; `--permissive` downgrades such rejections to warnings for migration

## Architecture

```
COBOL Source
  -> Preprocessor (reference-format, COPY/REPLACE, NIST fixups)
  -> Lexer (ANTLR4, with SUBSCRIPT mode for data-name parentheses)
  -> Parser (ANTLR4, 9 imported grammar fragments)
  -> Binder (symbol table, type resolution, storage-form + record layout)
  -> Bound Tree (expression/statement binding, abbreviated condition expansion)
  -> C# Emission (idiomatic, typed-native C# source)
  -> Roslyn (-> .NET assembly)
  -> Runtime (typed-native numerics, strings, tables, file handlers)
```

### Solution Structure

```
CobolSharp.sln
  src/
    Cobol.Net.Cli/                    Command-line driver (produces the `cobol` executable)
    Cobol.Net.Frontend/               Preprocessor + ANTLR4 grammar, lexer, and parser
    Cobol.Net.Compiler/               Binder, bound tree, C# emitter, Roslyn backend
    Cobol.Net.Compiler.SourceGen/     Roslyn source generator (exhaustive bound-tree visitor)
    Cobol.Net.Editions/               Per-edition construct registry + version-conformance gating
    Cobol.Net.Runtime/                Typed-native runtime library linked into compiled programs
    CobolSharp.CLI/, .Compiler/, .Runtime/  Legacy byte-engine compiler, retained only as a differential oracle (until the G8 cut-over)
  tests/
    Cobol.Net.Tests.Unit/             281 unit tests
    Cobol.Net.Tests.Conformance/      3166 conformance tests
    Cobol.Net.Tests.Characterization/ 33 byte-exact snapshot tests
    nist/                             NIST CCVS test programs + expected output
  scripts/
    guard.sh                          Full regression gate
    guard-fast.sh                     Parallel fast regression gate
```

## Building

Requires .NET 10.0 SDK and Java (for ANTLR4 parser generation).

```bash
dotnet build                    # Build all projects
dotnet test                     # Run unit + conformance + characterization tests
bash scripts/guard.sh           # Full regression gate including NIST
```

After `dotnet clean`, the build automatically regenerates ANTLR4 parser files from the grammar.

## Known Gaps

- **Direct-CIL backend**: the Roslyn C#-source backend is the sole implemented backend; a selectable direct-CIL (Mono.Cecil) backend is a planned future phase
- **Rearchitecture in progress**: a clean-architecture refactor is underway — a fully structural `Place` lvalue model and a complete FUNCTION-argument grammar are the current work items

## License

Business Source License 1.1 -- Copyright (c) 2026 Brent Rector. See [LICENSE](LICENSE) for details.
